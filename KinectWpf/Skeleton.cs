using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kinect;
using System.Runtime.Serialization.Formatters.Binary;

using System.IO.Compression;

namespace KinectWpf
{
    public class KinectSerializer
    {
        static Stream s;
        static BinaryFormatter bf;

        public static void InitWrite(bool doDelete = false)
        {
            if (doDelete)
            {
                if (File.Exists(Config.FileName))
                {
                    File.Delete(Config.FileName);
                }
                if (File.Exists(Config.ZipName))
                {
                    File.Delete(Config.ZipName);
                }
            }

            s = File.OpenWrite(Config.FileName);
            bf = new BinaryFormatter();
        }

        public static void InitRead()
        {
            s = File.OpenRead(Config.FileName);
            bf = new BinaryFormatter();
        }

        public static void SerializeFrame(Skeleton steleton)
        {
            if (bf != null)
            {
                bf.Serialize(s, (MySkeleton2)steleton);
            }
        }

        public static MySkeleton2 DeserializeFrame()
        {
            MySkeleton2 result = new MySkeleton2();
            try
            {
                result = (MySkeleton2)bf.Deserialize(s);
            }
            catch (Exception e)
            {
                return null;
            }
            return result;
        }

        public static void TestSerialize(int val)
        {
            if (bf != null)
            {
                MySkeleton2 testSkeleton = new MySkeleton2(val);
                bf.Serialize(s, testSkeleton);
            }
        }

        public static void CompressData()
        {
            s.Close();
            if (!File.Exists(Config.ZipName))
            {
                ZipFile.CreateFromDirectory(Config.FilesPath, Config.ZipName);
            }
        }

        ~KinectSerializer()
        {
            if (s != null)
            {
                s.Close();
            }
        }
    }

    public class KinectDeserializer
    {
        static Stream s;
        static BinaryFormatter bf;
        
        public static void InitRead(string FileName = Config.FileName)
        {
            s = File.OpenRead(FileName);
            bf = new BinaryFormatter();
        }

        public static Queue<MySkeleton2> DeserializeAll()
        {
            Queue<MySkeleton2> result = new Queue<MySkeleton2>();
            MySkeleton2 tempSkel;
            tempSkel = DeserializeNextFrame();
            do
            {
                result.Enqueue(tempSkel);
                tempSkel = DeserializeNextFrame();
            } while (tempSkel != null);
            return result;
        }

        public static MySkeleton2 DeserializeNextFrame()
        {
            MySkeleton2 result = new MySkeleton2();
            try
            {
                result = (MySkeleton2)bf.Deserialize(s);
            }
            catch (Exception e)
            {
                return null;
            }
            return result;
        }
        public static void DecompressData(string ZipName = Config.ZipName, string FileName = Config.FilesPath)
        {
            ZipFile.ExtractToDirectory(ZipName, FileName);
        }

        ~KinectDeserializer()
        {
            if (s != null)
            {
                s.Close();
            }
        }
    }

    [Serializable]
    public class MySkeleton2
    {
        public long timeStamp;
        public const int jointsSize = 20;
        [Serializable]
        public struct Point
        {
            public float X, Y, Z;
        }

        [Serializable]
        public struct MyJoint
        {
            public Point Position;
            public short jointType;
            public short trackingState;
            public static implicit operator MyJoint(Joint input)
            {
                MyJoint ret = new MyJoint
                {
                    jointType = (short)input.JointType,
                    trackingState = (short)input.TrackingState
                };
                ret.Position = new Point();
                ret.Position.X = input.Position.X;
                ret.Position.Y = input.Position.Y;
                ret.Position.Z = input.Position.Z;
                return ret;         
            }
        }
        public MyJoint[] Joints;

        public MySkeleton2(Skeleton skeleton)
        {
            //Joint join = new Joint()
            timeStamp = DateTime.Now.ToFileTimeUtc();
            Joints = new MyJoint[20];
            for (int i = 0; i < 20; i++)
            {
                Joints[i] = skeleton.Joints[(JointType)i];
            }
        }
        public MySkeleton2(MySkeleton2 skeleton)
        {
            //Joint join = new Joint()
            timeStamp = skeleton.timeStamp;
            Joints = new MyJoint[20];
            for (int i = 0; i < 20; i++)
            {
                Joints[i] = skeleton.Joints[i];
            }
        }

        public static implicit operator MySkeleton2(Skeleton skeleton)
        {

            return new MySkeleton2(skeleton);
        }

        //test constructor
        public MySkeleton2(int timestamp)
        {
            this.timeStamp = timestamp;
            Joints = new MyJoint[20];
        }
        public MySkeleton2()
        {
            Joints = new MyJoint[20];
        }
    }
}

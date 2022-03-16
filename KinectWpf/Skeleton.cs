using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using System.IO.Compression;

namespace KinectWpf
{
    public class KinectSerializer
    {

        private const string filesDir = "./data";
        private const string dataFile = "./data/temp.dat";
        private const string zipFile = "temp.zip";

        static Stream s;
        static
            BinaryFormatter bf;

        public static void InitWrite(bool doDelete = false)
        {
            if (doDelete)
            {
                if (File.Exists(dataFile))
                {
                    File.Delete(dataFile);
                }
                if (File.Exists(zipFile))
                {
                    File.Delete(zipFile);
                }
            }

            s = File.OpenWrite(dataFile);
            bf = new BinaryFormatter();
        }

        public static void InitRead()
        {
            s = File.OpenRead(dataFile);
            bf = new BinaryFormatter();
        }

        public static void SerializeFrame(Skeleton steleton)
        {
            if (bf != null)
            {
                bf.Serialize(s, (MySkeleton2)steleton);
            }
        }

        public static void CompressData()
        {
            ZipFile.CreateFromDirectory(filesDir, zipFile);
        }

        ~SerializeKinectData()
        {
            if (s != null)
            {
                s.Close();
            }
        }
    }

    public class VirtualKinect
    {

    }

    [Serializable]
    public class MySkeleton2
    {
        long timeStamp;
        public const int jointsSize = 20;
        [Serializable]
        public struct MyJoint
        {
            public float X, Y, Z;
            public short jointType;
            public short trackingState;
            public static implicit operator MyJoint(Joint input)
            {
                return new MyJoint {jointType = (short)input.JointType, 
                                    trackingState = (short)input.TrackingState,
                                    X = input.Position.X,
                                    Y = input.Position.Y,
                                    Z = input.Position.Z };
            }
        }
        public MyJoint[] joints;

        public MySkeleton2(Skeleton skeleton)
        {
            //Joint join = new Joint()
            timeStamp = DateTime.Now.ToFileTimeUtc();
            joints = new MyJoint[20];
            for (int i = 0; i < 20; i++)
            {
                joints[i] = skeleton.Joints[(JointType)i];
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
            joints = new MyJoint[20];
        }
        public MySkeleton2()
        {
            joints = new MyJoint[20];
        }
    }
}

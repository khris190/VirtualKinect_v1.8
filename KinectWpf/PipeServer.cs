using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace KinectWpf
{
    class VirtualKinectPipeServer
    {
        static BinaryFormatter bf;
        static NamedPipeServerStream ss;

        public static void send(MySkeleton2 skel)
        {
            if (ss == null)
            {
                ss = new NamedPipeServerStream("VirtualKinectPipe", PipeDirection.Out);

            }
            if (!ss.IsConnected)
            {
                ss.WaitForConnection();
            }
            if (bf == null)
            {
                bf = new BinaryFormatter();
            }
            bf.Serialize(ss, skel);
            ss.Flush();
        }
    }

    public class VirtualKinectPipeClient
    {
        static BinaryFormatter bf;

        public static void startAsync()
        {
            Thread InstanceCaller = new Thread(
            new ThreadStart(wait));
            InstanceCaller.Start();
        }
        public static void start()
        {
            wait();
        }

        public static void wait()
        {
            NamedPipeClientStream cs = new NamedPipeClientStream(".", "VirtualKinectPipe", PipeDirection.In);
            if (cs.IsConnected != true)
            {
                cs.Connect();
            }
            if (bf == null)
            {
                bf = new BinaryFormatter();
            }
            var test = (MySkeleton2)bf.Deserialize(cs);
            //return test;
        }
        public static MySkeleton2 get()
        {
            NamedPipeClientStream cs = new NamedPipeClientStream(".", "VirtualKinectPipe", PipeDirection.In);
            if (cs.IsConnected != true)
            {
                cs.Connect();
            }
            if (bf == null)
            {
                bf = new BinaryFormatter();
            }
            MySkeleton2 temp;
            while ((temp = (MySkeleton2)bf.Deserialize(cs)) != null)
            {
                Console.WriteLine("Received from server: {0}, {1}", temp,  temp.timeStamp);
            }
            return temp;
        }
    }


}
using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace VirtualKinect
{
    public class VirtualKinectPipeServer
    {
        static BinaryFormatter bf;
        static NamedPipeServerStream ss;

        public static void send(MySkeleton2 skel)
        {
            VirtualKinectPipeServer.Start();
            if (ss.IsConnected)
            {
                if (bf == null)
                {
                    bf = new BinaryFormatter();
                }
                bf.Serialize(ss, skel);
                ss.Flush();
            }
        }
        public static void Start()
        {
            if (ss == null)
            {
                ss = new NamedPipeServerStream("VirtualKinectPipe", PipeDirection.Out);
                ss.WaitForConnectionAsync();
            }
        }
    }

    public class VirtualKinectPipeClient
    { 
        public event EventHandler<MySkeletonFrameEventArgs> SkeletonFrameReady;
        static BinaryFormatter bf;
        Thread InstanceCaller;
        public void Start()
        {
            InstanceCaller = new Thread(
            new ThreadStart(getWithResets));
            InstanceCaller.Start();
        }
        public void Stop()
        {
            if (InstanceCaller != null)
            {
                InstanceCaller.Abort();
            }
        }
        protected virtual void OnSkeletonFrameReady(MySkeletonFrameEventArgs e)
        {
            EventHandler<MySkeletonFrameEventArgs> handler = SkeletonFrameReady;
            handler?.Invoke(this, e);
        }

        public void getWithResets()
        {
            while (true)
            {
                get();
            }
        }
        public MySkeleton2 get()
        {
            try
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
                    //Console.WriteLine("Received from server: {0}, {1}", temp,  temp.timeStamp);
                    Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                    OnSkeletonFrameReady(new MySkeletonFrameEventArgs(new MySkeleton2(temp)));
                }
                return temp;
            }
            catch (Exception e)
            {
                if (e is System.Runtime.Serialization.SerializationException)
                {
                    Console.WriteLine("stream has ended");
                    Console.WriteLine(e.Message);
                }
                else
                {
                    throw;
                }
            }
            return null;
        }
    }


}
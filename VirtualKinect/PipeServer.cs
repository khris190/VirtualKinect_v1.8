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

        /// <summary>
        /// sends singular MySkeleton2 object through a PipeStream
        /// </summary>
        /// <param name="skel"></param>
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
       
        /// <summary>
        /// create pipe output server and wait for a connection asynchronusly
        /// </summary>
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

        /// <summary>
        /// start a thread listening for VK pipe that will try to reconect after a pipe is terminated
        /// </summary>
        public void Start()
        {
            InstanceCaller = new Thread(
            new ThreadStart(getWithResets));
            InstanceCaller.Start();
        }

        /// <summary>
        /// abort client thread
        /// </summary>
        public void Stop()
        {
            if (InstanceCaller != null)
            {
                InstanceCaller.Abort();
            }
        }

        /// <summary>
        /// invoke FrameReady Event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSkeletonFrameReady(MySkeletonFrameEventArgs e)
        {
            EventHandler<MySkeletonFrameEventArgs> handler = SkeletonFrameReady;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// retry gets on pipe close
        /// </summary>
        public void getWithResets()
        {
            while (true)
            {
                DeserializeSkeletonsFromPipe();
            }
        }

        /// <summary>
        /// deserializes MySkeleton2 objects from pipe stream and invokes OnSkeletonFrameReady events
        /// </summary>
        public MySkeleton2 DeserializeSkeletonsFromPipe()
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
                    OnSkeletonFrameReady(new MySkeletonFrameEventArgs(new MySkeleton2(temp)));
                }
                // this return is not used, it was when this functioon only read 1 object from pipe and returned, but it dodnt work well
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
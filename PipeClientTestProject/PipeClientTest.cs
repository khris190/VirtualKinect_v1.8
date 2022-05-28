using System;
using System.Threading;
using KinectWpf;

namespace PipeClientTestProject
{
    internal class PipeClientTest
    {
        public static int Main()
        {
            Console.WriteLine("MAIN: " + Thread.CurrentThread.ManagedThreadId);
            VirtualKinectStart();
            return 0;
        }

        private static VirtualKinectPipeClient _vkpipe;
        private static void VirtualKinectStart()
        {
            try
            {
                if (_vkpipe != null)
                {
                    _vkpipe.Stop();
                }
                _vkpipe = new VirtualKinectPipeClient();
                _vkpipe.SkeletonFrameReady += new EventHandler<MySkeletonFrameEventArgs>(MySkeletonFrameReady);
                _vkpipe.Start();
            }
            catch (VirtualKinectException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        static void MySkeletonFrameReady(object sender, MySkeletonFrameEventArgs args)
        {
            try
            {
                Console.WriteLine(args.user.timeStamp);
                Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            }
            catch (Exception e)
            {
                throw new VirtualKinectException(e.Message);
            }
        }
    }
}

using System;

using System.IO.Compression;

namespace KinectWpf
{
    public delegate void MySkeletonFrameReadyEventHandler(object sender, MySkeletonFrameEventArgs e);

    public class MySkeletonFrameEventArgs : EventArgs
    {
        public MySkeleton2 user;

        public MySkeletonFrameEventArgs(MySkeleton2 user)
        {
            this.user = user;
        }
    }

    public class VirtualKinect
    {
        public event EventHandler<MySkeletonFrameEventArgs> SkeletonFrameReady;

        public void Start()
        {
            var skeleton = new MySkeleton2();
            OnSkeletonFrameReady(new MySkeletonFrameEventArgs(skeleton));
        }

        protected virtual void OnSkeletonFrameReady(MySkeletonFrameEventArgs e)
        {
            EventHandler<MySkeletonFrameEventArgs> handler = SkeletonFrameReady;
            handler?.Invoke(this, e);
        }
    }
}
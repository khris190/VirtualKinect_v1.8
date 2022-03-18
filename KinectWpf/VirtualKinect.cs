using System;
using System.Threading;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;
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
        private static System.Timers.Timer aTimer;
        private Queue<MySkeleton2> _skeletons;

        //variable for offseting recorded times to real time clock;
        private long RecordingOffset;

        public void Start()
        {
            var skeleton = new MySkeleton2();
            KinectDeserializer.InitRead("nagranie-machanie.dat");
            _skeletons = KinectDeserializer.DeserializeAll();
            DeleteFirstTwo();
            CalcualteOffset();
            Thread InstanceCaller = new Thread(
            new ThreadStart(DoTheReplay));
            InstanceCaller.Start();
        }
        private void DoTheReplay()
        {
            TimeSpan ts;
            while (_skeletons.Count > 0)
            {
                var skelet = _skeletons.Dequeue();
                long test = (skelet.timeStamp - RecordingOffset);
                long tics = skelet.timeStamp + RecordingOffset - DateTime.Now.ToFileTimeUtc();
                if (tics > 0)
                {
                    ts = TimeSpan.FromTicks(tics);
                    Thread.Sleep(ts);
                }
                OnSkeletonFrameReady(new MySkeletonFrameEventArgs(new MySkeleton2(skelet)));
            }
        }

        private void setReplayOfNextFrame()
        {
            TimeSpan ts;
            if (_skeletons.Count > 0)
            {
                var skelet = _skeletons.Dequeue();
                long test = (skelet.timeStamp - RecordingOffset);
                long tics = skelet.timeStamp + RecordingOffset - DateTime.Now.ToFileTimeUtc();
                if (tics > 0)
                {
                    ts = TimeSpan.FromTicks(tics);
                }
                OnSkeletonFrameReady(new MySkeletonFrameEventArgs(skelet));
            }
        }
        private void DeleteFirstTwo()
        {
            _skeletons.Dequeue();
            _skeletons.Dequeue();
        }

        private void CalcualteOffset()
        {
            RecordingOffset = DateTime.Now.ToFileTimeUtc() - _skeletons.Peek().timeStamp;
        }

        protected virtual void OnSkeletonFrameReady(MySkeletonFrameEventArgs e)
        {
            EventHandler<MySkeletonFrameEventArgs> handler = SkeletonFrameReady;
            handler?.Invoke(this, e);
        }
    }
}
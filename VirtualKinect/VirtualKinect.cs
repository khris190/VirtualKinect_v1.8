using System;
using System.Collections.Generic;
using System.Threading;

namespace VirtualKinect
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
        private Queue<MySkeleton2> _skeletons;

        private SortedList<long, MySkeleton2> _skeletonsDictionary;
        private long? lastFrameTime;
        public static readonly object skeletonPointerLock = new object();
        public long skeletonsPointer;
        public volatile int skeletonsPointerIndex = 0;
        public volatile bool sliderManipulated = false;

        public long[] GetSkeletonsTimings()
        {
            long[] ret = new long[_skeletonsDictionary.Count];
            int i = 0;
            foreach (var s in _skeletonsDictionary)
            {
                ret[i] += (s.Key);
                i++;
            }
            return ret;
        }


        //variable for offseting recorded times to real time clock;
        private long RecordingOffset;
        private Thread InstanceCaller;
        private string _fileName;
        private long sleepStart;
        private bool isPaused = false;
        //on pause remember timestamp and add it to offset on unpause
        public bool IsPaused
        {
            get { return isPaused; }
            set
            {
                isPaused = value;
                if (value)
                {
                    sleepStart = DateTime.Now.ToFileTimeUtc();
                }
                else
                {
                    RecordingOffset += DateTime.Now.ToFileTimeUtc() - sleepStart;
                }
            }
        }

        public VirtualKinect(string fileName)
        {
            _fileName = fileName;
            if (_fileName == null)
            {
                throw new VirtualKinectException("Please, select a file first.");
            }
            KinectDeserializer.InitRead(_fileName);
            _skeletons = KinectDeserializer.DeserializeAll();
            _skeletonsDictionary = new SortedList<long, MySkeleton2>();

            DeleteFirstTwo();
            CalcualteOffset();
            foreach (MySkeleton2 skeleton2 in _skeletons)
            {
                if (null != skeleton2 && !_skeletonsDictionary.ContainsKey(skeleton2.timeStamp))
                {
                    _skeletonsDictionary.Add(skeleton2.timeStamp, skeleton2);
                }
            }
        }

        public void Start(int index = 0)
        {
            var skeleton = new MySkeleton2();
            this.skeletonsPointerIndex = index;
            try
            {

                InstanceCaller = new Thread(new ThreadStart(DoTheReplay));
                InstanceCaller.Start();
            }
            catch (Exception e)
            {
                throw new VirtualKinectException(e.Message);
            }
        }

        public void Stop()
        {
            if (InstanceCaller != null)
            {
                InstanceCaller.Abort();
            }
        }

        private void DoTheReplay()
        {

            TimeSpan ts;
            var queue = _skeletons;
            while (queue.Count > 0 && InstanceCaller.IsAlive)
            {
                MySkeleton2 skelet;
                if (skeletonsPointerIndex < _skeletonsDictionary.Count && skeletonsPointerIndex >= 0)
                {
                    if (!isPaused && !sliderManipulated)
                    {
                        skelet = _skeletonsDictionary.Values[skeletonsPointerIndex];

                        if (skeletonsPointerIndex == 0)
                        {
                            Thread.Sleep(16);
                        }
                        else
                        {
                            long tics = skelet.timeStamp - _skeletonsDictionary.Values[skeletonsPointerIndex - 1].timeStamp;
                            long offset = 0;
                            if (lastFrameTime != null)
                            {
                                offset = lastFrameTime.Value - DateTime.Now.ToFileTimeUtc();
                            }
                            tics += offset;
                            if (tics > 0)
                            {
                                ts = TimeSpan.FromTicks(tics);
                                Thread.Sleep(ts);
                            }
                        }
                        lastFrameTime = DateTime.Now.ToFileTimeUtc();
                        skeletonsPointerIndex++;
                        OnSkeletonFrameReady(new MySkeletonFrameEventArgs(new MySkeleton2(skelet)));
                    }
                    else
                    {
                        Thread.Sleep(32);
                    }
                }
            }
        }
        private void DoTheReplayPipe()
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
                VirtualKinectPipeServer.send(skelet);
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
            try
            {
                _skeletons.Dequeue();
                _skeletons.Dequeue();
            }
            catch (Exception)
            {
                throw new VirtualKinectException("A problem occured while reading the file.");
            }

        }

        private void CalcualteOffset()
        {
            try
            {
                RecordingOffset = DateTime.Now.ToFileTimeUtc() - _skeletons.Peek().timeStamp;
            }
            catch (Exception)
            {
                throw new VirtualKinectException("A problem occured while reading the file.");
            }
        }

        protected virtual void OnSkeletonFrameReady(MySkeletonFrameEventArgs e)
        {
            EventHandler<MySkeletonFrameEventArgs> handler = SkeletonFrameReady;
            handler?.Invoke(this, e);
        }
    }

    public class VirtualKinectException : Exception
    {
        public VirtualKinectException(string message) : base(message)
        {
            //
        }
    }
}
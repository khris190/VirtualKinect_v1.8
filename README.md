# VirtualKinect_v1.8
make first Kinect great again

![](https://github.com/khris190/VirtualKinect_v1.8/blob/main/Animation.gif)


<h1>VirtualKinect usage </h1>
    try
    {
        _vkpipe = new VirtualKinectPipeClient();
        #just setup event listener function here like in normal kinect
        _vkpipe.SkeletonFrameReady += new EventHandler<MySkeletonFrameEventArgs>(MySkeletonFrameReady);
        _vkpipe.Start();
    }
    catch (VirtualKinectException e)
    {
        Console.WriteLine(e.Message);
    }
  
<h1>Microsoft.Kinect usage </h1>
    kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
    if (null != kinect)
    {
        kinect.SkeletonStream.Enable();
        kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonFrameReady);
        kinect.Start();
    }

# VirtualKinect_v1.8
make first Kinect great again

![](https://github.com/khris190/VirtualKinect_v1.8/blob/main/Animation.gif)


<h1>VirtualKinect client usage </h1>
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

  
<h1>Methods</h1>
VK client pipe has only 2 public methods as of now
they are pretty self explanatory: <br/>
Start() <br/>
Stop() <br/>
Every event gets MySkeleton2 object from pipe server, its structure is as follows: <br/>
    
- public long timeStamp;
- public const int jointsSize = 20;
- public MyJoint[jointsSize] Joints;
  - public short jointType;
  - public short trackingState;
  - public Point Position;
    - public float X, Y, Z;

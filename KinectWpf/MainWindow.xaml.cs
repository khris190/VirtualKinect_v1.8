using System;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool recordActive = false;

        private string _loadedFilePath;
        private VirtualKinect _vk;

        public MainWindow()
        {
            InitializeComponent();
            SetPoints();
        }

        ~MainWindow()
        {
            KinectSerializer.CompressData();
        }

        private void Record()
        {
            KinectSerializer.InitWrite(true);
            for (int i = 0; i < 2; i++)
            {
                KinectSerializer.TestSerialize(i);
            }
            KinectStart();
        }

        private void Play()
        {
            KinectSerializer.InitRead();
            for (int i = 0; i < 3; i++)
            {
                KinectSerializer.DeserializeFrame();
            }
            VirtualKinectStart();
        }

        static List<System.Windows.Shapes.Ellipse> ellipses;
        Canvas canvas;
        private void SetPoints()
        {
            ellipses = new List<System.Windows.Shapes.Ellipse>();
            canvas = this.FindName("MainCanvas") as Canvas;
            for (int i = 0; i < 20; i++)
            {
                object test = this.FindName("Point0" + (i < 10 ? "0" : "") + i.ToString());
                
                if (test is System.Windows.Shapes.Ellipse)
                {
                    ellipses.Add(test as System.Windows.Shapes.Ellipse);
                    Canvas.SetLeft(ellipses[i], 400);
                }
            }
        }
        private void KinectStart()
        {
            var kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (null != kinect)
            {
                kinect.SkeletonStream.Enable();
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonFrameReady);
                kinect.Start();
            }
        }
        
        private void VirtualKinectStart()
        {
            if (_vk != null)
            {
                _vk.Stop();
            }

            try
            {
                _vk = new VirtualKinect(_loadedFilePath);
                _vk.SkeletonFrameReady += new EventHandler<MySkeletonFrameEventArgs>(MySkeletonFrameReady);
                _vk.Start();
            }
            catch (VirtualKinectException e)
            {
                ShowErrorMessage(e.Message);
            }
        }

        static void SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs args)
        {
            using (var frame = args.OpenSkeletonFrame())
                if (frame != null)
                {
                    Skeleton[] skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);
                    if (skeletons.Length > 0)
                    {
                        var user = skeletons.FirstOrDefault(
                            u => u.TrackingState == SkeletonTrackingState.Tracked
                        );

                        if (user != null)
                        {
                            KinectSerializer.SerializeFrame(user);
                            Console.WriteLine("User is not null");

                            SkeletonDrawer(user);
                        }
                        else
                        {
                            Console.WriteLine("No user found");
                        }
                    }
                }
        }
        //TODO background task <- check it

        static void MySkeletonFrameReady(object sender, MySkeletonFrameEventArgs args)
        {
            try
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => { SkeletonDrawer(args.user); });
                VirtualKinectPipeServer.send(args.user);
            }
            catch (Exception e)
            {
                throw new VirtualKinectException(e.Message);
            }
        }

        public static void SkeletonDrawer(MySkeleton2 user)
        {
            for (int i = 0; i < 20; i++)
            {
                Canvas.SetLeft(ellipses[i], (user.Joints[i].Position.X + 1) * 200);
                Canvas.SetTop(ellipses[i], 400 - (user.Joints[i].Position.Y + 1) * 200);
            }
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            Record();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result = openFileDlg.ShowDialog();
            if (result == true)
            {
                _loadedFilePath = openFileDlg.FileName;
                ShowInfoMessage("Loaded file: " + _loadedFilePath);
            }
        }

        private void ShowErrorMessage(string message)
        {
            HideMessageBlocks();
            ErrorBlock.Text = message;
            ErrorBlock.Visibility = Visibility.Visible;
        }

        private void HideErrorMessageBlock()
        {
            ErrorBlock.Visibility = Visibility.Collapsed;
        }

        private void ShowInfoMessage(string message)
        {
            HideMessageBlocks();
            InfoBlock.Text = message;
            InfoBlock.Visibility = Visibility.Visible;
        }

        private void HideInfoMessageBlock()
        {
            InfoBlock.Visibility = Visibility.Collapsed;
        }

        private void HideMessageBlocks()
        {
            HideErrorMessageBlock();
            HideInfoMessageBlock();
        }
    }
}
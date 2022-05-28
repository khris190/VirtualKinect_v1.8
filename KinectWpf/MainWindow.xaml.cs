using System;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VirtualKinect;
//TODO play into record not working
namespace KinectWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool recordActive = false;

        private string _loadedFilePath;
        private VirtualKinect.VirtualKinect _vk;
        private KinectSensor kinect;

        public MainWindow()
        {
            InitializeComponent();
            SetPoints();
        }

        ~MainWindow()
        {
        }

        private bool isRecording = false;
        private void Record()
        {
            if (!isRecording)
            {
                if (_vk != null)
                {
                    _vk.Stop();
                }
                isRecording = true;
                KinectSerializer.InitWrite(true);
                KinectStart();
            }
        }

        private void Play()
        {
            if (isRecording)
            {
                StopRecording();
            }
            VirtualKinectStart();
        }

        static Slider slider;
        static SortedList<double, long> sliderMap;
        static bool sliderInitialized = false;

        private void InitializeSlider()
        {
            sliderInitialized = false;

            slider = this.FindName("PlayerSlider") as Slider;
            long[] skeletonTimings = _vk.GetSkeletonsTimings();
            sliderMap = new SortedList<double, long>();
            foreach (var item in skeletonTimings)
            {
                if (!sliderMap.ContainsKey(item))
                {
                    sliderMap.Add((double)(item / 10000) / 1000, item);
                }
            }

            slider.Ticks = new DoubleCollection(sliderMap.Keys);
            slider.Minimum = sliderMap.Keys.First();
            slider.Maximum = sliderMap.Keys.Last();
            sliderInitialized = true;
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
            kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
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
                _vk = new VirtualKinect.VirtualKinect(_loadedFilePath);
                InitializeSlider();
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

        static void MySkeletonFrameReady(object sender, VirtualKinect.MySkeletonFrameEventArgs args)
        {
            try
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() => { SkeletonDrawer(args.user); });
                }
                VirtualKinectPipeServer.send(args.user);
            }
            catch (Exception e)
            {
                //an Exception during thread abortion
                if (e is System.Threading.ThreadAbortException)
                {
                    return;
                }
                else if (e is System.NullReferenceException)
                {
                    return;
                }
                else
                {
                    throw new VirtualKinectException(e.Message);
                }
            }
        }

        public static void SkeletonDrawer(MySkeleton2 user)
        {
            try
            {

                slider.Value = (double)(user.timeStamp / 10000) / 1000;
            }
            catch (Exception)
            {
            }
            for (int i = 0; i < 20; i++)
            {
                Canvas.SetLeft(ellipses[i], (user.Joints[i].Position.X + 1) * 160);
                Canvas.SetTop(ellipses[i], 400 - (user.Joints[i].Position.Y + 1) * 160);
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
            ErrorBlock1.Text = message;
            ErrorBlock1.Visibility = Visibility.Visible;
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
        //TODO: disable on recording
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _vk.IsPaused = !_vk.IsPaused;
        }

        private void StopRecording()
        {
            if (null != kinect)
            {
                kinect.Stop();
            }
            KinectSerializer.CloseStream();
            isRecording = false;

        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                if (_vk != null)
                {
                    _vk.Stop();

                }
            }
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog
            Microsoft.Win32.SaveFileDialog saveFileDlg = new Microsoft.Win32.SaveFileDialog();
            saveFileDlg.DefaultExt = ".dat";
            Nullable<bool> result = saveFileDlg.ShowDialog();
            if (result == true)
            {
                if (System.IO.File.Exists(saveFileDlg.FileName))
                {
                    System.IO.File.Delete(saveFileDlg.FileName);
                }
                System.IO.File.Copy(Config.FileName, saveFileDlg.FileName);
            }
        }

        private void RecorderTab_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                if (_vk != null)
                {
                    _vk.Stop();

                }
            }
        }

        private void PlayerTab_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
            else
            {
                if (_vk != null)
                {
                    _vk.Stop();

                }
            }
        }

        private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderInitialized)
            {
                Slider tmpsender = sender as Slider;
                //IsFocused, IsMouseDirectlyOver, IsMouseOver, IsKeyboardFocused, IsKeyboardFocusWithin
                    _vk.skeletonsPointerIndex = sliderMap.IndexOfKey(tmpsender.Value);
            }
        }

        private void PlayerSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vk != null)
            {
                _vk.sliderManipulated = false;
            }
        }

        private void PlayerSlider_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vk != null)
            {
                _vk.sliderManipulated = true;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (isRecording)
            {
                StopRecording();
            }
            if (_vk != null)
            {
                _vk.Stop();
            }
        }
    }
}

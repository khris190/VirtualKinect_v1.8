using System;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VirtualKinect;
using System.Text.RegularExpressions;
using System.Threading;

//TODO play into record not working
namespace KinectWpf
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public bool recordActive;
        private bool isRecording;

        private string _loadedFilePath;
        private VirtualKinect.VirtualKinect _vk;
        private KinectSensor kinect;

        static Slider slider;
        static SortedList<long, double> sliderMap;
        static bool sliderInitialized;


        static List<System.Windows.Shapes.Ellipse> ellipses;
        Canvas canvas;

        public MainWindow()
        {
            InitializeComponent();
            InitializeVariables();
        }

        private void InitializeVariables()
        {
            recordActive = false;
            sliderInitialized = false;
            isRecording = false;
            SetPoints();
        }

        ~MainWindow()
        {
        }

       
        /// <summary>
        /// if recording hasn't been started stop any possible virtual kinect
        /// init write to temp file and start physical kinect
        /// </summary>
        private void StartRecording()
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
                if (kinect != null)
                {
                    RecordingBorder.Visibility = Visibility.Visible;
                    HideMessageBlocks();
                } 
                else
                {
                    ShowErrorMessage("No kinect found.");
                }
            }
        }

        /// <summary>
        /// start Virtual Kinect, begin replay and sending data through a pipe
        /// </summary>
        private void Play()
        {
            //this if was put in here just to be sure that recording has been stopped in previous versions
            // now it's here just as a insurance
            Thread.Sleep(GetDelay() * 1000);
            if (isRecording)
            {
                StopRecording();
            }
            VirtualKinectStart();
        }


        /// <summary>
        /// initialize slider with data from recording file
        /// </summary>
        private void InitializeSlider()
        {
            sliderInitialized = false;

            slider = this.FindName("PlayerSlider") as Slider;
            long[] skeletonTimings = _vk.GetSkeletonsTimings();
            sliderMap = new SortedList<long, double>();
            foreach (var key in skeletonTimings)
            {
                double item = (double)(key / 10000) / 1000;
                if (!sliderMap.ContainsKey(key))
                {
                    sliderMap.Add(key, item);
                }
            }
            
            slider.Ticks = new DoubleCollection(sliderMap.Values);
            slider.Minimum = sliderMap.Values.First();
            slider.Maximum = sliderMap.Values.Last();
            sliderInitialized = true;
        }

        /// <summary>
        /// finds red points on canvas and sets them to the list of ellipses
        /// </summary>
        private void SetPoints()
        {
            ellipses = new List<System.Windows.Shapes.Ellipse>();
            canvas = this.FindName("MainCanvas") as Canvas;
            RecordingBorder.Visibility = Visibility.Hidden;
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
        
        
        /// <summary>
        /// try to conect to kinect device and connect frame ready event
        /// </summary>
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

        /// <summary>
        /// function that starts Virtual Kinect and initializes onSkeletonFrameReady event
        /// </summary>
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

        /// <summary>
        /// event function for native kinect that draws a player on server app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="VirtualKinectException"></exception>
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


        /// <summary>
        /// event function for virtual kinect that draws a player on server app and sends player data through a pipe
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <exception cref="VirtualKinectException"></exception>
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

        /// <summary>
        /// arranges 
        /// static List<System.Windows.Shapes.Ellipse> ellipses;
        /// into a shape that is encoded in param user
        /// </summary>
        /// <param name="user" cref="MySkeleton2"> Virtual Kinect skeleton object that encodes most important info about joints locations </param>
        public static void SkeletonDrawer(MySkeleton2 user)
        {
            try
            {
                if (slider != null)
                {
                    slider.Value = (double)(user.timeStamp / 10000) / 1000;
                }
                
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

        /// <summary>
        /// handle Play Button press
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        /// <summary>
        /// handle Record button click
        /// </summary>
        /// <param name="sender" cref="Button"></param>
        /// <param name="e"></param>
        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            StartRecording();
        }

        /// <summary>
        /// handle Load button click
        /// tries to open a file returned from a OpenFileDialog
        /// </summary>
        /// <param name="sender" cref="Button"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// shows error message on the bottom of the window
        /// </summary>
        /// <param name="message" cref="string">message to show</param>
        private void ShowErrorMessage(string message)
        {
            HideMessageBlocks();
            ErrorBlock.Text = message;
            ErrorBlock.Visibility = Visibility.Visible;
            ErrorBlock_Player.Text = message;
            ErrorBlock_Player.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// hides error message on the bottom of the window
        /// </summary>
        private void HideErrorMessageBlock()
        {
            ErrorBlock.Visibility = Visibility.Collapsed;
            ErrorBlock_Player.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// shows info message on the bottom of the window
        /// </summary>
        /// <param name="message" cref="string">message to show</param>
        private void ShowInfoMessage(string message)
        {
            HideMessageBlocks();
            InfoBlock.Text = message;
            InfoBlock.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// hides info message on the bottom of the window
        /// </summary>
        private void HideInfoMessageBlock()
        {
            InfoBlock.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// hides messages on bottom of the window
        /// </summary>
        private void HideMessageBlocks()
        {
            HideErrorMessageBlock();
            HideInfoMessageBlock();
        }


        /// <summary>
        /// pause handle, pauses/unpauses replay
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _vk.IsPaused = !_vk.IsPaused;
        }
        /// <summary>
        /// stop button handle, stopps both virtual and native kinect in both modes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopKinects();
        }

        /// <summary>
        /// saves file which was recorded 
        /// </summary>
        /// <param name="sender" cref="Button"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// stop any recording on context menu change 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecorderTab_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            StopKinects();
        }

        /// <summary>
        /// stop any recording on context menu change 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerTab_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            StopKinects();
        }

        /// <summary>
        /// set index of Skeletons list inside of VK on value change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sliderInitialized)
            {
                Slider tmpsender = sender as Slider;
                //IsFocused, IsMouseDirectlyOver, IsMouseOver, IsKeyboardFocused, IsKeyboardFocusWithin
                    _vk.skeletonsPointerIndex = sliderMap.IndexOfValue(tmpsender.Value);
            }
        }

        /// <summary>
        /// inform VK that user has stopped manipulating timeline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerSlider_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vk != null)
            {
                _vk.sliderManipulated = false;
            }
        }

        /// <summary>
        /// inform VK that user is manipulating timeline
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayerSlider_GotMouseCapture(object sender, MouseEventArgs e)
        {
            if (_vk != null)
            {
                _vk.sliderManipulated = true;
            }
        }

        /// <summary>
        /// turns off virtual kinect or normal kinect recording because normal window deconstructor didnt seem to work
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            StopKinects();
        }

        /// <summary>
        /// Stops both virtual and native kinects
        /// </summary>
        private void StopKinects()
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

        /// <summary>
        /// stops recording of kinect stream so it can be saved
        /// </summary>
        private void StopRecording()
        {
            if (null != kinect)
            {

                RecordingBorder.Visibility = Visibility.Hidden;
                kinect.Stop();
            }
            KinectSerializer.CloseStream();
            isRecording = false;

        }

        private void DelayTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DelayTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (DelayTextBox.Text == "Delay")
            {
                DelayTextBox.Text = "";
            }
        }

        private void DelayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (DelayTextBox.Text == "")
            {
                DelayTextBox.Text = "Delay";
            }
        }

        private int GetDelay()
        {
            if (DelayTextBox.Text == "Delay" || DelayTextBox.Text == "")
            {
                return 0;
            }
            return Int32.Parse(DelayTextBox.Text);
        }
    }
}

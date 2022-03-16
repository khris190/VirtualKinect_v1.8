﻿using System;
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

        public static bool recordActive = false;

        public MainWindow()
        {
            InitializeComponent();
            SetPoints();

            if (recordActive)
            {
                // Record
                SerializeKinectData.InitWrite(true);
                KinectStart();
            }
            else
            {
                // Play
                SerializeKinectData.InitRead();
                VirtualKinectStart();
            }
        }
        ~MainWindow()
        {
            SerializeKinectData.CompressData();
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
        private static void KinectStart()
        {
            var kinect = KinectSensor.KinectSensors.FirstOrDefault(s => s.Status == KinectStatus.Connected);
            if (null != kinect)
            {
                kinect.SkeletonStream.Enable();
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(SkeletonFrameReady);
                kinect.Start();
            }
        }

        private static void VirtualKinectStart()
        {
            var vk = new VirtualKinect();
            vk.Enable();
            vk.SkeletonFrameReady += new EventHandler<VirtualFrameReadyEventArgs>(SkeletonDrawer);
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
                            SerializeKinectData.SerializeFrame(user);
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

        public static void SkeletonDrawer(MySkeleton2 user)
        {
            for (int i = 0; i < 20; i++)
            {
                Canvas.SetLeft(ellipses[i], (user.joints[i].X + 1) * 200);
                Canvas.SetTop(ellipses[i], 400 - (user.joints[i].Y + 1) * 200);
            }
        }
    }
}
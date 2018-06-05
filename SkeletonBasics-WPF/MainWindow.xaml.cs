//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using System;
    using System.Windows.Media.Imaging;
    using System.Windows.Shapes;
    using System.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor kinectDevice;
        private readonly Brush[] skeletonBrushes;

        private WriteableBitmap depthImageBitMap;
        private Int32Rect depthImageBitmapRect;
        private Int32 depthImageStride;
        private DepthImageFrame lastDepthFrame;

        private WriteableBitmap colorImageBitmap;
        private Int32Rect colorImageBitmapRect;
        private int colorImageStride;
        private byte[] colorImagePixelData;
        private int CameraAngle;
        private Skeleton[] frameSkeletons;

        public MainWindow()
        {
            InitializeComponent();

            skeletonBrushes = new Brush[] { Brushes.CornflowerBlue };

            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.KinectDevice = potentialSensor;
                    break;
                }
            }
        }

        public KinectSensor KinectDevice
        {
            get { return this.kinectDevice; }
            set
            {
                if (this.kinectDevice != value)
                {
                    //Uninitialize
                    if (this.kinectDevice != null)
                    {
                        this.kinectDevice.Stop();
                        this.kinectDevice.SkeletonFrameReady -= kinectDevice_SkeletonFrameReady;
                        this.kinectDevice.ColorFrameReady -= kinectDevice_ColorFrameReady;
                        this.kinectDevice.DepthFrameReady -= kinectDevice_DepthFrameReady;
                        this.kinectDevice.SkeletonStream.Disable();
                        this.kinectDevice.DepthStream.Disable();
                        this.kinectDevice.ColorStream.Disable();
                        this.frameSkeletons = null;
                    }

                    this.kinectDevice = value;

                    //Initialize
                    if (this.kinectDevice != null)
                    {
                        if (this.kinectDevice.Status == KinectStatus.Connected)
                        {
                            this.kinectDevice.SkeletonStream.Enable();
                            this.kinectDevice.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                            this.kinectDevice.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                            this.frameSkeletons = new Skeleton[this.kinectDevice.SkeletonStream.FrameSkeletonArrayLength];
                            this.kinectDevice.SkeletonFrameReady += kinectDevice_SkeletonFrameReady;
                            this.kinectDevice.ColorFrameReady += kinectDevice_ColorFrameReady;
                            this.kinectDevice.DepthFrameReady += kinectDevice_DepthFrameReady;
                            this.kinectDevice.Start();

                            DepthImageStream depthStream = kinectDevice.DepthStream;
                            depthStream.Enable();

                            depthImageBitMap = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight, 96, 96, PixelFormats.Gray16, null);
                            depthImageBitmapRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                            depthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;

                            ColorImageStream colorStream = kinectDevice.ColorStream;
                            colorStream.Enable();
                            colorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                                                            96, 96, PixelFormats.Bgr32, null);
                            this.colorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth, colorStream.FrameHeight);
                            this.colorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                            ColorImage.Source = this.colorImageBitmap;

                            DepthImage.Source = depthImageBitMap;
                        }
                    }
                }
            }
        }

        void kinectDevice_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    short[] depthPixelDate = new short[depthFrame.PixelDataLength];
                    depthFrame.CopyPixelDataTo(depthPixelDate);
                    depthImageBitMap.WritePixels(depthImageBitmapRect, depthPixelDate, depthImageStride, 0);
                }
            }
        }

        void kinectDevice_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    this.colorImageBitmap.WritePixels(this.colorImageBitmapRect, pixelData, this.colorImageStride, 0);
                }
            }
        }
        private void AngleSolver(Skeleton skeleton)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom) && !skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                AngleThread(-1);
            }
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                AngleThread(1);
            }

        }
        public void ResetAngle()
        {
            KinectSensor sensor = this.KinectDevice;
            try
            {
                sensor.ElevationAngle = 0;
            }
            catch (System.Exception e) {

            }

           
        }
        public void AngleResetThread()
        {
            Thread t2 = new Thread(() => ResetAngle());
            t2.Start();
        }
        public void SetAngle(int angle)
        {

            KinectSensor sensor = this.KinectDevice;
            try
            {
                if (angle + CameraAngle > 27 || angle + CameraAngle < -27)
                {
                    CameraAngle = 0;
                    sensor.ElevationAngle = CameraAngle;
                }
                else
                {
                    CameraAngle = CameraAngle + angle;
                    sensor.ElevationAngle = CameraAngle;
                    Thread.Sleep(1000);
                }
            }
            catch (System.Exception e)
            {

            }
        }
        public void AngleThread(int angle)
        {
            Thread t = new Thread(() => SetAngle(angle));
            t.Start();
        }

        void kinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
               
                if (frame != null)
                {
                    Polyline figure;
                    Brush userBrush;
                    Skeleton skeleton;

                    LayoutRoot.Children.Clear();
                    frame.CopySkeletonDataTo(this.frameSkeletons);

                    if (frameSkeletons.Length == 0)
                    {
                        AngleResetThread();
                    }
                    for (int i = 0; i < this.frameSkeletons.Length; i++)
                    {
                        skeleton = this.frameSkeletons[i];
                        AngleSolver(skeleton);
                        
                        if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                        {
                            userBrush = this.skeletonBrushes[i % this.skeletonBrushes.Length];

                            //draw head and body
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.Head, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.Spine,
                                                                JointType.ShoulderRight, JointType.ShoulderCenter, JointType.HipCenter
                                                                });
                            LayoutRoot.Children.Add(figure);

                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipLeft, JointType.HipRight });
                            LayoutRoot.Children.Add(figure);

                            //draw left leg
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft });
                            LayoutRoot.Children.Add(figure);

                            //draw right leg
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight });
                            LayoutRoot.Children.Add(figure);

                            //draw left arm
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft });
                            LayoutRoot.Children.Add(figure);

                            //draw right arm
                            figure = CreateFigure(skeleton, userBrush, new[] { JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight });
                            LayoutRoot.Children.Add(figure);
                        }
                    }
                }
               
            }
        }

        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();
            
            figure.StrokeThickness = 8;
            figure.Stroke = brush;

            for (int i = 0; i < joints.Length; i++)
            {
                figure.Points.Add(GetJointPoint(skeleton.Joints[joints[i]]));
            }

            return figure;
        }

        private Point GetJointPoint(Joint joint)
        {
            CoordinateMapper cm = new CoordinateMapper(kinectDevice);

            DepthImagePoint point = cm.MapSkeletonPointToDepthPoint(joint.Position, this.KinectDevice.DepthStream.Format);
            //ColorImagePoint point = cm.MapSkeletonPointToColorPoint(joint.Position, this.KinectDevice.ColorStream.Format);
            point.X *= (int)this.LayoutRoot.ActualWidth / KinectDevice.DepthStream.FrameWidth;
            point.Y *= (int)this.LayoutRoot.ActualHeight / KinectDevice.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                case KinectStatus.NotPowered:
                case KinectStatus.NotReady:
                case KinectStatus.DeviceNotGenuine:
                    this.KinectDevice = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.                    
                    this.KinectDevice = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }

    }

}
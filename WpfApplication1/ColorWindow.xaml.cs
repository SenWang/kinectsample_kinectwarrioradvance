using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace WpfApplication1
{

    public partial class ColorWindow : Window
    {
        KinectSensor kinect;
        public ColorWindow(KinectSensor sensor) : this()
        {
            kinect = sensor;
        }

        public ColorWindow()
        {
            InitializeComponent();
            Loaded += ColorWindow_Loaded;
            Unloaded += ColorWindow_Unloaded;
        }
        void ColorWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ColorStream.Disable();
                kinect.SkeletonStream.Disable();
                kinect.Stop();
                kinect.ColorFrameReady -= myKinect_ColorFrameReady;
                kinect.SkeletonFrameReady -= mykinect_SkeletonFrameReady;
            }
        }
        private WriteableBitmap _ColorImageBitmap;
        private Int32Rect _ColorImageBitmapRect;
        private int _ColorImageStride;

        void ColorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                #region 繪圖資源初始化
                ColorImageStream colorStream = kinect.ColorStream;

                _ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth,colorStream.FrameHeight, 96, 96,PixelFormats.Bgr32, null);
                _ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,colorStream.FrameHeight);
                _ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                ColorData.Source = _ColorImageBitmap;
                #endregion

                kinect.ColorStream.Enable();
                kinect.ColorFrameReady += myKinect_ColorFrameReady;
                kinect.SkeletonStream.Enable();
                kinect.SkeletonFrameReady += mykinect_SkeletonFrameReady;
                kinect.Start();
            }
        }

        void mykinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skframe = e.OpenSkeletonFrame())
            {
                if (skframe == null)
                    return;

                #region 選擇骨架
                Skeleton[] FrameSkeletons = new Skeleton[skframe.SkeletonArrayLength];
                skframe.CopySkeletonDataTo(FrameSkeletons);

                Skeleton user = (from sk in FrameSkeletons
                                where sk.TrackingState == SkeletonTrackingState.Tracked
                                select sk).FirstOrDefault();
                #endregion

                if (user != null)
                {
                    Joint jpl = user.Joints[JointType.HandLeft];                    
                    ColorImagePoint cpl = MapToColorImagePoint(jpl);
                    double deg = GetOrientation(user);                   
                    Title = "左手掌旋轉角度: " + deg;
                    DrawShield(cpl,deg);
                    
                    Joint jpr = user.Joints[JointType.HandRight];
                    ColorImagePoint cpr = MapToColorImagePoint(jpr);
                    DrawSword(cpr);

                }
            }
        }

        private static double GetOrientation(Skeleton user)
        {
            BoneOrientation bol = user.BoneOrientations[JointType.HandLeft];
            float x = bol.AbsoluteRotation.Quaternion.X;
            float y = bol.AbsoluteRotation.Quaternion.Y;
            double r = Math.Sqrt(x * x + y * y);
            double rad = Math.Asin(x / r);
            return RadianToDegree(rad);
        }

        private static double RadianToDegree(double rad)
        {
            return rad * (180.0 / Math.PI);
        }

        ColorImagePoint MapToColorImagePoint(Joint jp)
        {
            ColorImagePoint cp = 
                kinect.CoordinateMapper.MapSkeletonPointToColorPoint(jp.Position, kinect.ColorStream.Format);
            return cp;
        }

        void DrawShield(ColorImagePoint cp,double deg)
        {
            LeftHandAngle.CenterX = LeftHand.Width / 2;
            LeftHandAngle.CenterY = LeftHand.Height / 2;
            LeftHandAngle.Angle = deg ;

            Canvas.SetLeft(LeftHand,cp.X - LeftHand.Width / 2);
            Canvas.SetTop(LeftHand, cp.Y - LeftHand.Height / 2);
        }

        void DrawSword(ColorImagePoint cp)
        {
            RightHand.X1 = cp.X;
            RightHand.Y1 = cp.Y;
            RightHand.X2 = cp.X;
            RightHand.Y2 = cp.Y - 100;
        }


        void myKinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    byte[] pixelData = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(pixelData);
                    _ColorImageBitmap.WritePixels(_ColorImageBitmapRect, pixelData,_ColorImageStride, 0);
                }
            }
        }
    }
}

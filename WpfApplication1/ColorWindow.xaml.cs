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
                    GestureDetection(user);

                    Joint jpl = user.Joints[JointType.HandLeft];                    
                    ColorImagePoint cpl = MapToColorImagePoint(jpl);
                    double degsh = GetOrientation(user,JointType.HandLeft);
                    DrawShield(cpl,degsh);
                    
                    Joint jpr = user.Joints[JointType.HandRight];
                    ColorImagePoint cpr = MapToColorImagePoint(jpr);
                    DrawSword(cpr);
                }
            }
        }

        void GestureDetection(Skeleton skeleton)
        {
            Joint jhl = skeleton.Joints[JointType.HandLeft];
            Joint jhr = skeleton.Joints[JointType.HandRight];
            Joint jsc = skeleton.Joints[JointType.ShoulderCenter];
            Joint jhc = skeleton.Joints[JointType.HipCenter];
            if (Distance(jhl, jhr) < 0.2 && Distance(jhl, jsc) < 0.2)
            {
                LeftHand.Visibility = Visibility.Visible;
                RightHand.Visibility = Visibility.Visible;
            }
            else if (Distance(jhl, jhr) < 0.2 && Distance(jhl, jhc) < 0.2)
            {
                LeftHand.Visibility = Visibility.Collapsed;
                RightHand.Visibility = Visibility.Collapsed;
            }
        }

        double Distance(Joint p1, Joint p2)
        {
            double dist = 0;
            dist = Math.Sqrt(Math.Pow(p2.Position.X - p1.Position.X, 2) + 
                             Math.Pow(p2.Position.Y - p1.Position.Y, 2));
            return dist;
        }

        private double GetOrientation(Skeleton user, JointType type)
        {
            BoneOrientation bol = user.BoneOrientations[type];
            float hx = bol.HierarchicalRotation.Quaternion.X;
            float hy = bol.HierarchicalRotation.Quaternion.Y;

            double r = Math.Sqrt(hx * hx + hy * hy);
            double rad = Math.Asin(hx / r);
            return RadianToDegree(rad);
        }

        //除錯用版本
        //private double GetOrientationDebug(Skeleton user, JointType type)
        //{
        //    BoneOrientation bol = user.BoneOrientations[type];
        //    startend.Text = "起始關節 " + bol.StartJoint + ", 結束關節 " + bol.EndJoint;
        //    float x = bol.AbsoluteRotation.Quaternion.X;
        //    float y = bol.AbsoluteRotation.Quaternion.Y;
        //    float z = bol.AbsoluteRotation.Quaternion.Z;
        //    absror.Text = String.Format("絕對方位 X={0:0.00}   Y={1:0.00}   Z={2:0.00}", x, y, z);
        //    float hx = bol.HierarchicalRotation.Quaternion.X;
        //    float hy = bol.HierarchicalRotation.Quaternion.Y;
        //    float hz = bol.HierarchicalRotation.Quaternion.Z;
        //    hirror.Text = String.Format("相對方位 X={0:0.00}   Y={1:0.00}   Z={2:0.00}", hx, hy, hz);

        //    double r = Math.Sqrt(hx * hx + hy * hy);
        //    double rad = Math.Asin(hx / r);
        //    Title = "角度: " + rad;
        //    return RadianToDegree(rad);
        //}

        private double RadianToDegree(double rad)
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

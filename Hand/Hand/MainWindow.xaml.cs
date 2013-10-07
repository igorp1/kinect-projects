using System;
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
using Microsoft.Kinect;

namespace Hand
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        public KinectSensor sensor;
        const int skeletonCount = 6;
        Skeleton[] allSkeletons = new Skeleton[skeletonCount];
        public DepthImagePoint rightDepthPoint;
        public int imageref;

        public MainWindow()
        {

            if (KinectSensor.KinectSensors.Count > 0)
            {
                sensor = KinectSensor.KinectSensors[0];
                sensor.DepthStream.Enable();
                sensor.ColorStream.Enable();
                sensor.SkeletonStream.Enable();
                sensor.AllFramesReady += sensor_AllFramesReady;
                sensor.Start();
            }//if


            InitializeComponent();
        }//main window

        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {


            /*****************get color image for the whole body**************************/

            using (ColorImageFrame cframe = e.OpenColorImageFrame())//define the use of the color image streaming
            {

                if (cframe == null)//be sure to eliminate the possibility of an error by "lost" by the kinect
                { return; }


                byte[] cbytes = new byte[cframe.PixelDataLength]; //preallocate a new array that will store the data from the color stream

                cframe.CopyPixelDataTo(cbytes);//copy the data from the stream to the array

                int stride = cframe.Width * 4;//the stride is the number of pixels per line...x4 to represent(Red Green Blue Empty)

                BodyIM.Source = BitmapImage.Create(600, 300, 96, 96, PixelFormats.Bgr32, null, cbytes, stride);
            }//using

            /*******************************************************************/


            /*******************************************************************/

            Skeleton first = GetFirstSkeleton(e);

            if (first == null)
            {
                return;
            }

            GetCameraPoint(first, e);

            using (DepthImageFrame depthimage = e.OpenDepthImageFrame())
            {
                if (depthimage == null)
                { return; }

                byte[] depthImagePixels = ConvertDepthtoRGB(depthimage);

                int stride = depthimage.Width * 4;

                HandIM.Source = BitmapImage.Create(depthimage.Width, depthimage.Height, 96, 96, PixelFormats.Bgr32, null, depthImagePixels, stride);

            }

            


        }//all sensors ready

        /*************************** GET X, Y and Z points for the hand *********************/
        void GetCameraPoint(Skeleton first, AllFramesReadyEventArgs e)
        {
            using (DepthImageFrame depth = e.OpenDepthImageFrame())
            {
                if (depth == null || sensor == null)
                {
                    return;
                }

                //Map a joint location to a point on the depth map

                //right hand
                DepthImagePoint rightDepthPoint =
                    depth.MapFromSkeletonPoint(first.Joints[JointType.HandRight].Position);

                PositionTX.Text = "X: " + rightDepthPoint.X.ToString() + "mm" +
                                "   Y: " + rightDepthPoint.Y.ToString() + "mm" +
                                "   Depth: " + rightDepthPoint.Depth.ToString() + "mm";

                imageref = rightDepthPoint.Depth;
               
                

                //Map a depth point to a point on the color image

                //right hand
                ColorImagePoint rightColorPoint =
                    depth.MapToColorImagePoint(rightDepthPoint.X, rightDepthPoint.Y,
                    ColorImageFormat.RgbResolution640x480Fps30);

                //if depth on hand is different from depth above it, change color

                
                

                //if(imageref+10 > 


                //Set location
                CameraPosition(HandArea, rightColorPoint);

            }//using
        }//GetCameraPoint
        /*******************************************************************/


        byte[] ConvertDepthtoRGB(DepthImageFrame depthimage)
        {
            //Depth data returns 16 bits data array(obs: class short == 16 bit)
            short[] depthRaw = new short[depthimage.PixelDataLength];

            depthimage.CopyPixelDataTo(depthRaw);

            byte[] depthImagePixels = new byte[depthimage.Height * (depthimage.Width * 4)];

            //preallocate all your variables
            const int blueindx = 0;
            const int greenindx = 1;
            const int redindx = 2;
            byte Gold_R = Colors.Gold.R;
            byte Gold_G = Colors.Gold.G;
            byte Gold_B = Colors.Gold.B;
            //byte Coral_R = Colors.Coral.R;
            //byte Coral_G = Colors.Coral.G;
            //byte Coral_B = Colors.Coral.B;
            int player;
            int depth;
            int reff = (rightDepthPoint.Depth) * 10;

            for (int i = 0, colorindx = 0;
                    i < depthRaw.Length && colorindx < depthImagePixels.Length;
                    i++, colorindx += 4)
            {

                depth = (short)(depthRaw[i] >> DepthImageFrame.PlayerIndexBitmaskWidth);
                player = depthRaw[i] & DepthImageFrame.PlayerIndexBitmask;
                /*********************************************************************/

                /*********to black out everything that is not a player****************/
                byte intensity = (byte)(0);
                depthImagePixels[blueindx + colorindx] = intensity;
                depthImagePixels[greenindx + colorindx] = intensity;
                depthImagePixels[redindx + colorindx] = intensity;
                /*********************************************************************/

                /**********to give colors to the players***************/
                if (player > 0)
                {
                    //if (imageref - 10 < depth && depth < imageref + 10)
                    //{
                    //    depthImagePixels[blueindx + colorindx] = Coral_B;
                    //    depthImagePixels[greenindx + colorindx] = Coral_G;
                    //    depthImagePixels[redindx + colorindx] = Coral_R;
                    //}
                    //else
                    //{
                        depthImagePixels[blueindx + colorindx] = Gold_B;
                        depthImagePixels[greenindx + colorindx] = Gold_G;
                        depthImagePixels[redindx + colorindx] = Gold_R;
                    //}
                }
                /*************************************************/



            }//for loop

            return depthImagePixels;

        }//ConvertDepthtoRGB


        /************************** GET FIRST SKELETON **********************/
        Skeleton GetFirstSkeleton(AllFramesReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrameData = e.OpenSkeletonFrame())
            {
                if (skeletonFrameData == null)
                {
                    return null;
                }

                skeletonFrameData.CopySkeletonDataTo(allSkeletons);

                //get the first skeleton

                Skeleton first = (from s in allSkeletons
                                  where s.TrackingState == SkeletonTrackingState.Tracked
                                  select s).FirstOrDefault();

                return first;


            }
        }
        /*******************************************************************/


        void CameraPosition(FrameworkElement element, ColorImagePoint point)
        {
            //Divide by 2 for width and height so point is right in the middle 
            // instead of in top/left corner



            Canvas.SetLeft(element, point.X - (element.Width / 2));
            Canvas.SetTop(element, point.Y - (element.Height / 2));

        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.Dispose();
            }//if
        }










    }//partial class
}//name space
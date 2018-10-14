using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
//using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;
using Coding4Fun.Kinect.WinForm;
using System.IO.Ports;
using System.Windows.Media;
using System.Windows;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        int count = 0;

        long avg = 0;

        int iteration = 0;

        int countFor250 = 0;

        int countFor300 = 0;

        int countFor350 = 0;

        int countFor400 = 0;

        int countFor450 = 0;

        int countFor500 = 0;

        int countFor550 = 0;

        int countFor600 = 0;

        bool iskinectDisconnected = false;

        long dataOut;
        private DepthImagePixel[] depthPixels;
        private byte[] colorPixels;
        private KinectSensor kSensor;
        private System.Windows.Media.Imaging.WriteableBitmap colorBitmap;
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            string[] ports = SerialPort.GetPortNames();
            foreach (var item in ports)
            {
                cboComPort.Items.Add(item);
                cboComPort.SelectedIndex = 0;

            }
            lblConnectionId.Visible = false;
            lblConnectionIdValue.Visible = false;
            serialPort1.PortName = cboComPort.Text;
            serialPort1.BaudRate = 19200;
            serialPort1.DataBits = 8;
            serialPort1.StopBits = StopBits.One;
            serialPort1.Parity = Parity.None;
            serialPort1.Open();

        }

        private void btnStream_Click(object sender, EventArgs e)
        {
            if (btnStream.Text == "Start")
            {

                if (KinectSensor.KinectSensors.Count > 0)
                {
                    this.btnStream.Text = "Stop";

                    kSensor = KinectSensor.KinectSensors[0];

                    KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
                }
                kSensor.Start();

                this.lblConnectionIdValue.Text = kSensor.DeviceConnectionId;

                kSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                kSensor.DepthStream.Enable();
                this.depthPixels = new DepthImagePixel[this.kSensor.DepthStream.FramePixelDataLength];
                this.colorPixels = new byte[this.kSensor.DepthStream.FramePixelDataLength * sizeof(int)];
                this.colorBitmap = new System.Windows.Media.Imaging.WriteableBitmap(this.kSensor.DepthStream.FrameWidth, this.kSensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);

                // Set the image we display to point to the bitmap where we'll put the image data
               // pictureBox1.Image = colorBitmap;
                kSensor.AllFramesReady += KSensor_AllFramesReady;

                kSensor.SkeletonStream.Enable();

                kSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;

            }
            else
            {
                if (kSensor != null && kSensor.IsRunning && btnStream.Text == "Stop")
                {
                    kSensor.Stop();

                    this.btnStream.Text = "Start";

                    this.pictureBox1.Image = null;

                    if (serialPort1.IsOpen)
                    {
                        serialPort1.Close();
                    }
                }
            }

        }

        //private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        //{
        //    using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
        //    {
        //        if (depthFrame != null)
        //        {
        //            depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
        //            // Get the min and max reliable depth for the current frame
        //            int minDepth = depthFrame.MinDepth;
        //            int maxDepth = depthFrame.MaxDepth;

        //            // Convert the depth to RGB
        //            int colorPixelIndex = 0;
        //            for (int i = 0; i < this.depthPixels.Length; ++i)
        //            {
        //                // Get the depth for this pixel
        //                short depth = depthPixels[i].Depth;

        //                // To convert to a byte, we're discarding the most-significant
        //                // rather than least-significant bits.
        //                // We're preserving detail, although the intensity will "wrap."
        //                // Values outside the reliable depth range are mapped to 0 (black).

        //                // Note: Using conditionals in this loop could degrade performance.
        //                // Consider using a lookup table instead when writing production code.
        //                // See the KinectDepthViewer class used by the KinectExplorer sample
        //                // for a lookup table example.
        //                byte intensity = (byte)(depth >= minDepth && depth <= 2000 ? depth : 0);

        //                // Write out blue byte
        //                this.colorPixels[colorPixelIndex++] = intensity;

        //                // Write out green byte
        //                this.colorPixels[colorPixelIndex++] = intensity;

        //                // Write out red byte                        
        //                this.colorPixels[colorPixelIndex++] = intensity;

        //                // We're outputting BGR, the last byte in the 32 bits is unused so skip it
        //                // If we were outputting BGRA, we would write alpha here.
        //                ++colorPixelIndex;
        //            }
        //            // Write the pixel data into our bitmap
        //            this.colorBitmap.WritePixels(
        //                new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight),
        //                this.colorPixels,
        //                this.colorBitmap.PixelWidth * sizeof(int),
        //                0);

        //        }
        //    }
        //}

            private void KSensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            using (var frame = e.OpenColorImageFrame())
                if (frame != null)
                {
                    if (!iskinectDisconnected)
                        pictureBox1.Image = CreateBitmapFromSensor(frame);

                }

            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null)
                {
                    return;

                }

                var skeletons = new Skeleton[frame.SkeletonArrayLength];
                frame.CopySkeletonDataTo(skeletons);

                var TrackedSkeleton = skeletons.FirstOrDefault(x => x.TrackingState == SkeletonTrackingState.Tracked);

                if (TrackedSkeleton == null)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                    serialPort1.WriteLine(Convert.ToString(-500));
                    iteration = 0;
                    return;
                }


                var position = TrackedSkeleton.Joints[JointType.HandRight].Position;

                var coordinatemapper = new CoordinateMapper(kSensor);

                var colorpoint = coordinatemapper.MapSkeletonPointToColorPoint(position, ColorImageFormat.InfraredResolution640x480Fps30);

                this.lblPosition.Text = string.Format(" Hand Position X:{0}, Y : {1}", colorpoint.X, colorpoint.Y);

                dataOut = colorpoint.X;

                count++;
                if (dataOut > 150)
                {
                    axWindowsMediaPlayer1.Ctlcontrols.play();

                    if (dataOut > 150)
                    {
                        countFor250++;
                    }
                    if (dataOut > 250 && dataOut < 300)
                    {
                        countFor300++;
                    }

                    if (dataOut > 300 && dataOut < 350)
                    {
                        countFor350++;
                    }
                    if (dataOut > 350 && dataOut < 400)
                    {
                        countFor400++;
                    }
                    if (dataOut > 400 && dataOut < 450)
                    {
                        countFor450++;
                    }
                    if (dataOut > 450 && dataOut < 500)
                    {
                        countFor500++;
                    }
                    if (dataOut > 500 && dataOut < 590)
                    {
                        countFor550++;
                    }
                    if (dataOut > 620)
                    {
                        countFor600++;
                    }
                    if (count > 50 && iteration == 0 && countFor250 > 1)
                    {
                        avg = 250;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 1;
                        avg = 0;
                        countFor300 = 0;

                    }
                    if (count > 50 && iteration == 1 && countFor300 > 1)
                    {
                        avg = 300;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 2;
                        avg = 0;
                        countFor300 = 0;

                    }
                    if (count > 50 && iteration <= 2 && countFor350 > 5)
                    {
                        avg = 350;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 3;
                        avg = 0;
                        countFor350 = 0;
                    }
                    if (count > 50 && iteration == 3 && countFor400 > 5)
                    {
                        avg = 400;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 4;
                        avg = 0;
                        countFor400 = 0;

                    }
                    if (count > 50 && iteration == 4 && countFor450 > 5)
                    {
                        avg = 450;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 5;
                        avg = 0;
                        countFor450 = 0;

                    }
                    if (count > 50 && iteration == 5 && countFor500 > 5)
                    {
                        avg = 500;
                        serialPort1.WriteLine(Convert.ToString(avg));
                       // count = 0;
                        iteration = 6;
                        avg = 0;
                        countFor500 = 0;

                    }
                    if (count > 100 && iteration == 6 )
                    {
                        avg = 550;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 7;
                        avg = 0;
                        countFor550 = 0;

                    }
                    if (count > 110 && iteration == 7)
                    {
                        avg = 700;
                        serialPort1.WriteLine(Convert.ToString(avg));
                        count = 0;
                        iteration = 0;
                        avg = 0;
                        countFor600 = 0;

                    }
                    dataOut = 0;
                }
                else
                {
                    axWindowsMediaPlayer1.Ctlcontrols.pause();
                    serialPort1.WriteLine(Convert.ToString(-500));
                    iteration = 0;

                }
            }
        }

      
        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            this.lblStatusValue.Text = kSensor.Status.ToString();
        }

        private System.Drawing.Bitmap CreateBitmapFromSensor(ColorImageFrame frame)
        {
            var pixelData = new byte[frame.PixelDataLength];

            frame.CopyPixelDataTo(pixelData);

            return pixelData.ToBitmap(frame.Width, frame.Height);
        }

        private void btnStream_Click_1(object sender, EventArgs e)
        {
            if (btnStream.Text == "Start")
            {

                if (KinectSensor.KinectSensors.Count > 0)
                {
                    this.btnStream.Text = "Stop";

                    kSensor = KinectSensor.KinectSensors[0];

                    this.lblStatusValue.Text = "Connected";

                    axWindowsMediaPlayer1.URL = "C:\\Users\\Songs\\0008.mp3";
                }
                kSensor.Start();
                iskinectDisconnected = false;

                this.lblConnectionIdValue.Text = kSensor.DeviceConnectionId;

                kSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                kSensor.DepthStream.Enable();

                kSensor.AllFramesReady += KSensor_AllFramesReady;
                //kSensor.DepthFrameReady += this.SensorDepthFrameReady;

                kSensor.SkeletonStream.Enable();

                kSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            }
            else
            {
                if (kSensor != null && kSensor.IsRunning && btnStream.Text == "Stop")
                {
                    kSensor.Stop();

                    iskinectDisconnected = true;

                    serialPort1.WriteLine(Convert.ToString(-500));
                    iteration = 0;
                    this.btnStream.Text = "Start";

                    this.lblStatusValue.Text = "Disconnected";

                }
            }
        }
    }
}

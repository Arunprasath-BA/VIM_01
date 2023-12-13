using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace VIM
{
    
    public partial class MainWindow : Window
    {

        #region Object assignments
        private SerialPort serialPort;
        private DispatcherTimer Timer;

        string data = string.Empty;
        bool displayFlag = false;
        bool resizeFlag = false;
        bool bgRemoveFlag = false;

        NeoAPI.Cam camera;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer();
            CameraInitialization();
            InitializeSerialPort(true);
        }
        #endregion

        #region Serial Port Initialize method
        private bool InitializeSerialPort(bool silent = false)
        {

            bool isInit = false;
            bool isSerialPortInitialized = false;

            if (serialPort == null)
            {
                isInit = true;
            }

            else
            {
                if (!serialPort.IsOpen)
                {
                    isInit = true;
                }
            }

            if (isInit)
            {
                serialPort = new SerialPort("COM11", 9600);

                serialPort.DataReceived += SerialPort_DataReceived;

                try
                {
                    serialPort.Open();
                    isSerialPortInitialized = true;
                    writeStr();
                }

                catch (Exception ex)
                {
                    if (!silent)
                    {
                        MessageBox.Show($"Error opening serial port: {ex.Message}");
                    }
                }

            }

            else
            {
                isSerialPortInitialized = true;
            }

            return isSerialPortInitialized;

        }
        #endregion

        async void writeStr()
        {

            await Task.Run(() => 
            {

                Thread.Sleep(1000);

                if (serialPort != null)
                {
                    serialPort.WriteLine("20,20,100,35,250,87,90,200,90,90,80,35,1,0,1100,1350,320,500"); //601
                    //serialPort.WriteLine("20,20,100,35,250,87,90,200,90,90,80,35,1,0,5,1350,320,500"); //143
                    Console.WriteLine("Parameters sent successfully");
                }

                else
                {
                    Console.WriteLine("Serial Port is unavailable");
                }

            });

        }

        #region Serial Port receiver method
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            data = sp.ReadLine();

            
            //Console.WriteLine("String received from serial port: " + data);
            
        }
        #endregion

        #region Serial Port Status checking method
        private void SerialPortStatus()
        {

            string[] pn = SerialPort.GetPortNames();
            bool isSerialPortInitialized = false;

            if (serialPort != null)
            {

                if (!serialPort.IsOpen)
                {
                    InitializeSerialPort(true);
                }

                if (pn.Contains(serialPort.PortName) && serialPort.IsOpen)
                {
                    isSerialPortInitialized = true;

                }

            }

            if (isSerialPortInitialized)
            {

                Dispatcher.Invoke(() =>
                {
                    lblCtrlr.Content = "Online";
                    rectCtrlr.Fill = Brushes.Green;
                });

            }

            else
            {

                Dispatcher.Invoke(() =>
                {
                    lblCtrlr.Content = "Offline";
                    rectCtrlr.Fill = Brushes.Red;
                });
                
            }

        }
        #endregion

        #region Camera Initialize method
        private void CameraInitialization()
        {

            try
            {

                camera = new NeoAPI.Cam();
                camera.Connect("700009600797"); //803 - Cam 2

                if(camera.IsConnected)
                {
                    rectCamera1.Fill = Brushes.Green;
                    lblCamera1.Content = "Online";
                    //Console.WriteLine("Serial Number: " + camera.f.DeviceSerialNumber.ValueString);
                    
                }

                else
                {
                    rectCamera1.Fill = Brushes.Red;
                }

            }

            catch(Exception e)
            {
                MessageBox.Show("Error while connecting to Camera: " + e.Message);
            }

        }
        #endregion

        #region Timer
        private void InitializeTimer()
        {
            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(0);
            Timer.Tick += Timer_Tick;
            Timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {

            SerialPortStatus();
            CaptureTrigger();
            DisplayInput();
            ResizePY();
            bgPY();
        }
        #endregion

        private void CaptureTrigger()
        {

            if (data == "c1\r")
            {
                data = null;
                //Thread.Sleep(1450); //Delay Time 1150 ms in C# + 5 ms in SP
                CaptureMethod();

            }

        }

        private void DisplayInput()
        {

            if(displayFlag)
            {
                displayFlag = false;
                displayImage();
            }

        }

        private void ResizePY()
        {

            if (resizeFlag)
            {
                resizeFlag = false;
                PyImageResizing(@"D:\VIM_001_\resize1.py");
            }

        }

        private void bgPY()
        {

            if (bgRemoveFlag)
            {
                bgRemoveFlag = false;
                PyImageProcessing(@"D:\VIM_001_\brown_bg_removal.py");
            }

        }

        #region Image Capture method
        private void CaptureMethod()
        {

            try
            {
                Stopwatch stopwatch = new Stopwatch();

                if (camera.IsConnected)
                {

                    stopwatch.Start();
                    NeoAPI.Image image = camera.GetImage();
                    image.Save(@"D:\VIM_001_\input_image\input_img.bmp");

                    string DTS = DateTime.Now.ToString();
                    string _DTS = DTS.Replace(" ", "_");
                    string dts = _DTS.Replace(":", ".");
                    image.Save(@"D:\VIM_001_\input_backup\Baumer_" + dts + ".bmp");
                    stopwatch.Stop();

                    Console.WriteLine($"Image Taken successfully. Time Taken: {stopwatch.ElapsedMilliseconds} ms");

                    displayFlag = true;
                    //stopwatch.Start();
                    //PyImageResizing(@"D:\VIM_001_\resize1.py");

                    //PyImageProcessing(@"D:\VIM_001_\brown_bg_removal.py");

                    // await Task.Run(() => PyImageProcessing(@"D:\VIM_001_\143.py"));
                    //stopwatch.Stop();

                    //Console.WriteLine($"Time Taken to run Python Code: {stopwatch.ElapsedMilliseconds} ms");

                }

                else
                {
                    Console.WriteLine("Error Capturing image");
                    return;
                }

            }

            catch (Exception exc)
            {
                MessageBox.Show("Error while capturing image: " + exc.Message);
            }

        }
        #endregion

        private void displayImage()
        {

            string folderPath1 = @"D:\VIM_001_\input_backup";
            var directory1 = new DirectoryInfo(folderPath1);

            var recentFile1 = directory1.GetFiles()
                                      .Where(file => IsImageFile(file.Name))
                                      .OrderByDescending(file => file.LastWriteTime)
                                      .FirstOrDefault();

            if (recentFile1 != null)
            {

                Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(recentFile1.FullName));
                    imgInput1.Source = bitmap;
                    resizeFlag = true;
                });

            }

        }

        #region Python Image Processing method
        private void PyImageResizing(string PYfilepath)
        {

            Dispatcher.Invoke(() =>
            {

                try
                {
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = @"C:\Users\Admin\anaconda3\python.exe";
                    start.Arguments = string.Format("{0}", PYfilepath);
                    start.UseShellExecute = false;
                    start.RedirectStandardOutput = true;
                    start.CreateNoWindow = true;
                    start.WindowStyle = ProcessWindowStyle.Hidden;

                    using (Process process = Process.Start(start))
                    {

                        using (StreamReader reader = process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            Console.WriteLine(result);
                            bgRemoveFlag = true;
                        }

                    }
                    stopwatch.Stop();

                    Console.WriteLine($"Time Taken for Python Resize code: {stopwatch.ElapsedMilliseconds} ms");

                }

                catch (Exception ex)
                {
                    MessageBox.Show("Unable to run python code: " + ex.Message);
                }

            });

        }

        private void PyImageProcessing(string PYfilepath)
        {

            Dispatcher.Invoke(() => 
            {

                try
                {
                    Stopwatch stopwatch = new Stopwatch();

                    stopwatch.Start();
                    ProcessStartInfo start = new ProcessStartInfo();
                    start.FileName = @"C:\Users\Admin\anaconda3\python.exe";
                    start.Arguments = string.Format("{0}", PYfilepath);
                    start.UseShellExecute = false;
                    start.RedirectStandardOutput = true;
                    start.CreateNoWindow = true;
                    start.WindowStyle = ProcessWindowStyle.Hidden;

                    using (Process process = Process.Start(start))
                    {

                        using (StreamReader reader = process.StandardOutput)
                        {
                            string result = reader.ReadToEnd();
                            Console.WriteLine(result);
                        }

                    }
                    stopwatch.Stop();

                    Console.WriteLine($"Time Taken for Python Background Removal code: {stopwatch.ElapsedMilliseconds} ms");

                    DisplayProcessedImage(@"D:\VIM_001_\output_image");

                }

                catch (Exception ex)
                {
                    MessageBox.Show("Unable to run python code: " + ex.Message);
                }

            });
            
        }
        #endregion

        private void DisplayProcessedImage(string filePath)
        {

            var directory1 = new DirectoryInfo(filePath);

            var recentFile1 = directory1.GetFiles()
                                      .Where(file => IsImageFile(file.Name))
                                      .OrderByDescending(file => file.LastWriteTime)
                                      .FirstOrDefault();

            if (recentFile1 != null)
            {

                Dispatcher.Invoke(() =>
                {
                    BitmapImage bitmap = new BitmapImage(new Uri(recentFile1.FullName));
                    imgOutput1.Source = bitmap;
                });
                
            }

            else
            {
                MessageBox.Show("No images found in the specified folder.");
            }

        }

        #region Image Formats
        private bool IsImageFile(string fileName)
        {
            string[] extensions = { ".jpg", ".jpeg", ".png", ".bmp" }; // Add more if needed
            return extensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }
        #endregion

        #region Window Closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            if(camera != null)
            {
                camera.Disconnect();
            }

            if(serialPort != null)
            {
                serialPort.Close();
            }

        }
        #endregion

        #region Temporary
        //private void Button_Click(object sender, RoutedEventArgs e)
        //{
        //    CaptureMethod();
        //}
        #endregion

    }

}

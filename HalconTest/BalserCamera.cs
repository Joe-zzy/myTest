using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Basler.Pylon;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using HalconDotNet;
namespace BalserCamera
{
    public enum TriggerModelEnum
    {
        FreeRun = 0,
        ExternTrigger =1,
        Software = 2
    }
    public enum GrabModelEnum
    {
        StartGrabbing  = 0 ,
        StopGrabbing = 1 ,
        GrabOne = 2 
    }
    public enum ImageType
    {
        ImageGrey = 0,
        ImageColor = 1
    }
    public class BalserCognexCamera
    {

        public TriggerModelEnum TriggerModel = TriggerModelEnum.FreeRun;
        public GrabModelEnum GrabModel = GrabModelEnum.StopGrabbing;

        public Camera camera = null;
        public static Camera[] myCamera = new Camera[13];

        public long imageWidth = 0;      // 图像宽
        public long imageHeight = 0;     // 图像高
        public long minExposureTime;    // 最小曝光时间
        public long maxExposureTime;    // 最大曝光时间
        public long minGain;            // 最小增益
        public long maxGain;            // 最大增益

        private long grabTime = 0;          // 采集图像时间

        private Stopwatch stopWatch = new Stopwatch();

        private string cameraName;
        public string CameraName
        {
            get
            {
                return cameraName;
            }
        }
        private string cameraSerialNumber;
        public string CameraSerialNumber
        {
            get
            {
                return cameraSerialNumber;
            }
        }
        private string cameraDeviceID;
        public string CameraDeviceID
        {
            get
            {
                return cameraDeviceID;
            }
        }

        /// <summary>
        /// 计算采集图像时间自定义委托
        /// </summary>
        /// <param name="time">采集图像时间</param>
        public delegate void delegateComputeGrabTime(long time);
        /// <summary>
        /// 计算采集图像时间委托事件
        /// </summary>
        public event delegateComputeGrabTime eventComputeGrabTime;

        /// <summary>
        /// 图像处理自定义委托
        /// </summary>
        /// <param name="hImage"></param>
        public delegate void processHImageDelegate(HObject Image,ImageType imageType);
        /// <summary>
        /// 图像处理委托事件
        /// </summary>
        public event processHImageDelegate processImageEvent;

        /// <summary>
        /// if >= Sfnc2_0_0,说明是ＵＳＢ３的相机
        /// </summary>
        static Version Sfnc2_0_0 = new Version(2, 0, 0);


        /******************    实例化相机    ******************/
        /// <summary>
        /// 实例化第一个找到的相机
        /// </summary>
        public BalserCognexCamera()
        {
            camera = new Camera();
        }

        /// <summary>
        /// 根据相机序列号实例化相机
        /// </summary>
        /// <param name="SN"></param>
        //public BaslerCam(string SN)
        //{
        //    camera = new Camera(SN);
        //}
        public static List<string> GetAllCamerasSerialNumber()
        {
            try
            {
                List<string> listSerialNumer = new List<string>();

                // 枚举相机列表
                List<ICameraInfo> allCameraInfos = CameraFinder.Enumerate();
                foreach (ICameraInfo cameraInfo in allCameraInfos)
                {
                    listSerialNumer.Add(cameraInfo[CameraInfoKey.SerialNumber]);
                }
                return listSerialNumer;
            }
            catch
            {
                return null;
            }
        }
       
        /// <summary>
        /// 根据相机序列号实例化相机
        /// </summary>
        /// <param name="UserID"></param>
        public BalserCognexCamera(string SerialNumber)
        {
            // 枚举相机列表
            List<ICameraInfo> allCameraInfos = CameraFinder.Enumerate();

            foreach (ICameraInfo cameraInfo in allCameraInfos)
            {

                if (SerialNumber == cameraInfo[CameraInfoKey.SerialNumber])
                {
                    cameraName = cameraInfo[CameraInfoKey.ModelName] + "[" + SerialNumber + "]";
                    cameraSerialNumber = SerialNumber;
                    cameraDeviceID = cameraInfo[CameraInfoKey.FriendlyName].Substring(0, cameraInfo[CameraInfoKey.FriendlyName].IndexOf("("));
                    camera = new Camera(cameraInfo);
                }
            }

        }
        /// <summary>
        /// 获取DeviceID
        /// </summary>
        /// <returns></returns>
        public static List<string> GetAllCamerasDeviceID()
        {
            try
            {
                List<string> listDeviceID = new List<string>();

                // 枚举相机列表
                List<ICameraInfo> allCameraInfos = CameraFinder.Enumerate();
                foreach (ICameraInfo cameraInfo in allCameraInfos)
                {
                    string DeviceID = cameraInfo[CameraInfoKey.FriendlyName].Substring(0, cameraInfo[CameraInfoKey.FriendlyName].IndexOf("("));
                    listDeviceID.Add(cameraInfo[CameraInfoKey.FriendlyName]);
                }
                return listDeviceID;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
       
        /*****************************************************/

        /******************    相机操作     ******************/
        public bool IsOpen
        {
            get
            {
                return camera.IsOpen;
            }
        }
        public bool IsGrabbing
        {
            get
            {
                return camera.StreamGrabber.IsGrabbing;
            }
        }

        /// <summary>
        /// 打开相机
        /// </summary>
        public void OpenCam()
        {
            if(camera.IsOpen)
                return;
            camera.Open();

            //关闭自动增益
            camera.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off);
            //关闭自动曝光
            // Some camera models may have auto functions enabled. To set the ExposureTime value to a specific value,
            // the ExposureAuto function must be disabled first (if ExposureAuto is available).
            camera.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off); // Set ExposureAuto to Off if it is writable.

            camera.Parameters[PLCamera.ExposureMode].TrySetValue(PLCamera.ExposureMode.Timed); // Set ExposureMode to Timed if it is writable.

            imageWidth = camera.Parameters[PLCamera.Width].GetValue();          // 获取图像宽 
            imageHeight = camera.Parameters[PLCamera.Height].GetValue();         // 获取图像高
            SetHeartBeatTime(5000);
            camera.StreamGrabber.ImageGrabbed += OnImageGrabbed;                      // 注册采集回调函数


        }

        /// <summary>
        /// 关闭相机,释放相关资源
        /// </summary>
        public void CloseCam()
        {
            if (!camera.IsOpen)
                return;
            camera.Close();

        }

        /// <summary>
        /// 单张采集
        /// </summary>
        public bool GrabOne()
        {
            if (camera.StreamGrabber.IsGrabbing)
            {
                return true;
                //throw new Exception("相机当前正处于采集状态！");
            }
            else
            {
                GrabModel = GrabModelEnum.GrabOne;
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.SingleFrame);
                camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                //stopWatch.Reset();
                //stopWatch.Start();// ****  重启采集时间计时器   ****
                return true;
            }

        }

        /// <summary>
        /// 开始采集
        /// </summary>
        public void StartGrabbing()
        {
        
            if (camera.StreamGrabber.IsGrabbing)
            {
                //throw new Exception("相机当前正处于采集状态！");
                return;

            }
            else
            {
                GrabModel = GrabModelEnum.StartGrabbing;
                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                //stopWatch.Reset();
                //stopWatch.Start();// ****  重启采集时间计时器   ****

            }
        }

        /// <summary>
        /// 停止采集
        /// </summary>
        public void StopGrabbing()
        {

            if (camera.StreamGrabber.IsGrabbing)
            {
                GrabModel = GrabModelEnum.StopGrabbing;
                camera.StreamGrabber.Stop();
            }

        }
        /*********************************************************/

        /******************    相机参数设置   ********************/
        /// <summary>
        /// 设置Gige相机心跳时间
        /// </summary>
        /// <param name="value"></param>
        public void SetHeartBeatTime(long value)
        {

            // 判断是否是网口相机
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(value);
            }

        }

        /// <summary>
        /// 设置相机曝光时间
        /// </summary>
        /// <param name="value"></param>
        public void SetExposureTime(long value)
        {


            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                // In previous SFNC versions, ExposureTimeRaw is an integer parameter,单位us
                // integer parameter的数据，设置之前，需要进行有效值整合，否则可能会报错
                long min = camera.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
                long max = camera.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
                long incr = camera.Parameters[PLCamera.ExposureTimeRaw].GetIncrement();
                if (value < min)
                {
                    value = min;
                }
                else if (value > max)
                {
                    value = max;
                }
                else
                {
                    value = min + (((value - min) / incr) * incr);
                }
                camera.Parameters[PLCamera.ExposureTimeRaw].TrySetValue(value,IntegerValueCorrection .Nearest );
                //IFloatParameter exposure = camera.Parameters[PLCamera.ExposureTimeRaw] as IFloatParameter;
                //exposure.SetValuePercentOfRange(value);
               
                // Or,here, we let pylon correct the value if needed.
                //camera.Parameters[PLCamera.ExposureTimeRaw].SetValue(value, IntegerValueCorrection.Nearest);
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                // In SFNC 2.0, ExposureTimeRaw is renamed as ExposureTime,is a float parameter, 单位us.
                camera.Parameters[PLUsbCamera.ExposureTime].SetValue((double)value);
            }

        }

        public long GetExposureTime()
        {
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                return camera.Parameters[PLCamera.ExposureTimeRaw].GetValue();
           
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                // In SFNC 2.0, ExposureTimeRaw is renamed as ExposureTime,is a float parameter, 单位us.
                return (long)camera.Parameters[PLUsbCamera.ExposureTime].GetValue();
            }
        }
        /// <summary>
        /// 获取最小最大曝光时间
        /// </summary>
        public void GetMinMaxExposureTime()
        {

            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                minExposureTime = camera.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
                maxExposureTime = camera.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
            }
            else
            {
                minExposureTime = (long)camera.Parameters[PLUsbCamera.ExposureTime].GetMinimum();
                maxExposureTime = (long)camera.Parameters[PLUsbCamera.ExposureTime].GetMaximum();
            }

        }

        /// <summary>
        /// 设置增益
        /// </summary>
        /// <param name="value"></param>
        public void SetGain(long value)
        {

            // Some camera models may have auto functions enabled. To set the gain value to a specific value,
            // the Gain Auto function must be disabled first (if gain auto is available).
            // Set GainAuto to Off if it is writable.

            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                // Some parameters have restrictions. You can use GetIncrement/GetMinimum/GetMaximum to make sure you set a valid value.                              
                // In previous SFNC versions, GainRaw is an integer parameter.
                // integer parameter的数据，设置之前，需要进行有效值整合，否则可能会报错
                long min = camera.Parameters[PLCamera.GainRaw].GetMinimum();
                long max = camera.Parameters[PLCamera.GainRaw].GetMaximum();
                long incr = camera.Parameters[PLCamera.GainRaw].GetIncrement();
                if (value < min)
                {
                    value = min;
                }
                else if (value > max)
                {
                    value = max;
                }
                else
                {
                    value = min + (((value - min) / incr) * incr);
                }
                camera.Parameters[PLCamera.GainRaw].TrySetValue(value);
               // IParameter Gain = camera.Parameters[PLCamera.GainRaw];
               // Gain.ParseAndSetValue(value.ToString());
                //// Or,here, we let pylon correct the value if needed.
                //camera.Parameters[PLCamera.GainRaw].SetValue(value, IntegerValueCorrection.Nearest);
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                // In SFNC 2.0, Gain is a float parameter.
                camera.Parameters[PLUsbCamera.Gain].SetValue(value);
            }

        }

        public long GetGain()
        {
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                return camera.Parameters[PLCamera.GainRaw].GetValue();

                //// Or,here, we let pylon correct the value if needed.
                //camera.Parameters[PLCamera.GainRaw].SetValue(value, IntegerValueCorrection.Nearest);
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                // In SFNC 2.0, Gain is a float parameter.
                return (long)camera.Parameters[PLUsbCamera.Gain].GetValue();
            }
        }
        /// <summary>
        /// 获取最小最大增益
        /// </summary>
        public void GetMinMaxGain()
        {

            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                minGain = camera.Parameters[PLCamera.GainRaw].GetMinimum();
                maxGain = camera.Parameters[PLCamera.GainRaw].GetMaximum();
            }
            else
            {
                minGain = (long)camera.Parameters[PLUsbCamera.Gain].GetMinimum();
                maxGain = (long)camera.Parameters[PLUsbCamera.Gain].GetMaximum();
            }

        }

        /// <summary>
        /// 设置相机Freerun模式
        /// </summary>
        public void SetFreerun()
        {
            if (TriggerModel == TriggerModelEnum.FreeRun)
                return;
            TriggerModel = TriggerModelEnum.FreeRun;
            // Set an enum parameter.
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                    }
                }
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                    }
                }
            }
            //stopWatch.Reset();
            //stopWatch.Start();// ****  重启采集时间计时器   ****

        }

        /// <summary>
        /// 设置相机软触发模式
        /// </summary>
        public void SetSoftwareTrigger()
        {
            if (TriggerModel == TriggerModelEnum.Software)
                return;

            TriggerModel = TriggerModelEnum.Software;
            // Set an enum parameter.
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                }
            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                }
            }
            //stopWatch.Reset();
            //stopWatch.Start();// ****  重启采集时间计时器   ****

        }


        /// <summary>
        /// 发送软触发命令
        /// </summary>
        public void SendSoftwareExecute()
        {

            if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
            {
                camera.ExecuteSoftwareTrigger();
                //stopWatch.Reset();
                //stopWatch.Start();// ****  重启采集时间计时器   ****
            }

        }
  
        /// <summary>
        /// 设置相机外触发模式
        /// </summary>
        public void SetExternTrigger()
        {
            if (TriggerModel == TriggerModelEnum.ExternTrigger)
                return;

            TriggerModel = TriggerModelEnum.ExternTrigger;
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Line1);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Line1);
                    }
                }

                //Sets the trigger delay time in microseconds.
                camera.Parameters[PLCamera.TriggerDelayAbs].SetValue(5);        // 设置触发延时

                //Sets the absolute value of the selected line debouncer time in microseconds
                camera.Parameters[PLCamera.LineSelector].TrySetValue(PLCamera.LineSelector.Line1);
                camera.Parameters[PLCamera.LineMode].TrySetValue(PLCamera.LineMode.Input);
                camera.Parameters[PLCamera.LineDebouncerTimeAbs].SetValue(5);       // 设置去抖延时，过滤触发信号干扰

            }
            else // For SFNC 2.0 cameras, e.g. USB3 Vision cameras
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);

                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Line1);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Line1);
                    }
                }

                //Sets the trigger delay time in microseconds.//float
                camera.Parameters[PLCamera.TriggerDelay].SetValue(5);       // 设置触发延时

                //Sets the absolute value of the selected line debouncer time in microseconds
                camera.Parameters[PLCamera.LineSelector].TrySetValue(PLCamera.LineSelector.Line1);
                camera.Parameters[PLCamera.LineMode].TrySetValue(PLCamera.LineMode.Input);
                camera.Parameters[PLCamera.LineDebouncerTime].SetValue(5);       // 设置去抖延时，过滤触发信号干扰

            }
            //stopWatch.Reset();
            //stopWatch.Start();// ****  重启采集时间计时器   ****

        }
        /****************************************************/

       public  HObject Image = null;
        // 相机取像回调函数.
        private void OnImageGrabbed(Object sender, ImageGrabbedEventArgs e)
        {
            try
            {
                stopWatch.Restart();
                // Acquire the image from the camera. Only show the latest image. The camera may acquire images faster than the images can be displayed.

                // Get the grab result.
                IGrabResult grabResult = e.GrabResult;

                // Check if the image can be displayed.
                if (grabResult.GrabSucceeded)
                {
                    ImageType imageType = ImageType.ImageGrey;
                        //stopWatch.Restart();
                        //判断是否是黑白图片格式
                        if (grabResult.PixelTypeValue == PixelType.Mono8)
                        {
                            this.Image = BuffersToImageGray(grabResult);
                        }
                        else if (grabResult.PixelTypeValue == PixelType.BayerBG8 || grabResult.PixelTypeValue == PixelType.BayerGB8
                                    || grabResult.PixelTypeValue == PixelType.BayerRG8 || grabResult.PixelTypeValue == PixelType.BayerGR8)
                        {
                            imageType = ImageType.ImageColor;
                            this.Image = BuffersToImage24PlanarColor(grabResult);
                        }
                  

                    if (GrabModel == GrabModelEnum.GrabOne)
                        GrabModel = GrabModelEnum.StopGrabbing;


                    if(processImageEvent!=null)
                        // 抛出图像处理事件
                        processImageEvent(this.Image, imageType);
                }
            }

            catch (Exception ex)
            {
                this.Image = null;
            }
            finally
            {
                // Dispose the grab result if needed for returning it to the grab loop.
                e.DisposeGrabResultIfClone();
                grabTime = stopWatch.ElapsedMilliseconds;
                if(eventComputeGrabTime!=null)
                    eventComputeGrabTime(grabTime);
            }
        }

        public HObject BuffersToImageGray(IGrabResult grabResult)
        {
            
                //byte[] Newbuffer  =
                byte[] buffer = grabResult.PixelData as byte[];
                HObject Hobj;
                HOperatorSet.GenEmptyObj(out Hobj);
                Bitmap bitmap = new Bitmap(grabResult.Width, grabResult.Height, PixelFormat.Format8bppIndexed);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                try
                {
                    IntPtr ptrBmp = bmpData.Scan0;

                    if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        ColorPalette colorPalette = bitmap.Palette;
                        for (int i = 0; i < 256; i++)
                        {
                            colorPalette.Entries[i] = Color.FromArgb(i, i, i);
                        }
                        bitmap.Palette = colorPalette;
                    }

                    int imageStride = grabResult.Width;

                    if (imageStride == bmpData.Stride)
                    {
                        Marshal.Copy(buffer, 0, ptrBmp, bmpData.Stride * bitmap.Height);
                    }
                    else
                    {
                        for (int i = 0; i < bitmap.Height; ++i)
                        {
                            Marshal.Copy(buffer, i * imageStride, new IntPtr(ptrBmp.ToInt64() + i * bmpData.Stride), grabResult.Width);
                        }
                    }
                int width = grabResult.Width;
                int height = grabResult.Height;
                
                unsafe
                {
                    int count = height * width;
                    byte[] data = new byte[count];
                    byte* bptr = (byte*)bmpData.Scan0;
                    fixed (byte* pData = data)
                    {
                        for (int i = 0; i < height; i++)
                            for (int j = 0; j < width; j++)
                            {
                                data[i * width + j] = bptr[i * imageStride + j];
                            }
                        HOperatorSet.GenImage1(out Hobj, "byte", width, height, new IntPtr(pData));
                    }
                }

                }
                catch (Exception ex)
                { }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }

                return Hobj;
            
            //    int Width = grabResult.Width;
            //    int Height = grabResult.Height;
            //    int imageStride = grabResult.Width;
            //   // IntPtr DataPtr =  Marshal.AllocHGlobal(Width * Height);
            //    GCHandle hObject = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            //    try
            //    {               
            //        IntPtr DataPtr = hObject.AddrOfPinnedObject();


            //        CogImage8Grey GreyImage = new CogImage8Grey();
            //        CogImage8Root GreyImageRoot = new CogImage8Root();
            //        //  Marshal.Copy(buffer, 0, DataPtr, Width * Height);
            //        GreyImage.Allocate(Width, Height);
               
            //        GreyImageRoot.Initialize(Width, Height, DataPtr, Width, null);
            //        GreyImage.SetRoot(GreyImageRoot);
            //        GC.Collect();
            //        return GreyImage;
            //    }
            //    finally
            //    {
            //        if (hObject.IsAllocated)
            //            hObject.Free();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    return null;
            //}
           
        }

        public HObject BuffersToImage24PlanarColor(IGrabResult grabResult)
        {
            HObject Hobj;
            HOperatorSet.GenEmptyObj(out Hobj);
            try
            {
                int imageWidth = grabResult.Width - 1;
                int imageHeight = grabResult.Height - 1;
                int payloadSize = imageWidth * imageHeight;
                PixelDataConverter converter = new PixelDataConverter();
                //// 设置最后一行和一列不进行Bayer转换
                converter.Parameters[PLPixelDataConverter.InconvertibleEdgeHandling].SetValue("Clip");
                converter.OutputPixelFormat = PixelType.BGR8packed;
                byte[] buffer = new byte[payloadSize * 3];
                converter.Convert(buffer, grabResult);
                Bitmap bitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format24bppRgb);
                BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);
                try
                {
                    IntPtr ptrBmp = bmpData.Scan0;
                    int imageStride = imageWidth * 3;
                    if (imageStride == bmpData.Stride)
                        Marshal.Copy(buffer, 0, ptrBmp, bmpData.Stride * bitmap.Height);
                    else
                    {
                        /* The widths in bytes are not equal, copy line by line. 
                        * This can happen if the image width is not divisible by four.*/
                        for (int i = 0; i < bitmap.Height; ++i)
                        {
                            Marshal.Copy(buffer, i * imageStride, new IntPtr(ptrBmp.ToInt64() + i * bmpData.Stride), imageWidth * 3);
                        }
                    }
                    int width = grabResult.Width;
                    int height = grabResult.Height;
                    unsafe
                  {
                    int count = height * width;
                        ////计算3通道图像指针
                        byte[] r = new byte[count];
                        byte[] g = new byte[count];
                        byte[] b = new byte[count];
                        byte* r1 = null;
                        byte* g1 = null;
                        byte* b1 = null;
                        Marshal.Copy((IntPtr)r1, r, 0, r.Length);
                        Marshal.Copy((IntPtr)g1, g, 0, r.Length);
                        Marshal.Copy((IntPtr)b1, b, 0, r.Length);
                        byte* r_c1 = r1;
                        byte* g_c1 = g1;
                        byte* b_c1 = b1;
                        byte* p1 = (byte*)bmpData.Scan0;
                        //B G R A ->R G B
                        for (int i = height - 1; i >= 0; i--)
                        {
                            for (int j = 0; j < width; j = j + 4)
                            {
                                //R channel
                                *r_c1 = p1[i * width + (j + 2)];
                                r_c1++;
                                *g_c1 = p1[i * width + (j + 1)];
                                ++g_c1;
                                *b_c1 = p1[i * width + (j + 0)];
                                ++b_c1;
                            }
                        }
                        HOperatorSet.GenImage3(out Hobj, "byte", width, height, new IntPtr(r1), new IntPtr(g1), new IntPtr(b1));
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmpData);
                }
                return Hobj;
            }
            catch (Exception ex)
            {
                return null;
            }

        }
    }
}

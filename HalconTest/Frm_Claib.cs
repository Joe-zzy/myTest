using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using HalconDotNet;

//using FinsTcp;
//using Cognex.VisionPro;

namespace HalconTest
{
    public partial class Frm_Claib : Form
    {
        //public delegate void CheckPower();//委托
        private bool bIsCalibRunning = false;  //标定启动
        private int calibStep = -1;            //标定序号
        private Thread calibThread = null;     //九点标定线程
        private Message_Show mess = new Message_Show();
        private Error er = new Error();

        private bool bIsRotationRunning = false;  //旋转中心运算启动
        private int rotationStep = -1;            //旋转中心运算序号
        private Thread rotationThread = null;     //旋转中心运算线程

        private HObject image = new HObject();
        private string path = Application.StartupPath ;

        public Frm_Claib()
        {
            InitializeComponent();
        }
        //标定数据加载
        private void Frm_Claib_Load(object sender, EventArgs e)
        {
            dgv_NinePoint.Rows.Clear(); //清除之前可能留存的点位数据
            dgv_Rotate.Rows.Clear();  //清除之前可能留存的点位数据
            switch (RunParams._StationNO) //根据工位信息加载并显示数据
            {
                case 0:   //工位1 标定数据
                    this.Text = "工位1";
                    ShowCalibData(RunParams.Station1);
                    ShowRotateData(RunParams.Station1);
                    break;
                case 1:  //工位2  标定数据
                    this.Text = "工位2";
                    ShowCalibData(RunParams.Station2);
                    ShowRotateData(RunParams.Station2);
                    break;
            }
            ShowSystemSetData();
            if (RunParams._StationNO == 1)
            {
                //if (RunParams.RobotConnect.TcpServer.IsRunning)
                //{
                //    RunParams.RobotConnect.TCPReceived += new TCPCommunicate(RobotConnect_TCPReceived);
                //}
            }
            nud_RotateNum.Value = 6;
        }

        void RobotConnect_TCPReceived(int type, string message) //接收TCP消息
        {
            switch (type)
            {
                case -1: //机器人断开连接
                    ShowMessage(message, true);
                    break;
                case 0: //机器人连接
                    ShowMessage(message, true);
                    break;
                case 1: //接受机器人发送消息
                    ShowMessage("接收到：" + message, true);
                    string[] ss = message.Split('#');
                    if (ss[0] == "CALIB")//预设自动标定打开界面
                    {
                        int pointNum = Convert.ToInt32(ss[1]);
                        double robotMX = Convert.ToDouble(ss[2]);
                        double robotMY = Convert.ToDouble(ss[3]);
                        //Station2_AutoNineCalib(pointNum, robotMX, robotMY);
                    }
                    else //消息格式预留
                    {

                    }
                    break;
                default:

                    break;
            }
        }

        public void ShowSystemSetData()
        {
            switch (RunParams._StationNO)
            {
                case 0:
                //    nud_CalibMoveX.Value = (decimal)RunParams._StationData.state1CalibMoveX;
                //    nud_CalibMoveY.Value = (decimal)RunParams._StationData.state1CalibMoveY;
                //    nud_RotateAngleRang.Value = (decimal)RunParams._StationData.state1CalibMoveR;
                    break;
                case 1:
                //    nud_CalibMoveX.Value = (decimal)RunParams._StationData.state2CalibMoveX;
                //    nud_CalibMoveY.Value = (decimal)RunParams._StationData.state2CalibMoveY;
                //    nud_RotateAngleRang.Value = (decimal)RunParams._StationData.state2CalibMoveR;
                    break;
            }
        }

        #region  加载方法
        //显示九点标定数据
        public void ShowCalibData(paramCalibData station)
        {
            //得到点位数据个数
            int pointNum = 0;
          //  int pointNum = station.calib.Calibration.NumPoints;
            if (pointNum == 0) //若工具内无点位，则提示无点位
            {
                ShowMessage("无标定数据", false);
                return;
            }
            //显示
            for (int i = 0; i < pointNum; i++)
            {
                Error er = new Error();
                AddDataToCalibDataView(station.calibPictrueX[i], station.calibPictrueY[i], station.calibMachineX[i], station.calibMachineY[i], out er);
                ShowMessage(er.errorInfo, er.flag);
            }
            nud_PictrueX.Value = (decimal)station.calibPictrueX[pointNum - 1];
            nud_PictrueY.Value = (decimal)station.calibPictrueY[pointNum - 1];
            nud_MachineX.Value = (decimal)station.calibMachineX[pointNum - 1];
            nud_MachineY.Value = (decimal)station.calibMachineY[pointNum - 1];

            txb_CalibRMS.Text = station.calibRMS.ToString("0.000");

        }
        //更新九点标定数据
        public void UpCalibData(paramCalibData station)
        {
            int pointNum = dgv_NinePoint.Rows.Count;
          //  station.calib.Calibration.NumPoints = pointNum;

            station.calibPictrueX = new double[pointNum];
            station.calibPictrueY = new double[pointNum];
            station.calibMachineX = new double[pointNum];
            station.calibMachineY = new double[pointNum];
            //获取
            for (int i = 0; i < pointNum; i++)
            {
                station.calibPictrueX[i] = Convert.ToDouble(dgv_NinePoint.Rows[i].Cells[0].Value.ToString());
                station.calibPictrueY[i] = Convert.ToDouble(dgv_NinePoint.Rows[i].Cells[1].Value.ToString());
                station.calibMachineX[i] = Convert.ToDouble(dgv_NinePoint.Rows[i].Cells[2].Value.ToString());
                station.calibMachineY[i] = Convert.ToDouble(dgv_NinePoint.Rows[i].Cells[3].Value.ToString());
            }
        }

        //显示旋转中心标定数据
        public void ShowRotateData(paramCalibData station)
        {
            //得到点位数据个数
            int pointNum = 0;
         //   int pointNum = station.rotateCenter.RunParams.NumPoints;
            if (pointNum == 0)
            {
                ShowMessage("无拟合圆数据", false);
                return;
            }
            //显示
            for (int i = 0; i < pointNum; i++)
            {
                Error er = new Error();
                AddDataToRotateDataView(station.rotatePictrueX[i], station.rotatePictrueY[i], out er);
                ShowMessage(er.errorInfo, er.flag);
            }
            nud_RotatePictrueX.Value = (decimal)station.rotatePictrueX[pointNum - 1];
            nud_RotatePictrueY.Value = (decimal)station.rotatePictrueY[pointNum - 1];
            nud_RotateCenterX.Value = (decimal)station.rotateCenterMachineX;
            nud_RotateCenterY.Value = (decimal)station.rotateCenterMachineY;

            txb_RatateRMS.Text = station.rotateRMS.ToString("0.000");
        }
        //更新旋转中心标定数据
        public void UpRotateData(paramCalibData station)
        {
            int pointNum = dgv_Rotate.Rows.Count;
          //  station.rotateCenter.RunParams.NumPoints = pointNum;

            station.rotatePictrueX = new double[pointNum];
            station.rotatePictrueY = new double[pointNum];
            //将参数值导入到拟合圆工具中
            for (int i = 0; i < pointNum; i++)
            {
                station.rotatePictrueX[i] = double.Parse(dgv_Rotate.Rows[i].Cells[0].Value.ToString());
                station.rotatePictrueY[i] = double.Parse(dgv_Rotate.Rows[i].Cells[1].Value.ToString());

               // station.rotateCenter.RunParams.SetX(i, station.rotatePictrueX[i]);
              //  station.rotateCenter.RunParams.SetY(i, station.rotatePictrueY[i]);
            }
        }

        //信息显示和日志记录
        private void ShowMessage(string message, bool wrong)
        {
            //if (RunParams._SystemSet.saveLog)//保存日志
            //{
            //    mess.FlushMessage("标定", 0.000, 0.000, 0.000, message);
            //    switch (RunParams._StationNO)
            //    {
            //        case 0:
            //            mess.type = "工位1标定";
            //            break;
            //        case 1:
            //            mess.type = "工位2标定";
            //            break;
            //    }
            //    SysLog.Message = mess;
            //}
            this.BeginInvoke((MethodInvoker)delegate() //显示消息
            {
                this.listView1.BeginUpdate();

                ListViewItem lvi = new ListViewItem();
                if (wrong)
                {
                    lvi.BackColor = Color.White;
                }
                else
                {
                    lvi.BackColor = Color.Yellow;
                }
                lvi.Text = DateTime.Now.ToString("HH:mm:ss");

                lvi.SubItems.Add(message);
                this.listView1.Items.Add(lvi);
                this.listView1.Items[listView1.Items.Count - 1].EnsureVisible();
                this.listView1.EndUpdate();
            });
        }

        //向九点标定的数据显示DataView控件添加数据
        private void AddDataToCalibDataView(double px, double py, double mx, double my, out Error error)
        {
            //error = new Error();
            try
            {
                int index = dgv_NinePoint.Rows.Add();
                dgv_NinePoint.Rows[index].Cells[0].Value = px.ToString("0.000");
                dgv_NinePoint.Rows[index].Cells[1].Value = py.ToString("0.000");
                dgv_NinePoint.Rows[index].Cells[2].Value = mx.ToString("0.000");
                dgv_NinePoint.Rows[index].Cells[3].Value = my.ToString("0.000");
                error.flag = true;
                error.errorInfo = "点位" + (index + 1).ToString() + "添加完成；";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }

        //自动向九点标定的数据显示DataView控件添加数据
        private void AutoAddDataToCalibDataView(double px, double py, double mx, double my, out Error error)
        {
            int index = 0;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate()
                {
                    index = dgv_NinePoint.Rows.Add();
                    dgv_NinePoint.Rows[index].Cells[0].Value = px.ToString("0.000");
                    dgv_NinePoint.Rows[index].Cells[1].Value = py.ToString("0.000");
                    dgv_NinePoint.Rows[index].Cells[2].Value = mx.ToString("0.000");
                    dgv_NinePoint.Rows[index].Cells[3].Value = my.ToString("0.000");
                });
                error.flag = true;
                error.errorInfo = "点位" + (index + 1).ToString() + "添加完成；";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }
        //删除DataView最后一行数据或删除选中行
        private void DeleteDataFromCalibDataView()
        {
            int m_Row = dgv_NinePoint.Rows.Count;
            var m_Data = dgv_NinePoint.CurrentRow;

            if ((m_Data.Index >= m_Row - 1) && m_Row <= 1)
            {
                return;
            }
            dgv_NinePoint.Rows.Remove(m_Data);
        }

        //向旋转中心标定的数据显示DataView控件添加数据
        private void AddDataToRotateDataView(double px, double py, out Error error)
        {
            try
            {
                int index = dgv_Rotate.Rows.Add();
                dgv_Rotate.Rows[index].Cells[0].Value = px.ToString("0.000");
                dgv_Rotate.Rows[index].Cells[1].Value = py.ToString("0.000");
                error.flag = true;
                error.errorInfo = "点位" + (index + 1).ToString() + "添加完成；";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }
        //自动添加旋转中心数据到列表中
        private void AutoAddDataToRotateDataView(double px, double py, out Error error)
        {
            int index = 0;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate()
                {
                    index = dgv_Rotate.Rows.Add();
                    dgv_Rotate.Rows[index].Cells[0].Value = px.ToString("0.000");
                    dgv_Rotate.Rows[index].Cells[1].Value = py.ToString("0.000");
                });
                error.flag = true;
                error.errorInfo = "点位" + (index + 1).ToString() + "添加完成；";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }

        //删除DataView最后一行数据
        private void DeleteDataFromRotateDataView()
        {
            int m_Row = dgv_Rotate.Rows.Count;
            var m_Data = dgv_Rotate.CurrentRow;

            if ((m_Data.Index >= m_Row - 1) && m_Row <= 1)
            {
                return;
            }
            dgv_Rotate.Rows.Remove(m_Data);
        }
        #endregion
/*
        #region 标志点获取方法
        //获取图像
        public void GetImage(visionproParm station, ref CogImage8Grey img, out Error error)
        {
            if (RunParams._StationNO == 0)
            {
                //判断是否启用畸变矫正工具
                if (RunParams._StationData.state1_UseChecker)
                {
                    station.GetImage(ref img, out error);
                    if (error.flag)
                    {
                        station.CheckerImage(cogRecordDisplay1, ref img, out error);
                    }
                }
                else
                {
                    station.GetImage(cogRecordDisplay1, ref img, out error);
                }
            }
            else
            {
                if (RunParams._StationData.state2_UseChecker)
                {
                    station.GetImage(ref img, out error);
                    if (error.flag)
                    {
                        station.CheckerImage(cogRecordDisplay1, ref img, out error);
                    }
                }
                else
                {
                    station.GetImage(cogRecordDisplay1, ref img, out error);
                }
            }
            ShowMessage(error.errorInfo, error.flag);
        }

        //获取标志点方法
        public void GetMark(CogImage8Grey img, visionproParm station, out double px, out double py, out Error error)
        {
            px = 0;
            py = 0;
            string[] result;

            station.GetMark(img, cogRecordDisplay1, out result, out error); //获取标志点点位坐标
            ShowMessage(error.errorInfo, error.flag); //显示
            if (error.flag)
            {
                px = Convert.ToDouble(result[0]);
                py = Convert.ToDouble(result[1]);
            }
        }

        //运行标定方法
        public void CreatHommat(visionproParm station, int pointNum, out Error error)
        {
            station.CreatCalib(image, station.calibPictrueX, station.calibPictrueY, station.calibMachineX, station.calibMachineY, pointNum, out error); //生成标定矩阵
            ShowMessage(error.errorInfo, error.flag);
            //输出处理结果
            if (error.flag) //运行成功，则保存工具
            {
                station.SaveCalibTool(RunParams._SystemSet.nowCellType, out error);
                ShowMessage(error.errorInfo, error.flag);
            }

        }

        //转换方法
        public void PictrueTransToMachine(visionproParm station, double px, double py, out double mx, out double my, out Error error)
        {
            mx = 0;
            my = 0;
            station.Trans(px, py, out mx, out my, out error);
            ShowMessage(error.errorInfo, error.flag);
        }


        //拟合圆方法 获取旋转中心的机械坐标
        public void FitCircle(paramCalibData station, HObject img, out double centerX, out double centerY, out Error error)
        {
            centerX = 0;
            centerY = 0;

            station.RotateCenter(img, station.rotatePictrueX, station.rotatePictrueY, out error); //使用图像坐标拟合圆
            ShowMessage(error.errorInfo, error.flag);
            if (error.flag) //判断转换是否成功
            {
                //显示运行结果
                cogRecordDisplay1.StaticGraphics.Clear();
                cogRecordDisplay1.InteractiveGraphics.Clear();
                cogRecordDisplay1.Image = image;
                cogRecordDisplay1.Record = station.rotateCenter.CreateLastRunRecord();
                cogRecordDisplay1.Fit();

                station.Trans(station.rotateCenterX, station.rotateCenterY, out centerX, out centerY, out error); //图像坐标圆心转换为机械坐标圆心
                ShowMessage(error.errorInfo, error.flag);
            }
        }

        #endregion


        #region  标定控件功能
        //生成标定文件
        private void AutoCalibExecute(visionproParm state, out double calibRms)
        {
            calibRms = 0;
            Error er = new Error();

            try
            {
                int pointNum = dgv_NinePoint.Rows.Count; //获取点位个数
                if (cogRecordDisplay1.LiveDisplayRunning) //正在进行实时采集 则关闭
                {
                    cogRecordDisplay1.StopLiveDisplay();
                }
                UpCalibData(state); //根据工位1点位个数和信息
                CreatHommat(state, pointNum, out er); //生成标定工具开始标定
                ShowMessage(er.errorInfo, er.flag); //运行结果显示
                if (er.flag)
                {
                    calibRms = state.calibRMS;
                }
            }
            catch (Exception ex)
            {
                er.flag = false;
                er.errorInfo = ex.Message;
                calibRms = 0;
            }
        }
        //手动标定按钮
        private void btn_ManulCalib_Click(object sender, EventArgs e)
        {
            double calibRms = 0;
            switch (RunParams._StationNO) //根据工位编号信息进行生成标定工具信息
            {
                case 0:  //工位1
                    AutoCalibExecute(RunParams.Station1, out calibRms);
                    break;
                case 1:  //工位2
                    AutoCalibExecute(RunParams.Station2, out calibRms);
                    break;
            }
            txb_CalibRMS.Text = calibRms.ToString("0.000");
        }
        //自动九点标定图像处理
        private void stateCalibProcessing(visionproParm state, out double pixelX, out double pixelY, out Error er)
        {
            try
            {
                pixelX = 0;
                pixelY = 0;
                if (cogRecordDisplay1.LiveDisplayRunning) //正在进行实时采集 则关闭
                {
                    cogRecordDisplay1.StopLiveDisplay();
                }
                GetImage(state, ref image, out er);
                ShowMessage(er.errorInfo, er.flag); //运行结果显示
                if (er.flag)
                {
                    //获取标志点
                    GetMark(image, state, out pixelX, out pixelY, out er);
                    ShowMessage(er.errorInfo, er.flag); //运行结果显示
                }
            }
            catch (Exception ex)
            {
                pixelX = 0;
                pixelY = 0;
                ShowMessage(ex.Message, false); //运行结果显示
                er.flag = false;
                er.errorInfo = ex.Message;
            };
        }
        //运行标志点获取工具手动获得一个标志点
        private void btn_NinePointTest_Click(object sender, EventArgs e)
        {
            try
            {
                Error er = new Error();
                double px = 0, py = 0;
                switch (RunParams._StationNO) //根据工位编号信息进行生成标定工具信息
                {
                    case 0:  //工位1
                        stateCalibProcessing(RunParams.Station1, out px, out py, out er);
                        break;
                    case 1:  //工位2
                        stateCalibProcessing(RunParams.Station2, out px, out py, out er);
                        break;
                }
                // 显示测试值
                nud_PictrueX.Value = (decimal)px;
                nud_PictrueY.Value = (decimal)py;
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, false); //运行结果显示
                MessageBox.Show("测试失败！");
            }
        }

        #region 自动标定
        private bool bIsYetAutoCalib = false;
        private void btn_NinePointAutoCalib_Click(object sender, EventArgs e)
        {
            if (!bIsYetAutoCalib)
            {
                if (MessageBox.Show("是否开始自动标定？", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    if (btn_AutoCalib.Text.Trim() == "自动标定")
                    {
                        
                        dgv_NinePoint.Rows.Clear();
                        btn_AutoCalib.Text = "停止标定";
                        calibThread = new Thread(state1Calib);
                        calibThread.IsBackground = true;
                        calibThread.Start();
                        calibStep = 0;
                        bIsCalibRunning = true;
                        bIsYetAutoCalib = true;
                        dgv_NinePoint.Rows.Clear();
                    }
                }
            }
            else
            {
                if (btn_AutoCalib.Text.Trim() == "停止标定")
                {
                    btn_AutoCalib.Text = "自动标定";
                    calibThread.Abort();
                    calibThread.Join();
                    calibStep = -1;
                    bIsCalibRunning = false;
                    bIsYetAutoCalib = false;
                    Error er = new Error();
                    RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 0, out er);//给PLC走位信号
                    //RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 0, out er);    //测试完成置位
                }
            }
        }

        //十字平台工位九点标定函数
        private void state1Calib()
        {
            float machineX = 0; //机械需走到的位置X
            float machineY = 0; //机械需走到的位置Y
            float currentX = 0; //机械当前坐标X
            float currentY = 0; //机械当前坐标Y
            double pixX = 0;  //标志点图像坐标X
            double pixY = 0;  //标志点图像坐标Y
            Error er = new Error();
            bool startTestFlag = false;
            double calibRms = 0;//标定误差系数
            short outData = 0;
            short i = 1,readWord=0;
            int m = 1, mX, mY;
            string ReadStr=null;

            RunParams._StationData.state1CalibMoveX = (float)nud_CalibMoveX.Value;
            RunParams._StationData.state1CalibMoveY = (float)nud_CalibMoveY.Value;
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 11, out er);//给PLC走位信号
            while (true) //循环检测是否可以开始进行检测
            {
                RunParams.PLCConnect.ReadWord(PlcMemory.DM, 2404, out readWord, out er); //接收到PLC到位信号
                if (er.flag)
                {
                    if (readWord == 12) //若接收到拍照信号，则跳出当前循环
                    {
                        break;
                    }
                }
                else //读取失败，退出当前线程
                {
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", false, out er);   //开始标定信号复位
                    MessageBox.Show("PLC读取失败");
                    return;
                }
            }
            //走九点循环
            for (int x = 0; x < 3; x++)  //X方向循环
            {
                mX = x - m;
                machineX = x*RunParams._StationData.state1CalibMoveX;// -mX * RunParams._StationData.state1CalibMoveX; //获取X坐标//与机械手初始位有关！
                for (int y = 0; y < 3; y++)  //Y方向循环
                {
                    mY = y - m;
                    machineY = RunParams._StationData.state1CalibMoveY - mY * RunParams._StationData.state1CalibMoveY; //获取Y坐标
                    //发送需要走到的坐标数据到PLC
                    RunParams.PLCConnect.WriteData(PlcMemory.DM, 2732, machineX, out er);  //机械走位坐标X
                    RunParams.PLCConnect.WriteData(PlcMemory.DM, 2736, machineY, out er);  //机械走位坐标Y
                    RunParams.PLCConnect.WriteData(PlcMemory.DM, 2740, 0, out er); //发送旋转到的角度值

                    RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2730, i, out er);//给PLC走位信号
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.02", false, out er);    //测试完成信号复位
                    if (x == 0 && y == 0) //若为第一次则发送开始进行九点标定信号
                    {
                        //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", true, out er);   //开始进行标定
                        //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.04", false, out er);    //标定完成复位
                    }

                    while (true) //循环检测是否可以开始进行检测
                    {
                        RunParams.PLCConnect.ReadWord(PlcMemory.DM, 2730, out outData, out er); //接收到PLC到位信号
                        if (er.flag)
                        {
                            if (outData == 52) //若接收到拍照信号，则跳出当前循环
                            {
                                break;
                            }
                        }
                        else //读取失败，退出当前线程
                        {
                            //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", false, out er);   //开始标定信号复位
                            MessageBox.Show("PLC读取失败");
                            return;
                        }
                        Thread.Sleep(40); //休眠
                    }

                    Thread.Sleep(100);
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.01", false, out er);  //拍照信号复位
                    RunParams.PLCConnect.ReadString(PlcMemory.DM, 2744, 30, out ReadStr, out er);
                    if (er.flag)
                    {
                        //SplitWord(ReadStr, currentX, currentY);
                        SplitWord(ReadStr, out  currentX, out  currentY);
                    }

                    stateCalibProcessing(RunParams.Station1, out pixX, out pixY, out er);
                    string[] ms = { "X:" + pixX, "Y :" + pixY };
                    if (er.flag)
                    {
                        RunParams.Station1.DispMessage(ms, RunParams._StationData.state1_DispX, RunParams._StationData.state1_DispY,
                           RunParams._StationData.state1_Size, CogColorConstants.Green, cogRecordDisplay1, out er);
                    }
                    else
                    {
                        RunParams.Station1.DispMessage(ms, RunParams._StationData.state1_DispX, RunParams._StationData.state1_DispY,
                           RunParams._StationData.state1_Size, CogColorConstants.Red, cogRecordDisplay1, out er);
                    }
                    AutoAddDataToCalibDataView(pixX, pixY, (double)currentX, (double)currentY, out er);  //加入列表
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.02", true, out er);   //测试完成

                    Thread.Sleep(RunParams._StationData.state1_ResetTime); //休眠时间按正常运行时的复位时间计算
                    i += 1;
                }
            }
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2730, 10, out er);   //测试完成X`
            //九点走完，开始生成仿射矩阵
            //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", false, out er);  //标定开始复位
            //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.04", true, out er);  //标定完成置位
            //AutoCalibExecute
            AutoCalibExecute(RunParams.Station1, out calibRms); //生成标定文件
            this.BeginInvoke((MethodInvoker)delegate() //显示错误信息
            {
                txb_CalibRMS.Text = calibRms.ToString("0.000");
            });
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 0, out er);//给PLC走位信号
            MessageBox.Show("自动标定完成！请点击停止标定！");
        }
        //分割字符
        public void SplitWord(string str, out float robX, out float robY)
        {
            robX = robY = 0;
            try
            {
                char[] separator = { ';' };
                string[] str1 = str.Split(separator);
                robX = Convert.ToSingle(str1[2]);
                robY = Convert.ToSingle(str1[3]);
            }
            catch (System.Exception ex)
            {
                ShowMessage(ex.Message,false);
            }
            
        }
        //十字平台工位旋转中心标定函数
        private void state1Rotate()
        {
            Error er = new Error();
            double currentPixX = 0;  //当前图像坐标X
            double currentPixY = 0;  //当前图像坐标Y
            double rotateCenterX = 0; //旋转中心机械坐标X
            double rorateCenterY = 0;  //旋转中心机械坐标Y
            double RMS = 0;  //偏差值
            bool StartTestFlag = false;
            int testNum = (int)nud_RotateNum.Value;
            double angle = 0;//角度值
            double angleRang = (double)nud_RotateAngleRang.Value;
            short j = 11;
            short outData = 0;
            short readWord = 0;
            //判断角度范围是否合适
            if (angleRang > 25 || angleRang < 0)
            {
                MessageBox.Show("设置的角度范围不合适！");
                return;
            }
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 11, out er);//给PLC走位信号
            while (true) //循环检测是否可以开始进行检测
            {
                RunParams.PLCConnect.ReadWord(PlcMemory.DM, 2404, out readWord, out er); //接收到PLC到位信号
                if (er.flag)
                {
                    if (readWord == 12) //若接收到拍照信号，则跳出当前循环
                    {
                        break;
                    }
                }
                else //读取失败，退出当前线程
                {
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", false, out er);   //开始标定信号复位
                    MessageBox.Show("PLC读取失败");
                    return;
                }
            }
            //循环测试
            for (int i = 0; i < testNum; i++)
            {
                //angle = (-1 * angleRang / 2) + (i / (testNum - 1)) * angleRang;
                angle = angleRang*(i-testNum / 2);
                RunParams.PLCConnect.WriteData(PlcMemory.DM, 2740, (float)angle, out er); //发送旋转到的角度值
                if (!er.flag)
                {
                    MessageBox.Show("PLC连接失败！");
                    return;
                }
                RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2730, j, out er);//给PLC走位信号
                //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.02", false, out er);     //测试完成复位
                if (i == 0) //发送开始进行旋转标定信号
                {
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.04", false, out er);     //旋转中心标定结束复位
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.00", true, out er);      //旋转中心标定开始置位
                }
                //等待接受运动到位信号
                while (true)
                {
                    RunParams.PLCConnect.ReadWord(PlcMemory.DM, 2730, out outData, out er); //接收到PLC到位信号
                    if (er.flag)
                    {
                        if (outData == 52) //若接收到拍照信号，则跳出当前循环
                        {
                            break;
                        }
                    }
                    else //读取失败，退出当前线程
                    {
                        //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "1.00", false, out er);   //开始标定信号复位
                        MessageBox.Show("PLC读取失败");
                        return;
                    }
                    Thread.Sleep(40); //休眠
                }
                //测试信号复位
                //RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2730, j, out er);   //测试完成X`
                //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.01", false, out er);
                //测试
                stateCalibProcessing(RunParams.Station1, out currentPixX, out currentPixY, out er);
                AutoAddDataToRotateDataView(currentPixX, currentPixY, out er);       //加入列表
                //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.02", true, out er);  //测试完成
                j += 1;
                Thread.Sleep(RunParams._StationData.state1_ResetTime); //休眠时间按正常运行时的复位时间计算
            }
            //点位运行完成
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2730, 20, out er);   //测试完成X`
            //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.00", false, out er);      //标定开始复位
            //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.04", true, out er);      //流程结束置位
            AutoCalibRotationExecute(RunParams.Station1, out rotateCenterX, out rorateCenterY, out RMS);
            this.BeginInvoke((MethodInvoker)delegate() //显示运行结果
            {
                nud_RotateCenterX.Value = (decimal)rotateCenterX;
                nud_RotateCenterY.Value = (decimal)rorateCenterY;
                txb_CalibRMS.Text = RMS.ToString("0.000");
            });
            RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 0, out er);//给PLC走位信号
            MessageBox.Show("自动拟合圆标定完成！请点击停止拟合！");
        }
        //机械手自动九点标定
        public void Station2_AutoNineCalib(int PointNum, double mx, double my)
        {
            double px = 0, py = 0;
            double RMS = 0;
            string[] result = new string[1] { " " };
            Thread.Sleep(200); //等待200ms，等待机械手停稳
            Error er = new Error();
            //取图+图像处理

            stateCalibProcessing(RunParams.Station2, out px, out py, out er);

            string[] ms = { "X:" + px, "Y :" + py };
            if (er.flag) //图像窗口显示
            {
                RunParams.Station1.DispMessage(ms, RunParams._StationData.state2_DispX, RunParams._StationData.state2_DispY,
                   RunParams._StationData.state2_Size, CogColorConstants.Green, cogRecordDisplay1, out er);
            }
            else
            {
                RunParams.Station1.DispMessage(ms, RunParams._StationData.state1_DispX, RunParams._StationData.state1_DispY,
                   RunParams._StationData.state1_Size, CogColorConstants.Red, cogRecordDisplay1, out er);
            }
            AutoAddDataToCalibDataView(px, py, mx, my, out er);  //加入列表

            if (PointNum == 9) //赋值完成，开始生成标定文件
            {
                AutoCalibExecute(RunParams.Station2, out RMS); //生成标定文件
                this.BeginInvoke((MethodInvoker)delegate() //显示错误信息
                {
                    txb_CalibRMS.Text = RMS.ToString("0.000");
                });
            }
            //发送消息
            RunParams.RobotConnect.send("OK#", out er);
            ShowMessage(er.errorInfo + "OK#", er.flag);
        }




        #endregion
        //行头添加序号
        private void dgv_NinePoint_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
        {
            for (int i = 0; i < dgv_NinePoint.Rows.Count; i++)
            {
                DataGridViewRow r = this.dgv_NinePoint.Rows[i];
                r.HeaderCell.Value = (i + 1).ToString();
            }
        }
        //将最后一组点位信息删除
        private void btn_NinePointDelete_Click(object sender, EventArgs e)
        {
            DeleteDataFromCalibDataView();
        }
        //在最后添加一组点位信息
        private void btn_NinePointAdd_Click(object sender, EventArgs e)
        {
            Error er = new Error();
            //赋值
            double px = (double)nud_PictrueX.Value;
            double py = (double)nud_PictrueY.Value;
            double mx = (double)nud_MachineX.Value;
            double my = (double)nud_MachineY.Value;
            AddDataToCalibDataView(px, py, mx, my, out er); //添加
        }
        //编辑和查看标定工具
        private void btn_OpenCalibTool_Click(object sender, EventArgs e)
        {
            Frm_NinePointTool f_calib = new Frm_NinePointTool();
            switch (RunParams._StationNO)
            {
                case 0:
                    f_calib.cogCalibNPointToNPointEditV21.Subject = RunParams.Station1.calib;
                    break;
                case 1:
                    f_calib.cogCalibNPointToNPointEditV21.Subject = RunParams.Station2.calib;
                    break;
            }
            f_calib.ShowDialog();
        }
        //编辑和查看获取标志点工具
        private void btn_OpenGetMarkTool_Click(object sender, EventArgs e)
        {
            RunParams._IsGetMarkOrRun = 0;
            Frm_ToolBlock f_BlockTool = new Frm_ToolBlock();
            switch (RunParams._StationNO)
            {
                case 0:
                    f_BlockTool.CogToolBlockEditV21.Subject = RunParams.Station1.calibTool;
                    break;
                case 1:
                    f_BlockTool.CogToolBlockEditV21.Subject = RunParams.Station2.calibTool;
                    break;
            }
            f_BlockTool.ShowDialog();
        }

        #endregion

        #region 旋转中心标定控制功能
        //添加一组数据
        private void btn_RotateAdd_Click(object sender, EventArgs e)
        {
            Error er = new Error();
            double px = (double)nud_RotatePictrueX.Value;
            double py = (double)nud_RotatePictrueY.Value;
            AddDataToRotateDataView(px, py, out er); //添加
            //显示添加信息
            ShowMessage(er.errorInfo, er.flag);
        }
        //删除最后一组点位数据
        private void btn_RoatateDelete_Click(object sender, EventArgs e)
        {
            DeleteDataFromRotateDataView();
        }
        //编辑和查看拟合圆工具
        private void btn_RotateOpenTool_Click(object sender, EventArgs e)
        {
            Frm_ToolBlock f_BlockTool = new Frm_ToolBlock();
            RunParams._IsGetMarkOrRun = 2;
            switch (RunParams._StationNO)
            {
                case 0:
                    f_BlockTool.CogToolBlockEditV21.Subject = RunParams.Station1.rotateTool;
                    break;
                case 1:
                    f_BlockTool.CogToolBlockEditV21.Subject = RunParams.Station2.rotateTool;
                    break;
            }
            f_BlockTool.ShowDialog();
        }
        //手动运行标志点获取工具，获取一组标志点
        private void btn_RatateTest_Click(object sender, EventArgs e)
        {
            try
            {
                Error er = new Error();
                double px = 0, py = 0;
                switch (RunParams._StationNO) //根据工位编号信息进行生成标定工具信息
                {
                    case 0:  //工位1
                        stateCalibProcessing(RunParams.Station1, out px, out py, out er);
                        break;
                    case 1:  //工位2
                        stateCalibProcessing(RunParams.Station2, out px, out py, out er);
                        break;
                }
                // 显示测试值
                nud_RotatePictrueX.Value = (decimal)px;
                nud_RotatePictrueY.Value = (decimal)py;
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, false); //运行结果显示
                MessageBox.Show("测试失败！");
            }
        }
        //采集一张图像
        private void btn_RotateGetOneImage_Click(object sender, EventArgs e)
        {
            Error er = new Error();
            if (cogRecordDisplay1.LiveDisplayRunning) //正在进行实时采集 则关闭
            {
                cogRecordDisplay1.StopLiveDisplay();
            }
            switch (RunParams._StationNO)
            {
                case 0:
                    GetImage(RunParams.Station1, ref image, out er);
                    break;
                case 1:
                    GetImage(RunParams.Station2, ref image, out er);
                    break;
            }
        }
        //实时采集
        private void btn_RotateLiveShow_Click(object sender, EventArgs e)
        {
            if (cogRecordDisplay1.LiveDisplayRunning) //正在进行实时采集 则关闭
            {
                cogRecordDisplay1.StopLiveDisplay();
                ShowMessage("关闭实时取像", true);
            }
            else  //若未进行实时采集则打开
            {
                switch (RunParams._StationNO)
                {
                    case 0:
                        cogRecordDisplay1.StaticGraphics.Clear();
                        cogRecordDisplay1.InteractiveGraphics.Clear();
                        cogRecordDisplay1.StartLiveDisplay(RunParams.Station1.camera.Operator, false);
                        cogRecordDisplay1.Fit();
                        ShowMessage("工位1，正在实时取像", true);
                        break;
                    case 1:
                        cogRecordDisplay1.StaticGraphics.Clear();
                        cogRecordDisplay1.InteractiveGraphics.Clear();
                        cogRecordDisplay1.StartLiveDisplay(RunParams.Station2.camera.Operator, false);
                        cogRecordDisplay1.Fit();
                        ShowMessage("工位2，正在实时取像", true);
                        break;
                }
            }
        }
        //拟合圆，得到旋转中心
        private void btn_RotateFitCircle_Click(object sender, EventArgs e)
        {
            double centerX = 0;
            double centerY = 0;
            double rotRms = 0;
            switch (RunParams._StationNO) //根据工位编号信息进行生成标定工具信息
            {
                case 0:  //工位1
                    AutoCalibRotationExecute(RunParams.Station1, out centerX, out centerY, out rotRms);
                    break;
                case 1:  //工位2
                    AutoCalibRotationExecute(RunParams.Station2, out centerX, out centerY, out rotRms);
                    break;
            }
            nud_RotateCenterX.Value = (decimal)centerX;
            nud_RotateCenterY.Value = (decimal)centerY;
            txb_RatateRMS.Text = rotRms.ToString("0.000");
        }

        //自动拟合圆，得到旋转中心
        private void AutoCalibRotationExecute(paramCalibData state, out double cenX, out double cenY, out double rmsRot)
        {
            Error er = new Error();
            cenX = 0;
            cenY = 0;
            rmsRot = 0;
            double centerX = 0;
            double centerY = 0;
            try
            {
                int pointNum = dgv_NinePoint.Rows.Count; //获取点位个数
                //if (cogRecordDisplay1.LiveDisplayRunning) //正在进行实时采集 则关闭
                //{
                //    cogRecordDisplay1.StopLiveDisplay();
                //}

                UpRotateData(state); //根据工位1点位个数和信息
                FitCircle(state, image, out centerX, out centerY, out er);
                state.rotateCenterMachineX = centerX;
                state.rotateCenterMachineY = centerY;
                ShowMessage(er.errorInfo, er.flag); //运行结果显示
                if (er.flag)
                {
                    cenX = centerX;
                    cenY = centerY;
                    rmsRot = state.rotateRMS;
                    state.SaveRotateTool(RunParams._SystemSet.nowCellType, out er);
                }
            }
            catch (Exception ex)
            {
                ShowMessage(ex.Message, false); //运行结果显示
                cenX = 0;
                cenY = 0;
                rmsRot = 0;
            }
        }

        //在行头添加序号
        private void dgv_Rotate_RowStateChanged(object sender, DataGridViewRowStateChangedEventArgs e)
        {
            for (int i = 0; i < dgv_Rotate.Rows.Count; i++)
            {
                DataGridViewRow r = this.dgv_Rotate.Rows[i];
                r.HeaderCell.Value = (i + 1).ToString();
            }
        }


        //与设备通过消息发送获得完成旋转中心标定  (功能暂无)
        private bool bIsYetAutoRotation = false;
        private void btn_RotateAutoFitCircle_Click(object sender, EventArgs e)
        {
            int testNum = (int)nud_RotateNum.Value;
            if (!bIsYetAutoRotation)
            {
                if (MessageBox.Show("是否开始自动拟合？", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                {
                    if (testNum > 3)
                    {
                        if (btn_AutoFitCircle.Text.Trim() == "自动拟合")
                        {
                            dgv_Rotate.Rows.Clear();
                            btn_AutoFitCircle.Text = "停止拟合";
                            rotationThread = new Thread(state1Rotate);
                            rotationThread.IsBackground = true;
                            rotationThread.Start();
                            rotationStep = 0;
                            bIsRotationRunning = true;
                            bIsYetAutoRotation = true;
                            dgv_Rotate.Rows.Clear();
                        }
                    }
                    else 
                    {
                        MessageBox.Show("自动拟合圆心点数小于3点，请重新设置！");
                        return;
                    }
                }
            }
            else
            {
                if (btn_AutoFitCircle.Text.Trim() == "停止拟合")
                {
                    btn_AutoFitCircle.Text = "自动拟合";
                    rotationThread.Abort();
                    rotationThread.Join();
                    rotationStep = -1;
                    bIsRotationRunning = false;
                    bIsYetAutoRotation = false;
                    Error er = new Error();
                 //   RunParams.PLCConnect.WriteWord(PlcMemory.DM, 2404, 0, out er);//给PLC走位信号
                    //RunParams.PLCConnect.WriteBit(PlcMemory.CIO, "2.00", false, out er);     //测试完成置位
                }
            }
        }
        #endregion

        private void Frm_Claib_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (calibThread != null)
            {
                calibThread.Abort();
                calibThread.Join();
                bIsCalibRunning = false;
                calibStep = 0;
            }
            if (rotationThread != null)
            {
                rotationThread.Abort();
                rotationThread.Join();
                bIsRotationRunning = false;
                rotationStep = 0;
            }
            if (RunParams._StationNO == 1)
            {
                //if (RunParams.RobotConnect.TcpServer.IsRunning)
                //{
                //    RunParams.RobotConnect.TCPReceived -= new TCPCommunicate(RobotConnect_TCPReceived); //注销事件
                //}
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            //Error er = new Error();
            //switch (RunParams._StationNO)
            //{
            //    case 0:
            //        RunParams._StationData.state1CalibMoveX = (float)nud_CalibMoveX.Value;
            //        RunParams._StationData.state1CalibMoveY = (float)nud_CalibMoveY.Value;
            //        RunParams._StationData.state1CalibMoveR = (float)nud_RotateAngleRang.Value;
            //        break;
            //    case 1:
            //        RunParams._StationData.state2CalibMoveX = (float)nud_CalibMoveX.Value;
            //        RunParams._StationData.state2CalibMoveY = (float)nud_CalibMoveY.Value;
            //        RunParams._StationData.state2CalibMoveR = (float)nud_RotateAngleRang.Value;
            //        break;
            //}
            //RunParams._StationData.Write(path, RunParams._StationData, out er);
            ShowMessage(er.errorInfo, er.flag);
        }*/
    }
}

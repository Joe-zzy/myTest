using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Ports;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using BalserCamera;
using HalconDotNet;
using HalconTools;

namespace HalconTest
{
    class RunParams
    {
        //系统运行数据，需要在各个界面中进行调用
        //public static UserPassWord _User = new UserPassWord();
        //public static LightControl _Light = new LightControl();
        //public static TcpConnect _TcpConnect = new TcpConnect();
        //public static SystemParam _SystemSet = new SystemParam();
        //public static StationData _StationData = new StationData();
        //TCP通讯
      //  public static TCPCom RobotConnect = new TCPCom();
        //PLC通讯
     //   public static FinsTcpCom PLCConnect = new FinsTcpCom();
        //所有已输入的电芯型号
        public static String[] AllCellName;
        public static  BalserCognexCamera CurrentCamera ;

        public static HWindow_Final[] mw = new HWindow_Final[6];

        public static HWindow[] myWindow = new HWindow[13];

        //工位使用VP实例
        public static int _StationNO = 0;      //工位序号，给标定界面提示工位 0：工位1，1：工位2
        public static int _IsGetMarkOrRun = 0; //判断是运行工具还是标定获取标志点工具；0：标定获取标志点工具，1：运行工具;2:打开拟合圆工具；3：不良品处检测有无
        public static paramCalibData Station1 = new paramCalibData();
        public static paramCalibData Station2 = new paramCalibData();

        //系统数据文件保存文件夹路径
        public string dataPath = Application.StartupPath + "\\Data";

        //public RunParams()
        //{

        //}

        //保存数据 angle 为弧度值
        public void WriteDataToCSV(string path, string stationName, string m_Type, string m_ID, double px, double py, double mx, double my, double angle, double diffx, double diffy, double diffr)
        {
            try
            {
                string fullPath = path + "\\" + DateTime.Now.ToLongDateString() + "\\data\\" + DateTime.Now.ToLongDateString() + ".csv"; //文件保存完整路径
                FileInfo fi = new FileInfo(fullPath);
                if (!fi.Directory.Exists) //检查文件所在的文件夹是否存在
                {
                    fi.Directory.Create(); //不存在则创建
                }

                if (!fi.Exists) //检查文件是否存在
                {
                    //写入文件头部
                    using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);
                        string data = "";
                        data = "时间,";
                        data += "工位名称,";
                        data += "机型号,";
                        data += "产品ID,";
                        data += "图像坐标X,";
                        data += "图像坐标Y,";
                        data += "机械坐标X,";
                        data += "机械坐标Y,";
                        data += "角度,";
                        data += "偏差X,";
                        data += "偏差Y,";
                        data += "偏差R";

                        sw.WriteLine(data);
                        sw.Close();
                        fs.Close();
                    }
                }

                using (FileStream fs = new FileStream(fullPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                {
                    StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8);

                    string data = "";
                    data = DateTime.Now.ToString("HH:mm:ss:fff") + ",";
                    data += stationName + ",";
                    data += m_Type + ",";
                    data += m_ID + ",";
                    data += px.ToString() + ",";
                    data += py.ToString() + ",";
                    data += mx.ToString() + ",";
                    data += my.ToString() + ",";
                    data += angle.ToString() + ",";
                    data += diffx.ToString() + ",";
                    data += diffy.ToString() + ",";
                    data += diffr.ToString();

                    sw.WriteLine(data);
                    sw.Close();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        //刷新所有已创建的电芯型号
        public static void RefreshCellType(ref string[] cellName, out Error error)
        {
            try
            {
                string path = Application.StartupPath + "\\Vision"; //所有型号保存文件夹
                DirectoryInfo allDirector = new DirectoryInfo(path);
                DirectoryInfo[] cellDirector = allDirector.GetDirectories(); //获取文件夹下所有子文件件
                int DirectorNum = cellDirector.Length; //获得子文件夹个数
                if (DirectorNum > 0) //个数大于0
                {
                    Array.Sort(cellDirector, delegate (DirectoryInfo x, DirectoryInfo y) { return x.CreationTime.CompareTo(y.CreationTime); }); //按创建时间排序
                    cellName = new string[DirectorNum]; //刷新存放电芯型号名数组
                    for (int i = 0; i < DirectorNum; i++)
                    {
                        cellName[i] = cellDirector[i].Name; //获取电芯型号名称
                    }
                    error.flag = true;
                    error.errorInfo = "已获取创建型号；";
                }
                else //个数小于等于0
                {
                    error.flag = false;
                    error.errorInfo = "未创建型号；";
                }
            }
            catch (Exception ex) //运行出错
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }
        //检索型号是否存在
        public static void FindCellType(string[] AllCell, string cellName, out int index, out Error error)
        {
            index = -1;
            bool flag = false;
            try
            {
                for (int i = 0; i < AllCell.Length; i++)
                {
                    if (cellName == AllCell[i])
                    {
                        index = i;
                        flag = true;
                        break;
                    }
                }
                if (flag)
                {
                    error.flag = true;
                    error.errorInfo = "查找完成";
                }
                else
                {
                    error.flag = false;
                    error.errorInfo = "型号不存在";
                }
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }
        //复制文件夹到目标文件夹中
        public static void DirectorCopy(string sourceCellName, string targetCellName, out Error error)
        {
            try
            {
                string fullSourcePath = Application.StartupPath + "\\Vision\\" + sourceCellName;
                string fullTargetPath = Application.StartupPath + "\\Vision\\" + targetCellName;
                DirectoryInfo sourceDir = new DirectoryInfo(fullSourcePath);
                DirectoryInfo targetDir = new DirectoryInfo(fullTargetPath);
                if (!targetDir.Exists) //创建目标文件夹
                {
                    targetDir.Create();
                }
                DirectoryInfo[] allSourceDir = sourceDir.GetDirectories(); //搜索所有子文件夹
                for (int i = 0; i < allSourceDir.Length; i++)
                {
                    string name = allSourceDir[i].Name;
                    DirectoryInfo targetDirName = new DirectoryInfo(fullTargetPath + "\\" + name);
                    if (!targetDirName.Exists) //创建目标文件夹
                    {
                        targetDirName.Create();
                    }
                    FileInfo[] allSourceFile = allSourceDir[i].GetFiles();
                    for (int j = 0; j < allSourceFile.Length; j++)
                    {
                        allSourceFile[j].CopyTo(targetDirName.FullName + "\\" + allSourceFile[j].Name, true);
                    }
                }
                FileInfo[] allSourFile = sourceDir.GetFiles(); //搜索所有文件
                for (int j = 0; j < allSourFile.Length; j++)
                {
                    allSourFile[j].CopyTo(fullTargetPath + "\\" + allSourFile[j].Name, true);
                }
                error.flag = true;
                error.errorInfo = "检索并复制完成";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }

        public static void DirectorCopy(string targetCellName, out Error error)
        {
            try
            {
                string fullSourcePath = Application.StartupPath + "\\Demo";
                string fullTargetPath = Application.StartupPath + "\\Vision\\" + targetCellName;
                DirectoryInfo sourceDir = new DirectoryInfo(fullSourcePath);
                DirectoryInfo targetDir = new DirectoryInfo(fullTargetPath);
                if (!targetDir.Exists) //创建目标文件夹
                {
                    targetDir.Create();
                }
                DirectoryInfo[] allSourceDir = sourceDir.GetDirectories(); //搜索所有子文件夹
                for (int i = 0; i < allSourceDir.Length; i++)
                {
                    string name = allSourceDir[i].Name;
                    DirectoryInfo targetDirName = new DirectoryInfo(fullTargetPath + "\\" + name);
                    if (!targetDirName.Exists) //创建目标文件夹
                    {
                        targetDirName.Create();
                    }
                    FileInfo[] allSourceFile = allSourceDir[i].GetFiles();
                    for (int j = 0; j < allSourceFile.Length; j++)
                    {
                        allSourceFile[j].CopyTo(targetDirName.FullName + "\\" + allSourceFile[j].Name, true);
                    }
                }
                FileInfo[] allSourFile = sourceDir.GetFiles(); //搜索所有文件
                for (int j = 0; j < allSourFile.Length; j++)
                {
                    allSourFile[j].CopyTo(fullTargetPath + "\\" + allSourFile[j].Name, true);
                }
                error.flag = true;
                error.errorInfo = "检索并复制完成";
            }
            catch (Exception ex)
            {
                error.flag = false;
                error.errorInfo = ex.Message;
            }
        }

        //获取系统所在硬盘的序列号
        public static string GetSystemDiskNo()
        {
            ManagementClass cimObject = new ManagementClass("Win32_PhysicalMedia");
            ManagementObjectCollection moc = cimObject.GetInstances();
            Dictionary<string, string> dict = new Dictionary<string, string>();
            foreach (ManagementObject mo in moc)
            {
                string tag = mo.Properties["Tag"].Value.ToString().ToLower().Trim();
                string hdId = (string)mo.Properties["SerialNumber"].Value ?? string.Empty;
                hdId = hdId.Trim();
                dict.Add(tag, hdId);
            }
            cimObject = new ManagementClass("Win32_OperatingSystem");
            moc = cimObject.GetInstances();
            string currentSysRunDisk = string.Empty;
            foreach (ManagementObject mo in moc)
            {
                currentSysRunDisk = Regex.Match(mo.Properties["Name"].Value.ToString().ToLower(), @"harddisk\d+").Value;
            }
            var results = dict.Where(x => Regex.IsMatch(x.Key, @"physicaldrive" + Regex.Match(currentSysRunDisk, @"\d+$").Value));
            if (results.Any()) return results.ElementAt(0).Value;
            return "";
        }
        private static readonly object saveLock = new object();
        private static readonly object scrLock = new object();
        public static void SaveOriImage(string path, ref HObject Img1, string winName)
        {
            lock (saveLock)
            {
                try
                {
                    path = path + DateTime.Now.ToString("yyyy_MM_dd") + "\\";
                    if (!Directory.Exists(path + "原图\\"))
                    {
                        Directory.CreateDirectory(path + "原图\\");
                    }
                    HOperatorSet.WriteImage(Img1, "bmp", 0, path + "原图\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + winName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("图片保存错误：" + ex.ToString());
                }
                GC.Collect();
            }
        }
        public static void SaveScrImage(string path, HTuple hWindow, string winName)
        {
            lock (scrLock)
            {
                try
                {
                    path = path + DateTime.Now.ToString("yyyy_MM_dd") + "\\";
                    if (!Directory.Exists(path + "截图\\"))
                    {
                        Directory.CreateDirectory(path + "截图\\");
                    }
                    HObject Img1;
                    HOperatorSet.DumpWindowImage(out Img1, hWindow);
                    HOperatorSet.WriteImage(Img1, "bmp", 0, path + "截图\\" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + winName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("图片保存错误：" + ex.ToString());
                }
                GC.Collect();
            }
        }

    }

}

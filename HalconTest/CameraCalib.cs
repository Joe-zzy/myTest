using BalserCamera;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace HalconTest
{
    public partial class CameraCalib : Form
    {
        public BalserCognexCamera CalibCamera;
        private HObject ImageCalib;
        public CameraCalib()
        {
            InitializeComponent();
        }

        private void CameraCalib_Load(object sender, EventArgs e)
        {
            dgv_NinePoint.Rows.Clear(); //清除之前可能留存的点位数据
            dgv_Rotate.Rows.Clear();  //清除之前可能留存的点位数据
            CalibCamera = RunParams.CurrentCamera;
            try
            {
                if (!CalibCamera.IsOpen)
                {
                    CalibCamera.OpenCam();
                }              
                CalibCamera.processImageEvent += processHImage1;
            }
            catch (Exception ex)
            {
                ShowMessage("打开相机失败!原因：" + ex.Message,false);
            }
        }
        private void CameraCalib_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (CalibCamera!=null)
            {
                CalibCamera.processImageEvent -= processHImage1;
            }
            GC.Collect();
        }
        private void processHImage1(HObject Image, ImageType imageType)
        {
            this.hWindow_Final1.HobjectToHimage(Image);
            HOperatorSet.CopyImage(Image,out ImageCalib);
        }
        #region dgv操作
        //信息显示和日志记录
        private void ShowMessage(string message, bool wrong)
        {
            this.BeginInvoke((MethodInvoker)delegate () //显示消息
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
        //自动向九点标定的数据显示DataView控件添加数据
        private void AddDataToCalibDataView(double px, double py, double mx, double my, out Error error)
        {
            int index = 0;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate ()
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
        //自动添加旋转中心数据到列表中
        private void AddDataToRotateDataView(double px, double py, out Error error)
        {
            int index = 0;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate ()
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

        private void btn_NinePointDelete_Click(object sender, EventArgs e)
        {
            DeleteDataFromCalibDataView();
        }
        private void btn_RotateAdd_Click(object sender, EventArgs e)
        {
            Error er = new Error();
            double px = (double)nud_RotatePictrueX.Value;
            double py = (double)nud_RotatePictrueY.Value;
            AddDataToRotateDataView(px, py, out er); //添加

        }

        private void btn_RoatateDelete_Click(object sender, EventArgs e)
        {
            DeleteDataFromRotateDataView();
        }
        private void btnSave_Click(object sender, EventArgs e)
        {

        }
        private void btn_RotateGetOneImage_Click(object sender, EventArgs e)
        {
            if (CalibCamera == null)
            {
                ShowMessage("链接相机失败",false);
                return;
            }
            try
            {
                if (CalibCamera.IsGrabbing)
                    CalibCamera.StopGrabbing();
                CalibCamera.GrabOne();
            }
            catch (Exception ex)
            {
                ShowMessage("单张采集失败!原因：" + ex.Message, false);
            }

        }
        private void btn_RotateLiveShow_Click(object sender, EventArgs e)
        {
            if (CalibCamera == null)
            {
                ShowMessage("链接相机失败", false);
                return;
            }
            try
            {
                if (CalibCamera.IsGrabbing)
                    CalibCamera.StopGrabbing();
                else
                   CalibCamera.StartGrabbing();
            }
            catch (Exception ex)
            {
                ShowMessage("实时采集失败!原因：" + ex.Message, false);
            }
        }

        private void btn_NinePointTest_Click(object sender, EventArgs e)
        {

        }

        private void btn_ManulCalib_Click(object sender, EventArgs e)
        {

        }
    }
}

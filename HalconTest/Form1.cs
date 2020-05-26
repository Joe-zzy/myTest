using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HalconDotNet;
using MatchingModule;
using ViewROI;
using INIFILE;
using System.IO;
using BalserCamera;

namespace HalconTest
{
    struct paramCircle
    {
        public HTuple circleRadius;
        public HTuple circleRow;
        public HTuple circleColumn;
        public HTuple initialRow;
        public HTuple initialColumn;
        public HTuple MeasureLenght1;
        public HTuple MeasureLenght2;
        public HTuple MeasureThreshold;
        public HTuple MeasureTransition;
        public HTuple MinScore;
        public HTuple NumInstances;
        public HTuple NumMeasures;

    }
    struct paramLine
    {
        public HTuple lineRow1;
        public HTuple lineColumn1;
        public HTuple lineRow2;
        public HTuple lineColumn2;
        public HTuple linePhi;
        public HTuple initialRow1;
        public HTuple initialColumn1;
        public HTuple initialRow2;
        public HTuple initialColumn2;
        public HTuple MeasureLenght1;
        public HTuple MeasureLenght2;
        public HTuple MeasureThreshold;
        public HTuple MeasureTransition;
        public HTuple MinScore;
        public HTuple NumInstances;
        public HTuple NumMeasures;

    }
    public  struct paramCalibData
    {
        //........标定点位数据..........
        public double[] calibPictrueX;
        public double[] calibPictrueY;
        public double[] calibMachineX;
        public double[] calibMachineY;
        public double calibRMS;

        //.......拟合点位数据...........
        public double[] rotatePictrueX;
        public double[] rotatePictrueY;
        public double rotateCenterX ;
        public double rotateCenterY ;
        public double rotateCenterMachineX ;
        public double rotateCenterMachineY ;
        public double rotateRMS;
    }
    public partial class Form1 : Form
    {
        public IniFile paramIni;
        public BalserCognexCamera[] MyCamera=new BalserCognexCamera[13];
        //private MatchingAssistant mAssistant;
        //private MatchingParam parameterSet;
        private HTuple mModelID = null;
        private HObject image = null;
        private double iniHeight = 0.0;
        private double iniWidth = 0.0;
        private HTuple picHeight;
        private HTuple picWidth;
        private HTuple finalRow, finalCol, finalRadius, centerArea, centerRow, centerCol;
        private HTuple finalRow11, finalCol1, finalRow2, finalCol2, finalPhi;
        private HObject final_region = new HObject();
        private HTuple Hhandle;
        private HTuple Lhandle;
        HObject rectangle = new HObject();
        private HObject mTransContours = null;
        private HTuple hv_WindowHandle;
        private HObject brush_region = new HObject();
        private paramCircle ParamCircle = new paramCircle();
        private paramLine ParamLine = new paramLine();
        private static string systemCfgPath = Application.StartupPath + "\\Config\\";
        private static string paramPath = systemCfgPath + "param.ini";
        //  public HWndCtrl mView;
        public Form1()
        {
            InitializeComponent();
            this.hv_WindowHandle = this.hWindow_Final1.hWindowControl.HalconWindow;
            
            paramIni = new IniFile(paramPath);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            IniParam();
            for (int i = 0; i < 6; i++)
            {
                RunParams.mw[i] = new HalconTools.HWindow_Final();
            }
            this.iniWidth = this.hWindow_Final1.Width;
            this.iniHeight = this.hWindow_Final1.Height;
            MetricBox.SelectedIndex = 0;
            OptimizationBox.SelectedIndex = 0;
            SubPixelBox.SelectedIndex = 2;
            cb_cameraList.SelectedIndex = 0;
            IniCamera();

            rectangle.Dispose();

        }
        string selectedImageFileName;
        private void btnOpenImage_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = "";
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {

                selectedImageFileName = this.openFileDialog1.FileName;
                try
                {
                    HImage image = new HImage(this.openFileDialog1.FileName);
                    this.image = new HObject(image);
                    int cout = this.image.CountObj();
                    if (cout == 0)
                    {

                    }
                    HOperatorSet.GenEmptyObj(out this.image);
                    cout = this.image.CountObj();
                    if (cout == 0)
                    {

                    }

                    image.Dispose();
                    this.selfPic();
                    this.final_region.Dispose();
                    // this.mModelID = null;
                    //  this.mTransContours = null;
                    this.hWindow_Final1.HobjectToHimage(this.image);
                    //   this.labModelResult.Text = "";
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message, "读图错误", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }      
        private void button_draw_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.hWindow_Final1.DrawModel = true;
            this.hWindow_Final1.Focus();
            try
            {
                this.hWindow_Final1.HobjectToHimage(this.image);
                HTuple circleRadius, circleRow, circleColumn;
                HOperatorSet.SetColor(this.hv_WindowHandle, "red");
                HOperatorSet.DrawCircle(this.hv_WindowHandle,out circleRow,out circleColumn,out circleRadius);
                HOperatorSet.GenCircle(out final_region, circleRow, circleColumn, circleRadius);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                this.hWindow_Final1.HobjectToHimage(this.image);
                this.GenModel(this.image.Clone(), this.final_region);
                HOperatorSet.AreaCenter(final_region,out centerArea,out centerRow,out centerCol);
                this.hWindow_Final1.DrawModel = false;
            }
        }
        private void button_wipe_Click(object sender, EventArgs e)
        {
            HTuple tuple4;
            HTuple tuple5;
            HTuple tuple6;
            HObject regionAffineTrans = new HObject();
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (this.mModelID == null)
            {
                MessageBox.Show("操作不能进行", "请先创建模板区域", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            try
            {
                HOperatorSet.GenCircle(out rectangle, picWidth / 2, picHeight / 2,(int)circleRadius.Value);
                this.brush_region.Dispose();
                this.brush_region = rectangle;
                HObject objectVal = new HObject(this.image);
                HOperatorSet.AreaCenter(this.brush_region, out tuple4, out tuple5, out tuple6);
                HTuple button = 0;
                while (button != 4)
                {
                    HTuple tuple7;
                    HObject obj1;
                    Application.DoEvents();
                    HTuple row = -1;
                    HTuple column = -1;
                    try
                    {
                        HOperatorSet.GetMposition(this.hv_WindowHandle, out row, out column, out button);
                    }
                    catch (HalconException)
                    {
                        button = 0;
                    }
                    HOperatorSet.SetSystem("flush_graphic", "false");
                    HOperatorSet.DispObj(objectVal, this.hv_WindowHandle);
                    if (this.mTransContours != null)
                    {
                        this.hWindow_Final1.DispObj(this.mTransContours, "green");
                    }
                    if (this.final_region.IsInitialized())
                    {
                        HOperatorSet.DispObj(this.final_region, this.hv_WindowHandle);
                    }
                    if ((row < 0) || (column < 0))
                    {
                        goto Label_04C9;
                    }
                    HOperatorSet.VectorAngleToRigid(tuple5, tuple6, 0, row, column, 0, out tuple7);
                    regionAffineTrans.Dispose();
                    HOperatorSet.AffineTransRegion(this.brush_region, out regionAffineTrans, tuple7, "nearest_neighbor");

                    this.hWindow_Final1.viewWindow.ClearWindow();
                    this.hWindow_Final1.DispObj(regionAffineTrans, "red");
                    this.hWindow_Final1.DispObj(this.final_region, "red");

                    HOperatorSet.SetSystem("flush_graphic", "true");
                    if (button == 1)
                    {
                        goto Label_048D;
                    }
                    continue;
                    Label_048D:
                    HOperatorSet.Difference(this.final_region, regionAffineTrans, out obj1);
                    this.final_region.Dispose();
                    this.final_region = obj1;
                    continue;
                    Label_04C9:
                    disp_message(this.hv_WindowHandle, "请将鼠标移动到窗口内部", "window", 20, 20, "red", "false");
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                this.hWindow_Final1.HobjectToHimage(this.image);
                this.GenModel(this.image.Clone(), this.final_region);
                this.hWindow_Final1.DrawModel = false;
            }

        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            if (this.mModelID == null)
            {
                MessageBox.Show("操作不能进行", "请先创建模板", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else
            {
                try
                {
                    if (this.saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        string fileName = this.saveFileDialog1.FileName;
                        if (!(fileName.EndsWith(".shm") || fileName.EndsWith(".SHM")))
                        {
                            fileName = fileName + ".shm";
                        }
                        HOperatorSet.WriteShapeModel(this.mModelID, fileName);
                        MessageBox.Show("模板成功保存到：" + fileName, "模板保存成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show("保存模板失败", exception.Message, MessageBoxButtons.OK, MessageBoxIcon.Hand);
                }
            }
        }
       
        HTuple row, col, angle,scale, score;

        private void btnMatchFindCL_Click(object sender, EventArgs e)
        {
            if (this.mModelID == null)
            {
                MessageBox.Show("操作不能进行", "请加载/创建模板", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            try
            {
                HTuple resultRow, resultCol;
                HObject circleResult1, circleResult2;
                HObject finalobj, contours, ho_Cross;
                HObject finalobj2, contours2, ho_Cross2;
                this.hWindow_Final1.HobjectToHimage(this.image);
                LocationInterface.affineTransPixel(ParamCircle.initialRow, ParamCircle.initialColumn, row[0], col[0], angle[0], out resultRow, out resultCol);
                HOperatorSet.GenCircle(out circleResult1, resultRow, resultCol, ParamCircle.circleRadius);
                this.hWindow_Final1.DispObj(circleResult1, "blue");

                ParamCircle.MeasureLenght1 = (int)CircleMeasureLenght1.Value;
                ParamCircle.MeasureLenght2 = (int)CircleMeasureLenght2.Value;
                ParamCircle.MeasureThreshold = (int)CircleMeasureThreshold.Value;
                ParamCircle.MeasureTransition = (string)CircleMeasureTransition.SelectedItem;
                ParamCircle.MinScore = (int)CircleMinScore.Value;
                ParamCircle.NumInstances = (int)CircleNumInstances.Value;
                ParamCircle.NumMeasures = (int)CircleNumMeasures.Value;
                ParamCircle.circleRow = resultRow;
                ParamCircle.circleColumn = resultCol;

                LocationInterface.createCircleMetrology(ParamCircle, ref Hhandle);
                bool result = LocationInterface.findCircle(image, Hhandle, out finalRow, out finalCol, out finalRadius, out finalobj, out contours, out ho_Cross);
                this.hWindow_Final1.DispObj(finalobj, "red");
                this.hWindow_Final1.DispObj(contours, "blue");
                this.hWindow_Final1.DispObj(ho_Cross, "green");
                this.hWindow_Final1.DrawModel = false;
                circleResult.Text = result.ToString() + "\r\n";
                circleResult.ForeColor = result ? Color.Green : Color.Red;
                circleResult.Text += "圆心坐标X：" + finalCol + "\r\n"; ;
                circleResult.Text += "圆心坐标Y：" + finalRow + "\r\n"; ;
                circleResult.Text += "圆心半径R：" + finalRadius;

                LocationInterface.affineTransPixel(ParamCircle.initialRow, ParamCircle.initialColumn, row[1], col[1], angle[1], out resultRow, out resultCol);
                HOperatorSet.GenCircle(out circleResult2, resultRow, resultCol, ParamCircle.circleRadius);
                this.hWindow_Final1.DispObj(circleResult2, "blue");
                ParamCircle.circleRow = resultRow;
                ParamCircle.circleColumn = resultCol;

                LocationInterface.createCircleMetrology(ParamCircle, ref Hhandle);
                result = LocationInterface.findCircle(image, Hhandle, out finalRow, out finalCol, out finalRadius, out finalobj2, out contours2, out ho_Cross2);
                this.hWindow_Final1.DispObj(finalobj2, "red");
                this.hWindow_Final1.DispObj(contours2, "blue");
                this.hWindow_Final1.DispObj(ho_Cross2, "green");


            }
            catch (Exception ex)
            {

                MessageBox.Show("保存模板失败", ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
          

        }

        private void button_SaveRegion_Click(object sender, EventArgs e)
        {
            if (!Directory.Exists(systemCfgPath))
            {
                Directory.CreateDirectory(systemCfgPath);
            }
            if (!File.Exists(paramPath))
            {
                File.Create(paramPath);
            }
            try
            {

                paramIni.WriteString("CircleParam", "MeasureLenght1",CircleMeasureLenght1.Value.ToString());
                paramIni.WriteString("CircleParam", "MeasureLenght2", CircleMeasureLenght2.Value.ToString());
                paramIni.WriteString("CircleParam", "MeasureThreshold", CircleMeasureThreshold.Value.ToString());
                paramIni.WriteString("CircleParam", "MeasureTransition", CircleMeasureTransition.SelectedItem.ToString());
                paramIni.WriteString("CircleParam", "MinScore", CircleMinScore.Value.ToString());
                paramIni.WriteString("CircleParam", "NumInstances", CircleNumInstances.Value.ToString());
                paramIni.WriteString("CircleParam", "NumMeasures", CircleNumMeasures.Value.ToString());
                paramIni.WriteString("CircleRegion", "circleRadius", ParamCircle.circleRadius.ToString());
                paramIni.WriteString("CircleRegion", "circleRow", ParamCircle.circleRow.ToString());
                paramIni.WriteString("CircleRegion", "circleColumn", ParamCircle.circleColumn.ToString());
                ParamCircle.initialRow = ParamCircle.circleRow - centerRow;
                ParamCircle.initialColumn = ParamCircle.circleColumn - centerCol;
                paramIni.WriteString("CircleRegion", "initialRow", ParamCircle.initialRow.ToString());
                paramIni.WriteString("CircleRegion", "initialColumn", ParamCircle.initialColumn.ToString());

                paramIni.WriteString("LineParam", "MeasureLenght1", LineMeasureLenght1.Value.ToString());
                paramIni.WriteString("LineParam", "MeasureLenght2", LineMeasureLenght2.Value.ToString());
                paramIni.WriteString("LineParam", "MeasureThreshold", LineMeasureThreshold.Value.ToString());
                paramIni.WriteString("LineParam", "MeasureTransition", LineMeasureTransition.SelectedItem.ToString());
                paramIni.WriteString("LineParam", "MinScore", LineMinScore.Value.ToString());
                paramIni.WriteString("LineParam", "NumInstances", LineNumInstances.Value.ToString());
                paramIni.WriteString("LineParam", "NumMeasures", LineNumMeasures.Value.ToString());
                paramIni.WriteString("LineRegion", "lineRow1", ParamLine.lineRow1.ToString());
                paramIni.WriteString("LineRegion", "lineColumn1", ParamLine.lineColumn1.ToString());
                paramIni.WriteString("LineRegion", "lineRow2", ParamLine.lineRow2.ToString());
                paramIni.WriteString("LineRegion", "lineColumn2", ParamLine.lineColumn2.ToString());

                MessageBox.Show("区域成功保存到：" + paramPath, "区域保存成功", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

            }
            catch (Exception exception)
            {
                MessageBox.Show("保存区域失败", exception.Message, MessageBoxButtons.OK, MessageBoxIcon.Hand);
            }
            
        }

        private void findCircleLine_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.hWindow_Final1.HobjectToHimage(this.image);
            try
            {
                HObject finalobj, contours, ho_Cross;

                ParamCircle.MeasureLenght1 = (int)CircleMeasureLenght1.Value;
                ParamCircle.MeasureLenght2 = (int)CircleMeasureLenght2.Value;
                ParamCircle.MeasureThreshold = (int)CircleMeasureThreshold.Value;
                ParamCircle.MeasureTransition = (string)CircleMeasureTransition.SelectedItem;
                ParamCircle.MinScore = (int)CircleMinScore.Value;
                ParamCircle.NumInstances = (int)CircleNumInstances.Value;
                ParamCircle.NumMeasures = (int)CircleNumMeasures.Value;
                this.hWindow_Final1.HobjectToHimage(this.image);
                LocationInterface.createCircleMetrology(ParamCircle, ref Hhandle);
                bool result = LocationInterface.findCircle(image, Hhandle, out finalRow, out finalCol, out finalRadius, out finalobj, out contours, out ho_Cross);
                this.hWindow_Final1.DispObj(finalobj, "red");
                this.hWindow_Final1.DispObj(contours, "blue");
                this.hWindow_Final1.DispObj(ho_Cross, "green");
                this.hWindow_Final1.DrawModel = false;
                circleResult.Text = result.ToString()+"\r\n";
                circleResult.ForeColor = result ? Color.Green : Color.Red;
                circleResult.Text += "圆心坐标X：" + finalCol + "\r\n"; ;
                circleResult.Text += "圆心坐标Y：" + finalRow + "\r\n"; ;
                circleResult.Text += "圆心半径R：" + finalRadius;

                ParamLine.MeasureLenght1 = (int)LineMeasureLenght1.Value;
                ParamLine.MeasureLenght2 = (int)LineMeasureLenght2.Value;
                ParamLine.MeasureThreshold = (int)LineMeasureThreshold.Value;
                ParamLine.MeasureTransition = (string)LineMeasureTransition.SelectedItem;
                ParamLine.MinScore = (int)LineMinScore.Value;
                ParamLine.NumInstances = (int)LineNumInstances.Value;
                ParamLine.NumMeasures = (int)LineNumMeasures.Value;
                LocationInterface.createLineMetrology(ParamLine, ref Lhandle);
                bool result2 = LocationInterface.findLine(image, Lhandle, out finalRow11, out finalCol1, out finalRow2, out finalCol2, out finalobj, out contours, out ho_Cross, out finalPhi);
                this.hWindow_Final1.DispObj(finalobj, "red");
                this.hWindow_Final1.DispObj(contours, "yellow");
                this.hWindow_Final1.DispObj(ho_Cross, "blue");
                this.hWindow_Final1.DrawModel = false;
                lineResult.Text = result2.ToString();
                lineResult.ForeColor = result2 ? Color.Green : Color.Red;

            }
            catch (Exception ex)
            {

                MessageBox.Show("查找圆失败", "", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void findLineButton_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.hWindow_Final1.DrawModel = true;
            this.hWindow_Final1.Focus();
          
            HObject finalobj, contours, ho_Cross;
            try
            {
                this.hWindow_Final1.HobjectToHimage(this.image);
                HOperatorSet.SetColor(this.hv_WindowHandle, "red");
                HOperatorSet.DrawLine(this.hv_WindowHandle, out ParamLine.lineRow1, out ParamLine.lineColumn1, out ParamLine.lineRow2, out ParamLine.lineColumn2);
                HOperatorSet.DispLine(hv_WindowHandle, ParamLine.lineRow1, ParamLine.lineColumn1, ParamLine.lineRow2, ParamLine.lineColumn2);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //   paramLine param = new paramLine();
                ParamLine.MeasureLenght1 = (int)LineMeasureLenght1.Value;
                ParamLine.MeasureLenght2 = (int)LineMeasureLenght2.Value;
                ParamLine.MeasureThreshold = (int)LineMeasureThreshold.Value;
                ParamLine.MeasureTransition = (string)LineMeasureTransition.SelectedItem;
                ParamLine.MinScore = (int)LineMinScore.Value;
                ParamLine.NumInstances = (int)LineNumInstances.Value;
                ParamLine.NumMeasures = (int)LineNumMeasures.Value;

                this.hWindow_Final1.HobjectToHimage(this.image);
                LocationInterface.createLineMetrology( ParamLine, ref Lhandle);
                bool result = LocationInterface.findLine(image, Lhandle,out finalRow11,out finalCol1,out finalRow2,out finalCol2, out finalobj,out contours, out ho_Cross , out finalPhi);
                this.hWindow_Final1.DispObj(finalobj, "red");
                this.hWindow_Final1.DispObj(contours, "blue");
                this.hWindow_Final1.DispObj(ho_Cross, "green");
                this.hWindow_Final1.DrawModel = false;
                lineResult.Text = result.ToString();
                lineResult.ForeColor = result ? Color.Green : Color.Red;
            }
        }

        private void findModelButton_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.hWindow_Final1.HobjectToHimage(this.image);
            LocationInterface.modelMatch(image, mModelID,"形状模板", new HTuple((int)this.StartingAngleUpDown.Value).TupleRad(), new HTuple((int)this.AngleExtentUpDown.Value).TupleRad(), (int)MinScoreUpDown.Value * 0.01,(int)NumMatchesUpDown.Value,out mTransContours, out row, out col, out angle, out score);
            lbResultNum.Text = row.Length.ToString();
            this.hWindow_Final1.DispObj(mTransContours, "green");
        }

        private void findCircleButton_Click(object sender, EventArgs e)
        {
            if (this.image == null)
            {
                MessageBox.Show("操作不能进行", "请先加载或者采集图片", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            this.hWindow_Final1.DrawModel = true;
            this.hWindow_Final1.Focus();
           
            try
            {
                this.hWindow_Final1.HobjectToHimage(this.image);
                HOperatorSet.SetColor(this.hv_WindowHandle, "red");
                HOperatorSet.DrawCircle(this.hv_WindowHandle, out ParamCircle.circleRow, out ParamCircle.circleColumn, out ParamCircle.circleRadius);
                HOperatorSet.GenCircle(out final_region, ParamCircle.circleRow, ParamCircle.circleColumn, ParamCircle.circleRadius);
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.ToString());
            }
            finally
            {
                HObject finalobj, contours, ho_Cross;

                ParamCircle.MeasureLenght1 = (int)CircleMeasureLenght1.Value;
                ParamCircle.MeasureLenght2 = (int)CircleMeasureLenght2.Value;
                ParamCircle.MeasureThreshold = (int)CircleMeasureThreshold.Value;
                ParamCircle.MeasureTransition = (string)CircleMeasureTransition.SelectedItem;
                ParamCircle.MinScore = (int)CircleMinScore.Value;
                ParamCircle.NumInstances = (int)CircleNumInstances.Value;
                ParamCircle.NumMeasures = (int)CircleNumMeasures.Value;

                this.hWindow_Final1.HobjectToHimage(this.image);
                LocationInterface.createCircleMetrology(ParamCircle, ref Hhandle);
                bool result = LocationInterface.findCircle(image, Hhandle, out finalRow, out finalCol, out finalRadius, out finalobj,out contours,out ho_Cross);
                this.hWindow_Final1.DispObj(finalobj, "red");
                this.hWindow_Final1.DispObj(contours, "blue");
                this.hWindow_Final1.DispObj(ho_Cross, "green");
                this.hWindow_Final1.DrawModel = false;
                circleResult.Text = result.ToString();
                circleResult.ForeColor = result? Color.Green:Color.Red;
            }
        }

        private void loadModelbutton_Click(object sender, EventArgs e)
        {
            if (this.openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string fileName = this.openFileDialog1.FileName;
                if (!(fileName.EndsWith(".shm")))
                {
                    MessageBox.Show("操作不能进行", "请先加载正确模板", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                }
                else
                {

                     HOperatorSet.ReadShapeModel(fileName,out mModelID);

                }
            }
        }
        #region 相机
        private void rdbFreerun1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (MyCamera[cb_cameraList.SelectedIndex] == null || MyCamera[cb_cameraList.SelectedIndex].IsOpen == false)
                    return;
                if (rdbFreerun1.Checked)
                {
                    MyCamera[cb_cameraList.SelectedIndex].SetFreerun();
                   // UpdateControl();
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("设置自由运行模式失败!原因：" + ex.Message);
            }
        }

        private void rdbSWTrigger1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (MyCamera[cb_cameraList.SelectedIndex] == null || MyCamera[cb_cameraList.SelectedIndex].IsOpen == false)
                    return;
                if (rdbSWTrigger1.Checked)
                {
                    MyCamera[cb_cameraList.SelectedIndex].SetSoftwareTrigger();
                  //  UpdateControl();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置软触发模式失败!原因：" + ex.Message);
            }
        }

        private void rdbHWTrigger1_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (rdbHWTrigger1.Checked)
                {
                    MyCamera[cb_cameraList.SelectedIndex].SetExternTrigger();
                  //  UpdateControl();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置硬触发模式失败!原因：" + ex.Message);
            }

        }

        private void btnExecute1_Click(object sender, EventArgs e)
        {
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].SendSoftwareExecute();
             //   UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show(" 发送软触发命令失败!原因：" + ex.Message);
            }
        }

        private void btnStart1_Click(object sender, EventArgs e)
        {
           int Count = 0;
            lblCount1.Text = "[ 图像采集数量 : " + Count + "  ]";
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].StartGrabbing();
               
                //   UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show(" 开始采集失败!原因：" + ex.Message);
            }
        }

        private void btnStop1_Click(object sender, EventArgs e)
        {
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].StopGrabbing();
                
                //  UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show(" 停止采集失败!原因：" + ex.Message);
            }
        }

        private void btnSingleFrame1_Click(object sender, EventArgs e)
        {
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].GrabOne();
              //  this.hWindow_Final1.HobjectToHimage(MyCamera.Image);
                //  UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show("单张采集失败!原因：" + ex.Message);
            }
        }

        private void cb_cameraList_SelectedIndexChanged(object sender, EventArgs e)
        {
            RunParams.CurrentCamera = MyCamera[cb_cameraList.SelectedIndex];

            RunParams.mw[cb_cameraList.SelectedIndex] = hWindow_Final2;

            if (MyCamera[cb_cameraList.SelectedIndex] == null)
                return;
            try
            {
                //if (MyCamera[cb_cameraList.SelectedIndex].IsOpen)
                //{
                //    MyCamera[cb_cameraList.SelectedIndex].processImageEvent += processHImage1;
                //}
                //else
                //{
                //    MyCamera[cb_cameraList.SelectedIndex].OpenCam();
                //    MyCamera[cb_cameraList.SelectedIndex].processImageEvent += processHImage1;
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开相机失败!原因：" + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CameraCalib calib = new CameraCalib();
            calib.Show();
        }

        private void Num_Exposure_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int value = (int)Num_Exposure.Value;
                MyCamera[cb_cameraList.SelectedIndex].SetExposureTime(value);
                Tkb_Exposure.Value = value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置曝光时间失败!原因：" + ex.Message);
            }
        }
        private bool bIsHideCommFlag = false; //false=显示；true=隐藏；
        private void button1_Click_1(object sender, EventArgs e)
        {
            //RunParams.SaveOriImage("C://Users//Administrator//Desktop//Atest//",ref this.image,"A");
            HTuple hv_WindowHandle = RunParams.mw[0].hWindowControl.HalconWindow;
            RunParams.mw[0].HobjectToHimage(this.image);
            HObject re;
            HOperatorSet.GenRectangle1(out re,0,0,500,500);
            HOperatorSet.SetColor(hv_WindowHandle,"red");
            RunParams.mw[0].DispObj(re);
           // RunParams.SaveScrImage("C://Users//Administrator//Desktop//Atest//", hv_WindowHandle,"A");
        }

        private void button2_Click(object sender, EventArgs e)
        {
         //   RunParams.SaveOriImage("C://Users//Administrator//Desktop//Atest//", ref this.image, "B");
            HTuple hv_WindowHandle = RunParams.mw[1].hWindowControl.HalconWindow;
            RunParams.mw[1].HobjectToHimage(this.image);
            HObject re;
            HOperatorSet.GenRectangle1(out re, 0, 0, 1500, 1500);
            HOperatorSet.SetColor(hv_WindowHandle, "blue");
            RunParams.mw[1].DispObj(re,"blue");
        //    RunParams.SaveScrImage("C://Users//Administrator//Desktop//Atest//", hv_WindowHandle, "B");
        }

        private void button1_Click_2(object sender, EventArgs e)
        {

        }

        private void Num_Gain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int value = (int)Num_Gain.Value;
                MyCamera[cb_cameraList.SelectedIndex].SetGain(value);
                Tkb_Gain.Value = value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置曝光时间失败!原因：" + ex.Message);
            }
        }

        private void Btn_OpenCamera_Click(object sender, EventArgs e)
        {
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].OpenCam();
                if (cb_cameraList.SelectedIndex==0)
                {
                    MyCamera[cb_cameraList.SelectedIndex].processImageEvent += processHImage1;
                }
                else
                {
                    MyCamera[cb_cameraList.SelectedIndex].processImageEvent += processHImage2;
                }
                
                //    AddEvent();
                //   MyCamera.SetHeartBeatTime(5000);          // 设置相机1心跳时间
                //  UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show("打开相机失败!原因：" + ex.Message);
            }
        }

        private void Btn_CloseCamera_Click(object sender, EventArgs e)
        {
            try
            {
                MyCamera[cb_cameraList.SelectedIndex].StopGrabbing();
                if (cb_cameraList.SelectedIndex==0)
                {
                    MyCamera[cb_cameraList.SelectedIndex].processImageEvent -= processHImage1;
                }
               else
                {
                    MyCamera[cb_cameraList.SelectedIndex].processImageEvent -= processHImage2;

                }
                //  RemoveEvent();
                MyCamera[cb_cameraList.SelectedIndex].CloseCam();
              //  UpdateControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置曝光时间失败!原因：" + ex.Message);
            }
        }

        private void Tkb_Exposure_ValueChanged(object sender, EventArgs e)
        {

            try
            {
                int value = Tkb_Exposure.Value;
                MyCamera[cb_cameraList.SelectedIndex].SetExposureTime(value);
                Num_Exposure.Value = value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置曝光时间失败!原因：" + ex.Message);
            }
        }

        private void Tkb_Gain_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                int value = Tkb_Gain.Value;
                MyCamera[cb_cameraList.SelectedIndex].SetGain(value);
                Num_Gain.Value = value;
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置曝光时间失败!原因：" + ex.Message);
            }
        }
        #endregion
        #region Common

        private void IniParam()
        {
            CircleMeasureLenght1.Value = decimal.Parse(paramIni.ReadString("CircleParam", "MeasureLenght1", ""));
            CircleMeasureLenght2.Value = decimal.Parse(paramIni.ReadString("CircleParam", "MeasureLenght2", ""));
            CircleMeasureThreshold.Value = decimal.Parse(paramIni.ReadString("CircleParam", "MeasureThreshold", ""));
            CircleMeasureTransition.SelectedItem = paramIni.ReadString("CircleParam", "MeasureTransition", "");
            CircleMinScore.Value = decimal.Parse(paramIni.ReadString("CircleParam", "MinScore", ""));
            CircleNumInstances.Value = decimal.Parse(paramIni.ReadString("CircleParam", "NumInstances", ""));
            CircleNumMeasures.Value = decimal.Parse(paramIni.ReadString("CircleParam", "NumMeasures", ""));
            ParamCircle.circleRadius = double.Parse(paramIni.ReadString("CircleRegion", "circleRadius", ""));
            ParamCircle.circleRow = double.Parse(paramIni.ReadString("CircleRegion", "circleRow", ""));
            ParamCircle.circleColumn = double.Parse(paramIni.ReadString("CircleRegion", "circleColumn", ""));
            ParamCircle.initialRow = double.Parse(paramIni.ReadString("CircleRegion", "initialRow", ""));
            ParamCircle.initialColumn = double.Parse(paramIni.ReadString("CircleRegion", "initialColumn", ""));

            LineMeasureLenght1.Value = decimal.Parse(paramIni.ReadString("LineParam", "MeasureLenght1", ""));
            LineMeasureLenght2.Value = decimal.Parse(paramIni.ReadString("LineParam", "MeasureLenght2", ""));
            LineMeasureThreshold.Value = decimal.Parse(paramIni.ReadString("LineParam", "MeasureThreshold", ""));
            LineMeasureTransition.SelectedItem = paramIni.ReadString("LineParam", "MeasureTransition", "");
            LineMinScore.Value = decimal.Parse(paramIni.ReadString("LineParam", "MinScore", ""));
            LineNumInstances.Value = decimal.Parse(paramIni.ReadString("LineParam", "NumInstances", ""));
            LineNumMeasures.Value = decimal.Parse(paramIni.ReadString("LineParam", "NumMeasures", ""));
            ParamLine.lineRow1 = double.Parse(paramIni.ReadString("LineRegion", "lineRow1", ""));
            ParamLine.lineColumn1 = double.Parse(paramIni.ReadString("LineRegion", "lineColumn1", ""));
            ParamLine.lineRow2 = double.Parse(paramIni.ReadString("LineRegion", "lineRow2", ""));
            ParamLine.lineColumn2 = double.Parse(paramIni.ReadString("LineRegion", "lineColumn2", ""));
        }
        public enum cameraID
        {
            A = 0,
            B = 1,
            C = 2,
            D = 3,
            E = 4,
            F = 5
        }
        private void IniCamera()
        {
            List<string> CameraSerialList = BalserCognexCamera.GetAllCamerasSerialNumber();
            BalserCognexCamera[] MyCameraList = new BalserCognexCamera[13];
            for (int i = 0; i < CameraSerialList.Count; i++)
            {
                MyCameraList[i] = new BalserCognexCamera(CameraSerialList[i]);
                foreach (string name in Enum.GetNames(typeof(cameraID)))
                {
                    if (MyCameraList[i].CameraDeviceID.Trim() == name)
                    {
                        MyCamera[(int)Enum.Parse(typeof(cameraID), name)] = MyCameraList[i];
                    }
                }
            }
            rdbFreerun1.Checked = true;
        }
        public static void disp_message(HTuple hv_WindowHandle, HTuple hv_String, HTuple hv_CoordSystem, HTuple hv_Row, HTuple hv_Column, HTuple hv_Color, HTuple hv_Box)
        {
            HTuple hv_GenParamName = new HTuple(), hv_GenParamValue = new HTuple();
            HTuple hv_Color_COPY_INP_TMP = new HTuple(hv_Color);
            HTuple hv_Column_COPY_INP_TMP = new HTuple(hv_Column);
            HTuple hv_CoordSystem_COPY_INP_TMP = new HTuple(hv_CoordSystem);
            HTuple hv_Row_COPY_INP_TMP = new HTuple(hv_Row);

            if ((int)((new HTuple(hv_Row_COPY_INP_TMP.TupleEqual(new HTuple()))).TupleOr(
                new HTuple(hv_Column_COPY_INP_TMP.TupleEqual(new HTuple())))) != 0)
            {

                hv_Color_COPY_INP_TMP.Dispose();
                hv_Column_COPY_INP_TMP.Dispose();
                hv_CoordSystem_COPY_INP_TMP.Dispose();
                hv_Row_COPY_INP_TMP.Dispose();
                hv_GenParamName.Dispose();
                hv_GenParamValue.Dispose();

                return;
            }
            if ((int)(new HTuple(hv_Row_COPY_INP_TMP.TupleEqual(-1))) != 0)
            {
                hv_Row_COPY_INP_TMP.Dispose();
                hv_Row_COPY_INP_TMP = 12;
            }
            if ((int)(new HTuple(hv_Column_COPY_INP_TMP.TupleEqual(-1))) != 0)
            {
                hv_Column_COPY_INP_TMP.Dispose();
                hv_Column_COPY_INP_TMP = 12;
            }
            //
            //Convert the parameter Box to generic parameters.
            hv_GenParamName.Dispose();
            hv_GenParamName = new HTuple();
            hv_GenParamValue.Dispose();
            hv_GenParamValue = new HTuple();
            if ((int)(new HTuple((new HTuple(hv_Box.TupleLength())).TupleGreater(0))) != 0)
            {
                if ((int)(new HTuple(((hv_Box.TupleSelect(0))).TupleEqual("false"))) != 0)
                {
                    //Display no box
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamName = hv_GenParamName.TupleConcat(
                                "box");
                            hv_GenParamName.Dispose();
                            hv_GenParamName = ExpTmpLocalVar_GenParamName;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamValue = hv_GenParamValue.TupleConcat(
                                "false");
                            hv_GenParamValue.Dispose();
                            hv_GenParamValue = ExpTmpLocalVar_GenParamValue;
                        }
                    }
                }
                else if ((int)(new HTuple(((hv_Box.TupleSelect(0))).TupleNotEqual("true"))) != 0)
                {
                    //Set a color other than the default.
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamName = hv_GenParamName.TupleConcat(
                                "box_color");
                            hv_GenParamName.Dispose();
                            hv_GenParamName = ExpTmpLocalVar_GenParamName;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamValue = hv_GenParamValue.TupleConcat(
                                hv_Box.TupleSelect(0));
                            hv_GenParamValue.Dispose();
                            hv_GenParamValue = ExpTmpLocalVar_GenParamValue;
                        }
                    }
                }
            }
            if ((int)(new HTuple((new HTuple(hv_Box.TupleLength())).TupleGreater(1))) != 0)
            {
                if ((int)(new HTuple(((hv_Box.TupleSelect(1))).TupleEqual("false"))) != 0)
                {
                    //Display no shadow.
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamName = hv_GenParamName.TupleConcat(
                                "shadow");
                            hv_GenParamName.Dispose();
                            hv_GenParamName = ExpTmpLocalVar_GenParamName;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamValue = hv_GenParamValue.TupleConcat(
                                "false");
                            hv_GenParamValue.Dispose();
                            hv_GenParamValue = ExpTmpLocalVar_GenParamValue;
                        }
                    }
                }
                else if ((int)(new HTuple(((hv_Box.TupleSelect(1))).TupleNotEqual("true"))) != 0)
                {
                    //Set a shadow color other than the default.
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamName = hv_GenParamName.TupleConcat(
                                "shadow_color");
                            hv_GenParamName.Dispose();
                            hv_GenParamName = ExpTmpLocalVar_GenParamName;
                        }
                    }
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        {
                            HTuple
                              ExpTmpLocalVar_GenParamValue = hv_GenParamValue.TupleConcat(
                                hv_Box.TupleSelect(1));
                            hv_GenParamValue.Dispose();
                            hv_GenParamValue = ExpTmpLocalVar_GenParamValue;
                        }
                    }
                }
            }
            //Restore default CoordSystem behavior.
            if ((int)(new HTuple(hv_CoordSystem_COPY_INP_TMP.TupleNotEqual("window"))) != 0)
            {
                hv_CoordSystem_COPY_INP_TMP.Dispose();
                hv_CoordSystem_COPY_INP_TMP = "image";
            }
            //
            if ((int)(new HTuple(hv_Color_COPY_INP_TMP.TupleEqual(""))) != 0)
            {
                //disp_text does not accept an empty string for Color.
                hv_Color_COPY_INP_TMP.Dispose();
                hv_Color_COPY_INP_TMP = new HTuple();
            }
            //
            if (HDevWindowStack.IsOpen())
            {
                HOperatorSet.DispText(HDevWindowStack.GetActive(), hv_String, "window", hv_Row_COPY_INP_TMP,
                    hv_Column_COPY_INP_TMP, "black", hv_GenParamName, hv_GenParamValue);
            }
            //disp_text (WindowHandle, String, CoordSystem, Row, Column, Color, GenParamName, GenParamValue)

            hv_Color_COPY_INP_TMP.Dispose();
            hv_Column_COPY_INP_TMP.Dispose();
            hv_CoordSystem_COPY_INP_TMP.Dispose();
            hv_Row_COPY_INP_TMP.Dispose();
            hv_GenParamName.Dispose();
            hv_GenParamValue.Dispose();
        }
        private bool GenModel(HObject ho_Image, HObject ho_Region)
        {
            try
            {
                HObject obj2;
                HObject obj3;
                HObject obj4;
                if (!ho_Region.IsInitialized())
                {
                    return false;
                }
                HTuple area = null;
                HTuple row = null;
                HTuple column = null;
                HOperatorSet.AreaCenter(ho_Region, out area, out row, out column);
                if (area == 0)
                {
                    this.mModelID = null;
                    this.mTransContours = null;
                    this.hWindow_Final1.HobjectToHimage(this.image);
                    return false;
                }
                HTuple channels = null;
                HTuple modelID = null;
                HTuple tuple6 = null;
                HTuple tuple7 = null;
                HTuple tuple8 = null;
                HTuple tuple9 = null;
                HOperatorSet.GenEmptyObj(out obj2);
                HOperatorSet.GenEmptyObj(out obj3);
                HOperatorSet.GenEmptyObj(out obj4);
                HOperatorSet.CountChannels(ho_Image, out channels);
                if (new HTuple(channels.TupleEqual(3)) != 0)
                {
                    HObject obj5;
                    HOperatorSet.Rgb1ToGray(ho_Image, out obj5);
                    ho_Image.Dispose();
                    ho_Image = obj5;
                }
                obj2.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, ho_Region, out obj2);
                /*  HOperatorSet.CreateScaledShapeModel(obj2, (int)this.PyramidLevelUpDown.Value, new HTuple((int)this.StartingAngleUpDown.Value).TupleRad(), 
                      new HTuple((int)this.AngleExtentUpDown.Value).TupleRad(), "auto", (int)MinScaleUpDown.Value, (int)MaxScaleUpDown.Value, "auto", this.OptimizationBox.Text, this.MetricBox.Text, 
                      (int)this.ContrastUpDown.Value, (int)this.MinContrastUpDown.Value, out modelID);*/

                HOperatorSet.CreateShapeModel(obj2, (int)this.PyramidLevelUpDown.Value, new HTuple((int)this.StartingAngleUpDown.Value).TupleRad(), new HTuple((int)this.AngleExtentUpDown.Value).TupleRad(), "auto", this.OptimizationBox.Text, this.MetricBox.Text, (int)this.ContrastUpDown.Value, (int)this.MinContrastUpDown.Value, out modelID);
                obj3.Dispose();
                HOperatorSet.GetShapeModelContours(out obj3, modelID, 1);
                HOperatorSet.AreaCenter(ho_Region, out tuple6, out tuple7, out tuple8);
                HOperatorSet.VectorAngleToRigid(0, 0, 0, tuple7, tuple8, 0, out tuple9);
                obj4.Dispose();
                HOperatorSet.AffineTransContourXld(obj3, out obj4, tuple9);
                this.mModelID = modelID;
                this.mTransContours = obj4.Clone();
                this.hWindow_Final1.viewWindow.ClearWindow();
                this.hWindow_Final1.HobjectToHimage(this.image);
                this.hWindow_Final1.DispObj(this.final_region, "blue");
                this.hWindow_Final1.DispObj(this.mTransContours, "green");
                ho_Image.Dispose();
                obj2.Dispose();
                obj3.Dispose();
                obj4.Dispose();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                this.labModelResult.Text = "模板创建成功";
                this.labModelResult.ForeColor = Color.Green;
                return true;
            }
            catch (Exception)
            {
                this.mModelID = null;
                this.mTransContours = null;
                this.hWindow_Final1.viewWindow.ClearWindow();
                this.hWindow_Final1.HobjectToHimage(this.image);
                this.hWindow_Final1.DispObj(this.final_region, "blue");
                this.labModelResult.Text = "模板创建失败";
                this.labModelResult.ForeColor = Color.Red;
                return false;
            }
        }
        private bool selfPic()
        {
            try
            {
                HOperatorSet.GetImageSize(this.image, out this.picWidth, out this.picHeight);
                double num = this.picWidth.D / this.picHeight.D;
                if ((this.iniWidth / this.iniHeight) > num)
                {
                    this.hWindow_Final1.Height = (int)this.iniHeight;
                    this.hWindow_Final1.Width = (int)(this.iniHeight * num);
                }
                else
                {
                    this.hWindow_Final1.Width = (int)this.iniWidth;
                    this.hWindow_Final1.Height = (int)(this.iniWidth / num);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private void processHImage1(HObject Image, ImageType imageType)
        {
            this.hWindow_Final1.HobjectToHimage(Image);
        }
        private void processHImage2(HObject Image, ImageType imageType)
        {
            this.hWindow_Final2.HobjectToHimage(Image);
        }
        #endregion

    }
}

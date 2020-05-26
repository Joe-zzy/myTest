using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

namespace HalconTest
{
    class LocationInterface
    {
        public static bool createCircleMetrology(paramCircle param, ref HTuple handle)
        {
            HTuple index;
            try
            {
                if (null != handle && 0 < handle.Length)
                {
                    HOperatorSet.ClearMetrologyModel(handle);
                }
                HOperatorSet.CreateMetrologyModel(out handle);
                HOperatorSet.AddMetrologyObjectCircleMeasure(handle, param.circleRow, param.circleColumn, param.circleRadius, param.MeasureLenght1, param.MeasureLenght2, 1, param.MeasureThreshold, "measure_transition", param.MeasureTransition, out index);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "num_measures", param.NumMeasures);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "num_instances", param.NumInstances);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "min_score", param.MinScore);

                if (0 < handle.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool findCircle(HObject image, HTuple handle, out HTuple row, out HTuple col, out HTuple radius, out HObject obj,out HObject contours,out HObject ho_Cross)
        {
            row = null;
            col = null;
            radius = null;
            HTuple hv_Row1, hv_Column1;
            HOperatorSet.GenEmptyObj(out obj);
            HOperatorSet.GenEmptyObj(out contours);
            HOperatorSet.GenEmptyObj(out ho_Cross);
            HTuple parameter;
            try
            {
                HOperatorSet.ApplyMetrologyModel(image, handle);
                HOperatorSet.GetMetrologyObjectResult(handle, 0, "all", "result_type", "all_param", out parameter);
                HOperatorSet.GetMetrologyObjectMeasures(out contours, handle, "all", "all", out hv_Row1, out hv_Column1);
                HOperatorSet.GenCrossContourXld(out ho_Cross, hv_Row1, hv_Column1, 6, 0.785398);
                if (3 == parameter.Length)
                {
                    row = parameter[0];
                    col = parameter[1];
                    radius = parameter[2];
                    HOperatorSet.GenCircle(out obj, row, col, radius);
                    return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool createLineMetrology(paramLine param, ref HTuple handle)
        {
            HTuple index;
            try
            {
                HOperatorSet.CreateMetrologyModel(out handle);
                HOperatorSet.AddMetrologyObjectLineMeasure(handle, param.lineRow1, param.lineColumn1, param.lineRow2, param.lineColumn2, param.MeasureLenght1, param.MeasureLenght2, 1, param.MeasureThreshold, "measure_transition",param.MeasureTransition, out index);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "num_measures", param.NumMeasures);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "num_instances", param.NumInstances);
                HOperatorSet.SetMetrologyObjectParam(handle, "all", "min_score", param.MinScore);

                if (0 < handle.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool findLine(HObject image, HTuple handle, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2, out HObject obj,out HObject contours,out HObject ho_Cross, out HTuple phi)
        {
            row1 = null;
            col1 = null;
            row2 = null;
            col2 = null;
            phi = null;
            HTuple hv_Row1, hv_Column1;
            HOperatorSet.GenEmptyObj(out obj);
            HOperatorSet.GenEmptyObj(out contours);
            HOperatorSet.GenEmptyObj(out ho_Cross);

            HTuple parameter;
            try
            {
                HOperatorSet.ApplyMetrologyModel(image, handle);
                HOperatorSet.GetMetrologyObjectResult(handle, 0, "all", "result_type", "all_param", out parameter);
                HOperatorSet.GetMetrologyObjectMeasures(out contours, handle, "all", "all", out hv_Row1, out hv_Column1);
                HOperatorSet.GenCrossContourXld(out ho_Cross, hv_Row1, hv_Column1, 6, 0.785398);
                if (4 == parameter.Length)
                {
                    row1 = parameter[0];
                    col1 = parameter[1];
                    row2 = parameter[2];
                    col2 = parameter[3];
                    HOperatorSet.GenContourPolygonXld(out obj, row1.TupleConcat(row2), col1.TupleConcat(col2));
                    HOperatorSet.LineOrientation(row1, col1, row2, col2, out phi);
                    HOperatorSet.ClearMetrologyObject(handle, "all");
                    HOperatorSet.ClearMetrologyModel(handle);

                    return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }

            return false;
        }

        public static bool lineIntersection(HTuple row11, HTuple col11, HTuple row12, HTuple col12, HTuple row21, HTuple col21, HTuple row22, HTuple col22, out HTuple row, out HTuple col)
        {
            HTuple isParallel;
            row = null;
            col = null;
            try
            {
                HOperatorSet.IntersectionLl(row11, col11, row12, col12, row21, col21, row22, col22, out row, out col, out isParallel);
                if (null != row && 0 < row.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool createModel(HWindow hv_WindowHandle, HObject image, string modelType, HTuple minContrast, HTuple startAngle, HTuple endAngle, out HTuple modelID)
        {
            // Local iconic variables 

            HObject  ho_Rectangle, ho_ImageReduced, ho_ContoursAffinTrans;

            // Local control variables 

            HTuple hv_Width = new HTuple(), hv_Height = new HTuple(),hv_Row1 = new HTuple();
            HTuple hv_Column1 = new HTuple(), hv_Row2 = new HTuple();
            HTuple hv_Column2 = new HTuple(), hv_ModelArea = new HTuple();
            HTuple hv_ModelRow = new HTuple(), hv_ModelColumn = new HTuple();
            HTuple hv_ModelType = new HTuple(), hv_NumLevels = new HTuple();
            HTuple hv_Contrast = new HTuple(), hv_ModelID = new HTuple();
            HTuple hv_AngleStart = new HTuple(), hv_AngleExtent = new HTuple();
            HTuple hv_AngleStep = new HTuple(), hv_ScaleMin = new HTuple();
            HTuple hv_ScaleMax = new HTuple(), hv_ScaleStep = new HTuple();
            HTuple hv_Metric = new HTuple(), hv_MinContrast = new HTuple();
            HTuple hv_HomMat2D = new HTuple();

            modelID = null;
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_ImageReduced);
            HOperatorSet.GenEmptyObj(out ho_ContoursAffinTrans);
            HObject ModelContours;
            try
            {
                hv_Row1.Dispose(); hv_Column1.Dispose(); hv_Row2.Dispose(); hv_Column2.Dispose();
                HOperatorSet.SetColor(hv_WindowHandle, "red");
                HOperatorSet.DrawRectangle1(hv_WindowHandle, out hv_Row1, out hv_Column1, out hv_Row2, out hv_Column2);
                ho_Rectangle.Dispose();
                HOperatorSet.GenRectangle1(out ho_Rectangle, hv_Row1, hv_Column1, hv_Row2, hv_Column2);
                //获取ROI重心坐标
                hv_ModelArea.Dispose(); hv_ModelRow.Dispose(); hv_ModelColumn.Dispose();
                HOperatorSet.AreaCenter(ho_Rectangle, out hv_ModelArea, out hv_ModelRow, out hv_ModelColumn);
                //分割
                ho_ImageReduced.Dispose();
                HOperatorSet.ReduceDomain(image, ho_Rectangle, out ho_ImageReduced);

                if ("形状模板" == modelType)
                {
                    HOperatorSet.CreateShapeModel(ho_ImageReduced, "auto", startAngle, endAngle - startAngle, "auto", "auto", "use_polarity", "auto", minContrast, out modelID);
                    HOperatorSet.GetShapeModelContours(out ModelContours,modelID,1);
                    hv_HomMat2D.Dispose();
                    HOperatorSet.VectorAngleToRigid(0, 0, 0, hv_ModelRow, hv_ModelColumn, 0, out hv_HomMat2D);
                    ho_ContoursAffinTrans.Dispose();
                    HOperatorSet.AffineTransContourXld(ModelContours, out ho_ContoursAffinTrans,hv_HomMat2D);
                    hv_WindowHandle.DispObj(ModelContours);

                }
                else if ("灰度模板" == modelType)
                {

                }

                if (null != modelID)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool modelMatch(HObject image, HTuple modelID, string modelType, HTuple startAngle, HTuple endAngle, HTuple minScore, HTuple matchNum, out HObject modelContour, out HTuple row, out HTuple col, out HTuple angle, out HTuple score)
        {
            HOperatorSet.GenEmptyObj(out modelContour);
            row = null;
            col = null;
            angle = null;
            score = null;

            try
            {
                if ("形状模板" == modelType)
                {
                    HOperatorSet.FindShapeModel(image, modelID, startAngle, endAngle, minScore, matchNum, 0.5, "least_squares", 0, 0.9, out row, out col, out angle, out score);
                    if (0 < row.Length)
                    {
                        HObject contour, contourAffined;
                       // HOperatorSet.GenEmptyObj(out contour);
                        HTuple homMat2D;
                        HOperatorSet.GetShapeModelContours(out contour, modelID, 1);
                        //HOperatorSet.VectorAngleToRigid(0, 0, 0, row[0], col[0], angle[0], out homMat2D);
                        //HOperatorSet.AffineTransContourXld(contour, out modelContour, homMat2D);
                        for (int i = 0; i < row.Length; i++)
                        {
                          //  HTuple homMat2D;
                            HOperatorSet.VectorAngleToRigid(0, 0, 0, row[i], col[i], angle[i], out homMat2D);
                            HOperatorSet.AffineTransContourXld(contour, out contourAffined, homMat2D);
                            HOperatorSet.ConcatObj(modelContour, contourAffined, out modelContour);
                        }
                    }

                }
                else if ("灰度模板" == modelType)
                {

                }

                if (matchNum == score.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }
        public  static bool affineTransPixel(HTuple initialRow,HTuple initialCol, HTuple checkRow, HTuple checkCol, HTuple checkPhi, out HTuple tansRow,out  HTuple transCol)
        {
            try
            {
                HTuple hv_HomMat2D = new HTuple();
                HTuple homMat2DTranslate = new HTuple();
                HTuple homMat2DRotate = new HTuple();
                hv_HomMat2D.Dispose();
                homMat2DTranslate.Dispose();
                homMat2DRotate.Dispose();
                HOperatorSet.HomMat2dIdentity(out hv_HomMat2D);
                HOperatorSet.HomMat2dTranslate(hv_HomMat2D, checkRow, checkCol, out homMat2DTranslate);
                HOperatorSet.HomMat2dRotate(homMat2DTranslate, checkPhi, checkRow, checkCol, out homMat2DRotate);
                HOperatorSet.AffineTransPixel(homMat2DRotate, initialRow, initialCol, out tansRow, out transCol);
                return true;
            }
            catch (Exception)
            {
                tansRow = -1;
                transCol = -1;
                return false;
            }           
            
        }
        public static bool clearMetrology(HTuple handle)
        {
            try
            {
                if (null != handle && 0 < handle.Length)
                {
                    HOperatorSet.ClearMetrologyModel(handle);
                }
                return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }
        public bool FitCircle(double[] X, double[] Y, out double RcX, out double RcY, out double R)
        {
            try
            {
                HTuple hTuple = new HTuple();
                HTuple hTuple2 = new HTuple();
                int num = 0;
                for (num = 0; num < X.Length; num++)
                {
                    if ((X[num] > 0.0) & (Y[num] > 0.0))//获得寻找到的模板中心装入hTuple2与hTuple
                    {
                        hTuple2.TupleConcat(X[num]);
                        hTuple.TupleConcat(Y[num]);
                    }
                }
                HObject contour;
                HOperatorSet.GenContourPolygonXld(out contour, hTuple, hTuple2);//使用模板中心生成多边形XLD轮廓
                HTuple row, column, radius, StartPhi, EndPhi, pointOrder;
                HOperatorSet.FitCircleContourXld(contour, "algebraic", -1, 0, 0, 3, 2, out row, out column, out radius, out StartPhi, out EndPhi, out pointOrder);//拟合圆形
                                                                                                                                                                  //得出结果
                RcY = row;
                RcX = column;
                R = radius;

                contour.Dispose();
                return true;
            }
            catch
            {
                RcY = -1.0;
                RcX = -1.0;
                R = -1.0;
                return false;
            }
        }
    }
}

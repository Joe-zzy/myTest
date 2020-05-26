using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

namespace HalconTest
{
    class CalibrationInterface
    {
        public static bool calculateAffineMatrix(HTuple col, HTuple row, HTuple x, HTuple y, out HTuple matrix, out HTuple err)
        {
            matrix = null;
            err = null;
            HTuple qx, qy;
            HTuple distance, mean, max, min;

            try
            {
                HOperatorSet.VectorToHomMat2d(col, row, x, y, out matrix);

                if (null != matrix && 0 < matrix.Length)
                {
                    HOperatorSet.AffineTransPoint2d(matrix, col, row, out qx, out qy);
                    HOperatorSet.DistancePp(y, x, qy, qx, out distance);
                    HOperatorSet.TupleAbs(distance, out distance);
                    HOperatorSet.TupleMean(distance, out mean);
                    HOperatorSet.TupleMax(distance, out max);
                    HOperatorSet.TupleMin(distance, out min);
                    err = new HTuple();
                    err[0] = mean;
                    err[1] = max;
                    err[2] = min;

                    return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool readAffineMatrix(string filePath, out HTuple matrix, out HTuple col, out HTuple row, out HTuple x, out HTuple y)
        {
            matrix = null;
            col = null;
            row = null;
            x = null;
            y = null;

            try
            {
                if (null != filePath)
                {
                    HOperatorSet.ReadTuple(filePath, out matrix);
                    string filePath2 = filePath.Substring(0, filePath.Length - 4);
                    HOperatorSet.ReadTuple(filePath2 + "_col.tup", out col);
                    HOperatorSet.ReadTuple(filePath2 + "_row.tup", out row);
                    HOperatorSet.ReadTuple(filePath2 + "_x.tup", out x);
                    HOperatorSet.ReadTuple(filePath2 + "_y.tup", out y);
                    if (null != matrix && 0 < matrix.Length)
                        return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool writeAffineMatrix(string filePath, HTuple matrix, HTuple col, HTuple row, HTuple x, HTuple y)
        {
            try
            {
                if (null != filePath)
                {
                    HOperatorSet.WriteTuple(matrix, filePath);
                    string filePath2 = filePath.Substring(0, filePath.Length - 4);
                    HOperatorSet.WriteTuple(col, filePath2 + "_col.tup");
                    HOperatorSet.WriteTuple(row, filePath2 + "_row.tup");
                    HOperatorSet.WriteTuple(x, filePath2 + "_x.tup");
                    HOperatorSet.WriteTuple(y, filePath2 + "_y.tup");
                    return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool fitCircle(HTuple rows, HTuple cols, out HTuple rowCenter, out HTuple colCenter)
        {
            rowCenter = null;
            colCenter = null;
            HObject contour;
            HTuple radius, startPhi, endPhi, pointOrder;

            try
            {
                HOperatorSet.GenContourPolygonXld(out contour, rows, cols);
                HOperatorSet.FitCircleContourXld(contour, "algebraic", -1, 0, 0, 3, 2, out rowCenter, out colCenter, out radius, out startPhi, out endPhi, out pointOrder);
                if (null != rowCenter && 0 < rowCenter.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool writeRotateCenter(string filePath, HTuple cols, HTuple rows, HTuple colCenter, HTuple rowCenter)
        {
            try
            {
                if (null != filePath)
                {
                    HTuple center = new HTuple();
                    center[0] = colCenter;
                    center[1] = rowCenter;
                    HOperatorSet.WriteTuple(center, filePath);
                    string filePath2 = filePath.Substring(0, filePath.Length - 4);
                    HOperatorSet.WriteTuple(cols, filePath2 + "_col");
                    HOperatorSet.WriteTuple(rows, filePath2 + "_row");
                    return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool readRotateCenter(string filePath, out HTuple cols, out HTuple rows, out HTuple colCenter, out HTuple rowCenter)
        {
            cols = null;
            rows = null;
            colCenter = null;
            rowCenter = null;
            HTuple rotateCenter;

            try
            {
                if (null != filePath)
                {
                    HOperatorSet.ReadTuple(filePath, out rotateCenter);
                    if (null != rotateCenter && 2 == rotateCenter.Length)
                    {
                        colCenter = rotateCenter[0];
                        rowCenter = rotateCenter[1];
                    }
                    string filePath2 = filePath.Substring(0, filePath.Length - 4);
                    HOperatorSet.ReadTuple(filePath2 + "_col", out cols);
                    HOperatorSet.ReadTuple(filePath2 + "_row", out rows);
                    if (null != rotateCenter && 0 < rotateCenter.Length)
                        return true;
                }
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }

        public static bool affineTransPoint(HTuple matrix, HTuple x, HTuple y, out HTuple xTrans, out HTuple yTrans)
        {
            xTrans = null;
            yTrans = null;

            try
            {
                HOperatorSet.AffineTransPoint2d(matrix, x, y, out xTrans, out yTrans);
                if (null != xTrans && 0 < xTrans.Length)
                    return true;
            }
            catch (HalconException ex)
            {
                string msg = ex.GetErrorMessage();
            }
            return false;
        }
        /// <summary>
        /// 先转换，再计算旋转
        /// </summary>
        /// <param name="ptX">标定转换后的坐标</param>
        /// <param name="ptY">标定转换后的坐标</param>
        /// <param name="cenX">旋转中心</param>
        /// <param name="cenY">旋转中心</param>
        /// <param name="rotAng">相对旋转角度</param>
        /// <param name="targetX">目标点坐标</param>
        /// <param name="targetY">目标点坐标</param>
        /// <returns></returns>
        public static bool RotWithCenter(HTuple ptX, HTuple ptY, HTuple cenX, HTuple cenY, double rotAng,out HTuple targetX,out HTuple targetY)
        {

            targetX = null;
            targetY = null;
            double angDiff = (rotAng) * 3.14159265358979323846f / 180.0;
            try
            {
                double diffx = ptX - cenX;
                double diffy = ptY - cenY;
                double cosang = Math.Cos(angDiff);
                double sinang = Math.Sin(angDiff);

                targetX = diffx * cosang - diffy * sinang + cenX;
                targetY = diffx * sinang + diffy * cosang + cenY;

                return true;
            }
            catch (HalconException ex)
            {

                string msg = ex.GetErrorMessage();
            }
            return false;
           
        }
    }
}

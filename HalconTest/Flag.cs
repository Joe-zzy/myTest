using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HalconTest
{
   /// <summary>
   /// 错误信息类
   /// </summary>
   public struct Error
   {
       public bool flag;        //false:NG；true:OK
       public string errorInfo; //显示结果信息
   }

   /// <summary>
   /// 显示信息类
   /// </summary>
   public class Message_Show
   {
       public string type;
       public double coordX;
       public double coordY;
       public double angle;
       public string message;

       public Message_Show()
       {
           type = "";
           coordX = 0.0;
           coordY = 0.0;
           angle = 0.0;
           message = "";
       }

       /// <summary>
       /// 信息显示
       /// </summary>
       /// <param name="station"></param>
       /// <param name="x"></param>
       /// <param name="y"></param>
       /// <param name="r"></param>
       /// <param name="mess"></param>
       public Message_Show(string station, double x, double y, double r, string mess)
       {
           type = station;
           coordX = x;
           coordY = y;
           angle = r;
           message = mess;
       }

       /// <summary>
       /// 更新信息
       /// </summary>
       /// <param name="station"></param>
       /// <param name="x"></param>
       /// <param name="y"></param>
       /// <param name="r"></param>
       /// <param name="mess"></param>
       public void FlushMessage(string station, double x, double y, double r, string mess)
       {
           type = station;
           coordX = x;
           coordY = y;
           angle = r;
           message = mess;
       }
   }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CYQ.Data;
namespace Log_SysLog_Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            ExeLog();
            ExeSysLog();
            Console.Read();
        }

        static void ExeLog()
        {
            AppConfig.Log.IsWriteLog = true;
            AppConfig.Log.LogPath = "自定义错误日志";
            Log.WriteLogToTxt("这是错误信息");
            Log.WriteLogToTxt("这是错误信息", LogType.Assert);
            Log.WriteLogToTxt("这是错误信息", LogType.Debug);
            Log.WriteLogToTxt("这是错误信息", LogType.Error);
            Log.WriteLogToTxt("这是错误信息", LogType.Info);
            Log.WriteLogToTxt("这是错误信息", LogType.Warn);
            Console.WriteLine("请查看Debug目录");
        }

        static void ExeSysLog()
        {
            AppConfig.Log.LogConn = "txt path={0}txtdb";//演示只有用文本数据库来演示了
            AppConfig.Log.LogTableName = "MyLogs";//可以更改表名
            using (SysLogs sl=new SysLogs())//往数据库里写一条错误日志
            {
                sl.Message = "这是错误信息";
                sl.PageUrl = AppDomain.CurrentDomain.BaseDirectory;
                sl.UserName = "cyq";
                sl.LogType = "Sys";
                sl.Insert();
                List<SysLogs> list = sl.Select<SysLogs>();
                Console.WriteLine("现在的有:" + list.Count + "条数据");
            }

            // 
            Log.WriteLogToDB("呵呵", LogType.Error, "cyq");//和使用sysLogs一样。
        }
        
    }
}

using CYQ.Data;
using System;
using System.Diagnostics;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 控制台输出
    /// </summary>
    internal class DTSConsole
    {
        public static void WriteLine(string msg)
        {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }

        public static void WriteDebugLine(string msg)
        {
            if(AppConfig.IsDebugMode || (DTSConfig.Client.IsEnable && DTSConfig.Client.IsPrintTraceLog) || (DTSConfig.Server.IsEnable && DTSConfig.Server.IsPrintTraceLog))
            {
                WriteLine(msg);
            }
        }
    }
}

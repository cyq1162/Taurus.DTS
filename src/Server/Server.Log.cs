using CYQ.Data;
using System;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        /// <summary>
        /// 分布式事务 提供端
        /// </summary>
        public static partial class Server
        {
            internal static partial class Log
            {
                public static void Print(string msg)
                {
                    if(DTSConfig.Server.IsPrintTraceLog)
                    {
                        CYQ.Data.Log.Write(msg, "DTS.Server.TraceLog");
                    }
                }
                public static void Error(Exception err)
                {
                    CYQ.Data.Log.Write(err, "DTS.Server.Error");
                }
            }
        }
    }
}

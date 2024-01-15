using System;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            internal static partial class Log
            {
                public static void Print(string msg)
                {
                    if(DTSConfig.Client.IsPrintTraceLog)
                    {
                        CYQ.Data.Log.Write(msg, "DTS.Client.TraceLog");
                    }
                }
                public static void Error(Exception err)
                {
                    CYQ.Data.Log.Write(err, "DTS.Client.Error");
                }
            }
        }
    }
}

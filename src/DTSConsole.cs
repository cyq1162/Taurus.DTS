using System;
using System.Diagnostics;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 控制台输出
    /// </summary>
    internal class DTSConsole
    {
        public static void WriteDebugLine(string msg)
        {
            Console.WriteLine(msg);
            Debug.WriteLine(msg);
        }
    }
}

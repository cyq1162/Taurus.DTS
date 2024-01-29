using CYQ.Data;
using System;
using System.Diagnostics;


namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// DTS 分布式任务协调器，Distributed Task Scheduler
    /// </summary>
    public partial class DTS
    {
        private static string _Version;
        /// <summary>
        /// 获取当前 Taurus 版本号
        /// </summary>
        internal static string Version
        {
            get
            {
                if (_Version == null)
                {
                    _Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                return _Version;
            }
        }
        private static int _ProcessID;
        /// <summary>
        /// 当前进程ID
        /// </summary>
        public static int ProcessID
        {
            get
            {
                if (_ProcessID == 0)
                {
                    _ProcessID = Process.GetCurrentProcess().Id;
                }
                return _ProcessID;
            }
        }
        public static partial class Client
        {
            /// <summary>
            /// 启动定时描述，并监听默认队列。
            /// </summary>
            public static void Start()
            {
                if (DTSConfig.Client.IsEnable)
                {
                    DTSConsole.WriteDebugLine("--------------------------------------------------");
                    DTSConsole.WriteDebugLine("DTS.Client.Start = true , Version = " + Version);
                    DTSConsole.WriteDebugLine("--------------------------------------------------");
                    DTS.Client.Worker.Scanner.Start();// 扫描数据库，重发任务
                    DTS.Client.CronWorker.Scanner.Start();//启动Cron表达式的定时任务扫描。
                }
            }
        }
        public static partial class Server
        { /// <summary>
          /// 启动定时描述，并监听默认队列。
          /// </summary>
            public static void Start()
            {
                if (DTSConfig.Server.IsEnable)
                {
                    DTSConsole.WriteDebugLine("--------------------------------------------------");
                    DTSConsole.WriteDebugLine("DTS.Server.Start = true , Version = " + Version);
                    DTSConsole.WriteDebugLine("--------------------------------------------------");
                    DTS.Server.Worker.Scanner.Start();//启动队列监听，同时清理过期数据。
                }
            }
        }

        /// <summary>
        /// 同时启动客户端和服务端定时扫描程序。
        /// </summary>
        public static void Start()
        {
            Client.Start();
            Server.Start();
        }
    }

}

using CYQ.Data;
using System;
using System.Diagnostics;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTSConfig
    {

        /// <summary>
        /// 客户端【调用端】配置项
        /// </summary>
        public static partial class Client
        {

            /// <summary>
            /// RabbitMQ相关配置项
            /// </summary>
            internal static class MQ
            {

                /// <summary>
                /// 项目交换机：绑定所有 Client 项目队列
                /// </summary>
                internal static string ProjectExChange
                {
                    get
                    {
                        return "DTS_Client_Project";
                    }
                }

                /// <summary>
                /// 任务队列：持久化，以项目为单位。
                /// </summary>
                internal static string ProjectQueue
                {
                    get
                    {
                        return "DTS_Client_" + ProjectName;
                    }
                }

                /// <summary>
                /// 进程交换机：绑定所有 Client 进程队列
                /// </summary>
                internal static string ProcessExChange
                {
                    get
                    {
                        return "DTS_Client_Process";
                    }
                }

                /// <summary>
                /// 进程队列：排它化，进程关则队列删，以进程为单位。
                /// </summary>
                internal static string ProcessQueue
                {
                    get
                    {
                        return "DTS_Client_" + pidName;
                    }
                }

                /// <summary>
                /// 延时队列名。
                /// </summary>
                internal static string DelayQueue
                {
                    get
                    {
                        return "DTS_Client_" + ProjectName + "_Delay_Minute";
                    }
                }
            }

        }
    }
}

using CYQ.Data;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTSConfig
    {
        /// <summary>
        /// 服务端【接口提供端】配置项
        /// </summary>
        public static partial class Server
        {
            /// <summary>
            /// MQ相关配置项
            /// </summary>
            internal static class MQ
            {
                #region Rabbit
                /// <summary>
                /// 项目交换机：绑定所有 Server 项目队列
                /// </summary>
                internal static string ProjectExChange
                {
                    get
                    {
                        return "DTS_Server_Proj";
                    }
                }

                /// <summary>
                /// 任务队列：持久化，以项目为单位。
                /// </summary>
                internal static string ProjectQueue
                {
                    get
                    {
                        return "DTS_Server_" + ProjectName;
                    }
                }

                /// <summary>
                /// 进程交换机：绑定所有 Server 进程队列
                /// </summary>
                internal static string ProcessExChange
                {
                    get
                    {
                        return "DTS_Server_Proc";
                    }
                }

                /// <summary>
                ///进程队列：排它化，进程关则队列删，以进程为单位。
                /// </summary>
                internal static string ProcessQueue
                {
                    get
                    {
                        return "DTS_Server_" + pidName;
                    }
                }

                #endregion

                #region Kafka
                /// <summary>
                /// 项目主题：以项目为单位，竞争消费。
                /// </summary>
                internal static string ProjectTopic
                {
                    get
                    {
                        return ProjectExChange + "_" + ProjectName;
                    }
                }

                /// <summary>
                /// 项目组：以项目为单位，竞争消费。
                /// </summary>
                internal static string ProjectGroup
                {
                    get
                    {
                        return ProjectTopic;
                    }
                }


                /// <summary>
                /// 进程主题：以进程为单位，从 AutoOffsetReset.Latest 开始读取数据。
                /// </summary>
                internal static string ProcessTopic
                {
                    get
                    {
                        return ProcessExChange + "_" + ProjectName;
                    }
                }

                /// <summary>
                /// 进程主题：以进程为单位，从 AutoOffsetReset.Earliest 开始读取数据。
                /// </summary>
                internal static string ProcessGroup
                {
                    get
                    {
                        return ProcessExChange + "_" + pidName;
                    }
                }

                #endregion
            }

        }
    }
}

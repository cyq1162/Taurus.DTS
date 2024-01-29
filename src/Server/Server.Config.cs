﻿using CYQ.Data;

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
            /// 配置是否启用 服务端【接口提供端】
            /// 如 DTS.Server.IsEnable ：true， 默认值：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    return AppConfig.GetAppBool("DTS.Server.IsEnable", true);
                }
                set
                {
                    AppConfig.SetApp("DTS.Server.IsEnable", value.ToString());
                }
            }

            /// <summary>
            /// 配置是否 打印追踪日志，用于调试
            /// 如 DTS.Client.IsPrintTraceLog ：true， 默认值：true
            /// </summary>
            public static bool IsPrintTraceLog
            {
                get
                {
                    return AppConfig.GetAppBool("DTS.Server.IsPrintTraceLog", false);
                }
                set
                {
                    AppConfig.SetApp("DTS.Server.IsPrintTraceLog", value.ToString());
                }
            }

            /// <summary>
            /// 数据库 - 数据库链接配置
            /// 配置项：DTS.Server.Conn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    return AppConfig.GetConn("DTS.Server.Conn");
                }
                set
                {
                    AppConfig.SetConn("DTS.Server.Conn", value);
                }
            }

            /// <summary>
            /// 数据库 - Task 表
            /// 配置项：DTS.Server.TaskTable ：DTS_Server_Task
            /// </summary>
            public static string TaskTable
            {
                get
                {
                    return AppConfig.GetApp("DTS.Server.TaskTable", "DTS_Server_Task");
                }
                set
                {
                    AppConfig.SetApp("DTS.Server.TaskTable", value);
                }
            }
            /// <summary>
            /// RabbitMQ - 链接配置
            /// 配置项：DTS.Server.Rabbit=127.0.0.1;guest;guest;/
            /// </summary>
            public static string Rabbit
            {
                get
                {
                    return AppConfig.GetApp("DTS.Server.Rabbit");
                }
                set
                {
                    AppConfig.SetApp("DTS.Server.Rabbit", value);
                }
            }
            /// <summary>
            /// Kafka - 链接配置
            /// 配置项：DTS.Server.Kafka=127.0.0.1:9092
            /// </summary>
            public static string Kafka
            {
                get
                {
                    return AppConfig.GetApp("DTS.Server.Kafka");
                }
                set
                {
                    AppConfig.SetApp("DTS.Server.Kafka", value);
                }
            }

            /// <summary>
            /// 工作线程处理模式
            /// </summary>
            public static class Worker
            {
                /// <summary>
                /// 数据已处理完成，但未收后客户端确认时：向客户端发起重新确认以便删除数据的间隔时间：单位（秒），默认10分钟。
                /// 配置项：DTS.Server.RetryIntervalSecond ：10 * 60
                /// </summary>
                public static int RetryIntervalSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Server.RetryIntervalSecond", 10 * 60);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Server.RetryIntervalSecond", ((int)value).ToString());
                    }
                }

                /// <summary>
                /// 数据确认完成后：0 删除（默认）、1 转移到历史表
                /// 配置项：DTS.Server.ConfirmClearMode ：0
                /// </summary>
                public static TaskClearMode ConfirmClearMode
                {
                    get
                    {
                        return (TaskClearMode)AppConfig.GetAppInt("DTS.Server.ConfirmClearMode", (int)TaskClearMode.Delete);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Server.ConfirmClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 数据超时后（即未处理或处理完未收到客户端确认时）保留时间：（单位秒），默认7天。
                /// 配置项：DTS.Server.TimeoutKeepSecond ：7 * 24 * 3600
                /// </summary>
                public static int TimeoutKeepSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Server.TimeoutKeepSecond", 7 * 24 * 3600);//7 * 24 * 3600
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Server.TimeoutKeepSecond", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 数据超时后：0删除、1转移到历史表（默认）
                /// 配置项：DTS.Server.TimeoutClearMode ：1
                /// </summary>
                public static TaskClearMode TimeoutClearMode
                {
                    get
                    {
                        return (TaskClearMode)AppConfig.GetAppInt("DTS.Server.TimeoutClearMode", (int)TaskClearMode.MoveToHistoryTable);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Server.TimeoutClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 向客户端发起重新确认以便删除数据的最大重试次数，默认7次。
                /// 配置项：DTS.Server.MaxRetries ：7
                /// </summary>
                public static int MaxRetries
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Server.MaxRetries", 7);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Server.MaxRetries", ((int)value).ToString());
                    }
                }
            }

        }
    }
}

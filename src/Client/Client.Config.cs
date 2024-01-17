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
            /// 配置是否启用 客户端【调用端】
            /// 如 DTS.Client.IsEnable ：true， 默认值：true
            /// </summary>
            public static bool IsEnable
            {
                get
                {
                    return AppConfig.GetAppBool("DTS.Client.IsEnable", true);
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.IsEnable", value.ToString());
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
                    return AppConfig.GetAppBool("DTS.Client.IsPrintTraceLog", AppConfig.IsDebugMode);
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.IsPrintTraceLog", value.ToString());
                }
            }
            /// <summary>
            /// 数据库 - 数据库链接配置
            /// 配置项：DTS.Client.Conn：server=.;database=x;uid=s;pwd=p;
            /// </summary>
            public static string Conn
            {
                get
                {
                    return AppConfig.GetConn("DTS.Client.Conn");
                }
                set
                {
                    AppConfig.SetConn("DTS.Client.Conn", value);
                }
            }

            /// <summary>
            /// 数据库 - Task 表
            /// 配置项：DTS.Client.Table ：DTS_Client
            /// </summary>
            public static string TaskTable
            {
                get
                {
                    return AppConfig.GetApp("DTS.Client.TaskTable", "DTS_Client_Task");
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.TaskTable", value);
                }
            }
            /// <summary>
            /// 数据库 - Cron 表
            /// 配置项：DTS.Client.CronTable ：DTS_Client_Cron
            /// </summary>
            public static string CronTable
            {
                get
                {
                    return AppConfig.GetApp("DTS.Client.CronTable", "DTS_Client_Cron");
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.CronTable", value);
                }
            }
            /// <summary>
            /// RabbitMQ - 链接配置
            /// 配置项：DTS.Client.Rabbit=127.0.0.1;guest;guest;/
            /// </summary>
            public static string Rabbit
            {
                get
                {
                    return AppConfig.GetApp("DTS.Client.Rabbit");
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.Rabbit", value);
                }
            }
            /// <summary>
            /// Kafka - 链接配置
            /// 配置项：DTS.Client.Kafka=127.0.0.1:9092
            /// </summary>
            public static string Kafka
            {
                get
                {
                    return AppConfig.GetApp("DTS.Client.Kafka");
                }
                set
                {
                    AppConfig.SetApp("DTS.Client.Kafka", value);
                }
            }

            /// <summary>
            /// 工作线程处理模式
            /// </summary>
            public static class Worker
            {
                /// <summary>
                /// 任务发送后，未收到服务端响应时，间隔多久重新发起任务：单位（秒），默认5分钟
                /// 配置项：DTS.Client.RetryIntervalSecond ：5 * 60
                /// </summary>
                public static int RetryIntervalSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Client.RetryIntervalSecond", 5 * 60);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Client.RetryIntervalSecond", ((int)value).ToString());
                    }
                }

                /// <summary>
                /// 任务完成，收到确认后：0 删除（默认）、1 转移到历史表
                /// 配置项：DTS.Client.ConfirmClearMode ：0
                /// </summary>
                public static TaskClearMode ConfirmClearMode
                {
                    get
                    {
                        return (TaskClearMode)AppConfig.GetAppInt("DTS.Client.ConfirmClearMode", (int)TaskClearMode.Delete);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Client.ConfirmClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 任务发起后，未收到确认时，数据保留时间：（单位秒），默认7天
                /// 配置项：DTS.Client.TimeoutKeepSecond ：7 * 24 * 3600
                /// </summary>
                public static int TimeoutKeepSecond
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Client.TimeoutKeepSecond", 3 * 24 * 3600);//7 * 24 * 3600
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Client.TimeoutKeepSecond", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 任务发起后，未收到确认，超时后数据：0 删除、1 转移到历史表（默认）
                /// 配置项：DTS.Client.TimeoutClearMode ：1
                /// </summary>
                public static TaskClearMode TimeoutClearMode
                {
                    get
                    {
                        return (TaskClearMode)AppConfig.GetAppInt("DTS.Client.TimeoutClearMode", (int)TaskClearMode.MoveToHistoryTable);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Client.TimeoutClearMode", ((int)value).ToString());
                    }
                }
                /// <summary>
                /// 任务发起后，未收到确认时，最大重试次数。
                /// 配置项：DTS.Client.TimeoutClearMode ：50
                /// </summary>
                public static int MaxRetries
                {
                    get
                    {
                        return AppConfig.GetAppInt("DTS.Client.MaxRetries", 50);
                    }
                    set
                    {
                        AppConfig.SetApp("DTS.Client.MaxRetries", ((int)value).ToString());
                    }
                }
            }
        }
    }
}

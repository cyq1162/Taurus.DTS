using CYQ.Data.Json;
using System;
using System.IO;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        /// <summary>
        /// 分布式任务 调用端
        /// </summary>
        public static partial class Client
        {
            /// <summary>
            /// 即时任务，相同 ProjectName 竞争同一个信息。
            /// </summary>
            public static class Instant
            {
                /// <summary>
                /// 发起一个任务消息。
                /// </summary>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                public static bool PublishAsync(string content, string taskKey)
                {
                    return ExeTaskAsync(TaskType.Instant, content, taskKey, null, 0, null, BroadcastType.None);
                }

                /// <summary>
                /// 发起一个任务消息。
                /// </summary>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTSClientCallBack 特性标注</param>
                public static bool PublishAsync(string content, string taskKey, string callBackKey)
                {
                    return ExeTaskAsync(TaskType.Instant, content, taskKey, callBackKey, 0, null, BroadcastType.None);
                }

                /// <summary>
                /// 内部使用：由 Cron 产生的即时任务。
                /// </summary>
                /// <returns></returns>
                internal static bool PublishAsync(TaskType taskType, string content, string taskKey, string callBackKey, string recurringID)
                {
                    return ExeTaskAsync(taskType, content, taskKey, callBackKey, 0, recurringID, BroadcastType.None);
                }
            }

            /// <summary>
            /// 延时任务，相同 ProjectName 竞争同一个信息。
            /// </summary>
            public static class Delay
            {
                /// <summary>
                /// 发起一个延时任务。
                /// </summary>
                /// <param name="delayMinutes">延时分钟数</param>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                public static bool PublishAsync(int delayMinutes, string content, string taskKey)
                {
                    return ExeTaskAsync(TaskType.Delay, content, taskKey, null, delayMinutes, null, BroadcastType.None);
                }
                /// <summary>
                /// 发起一个延时任务。
                /// </summary>
                ///  <param name="delayMinutes">延时分钟数</param>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTSClientCallBack 特性标注</param>
                public static bool PublishAsync(int delayMinutes, string content, string taskKey, string callBackKey)
                {
                    if (MQ.Client.MQType == MQType.Rabbit)
                    {
                        return ExeTaskAsync(TaskType.Delay, content, taskKey, callBackKey, delayMinutes, null, BroadcastType.None);
                    }
                    //生成 Cron 表达式，走定时任务
                    string cron = CronHelper.GetCron(DateTime.Now.AddMinutes(delayMinutes));
                    return Cron.PublishAsync(cron, content, taskKey, callBackKey, true);
                }
            }


            /// <summary>
            /// 基于 Cron 表达式的重复性任务，相同 ProjectName 竞争同一个信息。
            /// </summary>
            public static class Cron
            {
                /// <summary>
                /// 发起一个重复性任务，相同 ProjectName 竞争同一个信息。
                /// </summary>
                /// <param name="cron">cron 表达式</param>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                public static bool PublishAsync(string cron, string content, string taskKey)
                {
                    return PublishAsync(cron, content, taskKey, null, false);
                }
                /// <summary>
                /// 发起一个重复性任务，相同 ProjectName 竞争同一个信息。
                /// </summary>
                ///  <param name="cron">cron 表达式</param>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTSClientCallBack 特性标注</param>
                public static bool PublishAsync(string cron, string content, string taskKey, string callBackKey)
                {
                    return PublishAsync(cron, content, taskKey, callBackKey, false);
                }
                internal static bool PublishAsync(string cron, string content, string taskKey, string callBackKey, bool isDelayTask)
                {
                    if (CronHelper.GetNextDateTime(cron) == null)
                    {
                        throw new Exception("Invalid cron expression.");
                    }
                    CronTable table = new CronTable();
                    table.Cron = cron;
                    table.Content = content;
                    table.TaskKey = taskKey;
                    table.CallBackKey = callBackKey;
                    table.IsDelayTask = isDelayTask;
                    table.CreateTime = DateTime.Now;
                    table.EditTime = DateTime.Now;

                    return CronWorker.Add(table);
                }
                /// <summary>
                /// 删除 Cron 重复性任务
                /// </summary>
                /// <param name="taskKey">任务key</param>
                /// <returns></returns>
                public static bool DeleteAsync(string taskKey)
                {
                    return DeleteAsync(taskKey, null);
                }
                /// <summary>
                /// 删除 Cron 重复性任务
                /// </summary>
                /// <param name="taskKey">任务key</param>
                /// <param name="cron">cron 表达式，如果存在多个相同的taskKey，可以增加此条件搜索</param>
                /// <returns></returns>
                public static bool DeleteAsync(string taskKey, string cron)
                {
                    string content = JsonHelper.OutResult("TaskKey", taskKey, "Cron", cron);
                    return ExeTaskAsync(TaskType.Broadcast, content, StopCronTask, null, 0, null, BroadcastType.Client);
                }
                /// <summary>
                /// 删除 Cron 重复性任务
                /// </summary>
                /// <param name="taskKey">任务key</param>
                /// <param name="cron">cron 表达式，如果存在多个相同的taskKey，可以增加此条件搜索</param>
                /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTSClientCallBack 特性标注</param>
                /// <returns></returns>
                public static bool DeleteAsync(string taskKey, string cron, string callBackKey)
                {
                    string content = JsonHelper.OutResult("TaskKey", taskKey, "Cron", cron);
                    return ExeTaskAsync(TaskType.Broadcast, content, StopCronTask, callBackKey, 0, null, BroadcastType.Client);
                }
                internal const string StopCronTask = "DTS.Client.Broadcast.DeleteCron";
            }



            /// <summary>
            /// 广播任务，所有在线进程都收到同一个信息。
            /// </summary>
            public static class Broadast
            {

                /// <summary>
                /// 发起一个任务消息，以进程为单位。
                /// </summary>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                public static bool PublishAsync(string content, string taskKey)
                {
                    return ExeTaskAsync(TaskType.Broadcast, content, taskKey, null, 0, null, BroadcastType.Server);
                }
                /// <summary>
                /// 发起一个任务消息，以进程为单位。
                /// </summary>
                /// <param name="content">传递的信息</param>
                /// <param name="taskKey">指定任务key，即Server方监听的subKey</param>
                /// <param name="callBackKey">如果需要接收回调通知，指定本回调key，回调方法用 DTCClientCallBack 特性标注</param>
                public static bool PublishAsync(string content, string taskKey, string callBackKey)
                {
                    return ExeTaskAsync(TaskType.Broadcast, content, taskKey, callBackKey, 0, null, BroadcastType.Server);
                }
            }


            /// <summary>
            /// 基础执行
            /// </summary>
            private static bool ExeTaskAsync(TaskType taskType, string content, string taskKey, string callBackKey, int delayMinutes, string recurringID, BroadcastType broadcastType)
            {
                MQType mqType = MQ.Client.MQType;
                if (mqType == MQType.Empty) { throw new Exception("MQ can't be empty , you need a mq config."); }

                TaskTable table = new TaskTable();
                table.TaskType = taskType.ToString();
                table.Content = content;
                table.TaskKey = taskKey;
                table.CallBackKey = callBackKey;
                table.DelayMinutes = delayMinutes;
                table.Retries = 0;
                if (!string.IsNullOrEmpty(recurringID))
                {
                    table.MsgID = recurringID;
                }
                if (delayMinutes > 0)
                {
                    table.CreateTime = DateTime.Now.AddMinutes(delayMinutes);//Scanner 任务移除超时数据，根据此时间处理。
                    table.EditTime = DateTime.Now.AddMinutes(delayMinutes);//Scanner 处理任务重试，根据此时间处理。
                }
                else
                {
                    table.EditTime = DateTime.Now;
                    table.CreateTime = DateTime.Now;
                }
                bool isWriteTxt = false;
                if (!Worker.Save(table, out isWriteTxt))
                {
                    return false;
                }
                MQMsg msg = table.ToMQMsg();
                switch (taskType)
                {
                    case TaskType.Broadcast:
                        switch (broadcastType)
                        {
                            case BroadcastType.Client:
                                msg.ExChange = DTSConfig.Client.MQ.ProcessExChange;
                                break;
                            case BroadcastType.Server:
                                msg.ExChange = DTSConfig.Server.MQ.ProcessExChange;
                                break;
                        }
                        break;
                    default:
                        msg.ExChange = DTSConfig.Server.MQ.ProjectExChange;
                        break;
                }

                if (mqType == MQType.Rabbit)
                {
                    if (delayMinutes > 0)
                    {
                        msg.ExChange = DTSConfig.Server.MQ.ProjectExChange;
                        msg.QueueName = DTSConfig.Client.MQ.DelayQueue + "_" + delayMinutes;//延时队列超时，转移到默认交换机
                    }
                    msg.CallBackName = DTSConfig.Client.MQ.ProcessQueue;
                }
                else if (mqType == MQType.Kafka)
                {
                    msg.CallBackName = isWriteTxt ? DTSConfig.Client.MQ.ProcessTopic : DTSConfig.Client.MQ.ProjectTopic;
                }
                Worker.Start(msg);
                return true;
            }
        }
    }
}

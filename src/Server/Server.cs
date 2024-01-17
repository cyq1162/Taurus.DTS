﻿using System;
using System.Reflection;
using CYQ.Data;
using CYQ.Data.Lock;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        /// <summary>
        /// 分布式任务 提供端
        /// </summary>
        public static partial class Server
        {

            internal static void OnReceived(MQMsg msg)
            {
                MQType mqType = MQ.Server.MQType;
                if (mqType == MQType.Rabbit)
                {
                    // RabbitMQ 用临时队列，如果客户端服务重启，回调临时队列投递失效=》以广播回应。
                    msg.ExChange = DTSConfig.Client.MQ.ProcessExChange;//以广播回应，如果对方不在线，则消息丢失。
                    msg.CallBackName = DTSConfig.Server.MQ.ProcessQueue;
                }
                else if (mqType == MQType.Kafka)
                {
                    bool isWriteTxt = string.IsNullOrEmpty(DTSConfig.Server.Conn) && DistributedLock.Instance.LockType == LockType.Local;
                    msg.CallBackName = isWriteTxt ? DTSConfig.Server.MQ.ProcessTopic : DTSConfig.Server.MQ.ProjectTopic;
                }

                Log.Print("MQ.OnReceived : " + msg.ToJson());
                bool isDeleteAck = msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value;
                DTSConsole.WriteDebugLine("Server.MQ.OnReceived : " + msg.MsgID + " - " + msg.TaskType + " - " + (isDeleteAck ? " - DeleteAck" : "") + " - NextTo :" + msg.QueueName);

                if (isDeleteAck)
                {
                    //打印分隔线，以便查看
                    DTSConsole.WriteDebugLine("------------------------End  ：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "-------------------------");
                }
                else
                {
                    //打印分隔线，以便查看
                    DTSConsole.WriteDebugLine("------------------------Start：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "-------------------------");
                }

                var localLock = DistributedLock.Local;
                string key = "DTS.Server." + msg.MsgID;
                bool isLockOK = false;
                try
                {
                    isLockOK = localLock.Lock(key, 10000);
                    OnDoTask(msg);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }
                finally
                {
                    if (isLockOK)
                    {
                        localLock.UnLock(key);
                    }
                }
            }

            private static void OnDoTask(MQMsg msg)
            {
                if (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value)
                {
                    //可以删除数据
                    using (TaskTable table = new TaskTable())
                    {
                        if (msg.TaskType == TaskType.Cron.ToString())
                        {
                            table.Delete(msg.MsgID);//直接删除，以便执行下一次任务，避开【检测是否已执行过】。
                        }
                        else
                        {
                            table.ConfirmState = 2;
                            table.EditTime = DateTime.Now;
                            if (table.Update(msg.MsgID))
                            {
                                //DTCLog.WriteDebugLine("Server.OnDoTask.IsDeleteAck：更新表：" + msg.MsgID);
                            }
                        }
                    }
                    if (Worker.IO.Delete(msg.MsgID, msg.TaskType))
                    {
                        //DTCLog.WriteDebugLine("Server.OnDoTask.IsDeleteAck：更新表：" + msg.MsgID);
                    }
                    return;
                }


                #region 检测是否已执行过。
                using (TaskTable table = new TaskTable())
                {
                    if (table.Exists(msg.MsgID) || Worker.IO.Exists(msg.MsgID, msg.TaskType))
                    {
                        msg.IsFirstAck = false;
                        msg.DelayMinutes = 0;
                        Worker.MQPublisher.Add(msg);
                        //DTCLog.WriteDebugLine("Server.OnDoTask 方法已执行过，发送MQ响应：IsFirstAck = false。");
                        return;
                    }
                }
                #endregion

                MethodInfo method = MethodCollector.GetServerMethod(msg.CallBackKey);
                if (method == null) { return; }//没有对应的绑定信息，直接丢失信息。
                string returnContent = null;
                try
                {
                    DTSSubscribePara para = new DTSSubscribePara(msg);
                    object obj = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);
                    object result = method.Invoke(obj, new object[] { para });
                    if (result is bool && !(bool)result) { return; }
                    returnContent = para.CallBackContent;
                    Log.Print("Execute." + msg.TaskType + ".Method : " + method.Name + " - SubKey :" + msg.CallBackKey);
                    DTSConsole.WriteDebugLine("Server.Execute." + msg.TaskType + ".Method : " + method.Name + " - SubKey :" + msg.CallBackKey);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                    return;
                }

                msg.IsFirstAck = true;
                msg.Content = returnContent;
                msg.DelayMinutes = 0;
                msg.TaskTime = DateTime.Now;
                Worker.MQPublisher.Add(msg);
                //DTCLog.WriteDebugLine("Server.OnDoTask 首次回应：IsFirstAck = true ，并执行方法：" + method.Name);

                using (TaskTable table = new TaskTable())
                {
                    //开启新任务，上面已经反转，直接赋值即可。
                    table.TaskType = msg.TaskType;
                    //table.QueueName = msg.QueueName;
                    //table.CallBackName = msg.CallBackName;
                    table.TaskKey = msg.TaskKey;
                    table.CallBackKey = msg.CallBackKey;
                    table.MsgID = msg.MsgID;
                    table.Content = msg.Content;
                    table.ConfirmState = 1;//如果发送失败，则不设置确认，延时被删除。

                    //广播类型不写数据库
                    if (msg.TaskType != TaskType.Broadcast.ToString() && table.Insert(InsertOp.ID))
                    {
                        //DTCLog.WriteDebugLine("Server.OnDoTask 首次回应：插入数据表。");
                    }
                    else if (Worker.IO.Write(table))//缓存1份。
                    {
                        //DTCLog.WriteDebugLine("Server.OnDoTask 首次回应：写入缓存。");
                    }
                }
            }
        }
    }
}

using CYQ.Data;
using CYQ.Data.Json;
using System;
using System.Reflection;
using System.Web;
using Taurus.Plugin.DistributedLock;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            #region 接收 Subscribe

            /// <summary>
            /// 消息有回调，说明对方任务已完成
            /// </summary>
            internal static void OnReceived(MQMsg msg)
            {
                try
                {
                    //不加锁：一个广播可能多个进程同时执行。
                    string printMsg = "-------------------Client " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + " - " + msg.TaskType + " --------------------" + Environment.NewLine;
                    bool isFirstAck = !msg.IsFirstAck.HasValue || msg.IsFirstAck.Value;
                    printMsg += "Client.MQ.OnReceived : " + msg.MsgID + Environment.NewLine;
                    OnDoTask(msg, ref printMsg);
                    printMsg += "--------------------------" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff") + "-------------------------" + Environment.NewLine;
                    DTSConsole.WriteDebugLine(printMsg);
                    Log.Print(printMsg);
                }
                catch (Exception err)
                {
                    Log.Error(err);
                }

            }



            private static void OnDoTask(MQMsg msg, ref string printMsg)
            {
                if (!(!msg.IsFirstAck.HasValue || msg.IsFirstAck.Value) || (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value))
                {
                    DoTaskConfirm(msg, ref printMsg);
                    return;
                }

                if (!string.IsNullOrEmpty(msg.CallBackKey))
                {
                    #region 执行方法
                    //系统内部方法
                    if (msg.CallBackKey == Client.Cron.StopCronTask)
                    {
                        if (!CronWorker.Remove(msg.Content))
                        {
                            return;
                        }
                        printMsg += "Client.Execute." + msg.TaskType + ".Method : " + Client.Cron.StopCronTask + " - CallBackKey :" + msg.CallBackKey + Environment.NewLine;
                        printMsg += "NextTo :" + msg.QueueName + Environment.NewLine;
                        Worker.MQPublisher.Add(msg.SetFirstAsk());
                        return;

                    }
                    else
                    {
                        MethodInfo method = MethodCollector.GetClientMethod(msg.CallBackKey);
                        if (method != null)
                        {
                            try
                            {
                                printMsg += "Client.Execute." + msg.TaskType + ".Method : " + method.Name + " - CallBackKey :" + msg.CallBackKey + Environment.NewLine;
                                DTSCallBackPara para = new DTSCallBackPara(msg);
                                object obj = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);
                                object invokeResult = method.Invoke(obj, new object[] { para });
                                if (invokeResult is bool && !(bool)invokeResult)
                                {
                                    printMsg += ("Execute result return false, return directly." + Environment.NewLine);
                                    return;
                                }

                            }
                            catch (Exception err)
                            {
                                Log.Error(err);
                                return;
                            }
                        }
                    }
                    #endregion
                }
                DoTaskConfirm(msg, ref printMsg);
            }

            private static void DoTaskConfirm(MQMsg msg, ref string printMsg)
            {
                bool isUpdateOK = false;
                using (TaskTable table = new TaskTable())
                {
                    table.ConfirmState = 1;
                    table.EditTime = DateTime.Now;
                    isUpdateOK = table.Update(msg.MsgID);
                    if (isUpdateOK)
                    {
                        //DTCLog.WriteDebugLine("Client.OnDoTask 首次更新数据表。");
                    }
                    else if (Worker.IO.Delete(msg.MsgID, msg.TaskType))
                    {
                        isUpdateOK = true;
                        //DTCLog.WriteDebugLine("Client.OnDoTask 首次删除缓存：" + msg.MsgID);
                    }
                }

                //这边已经删除数据，告诉对方，也可以删除数据了。
                if (isUpdateOK || (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value))
                {
                    if (string.IsNullOrEmpty(msg.QueueName) && string.IsNullOrEmpty(msg.ExChange)) { return; }
                    Worker.MQPublisher.Add(msg.SetDeleteAsk());
                    printMsg += "NextTo :" + msg.QueueName + Environment.NewLine;
                }
            }

            #endregion
        }
    }
}

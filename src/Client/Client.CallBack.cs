using CYQ.Data;
using CYQ.Data.Json;
using CYQ.Data.Lock;
using System;
using System.Reflection;
using System.Web;

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
                Log.Print("MQ.OnReceived : " + msg.ToJson());
                bool isFirstAck = !msg.IsFirstAck.HasValue || msg.IsFirstAck.Value;
                DTSConsole.WriteDebugLine("Client.MQ.OnReceived : " + msg.MsgID + " - " + msg.TaskType + (isFirstAck ? " - IsFirstAck = true" : ""));

                var localLock = DistributedLock.Local;
                string key = "DTS.Client." + msg.MsgID;
                bool isLockOK = false;
                try
                {
                    isLockOK = localLock.Lock(key, 1000);
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
                if (!(!msg.IsFirstAck.HasValue || msg.IsFirstAck.Value) || (msg.IsDeleteAck.HasValue && msg.IsDeleteAck.Value))
                {
                    DoTaskConfirm(msg);
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
                        Log.Print("Execute." + msg.TaskType + ".CallBack.Method : " + Client.Cron.StopCronTask);
                        DTSConsole.WriteDebugLine("Client.Execute." + msg.TaskType + ".CallBack.Method : " + Client.Cron.StopCronTask);
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
                                DTSCallBackPara para = new DTSCallBackPara(msg);
                                object obj = method.IsStatic ? null : Activator.CreateInstance(method.DeclaringType);
                                object invokeResult = method.Invoke(obj, new object[] { para });
                                if (invokeResult is bool && !(bool)invokeResult) { return; }
                                Log.Print("Execute." + msg.TaskType + ".CallBack.Method : " + method.Name);
                                DTSConsole.WriteDebugLine("Client.Execute." + msg.TaskType + ".CallBack.Method : " + method.Name);
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
                DoTaskConfirm(msg);
            }

            private static void DoTaskConfirm(MQMsg msg)
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
                    //DTCLog.WriteDebugLine("Client.OnDoTask IsDeleteAck=true，让服务端确认及删除掉缓存。");
                }
                //打印分隔线，以便查看
                DTSConsole.WriteDebugLine("----------------------------------------------------------------");
            }

            #endregion
        }
    }
}

using CYQ.Data;
using CYQ.Data.Lock;
using CYQ.Data.Table;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Server
        {
            internal static partial class Worker
            {
                /// <summary>
                /// 1、扫描数据库
                /// 2、发送到MQ
                /// 3、程序运行时启动、服务调用时也检测启动。  
                /// </summary>
                internal static class Scanner
                {
                    static Scanner()
                    {
                        //这个会在初始化触发。
                        ThreadPool.QueueUserWorkItem(new WaitCallback(MQPublisher.InitQueueListen), null);
                    }

                    static bool threadIsWorking = false;
                    const string lockKey = "DTS.Server.Lock:Worker.ScanDB";
                    static readonly object lockObj = new object();
                    public static void Start()
                    {
                        //1 再加个分布式锁，保障只有一个应用在启动表描述
                        //1、数据库参数检测
                        //2、MQ参数检测
                        if (MQ.Server.MQType == MQType.Empty || threadIsWorking)
                        {
                            empty = 1;//保持任务不退出。
                            return;
                        }
                        lock (lockObj)
                        {
                            if (!threadIsWorking)
                            {
                                threadIsWorking = true;
                                empty = 0;
                                ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
                            }
                        }
                    }
                    static int empty = 0;
                    static DateTime scanTime = DateTime.Now;
                    private static void DoWork(object p)
                    {
                        while (true)
                        {
                            try
                            {
                                bool isLockOK = false;
                                try
                                {
                                    if (!string.IsNullOrEmpty(DTSConfig.Server.Conn))
                                    {
                                        isLockOK = DistributedLock.Instance.Lock(lockKey, 1);
                                        if (isLockOK)
                                        {
                                            if (empty % 3 == 0)
                                            {
                                                //删除确认数据要快。
                                                ScanDB_DeleteConfirm();
                                            }
                                            if (empty % 60 == 0)
                                            {
                                                //超时数据慢慢删除。
                                                ScanDB_DeleteTimeout();
                                                if (empty % 120 == 0)
                                                {
                                                    ScanDB_RetryForConfirm();//2分钟重试1次
                                                }
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    if (isLockOK)
                                    {
                                        DistributedLock.Instance.UnLock(lockKey);
                                    }
                                }

                                Thread.Sleep(999);
                                empty++;
                                if (empty > 5 * 60)  //扫描4分钟都没东西可以扫
                                {
                                    ScanIO_DeleteTimeout();
                                    threadIsWorking = false;
                                    break;//结束线程。
                                }
                            }
                            catch (Exception err)
                            {
                                threadIsWorking = false;
                                Log.Error(err);
                                break;
                            }
                        }
                    }



                    /// <summary>
                    /// 向客户端，发起重新确认，以便处理删除。
                    /// </summary>
                    private static void ScanDB_RetryForConfirm()
                    {
                        if (string.IsNullOrEmpty(DTSConfig.Server.Conn) || !DBTool.Exists(DTSConfig.Server.TaskTable, "U", DTSConfig.Server.Conn))
                        {
                            return;
                        }
                        int maxRetries = Math.Max(1, DTSConfig.Server.Worker.MaxRetries);
                        int scanInterval = Math.Max(60, DTSConfig.Server.Worker.RetryIntervalSecond);//最短1分钟

                        using (MAction action = new MAction(DTSConfig.Server.TaskTable, DTSConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;

                            #region 扫描数据库、发送到MQ队列
                            string whereConfirm = "ConfirmState=1 and Retries<" + maxRetries + " and EditTime<'" + DateTime.Now.AddSeconds(-scanInterval).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            MDataTable dtSend = action.Select(1000, whereConfirm);
                            while (dtSend != null && dtSend.Rows.Count > 0)
                            {
                                empty = -1;

                                bool isUpdateOK = false;

                                List<MQMsg> msgList = dtSend.ToList<MQMsg>();
                                foreach (MQMsg msg in msgList)
                                {
                                    msg.SetDeleteAsk();
                                    msg.ExChange = DTSConfig.Client.MQ.ProcessExChange;//发送不到时候，用广播群发。
                                }
                                if (MQ.Client.PublishBatch(msgList))
                                {
                                    Log.Print("ScanDB.MQ.PublishBatch :" + msgList.Count + " items.");
                                    DTSConsole.WriteDebugLine("Server.ScanDB.MQ.PublishBatch :" + msgList.Count + " items.");
                                    foreach (var row in dtSend.Rows)
                                    {
                                        row.Set("Retries", row.Get<int>("Retries") + 1, 2);
                                        row.Set("EditTime", DateTime.Now, 2);
                                    }
                                    isUpdateOK = dtSend.AcceptChanges(AcceptOp.Update, DTSConfig.Server.Conn, "ID");
                                }

                                if (isUpdateOK)
                                {
                                    dtSend = action.Select(1000, whereConfirm);
                                }
                                else
                                {
                                    break;
                                }
                                Thread.Sleep(1);
                            }


                            #endregion
                        }
                    }

                    private static void ScanDB_DeleteConfirm()
                    {
                        if (string.IsNullOrEmpty(DTSConfig.Server.Conn) || !DBTool.Exists(DTSConfig.Server.TaskTable, "U", DTSConfig.Server.Conn))
                        {
                            return;
                        }
                        using (MAction action = new MAction(DTSConfig.Server.TaskTable, DTSConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;
                            string whereDelete = "ConfirmState=2";

                            if (DTSConfig.Server.Worker.ConfirmClearMode == TaskClearMode.Delete)
                            {
                                action.Delete(whereDelete);//不讲道理直接清
                            }
                            else
                            {
                                #region 已确认的数据：清空数据、或转移到历史表
                                MDataTable dt = action.Select(1000, whereDelete + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTSConfig.Server.TaskTable + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTSConfig.Server.Conn))//仅插入
                                    {
                                        dt.TableName = DTSConfig.Server.TaskTable;
                                        dt.AcceptChanges(AcceptOp.Delete, DTSConfig.Server.Conn, "ID");
                                    }
                                }
                                #endregion
                            }
                        }
                    }

                    private static void ScanDB_DeleteTimeout()
                    {
                        if (string.IsNullOrEmpty(DTSConfig.Server.Conn) || !DBTool.Exists(DTSConfig.Server.TaskTable, "U", DTSConfig.Server.Conn))
                        {
                            return;
                        }
                        using (MAction action = new MAction(DTSConfig.Server.TaskTable, DTSConfig.Server.Conn))
                        {
                            action.IsUseAutoCache = false;
                            int noConfirmSecond = DTSConfig.Server.Worker.TimeoutKeepSecond;
                            string whereTimeout = "ConfirmState<2 and CreateTime<'" + DateTime.Now.AddSeconds(-noConfirmSecond).ToString("yyyy-MM-dd HH:mm:ss") + "'";
                            if (DTSConfig.Server.Worker.TimeoutClearMode == TaskClearMode.Delete)
                            {
                                action.Delete(whereTimeout);
                            }
                            else
                            {
                                #region 已超时的数据：删除或转移到超时表

                                MDataTable dt = action.Select(1000, whereTimeout + " order by id asc");
                                if (dt != null && dt.Rows.Count > 0)
                                {
                                    dt.TableName = DTSConfig.Server.TaskTable + "_History";
                                    if (dt.AcceptChanges(AcceptOp.Auto | AcceptOp.InsertWithID, DTSConfig.Server.Conn))
                                    {
                                        dt.TableName = DTSConfig.Server.TaskTable;
                                        dt.AcceptChanges(AcceptOp.Delete, DTSConfig.Server.Conn, "ID");
                                    }
                                }


                                #endregion
                            }
                        }
                    }

                    private static void ScanIO_DeleteTimeout()
                    {
                        IO.DeleteTimeoutTable();
                    }
                }
            }
        }
    }
}


using CYQ.Data;
using CYQ.Data.Json;
using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using System.Threading;
using static Taurus.Plugin.DistributedTask.DTS.Client;


namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            /// <summary>
            /// Step 1
            /// </summary>
            internal static partial class CronWorker
            {
                public static bool Add(CronTable table)
                {
                    bool result = false;
                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn) && table.Insert(InsertOp.None))
                    {
                        Log.Print("DB.Write : " + table.ToJson());
                        result = true;
                        table.Dispose();
                    }
                    if (!result)
                    {
                        result = IO.WriteCronTable(table);//写 DB => Redis、MemCache，失败则写文本。
                    }
                    if (result)
                    {
                        // Step 2
                        Scanner.Add(table);//描述器负责生产任务。
                        Scanner.Start();
                    }
                    return result;
                }

                public static bool Remove(string content)
                {
                    if (string.IsNullOrEmpty(content)) { return false; }
                    string taskKey = JsonHelper.GetValue(content, "TaskKey");
                    string cron = JsonHelper.GetValue(content, "Cron");
                    return Remove(taskKey, cron);
                }
                private static bool Remove(string taskKey, string cron)
                {
                    if (string.IsNullOrEmpty(taskKey)) { return false; }
                    CronTable cronTable = CronWorker.IO.SearchCronTable(taskKey, cron);
                    if (cronTable != null)
                    {
                        //若有IO文件，直接删除。
                        CronWorker.IO.DeleteCronTable(cronTable.MsgID, TaskType.Cron.ToString());
                    }

                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn))
                    {
                        using (CronTable table = new CronTable())
                        {
                            string where = "TaskKey=:?TaskKey";
                            table.SetPara("TaskKey", taskKey);
                            if (!string.IsNullOrEmpty(cron))
                            {
                                where += " and Cron=:?Cron";
                                table.SetPara("Cron", cron);
                            }
                            if (table.Fill(where))
                            {
                                if (Scanner.Remove(table.MsgID))
                                {
                                    return table.Delete();
                                }
                            }
                        }
                    }
                    if (cronTable != null)
                    {
                        return Scanner.Remove(cronTable.MsgID);
                    }

                    return false;
                }

                internal static bool Delete(string msgID)
                {
                    Scanner.Remove(msgID);
                    bool result = CronWorker.IO.DeleteCronTable(msgID, TaskType.Cron.ToString());
                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn))
                    {
                        using (CronTable table = new CronTable())
                        {
                            if (table.Delete(msgID))
                            {
                                result = true;
                            }
                        }
                    }
                    return result;
                }

            }

            internal static partial class CronWorker
            {

                /// <summary>
                /// 待处理的工作队列
                /// </summary>
                static ConcurrentQueue<CronTable> _dtcQueue = new ConcurrentQueue<CronTable>();
                static object lockObj = new object();
                static bool threadIsWorking = false;

                /// <summary>
                /// Step 3
                /// </summary>
                public static void ReadyToWork(CronTable table)
                {
                    _dtcQueue.Enqueue(table);
                    if (threadIsWorking) { return; }
                    lock (lockObj)
                    {
                        if (!threadIsWorking)
                        {
                            threadIsWorking = true;
                            ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
                        }
                    }
                }

                private static void DoWork(object p)
                {
                    try
                    {
                        int empty = 0;
                        while (true)
                        {

                            while (!_dtcQueue.IsEmpty)
                            {
                                empty = 0;

                                CronTable table;
                                if (_dtcQueue.TryDequeue(out table))
                                {
                                    string json = Worker.IO.Read(table.MsgID, TaskType.Cron.ToString());
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        continue;
                                    }
                                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn))
                                    {
                                        //检测是否存在任务
                                        using (TaskTable taskTable = new TaskTable())
                                        {
                                            if (taskTable.Fill(table.MsgID))
                                            {
                                                if (taskTable.ConfirmState == 0)
                                                {
                                                    continue;
                                                }
                                                if (!taskTable.Delete())//直接删除，以便下一个任务产生。
                                                {
                                                    continue;
                                                }
                                            }
                                        }
                                        table.EditTime = DateTime.Now;
                                        table.SetExpression("ConfirmNum=ConfirmNum+1");
                                        if (table.Update())
                                        {

                                        }
                                    }

                                    TaskType taskType = (table.IsDelayTask.HasValue && table.IsDelayTask.Value) ? TaskType.Delay : TaskType.Cron;
                                    //发起一个即时任务。
                                    Instant.PublishAsync(taskType, table.Content, table.TaskKey, table.CallBackKey, table.MsgID);
                                }
                            }

                            empty++;
                            Thread.Sleep(500);
                            if (empty > 20)
                            {
                                threadIsWorking = false;
                                break;//结束线程。
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        threadIsWorking = false;
                        //数据库异常，不处理。
                        Log.Error(err);
                    }
                }

            }
        }
    }
}

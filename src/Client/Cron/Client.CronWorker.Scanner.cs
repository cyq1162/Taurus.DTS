using CYQ.Data;
using System;
using System.Threading;
using System.Collections.Generic;
using CYQ.Data.Tool;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            internal static partial class CronWorker
            {
                internal static partial class Scanner
                {
                    static bool threadIsWorking = false;
                    static object lockObj = new object();
                    /// <summary>
                    /// 启动扫描线程
                    /// </summary>
                    public static void Start()
                    {
                        if (threadIsWorking) { return; }
                        lock (lockObj)
                        {
                            if (!threadIsWorking)
                            {
                                InitTableDictionaryFromDBAndIO();
                                if (_dtsDic.Count > 0)
                                {
                                    threadIsWorking = true;
                                    ThreadPool.QueueUserWorkItem(new WaitCallback(DoWork), null);
                                }
                            }
                        }
                    }

                    public static void Add(CronTable table)
                    {
                        if (!_dtsDic.ContainsKey(table.MsgID))
                        {
                            _dtsDic.Add(table.MsgID, table);
                        }
                    }
                    public static bool Remove(string msgID)
                    {
                        if (_dtsDic.ContainsKey(msgID))
                        {
                            return _dtsDic.Remove(msgID);
                        }
                        return false;
                    }
                    /// <summary>
                    /// 待处理的工作队列
                    /// </summary>
                    static MDictionary<string, CronTable> _dtsDic = new MDictionary<string, CronTable>();

                    static List<string> taskKeys = new List<string>();

                    private static void DoWork(object p)
                    {
                        try
                        {
                            DateTime now = DateTime.Now;
                            while (_dtsDic.Count > 0)
                            {
                                int empty = 0;
                                string nowString = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                                List<string> keys = _dtsDic.GetKeys();
                                foreach (string key in keys)
                                {
                                    var table = _dtsDic[key];
                                    if (table != null && !string.IsNullOrEmpty(table.Cron))
                                    {
                                        bool isDelayTask = table.IsDelayTask.HasValue && table.IsDelayTask.Value;
                                        string dateString = CronHelper.GetNextDateTime(table.Cron);
                                        if (dateString == null)
                                        {
                                            if (isDelayTask)
                                            {
                                                //检测是否因为服务故障，而没有执行。
                                                if (table.ConfirmNum.HasValue && table.ConfirmNum.Value > 0)
                                                {
                                                    CronWorker.Delete(table.MsgID);
                                                    continue;
                                                }
                                                CronWorker.ReadyToWork(table);//异步生产成任务
                                                CronWorker.Delete(table.MsgID);//删除Cron
                                                continue;
                                            }
                                        }
                                        if (dateString == nowString)
                                        {
                                            string taskKey = table.MsgID + dateString;
                                            if (taskKeys.Contains(taskKey))
                                            {
                                                //避免重复生成任务。
                                                continue;
                                            }
                                            empty = 0;
                                            taskKeys.Add(taskKey);
                                            CronWorker.ReadyToWork(table);//异步生产成任务
                                            if (isDelayTask)
                                            {
                                                CronWorker.Delete(table.MsgID);
                                            }
                                            continue;
                                        }
                                    }
                                }
                                Thread.Sleep(100);
                                empty++;
                                if (empty > 20)
                                {
                                    taskKeys.Clear();//超过2秒没有产生数据
                                }
                                else if (taskKeys.Count > 4000)
                                {
                                    taskKeys.RemoveRange(0, 2000);//预设最多创建2000个定时任务。
                                }
                            }

                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                        }
                        finally
                        {
                            threadIsWorking = false;
                        }
                    }


                    private static bool hasSetFromDB = false;
                    private static bool hasSetFromIO = false;

                    /// <summary>
                    /// 从数据库和IO初始化队列数据。
                    /// </summary>
                    private static void InitTableDictionaryFromDBAndIO()
                    {
                        List<CronTable> tables = null;
                        bool isDBOK = false;
                        if (!hasSetFromDB)
                        {
                            isDBOK = !string.IsNullOrEmpty(DTSConfig.Client.Conn) && DBTool.TestConn(DTSConfig.Client.Conn);
                            if (isDBOK)
                            {
                                hasSetFromDB = true;
                                using (CronTable table = new CronTable())
                                {
                                    tables = table.Select<CronTable>();//查询所有数据。
                                }
                            }
                        }
                        if (!hasSetFromIO)
                        {
                            hasSetFromIO = true;
                            List<CronTable> ioTables = IO.GetCronTable();
                            if (ioTables.Count > 0)
                            {
                                if (isDBOK)
                                {
                                    //检测数据库是否恢复，如果恢复，写入数据库
                                    foreach (CronTable ioTable in ioTables)
                                    {
                                        bool isExsits = false;
                                        foreach (CronTable table in tables)
                                        {
                                            if (table.MsgID == ioTable.MsgID)
                                            {
                                                break;//数据已存在。
                                            }
                                        }
                                        if (!isExsits)
                                        {
                                            tables.Add(ioTable);//添加进队列
                                            if (ioTable.Insert(InsertOp.None)) //写入数据库
                                            {
                                                //插入成功，删除IO数据
                                                Worker.IO.Delete(ioTable.MsgID, TaskType.Cron.ToString());
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    tables = ioTables;
                                }
                            }
                        }

                        if (tables != null && tables.Count > 0)
                        {
                            foreach (CronTable table in tables)
                            {
                                _dtsDic.Add(table.MsgID, table);
                            }
                        }
                    }
                }
            }

        }
    }
}

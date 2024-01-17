using CYQ.Data;
using CYQ.Data.Tool;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            internal static partial class Worker
            {
                /// <summary>
                /// 待处理的工作队列
                /// </summary>
                //static ConcurrentQueue<TaskTable> _dtcQueue = new ConcurrentQueue<TaskTable>();
                //static bool threadIsWorking = false;
                //static object lockObj = new object();


                public static void Start(MQMsg msg)
                {
                    Scanner.Start();//检测未启动则启动，已启动则忽略。
                    MQPublisher.Add(msg);//异步发送MQ
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(Save), table);
                    //_dtcQueue.Enqueue(table);
                    //if (threadIsWorking) { return; }
                    //lock (lockObj)
                    //{
                    //    if (!threadIsWorking)
                    //    {
                    //        threadIsWorking = true;
                    //        ThreadPool.QueueUserWorkItem(new WaitCallback(Save), null);
                    //    }
                    //}
                }

                public static bool Save(TaskTable table, out bool isWriteTxt)
                {
                    isWriteTxt = false;
                    bool result = false;
                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn) && table.Insert(InsertOp.None))
                    {
                        Log.Print("DB.Write : " + table.ToJson());
                        result = true;
                        table.Dispose();
                    }
                    if (!result)
                    {
                        isWriteTxt = true;
                        return IO.Write(table);//写 DB => Redis、MemCache，失败则写文本。
                    }
                    return result;
                }

                //public static void Save2(object taskTable)
                //{
                //    TaskTable table = taskTable as TaskTable;
                //    bool result = false;
                //    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn) && table.Insert(InsertOp.None))
                //    {
                //        Log.Print("DB.Write : " + table.ToJson());
                //        result = true;
                //        table.Dispose();
                //    }
                //    if (!result)
                //    {
                //        IO.Write(table);//写 DB => Redis、MemCache，失败则写文本。
                //    }
                //}
                //public static void Save2(object p)
                //{
                //    try
                //    {
                //        string path = AppConfig.WebRootPath + "App_Data/dts/client/task/aaa.txt";
                //        bool isDBOK = !string.IsNullOrEmpty(DTSConfig.Client.Conn) && DBTool.TestConn(DTSConfig.Client.Conn);
                //        int empty = 0;
                //        while (true)
                //        {
                //            while (!_dtcQueue.IsEmpty)
                //            {
                //                empty = 0;

                //                TaskTable table;
                //                if (_dtcQueue.TryDequeue(out table))
                //                {
                //                    //StreamReader reader = new StreamReader(path);
                //                    //reader.lget
                //                    //reader.Read()
                //                    //IOHelper.Append(path, table.ToJson());

                //                    //bool result = false;
                //                    //if (isDBOK && table.Insert(InsertOp.None))
                //                    //{
                //                    //    Log.Print("DB.Write : " + table.ToJson());
                //                    //    result = true;
                //                    //    table.Dispose();
                //                    //}
                //                    //if (!result)
                //                    //{
                //                    //    IO.Write(table);//写 DB => Redis、MemCache，失败则写文本。
                //                    //}
                //                }
                //            }
                //            empty++;
                //            Thread.Sleep(1);
                //            if (empty > 10000)
                //            {
                //                //超过10分钟没日志产生
                //                threadIsWorking = false;
                //                break;//结束线程。
                //            }
                //        }
                //    }
                //    catch (Exception err)
                //    {
                //        threadIsWorking = false;
                //        Log.Error(err);
                //    }




                //}
            }



        }
    }
}

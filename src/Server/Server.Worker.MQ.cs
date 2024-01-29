using CYQ.Data;
using CYQ.Data.Table;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Server
        {
            internal static partial class Worker
            {
                internal static partial class MQPublisher
                {
                    /// <summary>
                    /// 待处理的工作队列
                    /// </summary>
                    static ConcurrentQueue<MQMsg> _dtcQueue = new ConcurrentQueue<MQMsg>();
                    static object lockObj = new object();
                    static bool threadIsWorking = false;
                    public static void Add(MQMsg msg)
                    {
                        _dtcQueue.Enqueue(msg);
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
                                    List<MQMsg> mQMsgs = new List<MQMsg>();
                                    while (!_dtcQueue.IsEmpty && mQMsgs.Count < 500)
                                    {
                                        MQMsg msg;
                                        if (_dtcQueue.TryDequeue(out msg))
                                        {
                                            mQMsgs.Add(msg);
                                        }
                                    }
                                    if (mQMsgs.Count > 0)
                                    {
                                        string printMsg = "Server.MQ.PublishBatch : " + mQMsgs.Count + " items";
                                        if (MQ.Server.PublishBatch(mQMsgs))
                                        {
                                            printMsg += " - OK.";
                                        }
                                        else
                                        {
                                            printMsg += " - Fail.";
                                        }
                                        Log.Print(printMsg);
                                        DTSConsole.WriteDebugLine(printMsg);
                                        mQMsgs.Clear();
                                    }

                                    Thread.Sleep(1);
                                }
                                empty++;
                                Thread.Sleep(1000);
                                if (empty > 100)
                                {
                                    //超过10分钟没日志产生
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

                    public static void InitQueueListen(object p)
                    {
                        var mq = MQ.Server;
                        if (mq.MQType != MQType.Empty)
                        {
                            string printMsg = "--------------------------------------------------" + Environment.NewLine;
                            if (mq.MQType == MQType.Rabbit)
                            {
                                //对默认对列绑定交换机。
                                bool isOK = MQ.Server.Listen(DTSConfig.Server.MQ.ProjectQueue, Server.OnReceived, DTSConfig.Server.MQ.ProjectExChange, false);
                                printMsg += "DTS.Server." + mq.MQType + ".Listen : " + DTSConfig.Server.MQ.ProjectQueue + " - ExChange : " + DTSConfig.Server.MQ.ProjectExChange + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;

                                isOK = MQ.Server.Listen(DTSConfig.Server.MQ.ProcessQueue, Server.OnReceived, DTSConfig.Server.MQ.ProcessExChange, true);
                                printMsg += "DTS.Server." + mq.MQType + ".Listen : " + DTSConfig.Server.MQ.ProcessQueue + " - ExChange : " + DTSConfig.Server.MQ.ProcessExChange + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;
                            }
                            if (mq.MQType == MQType.Kafka)
                            {
                                // 项目队列，只能有一个项目组消费
                                bool isOK = MQ.Server.Listen(DTSConfig.Server.MQ.ProjectTopic, Server.OnReceived, DTSConfig.Server.MQ.ProjectGroup, false);
                                printMsg += "DTS.Server." + mq.MQType + ".Listen : " + DTSConfig.Server.MQ.ProjectTopic + " - Group : " + DTSConfig.Server.MQ.ProjectGroup + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;

                                // 进程队列，可以有多个进程组消费
                                isOK = MQ.Server.Listen(DTSConfig.Server.MQ.ProcessTopic, Server.OnReceived, DTSConfig.Server.MQ.ProcessGroup, true);
                                printMsg += "DTS.Server." + mq.MQType + ".Listen : " + DTSConfig.Server.MQ.ProcessTopic + " - Group : " + DTSConfig.Server.MQ.ProcessGroup + (isOK ? " - OK." : " - Fail.") + Environment.NewLine;
                            }
                            printMsg += "--------------------------------------------------" + Environment.NewLine;
                            DTSConsole.WriteLine(printMsg);
                        }
                    }
                }
            }

        }
    }
}

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
        public static partial class Client
        {
            internal static partial class Worker
            {
                internal static partial class MQPublisher
                {
                    /// <summary>
                    /// 待处理的工作队列
                    /// </summary>
                    static ConcurrentQueue<MQMsg> _dtcQueue = new ConcurrentQueue<MQMsg>();

                    static bool threadIsWorking = false;
                    static object lockObj = new object();
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
                                        if (MQ.Client.PublishBatch(mQMsgs))
                                        {
                                            Log.Print("MQ.PublishBatch : " + mQMsgs.Count + " items.");
                                            DTSConsole.WriteDebugLine("Client.MQ.PublishBatch : " + mQMsgs.Count + " items.");
                                        }
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
                        var mq = MQ.Client;
                        if (mq.MQType != MQType.Empty)
                        {
                            if (mq.MQType == MQType.Rabbit)
                            {
                                //对默认对列绑定交换机。
                                bool isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProjectQueue, Client.OnReceived, DTSConfig.Client.MQ.ProjectExChange, false);
                                DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProjectQueue + " - ExChange : " + DTSConfig.Client.MQ.ProjectExChange + (isOK ? " - OK." : " - Fail."));

                                isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProcessQueue, Client.OnReceived, DTSConfig.Client.MQ.ProcessExChange, true);
                                DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProcessQueue + " - ExChange : " + DTSConfig.Client.MQ.ProcessExChange + (isOK ? " - OK." : " - Fail."));
                            }
                            if (mq.MQType == MQType.Kafka)
                            {
                                // 项目队列，只能有一个项目组消费
                                bool isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProjectTopic, Client.OnReceived, DTSConfig.Client.MQ.ProjectGroup, false);
                                DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProjectTopic + " -  Group : " + DTSConfig.Client.MQ.ProjectGroup + (isOK ? " - OK." : " - Fail."));

                                // 进程队列，可以有多个进程组消费
                                isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProcessTopic, Client.OnReceived, DTSConfig.Client.MQ.ProcessGroup, true);
                                DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProcessTopic + " -  Group : " + DTSConfig.Client.MQ.ProcessGroup + (isOK ? " - OK." : " - Fail."));
                            }
                        }
                    }
                }
            }

        }
    }
}

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
                                            Log.Print("MQ.Publish : " + mQMsgs.Count + " items.");
                                            DTSConsole.WriteDebugLine("Client.MQ.Publish : " + mQMsgs.Count + " items.");
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
                            //对默认对列绑定交换机。
                            bool isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProjectQueue, Client.OnReceived, DTSConfig.Client.MQ.ProjectExChange + "," + DTSConfig.ProjectExChange, false);
                            DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProjectQueue + (isOK ? " - OK." : " - Fail."));

                            isOK = MQ.Client.Listen(DTSConfig.Client.MQ.ProcessQueue, Client.OnReceived, DTSConfig.Client.MQ.ProcessExChange + "," + DTSConfig.ProcessExChange, true);
                            DTSConsole.WriteDebugLine("DTS.Client." + mq.MQType + ".Listen : " + DTSConfig.Client.MQ.ProcessQueue + (isOK ? " - OK." : " - Fail."));
                        }
                    }
                }
            }

        }
    }
}

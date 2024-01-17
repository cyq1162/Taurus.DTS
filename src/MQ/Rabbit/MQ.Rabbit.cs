using CYQ.Data;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CYQ.Data.Tool;
using System.Collections.Concurrent;

namespace Taurus.Plugin.DistributedTask
{
    internal partial class MQRabbit : MQ
    {
        #region 错误链接断开重连机制处理
        public class ListenPara
        {
            public OnReceivedDelegate onReceivedDelegate { get; set; }
            public string BindExName { get; set; }
            public bool IsExclusive { get; set; }
        }
        static MDictionary<string, ListenPara> listenFailDic = new MDictionary<string, ListenPara>(StringComparer.OrdinalIgnoreCase);
        private bool _IsConnectOK = false;
        public bool IsConnectOK
        {
            get
            {
                if (!_IsConnectOK)
                {
                    TryConnect();
                }
                else if (_Connection != null && !_Connection.IsOpen)
                {
                    _IsConnectOK = false;
                    TryConnect();
                }
                return _IsConnectOK;
            }
            set { _IsConnectOK = value; }
        }
        private object lockObj = new object();
        static readonly object lockBroadcastObj = new object();
        private bool isThreadWorking = false;
        private void TryConnect()
        {
            if (isThreadWorking) { count = 0; return; }
            lock (lockObj)
            {
                if (isThreadWorking) { return; }
                isThreadWorking = true;
                count = 0;
                ThreadPool.QueueUserWorkItem(new WaitCallback(ConnectAgain), null);
            }
        }
        int count = 0;
        private void ConnectAgain(object p)
        {
            if (_IsConnectOK) { return; }
            while (true)
            {
                Thread.Sleep(3000);
                try
                {
                    _Connection = factory.CreateConnection();
                    _IsConnectOK = _Connection.IsOpen;
                    if (_IsConnectOK)
                    {
                        //重新开启监听
                        if (listenFailDic.Count > 0)
                        {
                            lock (lockBroadcastObj)
                            {
                                if (listenFailDic.Count > 0)
                                {
                                    List<string> keys = listenFailDic.GetKeys();
                                    foreach (string key in keys)
                                    {
                                        ListenPara para = listenFailDic[key];
                                        if (Listen(key, para.onReceivedDelegate, para.BindExName, para.IsExclusive))
                                        {
                                            listenFailDic.Remove(key);
                                            listenOKList.Add(key);
                                        }
                                    }
                                }

                            }
                        }
                        isThreadWorking = false;
                        break;
                    }
                }
                catch
                {

                }
                count++;
                if (count > 10)
                {
                    isThreadWorking = false;
                    break;
                }
            }

        }
        #endregion

        public override MQType MQType
        {
            get
            {
                return MQType.Rabbit;
            }
        }
        ConnectionFactory factory;
        public MQRabbit(string mqConn)
        {
            try
            {
                string[] items = mqConn.Split(new char[] { ';', ',' });
                if (items.Length >= 4)
                {
                    factory = new ConnectionFactory()
                    {
                        HostName = items[0],
                        UserName = items[1],
                        Password = items[2],
                        VirtualHost = items[3]
                    };
                    factory.AutomaticRecoveryEnabled = true;
                }
                _Connection = factory.CreateConnection();
                _IsConnectOK = _Connection.IsOpen;
            }
            catch (Exception err)
            {
                Log.Write(err, "MQ.Rabbit");
            }
        }


        private IConnection _Connection;
        public IConnection DefaultConnection
        {
            get
            {
                return _Connection;
            }
        }

        /// <summary>
        /// 已经定义过的队列
        /// </summary>
        static List<string> declareQueueNames = new List<string>();

        static List<string> listenOKList = new List<string>();
        public override bool Listen(string queueName, OnReceivedDelegate onReceivedDelegate, string bindExName, bool isExclusive)
        {
            if (string.IsNullOrEmpty(queueName) || onReceivedDelegate == null)
            {
                return false;
            }
            if (!IsConnectOK)
            {
                if (!listenFailDic.ContainsKey(queueName))
                {
                    listenFailDic.Add(queueName, new ListenPara() { onReceivedDelegate = onReceivedDelegate, BindExName = bindExName, IsExclusive = isExclusive });
                }
                return false;
            }
            try
            {
                if (!listenOKList.Contains(queueName))
                {
                    listenOKList.Add(queueName);
                    var channel = DefaultConnection.CreateModel();
                    if (isExclusive)
                    {
                        channel.QueueDeclare(queueName, false, true, true, null);//定义临时排它性队列
                    }
                    else
                    {
                        channel.QueueDeclare(queueName, true, false, false, null);//定义持久化队列
                    }
                    declareQueueNames.Add(queueName);
                    if (!string.IsNullOrEmpty(bindExName))
                    {
                        string[] items = bindExName.Split(',');
                        //定义交换机
                        channel.ExchangeDeclare(items[0], "fanout");
                        //绑定交换机
                        channel.QueueBind(queueName, items[0], "");

                        if (items.Length > 1)
                        {
                            //定义交换机
                            channel.ExchangeDeclare(items[1], "fanout");

                            //绑定交换机
                            channel.ExchangeBind(items[0], items[1], "", null);
                        }

                    }
                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        string json = GetBodyJson(ea);
                        MQMsg msg = MQMsg.Create(json);

                        //反转队列名称和监听key
                        msg.QueueName = msg.CallBackName;
                        msg.CallBackName = queueName;

                        string subKey = msg.TaskKey;
                        msg.TaskKey = msg.CallBackKey;
                        msg.CallBackKey = subKey;
                        msg.ExChange = null;//关闭交换机。
                        onReceivedDelegate(msg);
                    };
                    channel.BasicConsume(queueName, true, consumer);
                }
                return true;
            }
            catch (Exception err)
            {
                listenOKList.Remove(queueName);
                listenFailDic.Add(queueName, new ListenPara() { onReceivedDelegate = onReceivedDelegate, BindExName = bindExName, IsExclusive = isExclusive });
                Log.Write(err, "MQ.Rabbit");
                return false;
            }
        }
    }

    internal partial class MQRabbit
    {
        public override bool Publish(MQMsg msg)
        {
            if (msg == null || (string.IsNullOrEmpty(msg.QueueName) && string.IsNullOrEmpty(msg.ExChange)))
            {
                return false;
            }
            if (!IsConnectOK) { return false; }
            try
            {
                string json = msg.ToJson();
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                using (var channel = DefaultConnection.CreateModel())
                {
                    if (!string.IsNullOrEmpty(msg.QueueName))
                    {
                        if (!declareQueueNames.Contains(msg.QueueName))
                        {
                            IDictionary<string, object> arg = new Dictionary<string, object>();
                            if (msg.DelayMinutes.HasValue && msg.DelayMinutes.Value > 0 && !string.IsNullOrEmpty(msg.ExChange))
                            {
                                //绑定交换机

                                arg.Add("x-dead-letter-exchange", msg.ExChange);//使用默认交换机
                                //arg.Add("x-dead-letter-routing-key", "");//设置转移到的队列
                                arg.Add("x-message-ttl", msg.DelayMinutes.Value * 60 * 1000);//设置过期时间
                            }
                            channel.QueueDeclare(msg.QueueName, true, false, false, arguments: arg);//允许丢失，不需要持久化。
                            declareQueueNames.Add(msg.QueueName);
                        }
                        IBasicProperties basic = null;
                        if (!string.IsNullOrEmpty(msg.ExChange))
                        {
                            basic = channel.CreateBasicProperties();
                            basic.ReplyTo = msg.ExChange;
                            channel.BasicReturn += Channel_BasicReturn;
                        }
                        channel.BasicPublish("", msg.QueueName, true, basic, body: bytes);
                    }
                    else
                    {
                        channel.BasicPublish(msg.ExChange, "", null, body: bytes);
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                Log.Write(err, "MQ.Rabbit");
                return false;
            }
        }



        static ConcurrentQueue<BasicReturnEventArgs> basicReturnEventArgs = new ConcurrentQueue<BasicReturnEventArgs>();
        static bool threadIsWorking = false;
        static object lockRePublishObj = new object();
        private void Channel_BasicReturn(object sender, BasicReturnEventArgs e)
        {
            basicReturnEventArgs.Enqueue(e);
            if (threadIsWorking || !_IsConnectOK) { return; }
            lock (lockRePublishObj)
            {
                if (!threadIsWorking)
                {
                    threadIsWorking = true;
                    ThreadPool.QueueUserWorkItem(new WaitCallback(DoRePublishWork), null);
                }
            }
        }

        private void DoRePublishWork(object p)
        {
            try
            {
                while (_IsConnectOK && !basicReturnEventArgs.IsEmpty)
                {
                    List<BasicReturnEventArgs> list = new List<BasicReturnEventArgs>();
                    while (_IsConnectOK && !basicReturnEventArgs.IsEmpty && list.Count <= 500)
                    {
                        BasicReturnEventArgs e = null;
                        if (basicReturnEventArgs.TryDequeue(out e))
                        {
                            list.Add(e);
                        }
                    }
                    if (list.Count > 0)
                    {
                        if (_IsConnectOK)
                        {
                            using (var channel = DefaultConnection.CreateModel())
                            {
                                foreach (var e in list)
                                {
                                    channel.BasicPublish(e.BasicProperties.ReplyTo, "", false, null, e.Body);
                                }
                            }
                        }
                        else
                        {
                            //链接出问题，收回去。
                            foreach (var e in list)
                            {
                                basicReturnEventArgs.Enqueue(e);
                            }
                        }
                        list.Clear();
                    }
                    Thread.Sleep(10);
                }
                threadIsWorking = false;
            }
            catch (Exception err)
            {
                threadIsWorking = false;
                Log.Write(err, "MQ.Rabbit");
            }
        }
    }
}

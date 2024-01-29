using Confluent.Kafka;
using Confluent.Kafka.Admin;
using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Taurus.Plugin.DistributedTask
{

    internal class MQKafka : MQ
    {
        #region 错误链接断开重连机制处理

        MDictionary<string, ListenPara> listenFailDic = new MDictionary<string, ListenPara>(StringComparer.OrdinalIgnoreCase);
        private bool _IsListenOK = true;
        private object lockObj = new object();
        private bool isThreadWorking = false;
        private void TryConnect()
        {
            if (isThreadWorking) { return; }
            lock (lockObj)
            {
                if (isThreadWorking) { return; }
                isThreadWorking = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(ConnectAgain), null);
            }
        }

        private void ConnectAgain(object p)
        {
            if (_IsListenOK) { return; }
            while (true)
            {
                Thread.Sleep(5000);
                try
                {
                    //重新开启监听，只有监听是需要确认监听调用起来的，监听开启后的故障，由Confluent.Kafka内部处理。
                    if (listenFailDic.Count > 0)
                    {
                        List<string> keys = listenFailDic.GetKeys();
                        foreach (string key in keys)
                        {
                            ListenPara para = listenFailDic[key];
                            _IsListenOK = Listen(key, para.Event, null, para.IsBroadcast);
                            if (_IsListenOK)
                            {
                                listenFailDic.Remove(key);
                            }
                            else
                            {
                                //有一个失败，等下一次5秒循环。
                                break;
                            }
                        }
                    }
                    if (listenFailDic.Count == 0)
                    {
                        isThreadWorking = false;
                        break;
                    }
                }
                catch
                {

                }

            }

        }
        #endregion

        public override MQType MQType
        {
            get
            {
                return MQType.Kafka;
            }
        }

        string servers = string.Empty;
        public MQKafka(string mqConn, bool isClient)
        {
            servers = mqConn;
        }

        public override bool Publish(MQMsg msg)
        {
            try
            {
                if (msg == null || (string.IsNullOrEmpty(msg.QueueName) && string.IsNullOrEmpty(msg.ExChange)))
                {
                    return false;
                }
                if (!_IsListenOK) { return false; }
                var config = new ProducerConfig
                {
                    BootstrapServers = servers,
                    Acks = 0 //保持性能，不需要等待确认，即可发送下一条信息，允许数据丢失。
                };
                string json = msg.ToJson();
                var data = new Message<string, string> { Key = null, Value = json };

                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    if (string.IsNullOrEmpty(msg.QueueName))
                    {
                        List<string> topics = GetTopics(msg.ExChange);
                        if (topics == null || topics.Count == 0) { return false; }
                        foreach (var topic in topics)
                        {
                            producer.ProduceAsync(topic, data);
                        }
                    }
                    else
                    {
                        producer.ProduceAsync(msg.QueueName, data);
                    }
                    producer.Flush(TimeSpan.FromSeconds(10));
                }
                return true;
            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
                return false;
            }
        }
        public override bool PublishBatch(List<MQMsg> msgList)
        {
            if (msgList == null || msgList.Count == 0) { return false; }
            try
            {
                if (!_IsListenOK) { return false; }
                var config = new ProducerConfig
                {
                    BootstrapServers = servers,
                    Acks = 0 //保持性能，不需要等待确认，即可发送下一条信息，允许数据丢失。
                };
                using (var producer = new ProducerBuilder<string, string>(config).Build())
                {
                    foreach (var msg in msgList)
                    {
                        string json = msg.ToJson();
                        var data = new Message<string, string> { Key = null, Value = json };

                        if (string.IsNullOrEmpty(msg.QueueName))
                        {
                            List<string> topics = GetTopics(msg.ExChange);
                            if (topics == null || topics.Count == 0) { return false; }
                            foreach (var topic in topics)
                            {
                                producer.Produce(topic, data);
                            }
                        }
                        else
                        {
                            producer.Produce(msg.QueueName, data);
                        }
                    }
                    producer.Flush(TimeSpan.FromSeconds(10));
                }
                return true;
            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
                return false;
            }
        }

        public override bool Listen(string topic, OnReceivedDelegate onReceivedDelegate, string group, bool isBroadcast)
        {
            if (string.IsNullOrEmpty(topic) || onReceivedDelegate == null)
            {
                return false;
            }
            if (!listenFailDic.ContainsKey(topic))
            {
                listenFailDic.Add(topic, new ListenPara() { Event = onReceivedDelegate });
            }
            try
            {
                if (CreateTopicIfNoExists(topic))
                {
                    ListenPara para = new ListenPara() { Group = group, Topic = topic, Event = onReceivedDelegate, IsBroadcast = isBroadcast };
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ListenThread), para);
                    return true;
                }
                else
                {
                    _IsListenOK = false;
                    TryConnect();
                }
            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
            }
            return false;
        }


        private void ListenThread(object topicObj)
        {
            ListenPara para = topicObj as ListenPara;
            try
            {
                var config = new ConsumerConfig
                {
                    BootstrapServers = servers,
                    GroupId = para.Group,
                    EnableAutoCommit = true,
                    AutoOffsetReset = para.IsBroadcast ? AutoOffsetReset.Latest : AutoOffsetReset.Earliest
                };
                using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
                {
                    consumer.Subscribe(para.Topic);
                    listenFailDic.Remove(para.Topic);
                    while (true)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume();
                            ListenPara listenPara = new ListenPara();
                            listenPara.Message= consumeResult.Value;
                            listenPara.Event=para.Event;
                            listenPara.Topic= para.Topic;
                            OnReceive(listenPara);
                        }
                        catch (Exception err)
                        {
                            listenFailDic.Add(para.Topic, para);
                            TryConnect();
                            CYQ.Data.Log.Write(err, "MQ.Kafka");
                        }
                    }

                }

            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
            }

        }

        private void OnReceive(ListenPara para)
        {
            MQMsg msg = MQMsg.Create(para.Message);

            //反转队列名称和监听key
            msg.QueueName = msg.CallBackName;
            msg.CallBackName = para.Topic;

            string subKey = msg.TaskKey;
            msg.TaskKey = msg.CallBackKey;
            msg.CallBackKey = subKey;
            msg.ExChange = null;

            para.Event(msg);
        }

        static readonly object lockTopicObj = new object();
        static List<string> topics = new List<string>();
        /// <summary>
        /// 获取所有项目Topics
        /// </summary>
        public List<string> Topics
        {
            get
            {
                if (topics.Count == 0)
                {
                    lock (lockTopicObj)
                    {
                        if (topics.Count == 0)
                        {
                            ReSetTopics(servers);
                        }
                    }
                }

                return topics;
            }
        }

        /// <summary>
        /// 更新【获取所有】主题列表
        /// </summary>
        private void ReSetTopics(string servers)
        {
            try
            {
                var config = new AdminClientConfig
                {
                    BootstrapServers = servers // Kafka 服务器地址和端口
                };
                Metadata metadata;
                using (var adminClient = new AdminClientBuilder(config).Build())
                {
                    metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                }
                if (metadata != null && metadata.Topics != null)
                {
                    topics.Clear();
                    foreach (var topic in metadata.Topics)
                    {
                        if (topic.Topic.StartsWith("DTS_") && !topics.Contains(topic.Topic))
                        {
                            topics.Add(topic.Topic);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
            }
        }

        /// <summary>
        /// 根据交换机名称获取所有主题名称
        /// </summary>
        private List<string> GetTopics(string exName)
        {
            List<string> topicList = new List<string>();
            try
            {
                foreach (var topic in Topics)
                {
                    if (topic.StartsWith(exName))
                    {
                        topicList.Add(topic);
                    }
                }
            }
            catch (Exception err)
            {
                CYQ.Data.Log.Write(err, "MQ.Kafka");
            }
            return topicList;
        }
        private bool CreateTopicIfNoExists(string topic)
        {
            if (Topics.Contains(topic)) { return true; }
            try
            {
                var config = new AdminClientConfig
                {
                    BootstrapServers = servers // Kafka 服务器地址和端口
                };
                using (var adminClient = new AdminClientBuilder(config).Build())
                {
                    // 创建主题
                    var topicSpecification = new TopicSpecification
                    {
                        Name = topic,
                        NumPartitions = 1,
                        ReplicationFactor = 1
                    };
                    adminClient.CreateTopicsAsync(new[] { topicSpecification }).GetAwaiter().GetResult();
                    //发一条广播，通知大伙，我上线了。

                    if (!topics.Contains(topic))
                    {
                        topics.Add(topic);
                    }

                }
                return true;
            }
            catch (Exception err)
            {
                if (err.Message.Contains("already exists."))
                {
                    if (!topics.Contains(topic))
                    {
                        topics.Add(topic);
                    }
                    return true;
                }
                CYQ.Data.Log.Write(err, "MQ.Kafka");
                return false;
            }

        }

        public class ListenPara
        {
            public string Group { get; set; }
            public string Topic { get; set; }
            public string Message { get; set; }
            public OnReceivedDelegate Event { get; set; }
            public bool IsBroadcast { get; set; }

        }
    }


}

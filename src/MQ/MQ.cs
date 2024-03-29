﻿
using CYQ.Data.Tool;
using System.Collections.Generic;

namespace Taurus.Plugin.DistributedTask
{
    internal enum KeyType
    {
        ClientRabbit = 1,
        ServerRabbit = 2,

        ClientKafka = 11,
        ServerKafka = 12,

    }
    internal abstract partial class MQ
    {
        public delegate void OnReceivedDelegate(MQMsg msg);

        private static MDictionary<string, MQ> instanceDic = new MDictionary<string, MQ>();
        private static MQ GetInstance(KeyType keyType, string conn)
        {
            int type = (int)(keyType);
            string key = type + conn;
            if (instanceDic.ContainsKey(key))
            {
                return instanceDic[key];
            }
            if (type < 10)
            {
                var rabbit = new MQRabbit(conn);
                instanceDic.Add(key, rabbit);
            }
            else
            {
                var kafka = new MQKafka(conn, keyType == KeyType.ClientKafka);
                instanceDic.Add(key, kafka);
            }
            return instanceDic[key];

        }

        public static MQ Client
        {
            get
            {
                if (!string.IsNullOrEmpty(DTSConfig.Client.Rabbit))
                {
                    return GetInstance(KeyType.ClientRabbit, DTSConfig.Client.Rabbit);
                }
                else if (!string.IsNullOrEmpty(DTSConfig.Client.Kafka))
                {
                    return GetInstance(KeyType.ClientKafka, DTSConfig.Client.Kafka);
                }
                return Empty.Instance;
            }
        }
        public static MQ Server
        {
            get
            {
                if (!string.IsNullOrEmpty(DTSConfig.Server.Rabbit))
                {
                    return GetInstance(KeyType.ServerRabbit, DTSConfig.Server.Rabbit);
                }
                else if (!string.IsNullOrEmpty(DTSConfig.Server.Kafka))
                {
                    return GetInstance(KeyType.ServerKafka, DTSConfig.Server.Kafka);
                }
                return Empty.Instance;
            }
        }
        public abstract MQType MQType { get; }
        public abstract bool Publish(MQMsg msg);
        public abstract bool PublishBatch(List<MQMsg> msgList);
        /// <summary>
        /// 监听队列
        /// </summary>
        /// <param name="queueOrTopic">Rabbit：队列名；Kafka：主题名称</param>
        /// <param name="exNameOrGroup">Rabbit：交换机；Kafka：监听组名</param>
        /// <param name="isAutoDelete">Rabbit：是否临时队列，应用关闭时自动删除队列；Rabbit：此参数无效。</param>
        /// <returns></returns>
        public abstract bool Listen(string queueOrTopic, OnReceivedDelegate onReceivedDelegate, string exNameOrGroup, bool isAutoDelete);

    }

    internal class Empty : MQ
    {
        public static readonly Empty Instance = new Empty();

        public override MQType MQType
        {
            get
            {
                return MQType.Empty;
            }
        }


        public override bool Listen(string queueName, OnReceivedDelegate onReceivedDelegate, string bindExName, bool isAutoDelete)
        {
            return false;
        }

        public override bool Publish(MQMsg msg)
        {
            return false;
        }

        public override bool PublishBatch(List<MQMsg> msgList)
        {
            return false;
        }
    }

    internal enum MQType
    {
        Empty,
        Rabbit,
        Kafka
    }
}

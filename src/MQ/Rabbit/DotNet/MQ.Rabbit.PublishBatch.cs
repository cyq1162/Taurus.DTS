using CYQ.Data;
using CYQ.Data.Tool;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Taurus.Plugin.DistributedTask
{

    internal partial class MQRabbit
    {
        public override bool PublishBatch(List<MQMsg> msgList)
        {
            if (msgList == null || msgList.Count == 0)
            {
                return false;
            }
            if (!IsConnectOK) { return false; }
            try
            {
                //net 版本没有批量功能
                using (var channel = DefaultConnection.CreateModel())
                {
                    foreach (var msg in msgList)
                    {
                        if (string.IsNullOrEmpty(msg.QueueName) && string.IsNullOrEmpty(msg.ExChange))
                        {
                            continue;
                        }
                        string json = msg.ToJson();
                        byte[] bytes = Encoding.UTF8.GetBytes(json);
                        if (!string.IsNullOrEmpty(msg.QueueName))
                        {
                            if (!declareQueueNames.Contains(msg.QueueName))
                            {
                                if (msg.DelayMinutes.HasValue && msg.DelayMinutes.Value > 0 && !string.IsNullOrEmpty(msg.ExChange))
                                {
                                    //绑定交换机
                                    IDictionary<string, object> arg = new Dictionary<string, object>();
                                    arg.Add("x-dead-letter-exchange", msg.ExChange);//使用默认交换机
                                    //arg.Add("x-dead-letter-routing-key", "");//设置转移到的队列
                                    arg.Add("x-message-ttl", msg.DelayMinutes.Value * 60 * 1000);//设置过期时间
                                    channel.QueueDeclare(msg.QueueName, true, false, false, arguments: arg);//允许丢失，不需要持久化。
                                    declareQueueNames.Add(msg.QueueName);
                                }
                            }

                            channel.BasicPublish("", msg.QueueName, null, body: bytes);
                        }
                        else
                        {
                            channel.BasicPublish(msg.ExChange, "", null, body: bytes);
                        }
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

        private string GetBodyJson(BasicDeliverEventArgs ea)
        {
            return Encoding.UTF8.GetString(ea.Body);
        }

    }
}

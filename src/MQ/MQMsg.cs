using System;
using CYQ.Data.Json;

namespace Taurus.Plugin.DistributedTask
{

    /// <summary>
    /// MQ传递消息实体
    /// </summary>
    internal class MQMsg
    {
        public string MsgID { get; set; }
        public string TaskType { get; set; }
        public string Content { get; set; }
        /// <summary>
        /// 任务Key
        /// </summary>
        public string TaskKey { get; set; }


        /// <summary>
        /// 方法绑定Key
        /// </summary>
        public string CallBackKey { get; set; }

        /// <summary>
        /// 用于发送的交换机名称【对应 kafka 的 topic】
        /// </summary>
        public string ExChange { get; set; }
        /// <summary>
        /// 用于发送的队列名称【对应 kafka 的 groupID】
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// 用于回调的队列名称
        /// </summary>
        public string CallBackName { get; set; }

        /// <summary>
        /// >0 发送延时交换机
        /// </summary>
        public int? DelayMinutes { get; set; }

        /// <summary>
        /// 是否首次响应Ack
        /// </summary>
        public bool? IsFirstAck { get; set; }

        public bool? IsDeleteAck { get; set; }

        /// <summary>
        /// 任务创建时间
        /// </summary>
        public DateTime? TaskTime { get; set; }

        /// <summary>
        /// 设置成响应删除状态，同时减少不需要传递用的数据。
        /// </summary>
        internal MQMsg SetDeleteAsk()
        {
            IsDeleteAck = true;
            Content = null;
            TaskKey = null;
            TaskTime = null;
            CallBackKey = null;
            CallBackName = null;
            ExChange = null;
            IsFirstAck = null;
            DelayMinutes = null;
            return this;
        }
        /// <summary>
        /// 设置成响应纯属Ack状态，同时减少不需要传递用的数据。
        /// </summary>
        internal MQMsg SetFirstAsk()
        {
            IsFirstAck = true;
            Content = null;
            TaskTime = null;
            CallBackName = null;
            ExChange = null;
            IsDeleteAck = null;
            DelayMinutes = null;
            return this;
        }

        /// <summary>
        /// 直接实现，避免反射
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            JsonHelper js = new JsonHelper(false, false);
            js.Add("MsgID", this.MsgID);
            js.Add("TaskType", this.TaskType);
            if (this.Content != null)
            {
                js.Add("Content", this.Content);
            }
            if (this.TaskKey != null)
            {
                js.Add("TaskKey", this.TaskKey);
            }
            if (this.CallBackKey != null)
            {
                js.Add("CallBackKey", this.CallBackKey);
            }
            if (this.ExChange != null)
            {
                js.Add("ExChange", this.ExChange);
            }
            js.Add("QueueName", this.QueueName);
            if (this.CallBackName != null)
            {
                js.Add("CallBackName", this.CallBackName);
            }
            if (this.DelayMinutes.HasValue)
            {
                js.Add("DelayMinutes", this.DelayMinutes.Value);
            }
            if (this.IsFirstAck.HasValue)
            {
                js.Add("IsFirstAck", this.IsFirstAck.Value ? "true" : "false", true);
            }
            if (this.IsDeleteAck.HasValue)
            {
                js.Add("IsDeleteAck", this.IsDeleteAck.Value ? "true" : "false", true);
            }
            if (this.TaskTime.HasValue)
            {
                js.Add("TaskTime", this.TaskTime.Value.ToString());
            }
            return js.ToString();
        }
        public static MQMsg Create(string json)
        {
            MQMsg msg = new MQMsg();
            if (!string.IsNullOrEmpty(json))
            {
                var dic = JsonHelper.Split(json);
                if (dic.ContainsKey("MsgID")) { msg.MsgID = dic["MsgID"]; }
                if (dic.ContainsKey("TaskType")) { msg.TaskType = dic["TaskType"]; }
                if (dic.ContainsKey("Content")) { msg.Content = dic["Content"]; }
                if (dic.ContainsKey("TaskKey")) { msg.TaskKey = dic["TaskKey"]; }
                if (dic.ContainsKey("CallBackKey")) { msg.CallBackKey = dic["CallBackKey"]; }
                if (dic.ContainsKey("ExChange")) { msg.ExChange = dic["ExChange"]; }
                if (dic.ContainsKey("QueueName")) { msg.QueueName = dic["QueueName"]; }
                if (dic.ContainsKey("CallBackName")) { msg.CallBackName = dic["CallBackName"]; }
                if (dic.ContainsKey("DelayMinutes")) { msg.DelayMinutes = int.Parse(dic["DelayMinutes"]); }
                if (dic.ContainsKey("IsFirstAck")) { msg.IsFirstAck = Convert.ToBoolean(dic["IsFirstAck"]); }
                if (dic.ContainsKey("IsDeleteAck")) { msg.IsDeleteAck = Convert.ToBoolean(dic["IsDeleteAck"]); }
                if (dic.ContainsKey("TaskTime")) { msg.TaskTime = Convert.ToDateTime(dic["TaskTime"]); }
            }
            return msg;
        }
    }
}

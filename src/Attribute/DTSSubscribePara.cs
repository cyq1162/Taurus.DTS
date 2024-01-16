using CYQ.Data.Tool;

namespace Taurus.Plugin.DistributedTask
{

    /// <summary>
    /// DTSSubscribe 标注的方法传递参数
    /// </summary>
    public class DTSSubscribePara
    {
        internal DTSSubscribePara(MQMsg msg)
        {
            this.MsgID = msg.MsgID;
            this.TaskType = ConvertTool.ChangeType<TaskType>(msg.TaskType);
            this.Content = msg.Content;
            this.SubKey = msg.CallBackKey;
        }
        /// <summary>
        /// 消息唯一ID
        /// </summary>
        public string MsgID { get; set; }
        /// <summary>
        /// 执行类型
        /// </summary>
        public TaskType TaskType { get; set; }
        /// <summary>
        /// 传递的消息内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 如果需要写入内容发往回调处，可以对此赋值。
        /// </summary>
        public string CallBackContent { get; set; }
        /// <summary>
        /// 方法绑定Key
        /// </summary>
        public string SubKey { get; set; }

    }
}

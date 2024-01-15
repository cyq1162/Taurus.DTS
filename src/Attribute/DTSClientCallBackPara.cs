using System;
using System.Collections.Generic;
using System.Text;
using CYQ.Data.Tool;

namespace Taurus.Plugin.DistributedTask
{

    /// <summary>
    /// DTCClientCallBack 标注的方法传递参数
    /// </summary>
    public class DTSClientCallBackPara
    {
        internal DTSClientCallBackPara(MQMsg msg)
        {
            this.MsgID = msg.MsgID;
            this.TaskType = ConvertTool.ChangeType<TaskType>(msg.TaskType);
            this.CallBackContent = msg.Content;
            this.TaskKey = msg.TaskKey;
            this.CallBackKey = msg.CallBackKey;
        }
        /// <summary>
        /// 唯一消息ID
        /// </summary>
        public string MsgID { get; set; }
        /// <summary>
        /// 执行类型
        /// </summary>
        public TaskType TaskType { get; set; }

        /// <summary>
        /// 回调回来的消息内容
        /// </summary>
        public string CallBackContent { get; set; }
        /// <summary>
        /// 任务Key
        /// </summary>
        public string TaskKey { get; set; }

        /// <summary>
        /// 回调Key
        /// </summary>
        public string CallBackKey { get; set; }

    }
}

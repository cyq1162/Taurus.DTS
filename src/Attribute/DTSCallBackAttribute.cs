using System;
using System.Data;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 用于分布式任务客户端【即调用端】回调，CallBackKey 区分大小写。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DTSCallBackAttribute : Attribute
    {
        /// <summary>
        /// 获取设置的监听名称。
        /// </summary>
        public string CallBackKey { get; set; }


        /// <summary>
        /// 用于分布式事务回调订阅。
        /// </summary>
        /// <param name="callBackKey">监听的名称</param>
        public DTSCallBackAttribute(string callBackKey)
        {
            this.CallBackKey = callBackKey;
        }
    }
}

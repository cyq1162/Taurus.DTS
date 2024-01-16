using System;
using System.Data;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 用于分布式任务服务端【即提供端】订阅任务，SubKey 区分大小写。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DTSSubscribeAttribute : Attribute
    {
        /// <summary>
        /// 获取设置的监听名称。
        /// </summary>
        public string SubKey { get; set; }

        /// <summary>
        /// 用于分布式事务回调订阅。
        /// </summary>
        /// <param name="subKey">监听的名称</param>
        public DTSSubscribeAttribute(string subKey)
        {
            this.SubKey = subKey;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Taurus.Plugin.DistributedTask
{
    public enum TaskType
    {
        /// <summary>
        /// 即时任务
        /// </summary>
        Instant = 0,
        /// <summary>
        /// 延时任务
        /// </summary>
        Delay = 1,
        /// <summary>
        /// 基于 Cron 表达式的重复性任务
        /// </summary>
        Cron = 2,
        /// <summary>
        /// 广播任务
        /// </summary>
        Broadcast
    }

   

}

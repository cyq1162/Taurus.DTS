using System;
using System.Collections.Generic;
using System.Text;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 任务清除模式，仅对数据库有效，非数据库模式都是直接Delete模式。
    /// </summary>
    public enum TaskClearMode
    {
        /// <summary>
        /// 删除数据
        /// </summary>
        Delete = 0,
        /// <summary>
        /// 转移到历史表
        /// </summary>
        MoveToHistoryTable = 1
    }
}

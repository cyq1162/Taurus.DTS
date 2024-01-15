using System;
using System.Collections.Generic;
using System.Text;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 广播类型
    /// </summary>
    internal enum BroadcastType
    {
        None = -1,
        Client = 0,
        Server = 1,
        Both = 2
    }
}

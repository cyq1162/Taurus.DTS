using CYQ.Data;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        /// <summary>
        /// 分布式事务 提供端
        /// </summary>
        public static partial class Server
        {
            /// <summary>
            /// dtc 写数据库、写队列
            /// </summary>
            internal partial class Worker
            {
                public static bool Add(TaskTable table)
                {
                    bool result = false;
                    if (!string.IsNullOrEmpty(DTSConfig.Server.Conn) && table.Insert(InsertOp.None))
                    {
                        Log.Print("DB.Write : " + table.ToJson());
                        result = true;
                        table.Dispose();
                    }
                    if (!result)
                    {
                        result = IO.Write(table);//写 DB => Redis、MemCache，失败则写文本。;
                    }
                    if (result)
                    {
                        Scanner.Start();//检测未启动则启动，已启动则忽略。
                    }
                    return result;
                }
            }
        }
    }
}

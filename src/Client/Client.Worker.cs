using CYQ.Data;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            internal static partial class Worker
            {
                public static bool Save(TaskTable table,out bool isWriteTxt)
                {
                    isWriteTxt = false;
                    bool result = false;
                    if (!string.IsNullOrEmpty(DTSConfig.Client.Conn) && table.Insert(InsertOp.None))
                    {
                        Log.Print("DB.Write : " + table.ToJson());
                        result = true;
                        table.Dispose();
                    }
                    if (!result)
                    {
                        result = IO.Write(table, out isWriteTxt);//写 DB => Redis、MemCache，失败则写文本。
                    }
                    return result;
                }
                public static void Start(MQMsg msg)
                {
                    MQPublisher.Add(msg);//异步发送MQ
                    Scanner.Start();//检测未启动则启动，已启动则忽略。
                }
            }



        }
    }
}

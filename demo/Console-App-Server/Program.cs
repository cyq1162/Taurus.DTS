using CYQ.Data;
using System;
using Taurus.Plugin.DistributedTask;

namespace Console_App_Server
{
    internal class Program
    {
        static void Main(string[] args)
        {

            DTSConfig.Server.Rabbit = "127.0.0.1;guest;guest;/";
            //DTSConfig.Server.Kafka = "127.0.0.1:9092;";
            //DTSConfig.Server.Conn = DTSConfig.Client.Conn;

            DTSConfig.ProjectName = "ConsoleApp5";

            DTS.Server.Start();//start client and server

            Console.WriteLine("---------------------------------------");

            Console.ReadLine();
        }


    }


    /// <summary>
    /// 服务端 server class need to public
    /// </summary>
    public class Server
    {
        [DTSSubscribe("DoInstantTask")]
        public static bool A(DTSSubscribePara para)
        {
            para.CallBackContent = "show you a.";
            return true;
        }

        [DTSSubscribe("DoInstantTask")]
        private static bool B(DTSSubscribePara para)
        {
            para.CallBackContent = "show you b.";
            return true;
        }
        [DTSSubscribe("DoCronTask")]
        private static bool C(DTSSubscribePara para)
        {
            para.CallBackContent = "show you c.";
            return true;
        }
        /// <summary>
        /// 定时任务
        /// </summary>
        [DTSSubscribe("DoBroadastTask")]
        private static bool TimerTask(DTSSubscribePara para)
        {
            para.CallBackContent = "show you d.";
            return true;
        }
    }
}

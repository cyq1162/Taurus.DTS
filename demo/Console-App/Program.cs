using System;
using System.Net;
using System.Threading;
using Taurus.Plugin.DistributedTask;

namespace Console_App
{
    internal class Program
    {
        static void StartWithConfig()
        {
            DTSConfig.Client.IsPrintTraceLog = false;
            DTSConfig.Server.IsPrintTraceLog = false;

            DTSConfig.Client.Rabbit = "127.0.0.1;guest;guest;/";
            DTSConfig.Server.Rabbit = "127.0.0.1;guest;guest;/";

            //DTSConfig.Client.Conn = "server=.;database=mslog;uid=sa;pwd=123456";
            //DTSConfig.Server.Conn = DTSConfig.Client.Conn;

            //DTSConfig.ProjectName = "ConsoleApp";

            DTS.Start();//start client and server
        }

        static void Main(string[] args)
        {
            StartWithConfig();
            Thread.Sleep(1000);
            Console.WriteLine("---------------------------------------");
            Console.WriteLine("1-InstantTask、2-DelayTask（1Minutes）、3-CronTask、4-DeleteCronTask、5-BroadastTask");
            Console.WriteLine("Input ：1、2、3、4、5，Press Enter.");
            while (true)
            {
                string line = Console.ReadLine();
                try
                {
                    Client.Run(int.Parse(line));
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }

            }
        }
    }

    /// <summary>
    /// 客户端 client class need to public if has callback method.
    /// </summary>
    public class Client
    {
        public static void Run(int i)
        {
            if (i == 2)
            {
                //发布一个延时1分钟的任务
                DTS.Client.Delay.PublishAsync(1, "i publish a delay task.", "DoInstantTask", "DelayCallBack");
                Console.WriteLine("Wait for 1 minute...");
            }
            else if (i == 3)
            {
                //发布一个秒在30时的循环任务。
                DTS.Client.Cron.PublishAsync("10,30,50 * * * * ?", "i publish a timer task with cron express.", "DoCronTask", "CronCallBack");
                Console.WriteLine("Wait for execute task when second is 10,30,50...");
            }
            else if (i == 4)
            {
                //发布一个秒在30时的循环任务。
                DTS.Client.Cron.DeleteAsync("DoCronTask", null, "CronCallBack");
            }
            else if (i == 5)
            {
                //发布一个广播任务
                DTS.Client.Broadast.PublishAsync("i publish a task for all server.", "DoBroadastTask", "BroadastCallBack");
            }
            else
            {
                //发布一个即时任务
                DTS.Client.Instant.PublishAsync("i publish a task instantly.", "DoInstantTask", "InstantCallBack");
            }
        }

        [DTSCallBack("InstantCallBack")]
        [DTSCallBack("DelayCallBack")]
        [DTSCallBack("CronCallBack")]
        [DTSCallBack("BroadastCallBack")]
        private static void OnCallBack(DTSCallBackPara para)
        {
            Console.WriteLine("Client callback : " + para.TaskType + " - " + para.CallBackKey + " - " + para.CallBackContent);
        }
    }


    /// <summary>
    /// 服务端 server class need to public
    /// </summary>
    public class Server
    {
        [DTSSubscribe("DoInstantTask")]
        private bool A(DTSSubscribePara para)
        {
            para.CallBackContent = "show you a.";
            return true;
        }

        [DTSSubscribe("DoInstantTask")]
        private bool B(DTSSubscribePara para)
        {
            para.CallBackContent = "show you b.";
            return true;
        }
        [DTSSubscribe("DoCronTask")]
        private bool C(DTSSubscribePara para)
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

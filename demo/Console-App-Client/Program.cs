using CYQ.Data;
using System;
using System.Threading;
using Taurus.Plugin.DistributedTask;

namespace Console_App_Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DTSConfig.Client.IsPrintTraceLog = false;
            //AppConfig.Redis.Servers = "127.0.0.1:6379";

            DTSConfig.Client.Rabbit = "127.0.0.1;guest;guest;/";
            //DTSConfig.Client.Kafka = "127.0.0.1:9092;";
            DTSConfig.Client.Conn = "server=.;database=mslog;uid=sa;pwd=123456";

            DTSConfig.ProjectName = "ConsoleApp5";

            DTS.Client.Start();//start client and server
            
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
                catch(Exception err)
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
                for (int k = 0; k < 1; k++)
                {
                    //发布一个即时任务
                    DTS.Client.Instant.PublishAsync("i publish a task instantly.", "DoInstantTask", "InstantCallBack");
                    Console.WriteLine(k);
                }
                
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
}

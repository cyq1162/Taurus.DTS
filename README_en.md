# Taurus.DTS is Distributed task scheduler
<h3>【 <a href='./README.md'>中文</a> | <a href='./README_en.md'>English</a>】</h3>
<hr />
# Taurus DTS distributed task scheduler, using Net Core Example:：

<h4>Basic Description：</h4>
<p>1. The framework is divided into Client (client, i.e. task initiator) and Server (server, i.e. method subscriber).</p>
<p>2. The framework supports four methods: real-time tasks, delayed tasks, Cron expression tasks, scheduled tasks, and broadcast tasks.</p>
<p>3. Parameters that need to be configured for the project: 1. Database (optional); 2. MQ (mandatory).</p>

<h4>Data Storage：</h4>
<p>Database selection (one of the more than 10 databases supported by CYQ. Data, such as MSSQL, MySql, Oracle, PostgreSQL, etc.)</p>
<p>The MSSQL configuration example is as follows：</p>
<pre><code>{
  "ConnectionStrings": {
    "DTS.Server.Conn": "server=.;database=MSLog;uid=sa;pwd=123456"
  }
}</code></pre>
<h4>Message queue：</h4>

<p>At present, the message queue supports RabbitMQ or Kafka (one can be configured)：</p>
<pre><code>{
  "AppSettings": {
  "DTS.Server.Rabbit":"127.0.0.1;guest;guest;/",//ip;username;password;virtualpath;
  "DTS.Server.Kafka":"127.0.0.1:9092" 
  }
}</code></pre>
<p>The above configuration is for the server side, and the client can change the server to the client side。</p>


# Server side usage examples：
<p>1、Nuget search Taurus Introducing DTC into engineering projects。</p>
<p>2、ASP.Net Core ：Program or Startup add service usage introduction:</p>
<pre><code>  services.AddTaurusDtc(); 
  app.UseTaurusDtc(StartType.Server); 
</code></pre>
<p>3、appsettings.json config：</p>
<pre><code>  {
  "ConnectionStrings": {
    "DTS.Server.Conn": "host=localhost;port=3306;database=cyqdata;uid=root;pwd=123456;Convert Zero Datetime=True;"
  },
  "AppSettings": {
    "DTS.Server.Rabbit": "127.0.0.1;guest;guest;/" //IP;UserName;Password;VirtualPaath
}</code></pre>
<p>4、Select the corresponding dependency components for the database, such as MySql, which can：</p>
<pre><code>You can search for MySql on Nuget Data, or CYQ Data MySql (which will automatically import MySql. Data) is available, just import the project.
</code></pre>
<p>5、Code writing can refer to the example code provided in the source code, the console demo as follows：</p>
<pre><code>
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
    /// server class need to public
    /// </summary>
    public class Server
    {
        [DTSSubscribe("DoInstantTask")]
        public static bool A(DTSSubscribePara para)
        {
            para.CallBackContent = "show you a.";
            return true;
        }

        [DTSSubscribe("DoDelayTask")]
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

        [DTSSubscribe("DoBroadastTask")]
        private static bool D(DTSSubscribePara para)
        {
            para.CallBackContent = "show you d.";
            return true;
        }
    }
}

</code></pre>

# Client Example of End Use：
<p>1、Nuget search Taurus Introducing DTC into engineering projects。</p>
<p>2、ASP.Net Core : Program or Startup adding service usage introduction：</p>
<pre><code>  services.AddTaurusDtc(); 
  app.UseTaurusDtc(StartType.Client); 
</code></pre>
<p>3、appsettings.json config：</p>
<pre><code>  {
  "ConnectionStrings": {
    "DTS.Client.Conn": "host=localhost;port=3306;database=cyqdata;uid=root;pwd=123456;Convert Zero Datetime=True;"
  },
  "AppSettings": {
    "DTS.Client.Rabbit": "127.0.0.1;guest;guest;/" //IP;UserName;Password;VirtualPaath
}</code></pre>
<p>4、Select the corresponding dependency components for the database, such as MySql, which can：</p>
<pre><code>You can search for MySql on Nuget Data, or CYQ Data MySql (which will automatically import MySql. Data) is available, just import the project.
</code></pre>
<p>5、Code writing can refer to the example code provided in the source code, the console demo as follows：</p>
<pre><code>
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
    /// client class need to public if has callback method.
    /// </summary>
    public class Client
    {
        public static void Run(int i)
        {

            if (i == 2)
            {
                //Publish a task with a 1-minute delay
                DTS.Client.Delay.PublishAsync(1, "i publish a delay task.", "DoDelayTask", "DelayCallBack");
                Console.WriteLine("Wait for 1 minute...");
            }
            else if (i == 3)
            {
                //Publish a cyclic task with a duration of 10, 30, and 50 seconds.
                DTS.Client.Cron.PublishAsync("10,30,50 * * * * ?", "i publish a timer task with cron express.", "DoCronTask", "CronCallBack");
                Console.WriteLine("Wait for execute task when second is 10,30,50...");
            }
            else if (i == 4)
            {
                // delete cron task.
                DTS.Client.Cron.DeleteAsync("DoCronTask", null, "CronCallBack");
            }
            else if (i == 5)
            {
                // publish a broadcast task.
                DTS.Client.Broadast.PublishAsync("i publish a task for all server.", "DoBroadastTask", "BroadastCallBack");
            }
            else
            {
                for (int k = 0; k < 1; k++)
                {
                    // publish a instant task.
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

</code></pre>

# A comprehensive collection of various database linking statements
<pre><code>
###--------------------------------------------------------###

   Txt::  Txt Path=E:\
   Xml::  Xml Path=E:\
Access::  Provider=Microsoft.Jet.OLEDB.4.0; Data Source=E:\cyqdata.mdb
Sqlite::  Data Source=E:\cyqdata.db;failifmissing=false;
 MySql::  host=localhost;port=3306;database=cyqdata;uid=root;pwd=123456;Convert Zero Datetime=True;
 Mssql::  server=.;database=cyqdata;uid=sa;pwd=123456;provider=mssql; 
Sybase::  data source=127.0.0.1;port=5000;database=cyqdata;uid=sa;pwd=123456;provider=sybase; 
Postgre:  server=localhost;uid=sa;pwd=123456;database=cyqdata;provider=pg; 
    DB2:  Database=SAMPLE;User ID=administrator;Server=127.0.0.1;password=1234560;provider=db2; 
FireBird  user id=SYSDBA;password=123456;database=d:\\test.dbf;server type=Default;data source=127.0.0.1;port number=3050;provider=firebird;
Dameng::  user id=SYSDBA;password=123456789;data source=127.0.0.1;schema=test;provider=dameng;
KingBaseES server=127.0.0.1;User Id=system;Password=123456;Database=test;Port=54321;schema=public;provider=kingbasees;
Oracle ODP.NET::
Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=127.0.0.1)(PORT = 1521)))(CONNECT_DATA =(SID = orcl)));User ID=sa;password=123456

Due to the basic consistency of various database linking statements, except for specific writing methods, they can be supplemented through linking：provider=mssql、provider=mysql、provider=db2、provider=postgre。
###--------------------------------------------------------###
</code></pre>

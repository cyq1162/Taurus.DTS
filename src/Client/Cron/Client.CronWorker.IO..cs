using CYQ.Data;
using CYQ.Data.Tool;
using CYQ.Data.Cache;
using CYQ.Data.Lock;
using System.Diagnostics;
using System.Collections.Generic;
using CYQ.Data.Json;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            internal static partial class CronWorker
            {
                internal static partial class IO
                {
                    private static string GetKey(string id)
                    {
                        return Worker.IO.GetKey(id);
                    }

                    const string CronTableType = "cron-table";
                    public static bool WriteCronTable(CronTable table)
                    {
                        string id = table.MsgID;
                        string json = table.ToJson();
                        string path = AppConfig.WebRootPath + "App_Data/dts/client/" + CronTableType + "/" + id + ".txt";
                        bool isOK = IOHelper.Write(path, json);
                        if (isOK)
                        {
                            Log.Print("IO.Write : " + json);
                        }
                        return isOK;

                    }

                    public static CronTable SearchCronTable(string taskKey, string cron)
                    {
                        List<CronTable> list = GetCronTable();
                        foreach (var item in list)
                        {
                            if (item.TaskKey == taskKey)
                            {
                                if (string.IsNullOrEmpty(cron)) { return item; }
                                else if (item.Cron == cron) { return item; }
                            }
                        }
                        return null;
                    }


                    public static bool DeleteCronTable(string msgID, string taskType)
                    {
                        string id = msgID;
                        string path = AppConfig.WebRootPath + "App_Data/dts/client/" + CronTableType + "/" + id + ".txt";
                        return IOHelper.Delete(path);

                    }

                    /// <summary>
                    /// 获取Cron表达式，以便进行任务处理。
                    /// </summary>
                    public static List<CronTable> GetCronTable()
                    {
                        List<CronTable> tables = new List<CronTable>();

                        string folder = AppConfig.WebRootPath + "App_Data/dts/client/" + CronTableType;
                        if (System.IO.Directory.Exists(folder))
                        {
                            string[] files = IOHelper.GetFiles(folder, "*.txt", System.IO.SearchOption.TopDirectoryOnly);
                            if (files != null && files.Length > 0)
                            {
                                foreach (string file in files)
                                {
                                    string json = IOHelper.ReadAllText(file, 0);
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        tables.Add(JsonHelper.ToEntity<CronTable>(json));
                                    }
                                }
                            }
                        }

                        return tables;
                    }
                }
            }

        }
    }
}

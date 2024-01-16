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
                    internal static void SetIDListWithDisLock(DistributedCache disCache, string id, string taskType, bool isAdd)
                    {
                        Worker.IO.SetIDListWithDisLock(disCache, id, taskType, isAdd);
                    }

                    const string CronTableType = "cron-table";
                    public static bool WriteCronTable(CronTable table)
                    {
                        var disCache = DistributedCache.Instance;
                        string id = table.MsgID;
                        string json = table.ToJson();
                        //写入Redis
                        bool isOK = false;
                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            isOK = disCache.Set(GetKey(id), json, DTSConfig.Client.Worker.TimeoutKeepSecond / 60);//写入分布式缓存
                            SetIDListWithDisLock(disCache, id, CronTableType, true);
                        }
                        if (isOK)
                        {
                            Log.Print(disCache.CacheType + ".Write : " + json);
                        }
                        else
                        {
                            string path = AppConfig.WebRootPath + "App_Data/dts/client/" + CronTableType + "/" + id + ".txt";
                            isOK = IOHelper.Write(path, json);
                            if (isOK)
                            {
                                Log.Print("IO.Write : " + json);
                            }
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
                        var disCache = DistributedCache.Instance;
                        bool isOK = false;
                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            isOK = disCache.Remove(GetKey(id));//删除数据。
                            SetIDListWithDisLock(disCache, id, taskType, false);
                        }
                        if (!isOK)
                        {
                            string path = AppConfig.WebRootPath + "App_Data/dts/client/" + CronTableType + "/" + id + ".txt";
                            isOK = IOHelper.Delete(path);
                        }
                        return isOK;
                    }

                    /// <summary>
                    /// 获取Cron表达式，以便进行任务处理。
                    /// </summary>
                    public static List<CronTable> GetCronTable()
                    {
                        List<CronTable> tables = new List<CronTable>();
                        var disCache = DistributedCache.Instance;

                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            string taskID2 = disCache.Get<string>(GetKey(CronTableType));
                            var ids = taskID2;
                            if (!string.IsNullOrEmpty(ids))
                            {
                                foreach (string id in ids.Split(','))
                                {
                                    var json = disCache.Get<string>(GetKey(id));
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        var entity = JsonHelper.ToEntity<CronTable>(json);
                                        if (entity != null)
                                        {
                                            tables.Add(entity);
                                        }
                                    }
                                }
                            }
                        }
                        if (tables.Count == 0)
                        {
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
                        }
                        return tables;
                    }
                }
            }

        }
    }
}

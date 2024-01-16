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
            /// <summary>
            /// dtc 写数据库、写队列
            /// </summary>
            internal static partial class Worker
            {

                internal static partial class IO
                {
                    internal static string GetKey(string key)
                    {
                        return "DTS.Client:" + key;
                    }
                    public static bool Write(TaskTable table)
                    {
                        bool isWriteTxt = false;
                        return Write(table, out isWriteTxt);
                    }
                    /// <summary>
                    /// 写入数据
                    /// </summary>
                    public static bool Write(TaskTable table, out bool isWriteTxt)
                    {
                        isWriteTxt = false;
                        var disCache = DistributedCache.Instance;
                        string id = table.MsgID;
                        string json = table.ToJson();
                        //写入Redis
                        bool isOK = false;
                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            isOK = disCache.Set(GetKey(id), json, DTSConfig.Client.Worker.TimeoutKeepSecond / 60);//写入分布式缓存
                            SetIDListWithDisLock(disCache, id, table.TaskType, true);
                        }
                        if (isOK)
                        {
                            Log.Print(disCache.CacheType + ".Write : " + json);
                        }
                        else
                        {
                            string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + table.TaskType.ToLower() + "/" + id + ".txt";
                            isOK = IOHelper.Write(path, json);
                            if (isOK)
                            {
                                isWriteTxt = true;
                                Log.Print("IO.Write : " + json);
                            }
                        }
                        return isOK;
                    }

                    /// <summary>
                    /// 删除数据
                    /// </summary>
                    public static bool Delete(string msgID, string taskType)
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
                            string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + taskType.ToLower() + "/" + id + ".txt";
                            isOK = IOHelper.Delete(path);
                        }
                        return isOK;
                    }
                    /// <summary>
                    /// 读取数据
                    /// </summary>
                    public static string Read(string msgID, string taskType)
                    {
                        string id = msgID;
                        var disCache = DistributedCache.Instance;
                        string result = null;
                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            result = disCache.Get<string>(GetKey(id));
                        }
                        if (string.IsNullOrEmpty(result))
                        {
                            string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + taskType.ToLower() + "/" + id + ".txt";
                            result = IOHelper.ReadAllText(path);
                        }
                        return result;
                    }


                    /// <summary>
                    /// 获取需要扫描重发的数据。
                    /// </summary>
                    public static List<TaskTable> GetTaskRetryTable()
                    {
                        List<TaskTable> tables = new List<TaskTable>();
                        var disCache = DistributedCache.Instance;

                        if (disCache.CacheType == CacheType.Redis || disCache.CacheType == CacheType.MemCache)
                        {
                            string taskID1 = disCache.Get<string>(GetKey(TaskType.Instant.ToString()));
                            string taskID2 = disCache.Get<string>(GetKey(TaskType.Delay.ToString()));
                            string taskID3 = disCache.Get<string>(GetKey(TaskType.Cron.ToString()));
                            var ids = taskID1 + taskID2 + taskID3;
                            if (!string.IsNullOrEmpty(ids))
                            {
                                foreach (string id in ids.Split(','))
                                {
                                    if (string.IsNullOrEmpty(id)) { continue; }
                                    var json = disCache.Get<string>(GetKey(id));
                                    if (!string.IsNullOrEmpty(json))
                                    {
                                        var entity = JsonHelper.ToEntity<TaskTable>(json);
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
                            string folder = AppConfig.WebRootPath + "App_Data/dts/client/task/";
                            if (System.IO.Directory.Exists(folder))
                            {
                                string[] files = IOHelper.GetFiles(folder, "*.txt", System.IO.SearchOption.AllDirectories);
                                if (files != null && files.Length > 0)
                                {
                                    foreach (string file in files)
                                    {
                                        string json = IOHelper.ReadAllText(file, 0);
                                        if (!string.IsNullOrEmpty(json))
                                        {
                                            tables.Add(JsonHelper.ToEntity<TaskTable>(json));
                                        }
                                    }
                                }
                            }
                        }
                        return tables;
                    }

                    /// <summary>
                    /// 维护一个列表 DoTask=>{id1,id2,id3}
                    /// </summary>
                    internal static void SetIDListWithDisLock(DistributedCache disCache, string id, string taskType, bool isAdd)
                    {
                        #region 更新列表
                        double defaultCacheTime = DTSConfig.Client.Worker.TimeoutKeepSecond / 60;
                        var disLock = DistributedLock.Instance;
                        bool isGetLock = false;
                        string taskTypeKey = GetKey("TaskType:" + taskType);
                        string lockKey = taskTypeKey + ".lock";
                        try
                        {
                            isGetLock = disLock.Lock(lockKey, 5000);//锁定
                            if (isGetLock)
                            {
                                var ids = disCache.Get<string>(taskTypeKey);
                                if (isAdd)
                                {
                                    if (ids == null)
                                    {
                                        disCache.Set(taskTypeKey, id + ",", defaultCacheTime);

                                    }
                                    else
                                    {
                                        if (!ids.Contains(id))
                                        {
                                            disCache.Set(taskTypeKey, ids + id + ",", defaultCacheTime);
                                        }
                                    }
                                }
                                else
                                {
                                    //remove
                                    if (!string.IsNullOrEmpty(ids))
                                    {
                                        ids = ids.Replace(id + ",", "");
                                    }
                                    if (string.IsNullOrEmpty(ids))
                                    {
                                        disCache.Remove(taskTypeKey);
                                    }
                                    else
                                    {
                                        disCache.Set(taskTypeKey, ids, defaultCacheTime);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (isGetLock)
                            {
                                disLock.UnLock(lockKey);//释放锁。
                            }
                        }
                        #endregion
                    }
                }
            }

        }
    }
}

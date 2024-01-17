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
                        string id = table.MsgID;
                        string json = table.ToJson();

                        string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + table.TaskType.ToLower() + "/" + id + ".txt";
                        bool isOK = IOHelper.Write(path, json);
                        if (isOK)
                        {
                            Log.Print("IO.Write : " + json);
                        }

                        return isOK;
                    }

                    /// <summary>
                    /// 删除数据
                    /// </summary>
                    public static bool Delete(string msgID, string taskType)
                    {
                        string id = msgID;
                        string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + taskType.ToLower() + "/" + id + ".txt";
                        return IOHelper.Delete(path);
                    }
                    /// <summary>
                    /// 读取数据
                    /// </summary>
                    public static string Read(string msgID, string taskType)
                    {
                        string id = msgID;
                        string path = AppConfig.WebRootPath + "App_Data/dts/client/task/" + taskType.ToLower() + "/" + id + ".txt";
                        return IOHelper.ReadAllText(path);
                    }


                    /// <summary>
                    /// 获取需要扫描重发的数据。
                    /// </summary>
                    public static List<TaskTable> GetTaskRetryTable()
                    {
                        List<TaskTable> tables = new List<TaskTable>();

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

                        return tables;
                    }

                }
            }

        }
    }
}

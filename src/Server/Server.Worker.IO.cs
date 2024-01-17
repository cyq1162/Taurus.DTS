using CYQ.Data;
using CYQ.Data.Tool;
using System;
using System.IO;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Server
        {
            /// <summary>
            /// dtc 写数据库、写队列
            /// </summary>
            internal static partial class Worker
            {
                internal static class IO
                {
                    private static string GetKey(string key)
                    {
                        return "DTS.Server:" + key;
                    }

                    /// <summary>
                    /// 写入数据
                    /// </summary>
                    public static bool Write(TaskTable table)
                    {
                        string id = table.MsgID;
                        if (table.TaskType == TaskType.Broadcast.ToString())
                        {
                            id += "_" + DTS.ProcessID;//广播需要与进程关联
                        }

                        string json = table.ToJson();
                        string path = AppConfig.WebRootPath + "App_Data/dts/server/" + table.TaskType.ToLower() + "/" + id + ".txt";
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
                        if (taskType == TaskType.Broadcast.ToString())
                        {
                            msgID += "_" + DTS.ProcessID;//广播需要与进程关联
                        }
                        string path = AppConfig.WebRootPath + "App_Data/dts/server/" + taskType.ToLower() + "/" + msgID + ".txt";
                        return IOHelper.Delete(path);
                    }

                    /// <summary>
                    /// 是否存在数据
                    /// </summary>
                    public static bool Exists(string msgID, string taskType)
                    {
                        if (taskType == TaskType.Broadcast.ToString())
                        {
                            msgID += "_" + DTS.ProcessID;//广播需要与进程关联
                        }
                        string path = AppConfig.WebRootPath + "App_Data/dts/server/" + taskType.ToLower() + "/" + msgID + ".txt";
                        return IOHelper.ExistsDirectory(path);
                    }


                    /// <summary>
                    /// 获取超时需要删除的数据，仅硬盘文件需要删除。
                    /// </summary>
                    public static void DeleteTimeoutTable()
                    {
                        try
                        {
                            string folder = AppConfig.WebRootPath + "App_Data/dts/server/";
                            DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                            if (directoryInfo.Exists)
                            {
                                //System.IO.Directory.em
                                FileInfo[] files = directoryInfo.GetFiles("*.txt", SearchOption.AllDirectories);
                                if (files != null && files.Length > 0)
                                {

                                    int timeoutSecond = DTSConfig.Server.Worker.TimeoutKeepSecond;
                                    foreach (FileInfo file in files)
                                    {
                                        if (file.LastWriteTime < DateTime.Now.AddSeconds(-timeoutSecond))
                                        {
                                            file.Delete();
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            Log.Error(err);
                        }

                    }
                }
            }

        }
    }
}

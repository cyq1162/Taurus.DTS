using CYQ.Data.Json;
using CYQ.Data.Orm;
using System;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        public static partial class Client
        {
            /// <summary>
            /// 重复发布任务表
            /// </summary>
            internal partial class CronTable : SimpleOrmBase
            {
                public CronTable()
                {
                    SetInit(this, DTSConfig.Client.CronTable, DTSConfig.Client.Conn);
                }
                private long? _ID;
                /// <summary>
                /// 标识主键
                /// </summary>
                [Key(true, false, false)]
                public long? ID
                {
                    get
                    {
                        return _ID;
                    }
                    set
                    {
                        _ID = value;
                    }
                }
                private string _MsgID;
                /// <summary>
                /// 队列消息ID
                /// </summary>
                [Length(36)]
                [Key(false, true, false)]
                public string MsgID
                {
                    get
                    {
                        if (string.IsNullOrEmpty(_MsgID))
                        {
                            _MsgID = Guid.NewGuid().ToString();
                        }
                        return _MsgID;
                    }
                    set
                    {
                        _MsgID = value;
                    }
                }

                private string _Cron;
                /// <summary>
                /// Cron 表达式
                /// </summary>
                [Length(50)]
                public string Cron
                {
                    get { return _Cron; }
                    set { _Cron = value; }
                }

                private string _TaskKey;
                /// <summary>
                /// 任务key
                /// </summary>
                [Length(50)]
                public string TaskKey
                {
                    get { return _TaskKey; }
                    set { _TaskKey = value; }
                }

                private string _CallBackKey;
                /// <summary>
                /// 回调订阅key
                /// </summary>
                [Length(50)]
                public string CallBackKey
                {
                    get { return _CallBackKey; }
                    set { _CallBackKey = value; }
                }

                private string _Content;
                /// <summary>
                /// 写入内容
                /// </summary>
                [Length(2000)]
                public string Content
                {
                    get { return _Content; }
                    set { _Content = value; }
                }

                private int? _ConfirmNum;
                /// <summary>
                /// 重复执行类型：已经确认执行数量
                /// </summary>
                [DefaultValue(0)]
                public int? ConfirmNum
                {
                    get
                    {
                        return _ConfirmNum;
                    }
                    set
                    {
                        _ConfirmNum = value;
                    }
                }
                private DateTime? _CreateTime;

                /// <summary>
                ///  创建时间
                /// </summary>
                public DateTime? CreateTime
                {
                    get
                    {
                        return _CreateTime;
                    }
                    set
                    {
                        _CreateTime = value;
                    }
                }

                private DateTime? _EditTime;
                /// <summary>
                /// 更新时间
                /// </summary>
                public DateTime? EditTime
                {
                    get
                    {
                        return _EditTime;
                    }
                    set
                    {
                        _EditTime = value;

                    }
                }
            }

            internal partial class CronTable
            {

                public string ToJson()
                {
                    JsonHelper js = new JsonHelper(false, false);
                    if (this.ID.HasValue)
                    {
                        js.Add("ID", this.ID.Value);
                    }
                    js.Add("MsgID", this.MsgID);

                    if (this.Content != null)
                    {
                        js.Add("Content", this.Content);
                    }
                    if (this.TaskKey != null)
                    {
                        js.Add("TaskKey", this.TaskKey);
                    }
                    if (this.CallBackKey != null)
                    {
                        js.Add("CallBackKey", this.CallBackKey);
                    }

                    if (this.CreateTime.HasValue)
                    {
                        js.Add("CreateTime", this.CreateTime.Value);
                    }
                    if (this.EditTime.HasValue)
                    {
                        js.Add("EditTime", this.EditTime.Value);
                    }
                    return js.ToString();
                }

            }
        }
    }


}

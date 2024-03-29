﻿using System;
using CYQ.Data.Json;
using CYQ.Data.Orm;
using CYQ.Data.Tool;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTS
    {
        /// <summary>
        /// 分布式事务 提供端
        /// </summary>
        public static partial class Server
        {
            /// <summary>
            /// 本地消息表
            /// </summary>
            internal partial class TaskTable : SimpleOrmBase
            {
                public TaskTable()
                {
                    SetInit(this, DTSConfig.Server.TaskTable, DTSConfig.Server.Conn, true);
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

                private string _TaskType;
                /// <summary>
                /// 执行类型
                /// </summary>
                [Length(10)]
                public string TaskType
                {
                    get
                    {
                        return _TaskType;
                    }
                    set
                    {
                        _TaskType = value;
                    }
                }

                //private string _ExChange;
                ///// <summary>
                ///// 发送交换机名称
                ///// </summary>
                //[Length(50)]
                //public string ExChange
                //{
                //    get { return _ExChange; }
                //    set { _ExChange = value; }
                //}

                //private string _QueueName;
                ///// <summary>
                ///// 发送队列名称
                ///// </summary>
                //[Length(50)]
                //public string QueueName
                //{
                //    get { return _QueueName; }
                //    set { _QueueName = value; }
                //}
                //private string _CallBackName;
                ///// <summary>
                ///// 队列监听名称
                ///// </summary>
                //[Length(50)]
                //public string CallBackName
                //{
                //    get { return _CallBackName; }
                //    set { _CallBackName = value; }
                //}

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
                /// 订阅key
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

                private int? _Retries;
                /// <summary>
                /// MQ确认状态重试次数
                /// </summary>
                [DefaultValue(0)]
                public int? Retries
                {
                    get
                    {
                        return _Retries;
                    }
                    set
                    {
                        _Retries = value;
                    }
                }

                private int? _ConfirmState;
                /// <summary>
                /// MQ确认状态【0、未确认；1、已确认；2、可删除】
                /// </summary>
                [DefaultValue(0)]
                public int? ConfirmState
                {
                    get
                    {
                        return _ConfirmState;
                    }
                    set
                    {
                        _ConfirmState = value;
                    }
                }

                private DateTime? _CreateTime;
                /// <summary>
                /// 创建时间
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
            internal partial class TaskTable
            {
                public MQMsg ToMQMsg()
                {
                    MQMsg msg = new MQMsg();
                    msg.MsgID = this.MsgID;
                    msg.Content = this.Content;
                    //msg.ExChange = this.ExChange;
                    //msg.QueueName = this.QueueName;
                    //msg.CallBackName = this.CallBackName;
                    msg.TaskType = this.TaskType;
                    msg.TaskKey = this.TaskKey;
                    msg.CallBackKey = this.CallBackKey;
                    return msg;
                }
                public string ToJson()
                {
                    JsonHelper js = new JsonHelper(false, false);
                    if (this.ID.HasValue)
                    {
                        js.Add("ID", this.ID.Value);
                    }
                    js.Add("MsgID", this.MsgID);
                    //if (this.ExChange != null)
                    //{
                    //    js.Add("ExChange", this.ExChange);
                    //}
                    //if (this.QueueName != null)
                    //{
                    //    js.Add("QueueName", this.QueueName);
                    //}
                    //if (this.CallBackName != null)
                    //{
                    //    js.Add("CallBackName", this.CallBackName);
                    //}
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
                    if (this.TaskType != null)
                    {
                        js.Add("TaskType", this.TaskType);
                    }
                    if (this.Retries.HasValue)
                    {
                        js.Add("Retries", this.Retries.Value);
                    }
                    if (this.ConfirmState.HasValue)
                    {
                        js.Add("ConfirmState", this.ConfirmState.Value);
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

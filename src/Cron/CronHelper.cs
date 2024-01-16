using System;
using System.Collections.Generic;
using System.Text;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 在week上使用  5L表示本月最后一个星期五
    ///               7L表示本月最后一个星期天
    ///              
    /// 在week上使用  7#3表示每月的第三个星期天
    ///               2#4表示每月的第四个星期二
    /// </summary>
    internal class CronHelper
    {
        /// <summary>
        /// 根据时间生成执行1次的 Cron 表达式
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static string GetCron(DateTime dt)
        {
            return string.Format("{0} {1} {2} {3} {4} ? {5}", dt.Second, dt.Minute, dt.Hour, dt.Day, dt.Month, dt.Year);
        }

        /// <summary>
        /// Cron表达式转换(默认开始时间为当前)
        /// </summary>
        /// <param name="cron">表达式</param>
        /// <param name="count">最近N次要执行的时间</param>
        /// <returns>最近N次要执行的时间</returns>
        public static List<DateTime> GetExeTimeList(string cron, int count)
        {
            return GetExeTimeList(cron, DateTime.Now, count);
        }

        /// <summary>
        /// Cron表达式转换（自定义开始时间）
        /// </summary>
        /// <param name="cron">表达式</param>
        /// <param name="now">开始时间</param>
        /// <param name="count">最近N次要执行的时间</param>
        /// <returns>最近N次要执行的时间</returns>
        public static List<DateTime> GetExeTimeList(string cron, DateTime now, int count)
        {
            if (string.IsNullOrEmpty(cron)) { return null; }
            try
            {
                List<DateTime> lits = new List<DateTime>();
                Cron c = new Cron();
                string[] arr = cron.Split(' ');
                Seconds(c, arr[0]);
                Minutes(c, arr[1]);
                Hours(c, arr[2]);
                Month(c, arr[4]);
                if (arr.Length < 7)
                {
                    Year(c, null);
                }
                else
                {
                    Year(c, arr[6]);
                }
                int addtime = 1;
                while (true)
                {
                    if (c.Seconds[now.Second] == 1 && c.Minutes[now.Minute] == 1 && c.Hours[now.Hour] == 1 && c.Month[now.Month - 1] == 1 && c.Year[now.Year - 2019] == 1)
                    {
                        if (arr[3] != "?")
                        {
                            Days(c, arr[3], DateTime.DaysInMonth(now.Year, now.Month), now);
                            int DayOfWeek = (((int)now.DayOfWeek) + 6) % 7;
                            if (c.Days[now.Day - 1] == 1 && c.Weeks[DayOfWeek] == 1)
                            {
                                lits.Add(now);
                            }
                        }
                        else
                        {
                            Weeks(c, arr[5], DateTime.DaysInMonth(now.Year, now.Month), now);
                            int DayOfWeek = (((int)now.DayOfWeek) + 6) % 7;
                            if (c.Days[now.Day - 1] == 1 && c.Weeks[DayOfWeek] == 1)
                            {
                                lits.Add(now);
                            }
                        }
                    }
                    if (lits.Count >= count)
                    {
                        break;
                    }
                    c.Init();
                    if (!arr[1].Contains("-") && !arr[1].Contains(",") && !arr[1].Contains("*") && !arr[1].Contains("/"))
                    {
                        if (now.Minute == int.Parse(arr[1]))
                        {
                            addtime = 3600;
                        }
                    }
                    else if (arr[0] == "0" && now.Second == 0)
                    {
                        addtime = 60;
                    }
                    now = now.AddSeconds(addtime);
                }
                return lits;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cron表达式转换(默认开始时间为当前)
        /// </summary>
        /// <param name="cron">表达式</param>
        /// <returns>最近要执行的时间字符串</returns>
        public static string GetNextDateTime(string cron)
        {
            return GetNextDateTime(cron, DateTime.Now);
        }

        /// <summary>
        /// Cron表达式转换（自定义开始时间）
        /// </summary>
        /// <param name="cron">表达式</param>
        /// <param name="now">开始时间</param>
        /// <returns>最近要执行的时间字符串</returns>
        public static string GetNextDateTime(string cron, DateTime now)
        {
            if (string.IsNullOrEmpty(cron)) { return null; }
            try
            {
                string[] arr = cron.Split(' ');
                if (IsExeOnce(cron))
                {
                    DateTime date = Convert.ToDateTime(arr[6] + "/" + arr[4] + "/" + arr[3] + " " + arr[2] + ":" + arr[1] + ":" + arr[0] + ".999");
                    if (DateTime.Compare(date, now) >= 0)
                    {
                        return date.ToString("yyyy/MM/dd HH:mm:ss");
                    }
                    else
                    {
                        return null;
                    }
                }
                Cron c = new Cron();
                Seconds(c, arr[0]);
                Minutes(c, arr[1]);
                Hours(c, arr[2]);
                Month(c, arr[4]);
                if (arr.Length < 7)
                {
                    Year(c, null);
                }
                else
                {
                    Year(c, arr[6]);
                }
                int addtime = 1;
                while (true)
                {
                    if (c.Seconds[now.Second] == 1 && c.Minutes[now.Minute] == 1 && c.Hours[now.Hour] == 1 && c.Month[now.Month - 1] == 1 && c.Year[now.Year - 2019] == 1)
                    {
                        if (arr[3] != "?")
                        {
                            Days(c, arr[3], DateTime.DaysInMonth(now.Year, now.Month), now);
                            int DayOfWeek = (((int)now.DayOfWeek) + 6) % 7;
                            if (c.Days[now.Day - 1] == 1 && c.Weeks[DayOfWeek] == 1)
                            {
                                return now.ToString("yyyy/MM/dd HH:mm:ss");
                            }
                        }
                        else
                        {
                            Weeks(c, arr[5], DateTime.DaysInMonth(now.Year, now.Month), now);
                            int DayOfWeek = (((int)now.DayOfWeek) + 6) % 7;
                            if (c.Days[now.Day - 1] == 1 && c.Weeks[DayOfWeek] == 1)
                            {
                                return now.ToString("yyyy/MM/dd HH:mm:ss");
                            }
                        }
                    }
                    c.Init();
                    if (!arr[1].Contains("-") && !arr[1].Contains(",") && !arr[1].Contains("*") && !arr[1].Contains("/"))
                    {
                        if (now.Minute == int.Parse(arr[1]))
                        {
                            addtime = 3600;
                        }
                    }
                    else if (arr[0] == "0" && now.Second == 0)
                    {
                        addtime = 60;
                    }
                    now = now.AddSeconds(addtime);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Cron表达式转换成中文描述
        /// </summary>
        /// <param name="cronExp"></param>
        /// <returns></returns>
        public static string TranslateToChinese(string cronExp)
        {
            if (cronExp == null || cronExp.Length < 1)
            {
                return "cron表达式为空";
            }
            string[] tmpCorns = cronExp.Split(' ');
            StringBuilder sBuffer = new StringBuilder();
            if (tmpCorns.Length == 6)
            {
                //解析月
                if (!tmpCorns[4].Equals("*"))
                {
                    sBuffer.Append(tmpCorns[4]).Append("月");
                }
                else
                {
                    sBuffer.Append("每月");
                }
                //解析周
                if (!tmpCorns[5].Equals("*") && !tmpCorns[5].Equals("?"))
                {
                    char[] tmpArray = tmpCorns[5].ToCharArray();
                    foreach (char tmp in tmpArray)
                    {
                        switch (tmp)
                        {
                            case '1':
                                sBuffer.Append("星期天");
                                break;
                            case '2':
                                sBuffer.Append("星期一");
                                break;
                            case '3':
                                sBuffer.Append("星期二");
                                break;
                            case '4':
                                sBuffer.Append("星期三");
                                break;
                            case '5':
                                sBuffer.Append("星期四");
                                break;
                            case '6':
                                sBuffer.Append("星期五");
                                break;
                            case '7':
                                sBuffer.Append("星期六");
                                break;
                            case '-':
                                sBuffer.Append("至");
                                break;
                            default:
                                sBuffer.Append(tmp);
                                break;
                        }
                    }
                }

                //解析日
                if (!tmpCorns[3].Equals("?"))
                {
                    if (!tmpCorns[3].Equals("*"))
                    {
                        sBuffer.Append(tmpCorns[3]).Append("日");
                    }
                    else
                    {
                        sBuffer.Append("每日");
                    }
                }

                //解析时
                if (!tmpCorns[2].Equals("*"))
                {
                    sBuffer.Append(tmpCorns[2]).Append("时");
                }
                else
                {
                    sBuffer.Append("每时");
                }

                //解析分
                if (!tmpCorns[1].Equals("*"))
                {
                    sBuffer.Append(tmpCorns[1]).Append("分");
                }
                else
                {
                    sBuffer.Append("每分");
                }

                //解析秒
                if (!tmpCorns[0].Equals("*"))
                {
                    sBuffer.Append(tmpCorns[0]).Append("秒");
                }
                else
                {
                    sBuffer.Append("每秒");
                }
            }
            return sBuffer.ToString();
        }

        #region 初始化Cron对象
        private static void Seconds(Cron c, string str)
        {
            if (str == "*")
            {
                for (int i = 0; i < 60; i++)
                {
                    c.Seconds[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Seconds[i] = 1;
                }
            }
            else if (str.Contains("/"))
            {
                int begin = int.Parse(str.Split('/')[0]);
                int interval = int.Parse(str.Split('/')[1]);
                while (true)
                {
                    c.Seconds[begin] = 1;
                    if ((begin + interval) >= 60)
                        break;
                    begin += interval;
                }
            }
            else if (str.Contains(","))
            {

                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Seconds[int.Parse(str.Split(',')[i])] = 1;
                }
            }
            else
            {
                c.Seconds[int.Parse(str)] = 1;
            }
        }
        private static void Minutes(Cron c, string str)
        {
            if (str == "*")
            {
                for (int i = 0; i < 60; i++)
                {
                    c.Minutes[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Minutes[i] = 1;
                }
            }
            else if (str.Contains("/"))
            {
                int begin = int.Parse(str.Split('/')[0]);
                int interval = int.Parse(str.Split('/')[1]);
                while (true)
                {
                    c.Minutes[begin] = 1;
                    if ((begin + interval) >= 60)
                        break;
                    begin += interval;
                }
            }
            else if (str.Contains(","))
            {

                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Minutes[int.Parse(str.Split(',')[i])] = 1;
                }
            }
            else
            {
                c.Minutes[int.Parse(str)] = 1;
            }
        }
        private static void Hours(Cron c, string str)
        {
            if (str == "*")
            {
                for (int i = 0; i < 24; i++)
                {
                    c.Hours[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Hours[i] = 1;
                }
            }
            else if (str.Contains("/"))
            {
                int begin = int.Parse(str.Split('/')[0]);
                int interval = int.Parse(str.Split('/')[1]);
                while (true)
                {
                    c.Hours[begin] = 1;
                    if ((begin + interval) >= 24)
                        break;
                    begin += interval;
                }
            }
            else if (str.Contains(","))
            {

                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Hours[int.Parse(str.Split(',')[i])] = 1;
                }
            }
            else
            {
                c.Hours[int.Parse(str)] = 1;
            }
        }
        private static void Month(Cron c, string str)
        {
            if (str == "*")
            {
                for (int i = 0; i < 12; i++)
                {
                    c.Month[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Month[i - 1] = 1;
                }
            }
            else if (str.Contains("/"))
            {
                int begin = int.Parse(str.Split('/')[0]);
                int interval = int.Parse(str.Split('/')[1]);
                while (true)
                {
                    c.Month[begin - 1] = 1;
                    if ((begin + interval) >= 12)
                        break;
                    begin += interval;
                }
            }
            else if (str.Contains(","))
            {

                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Month[int.Parse(str.Split(',')[i]) - 1] = 1;
                }
            }
            else
            {
                c.Month[int.Parse(str) - 1] = 1;
            }
        }
        private static void Year(Cron c, string str)
        {
            if (str == null || str == "*")
            {
                for (int i = 0; i < 80; i++)
                {
                    c.Year[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin - 2019; i <= end - 2019; i++)
                {
                    c.Year[i] = 1;
                }
            }
            else
            {
                c.Year[int.Parse(str) - 2019] = 1;
            }
        }
        private static void Days(Cron c, string str, int len, DateTime now)
        {
            for (int i = 0; i < 7; i++)
            {
                c.Weeks[i] = 1;
            }
            if (str == "*" || str == "?")
            {
                for (int i = 0; i < len; i++)
                {
                    c.Days[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Days[i - 1] = 1;
                }
            }
            else if (str.Contains("/"))
            {
                int begin = int.Parse(str.Split('/')[0]);
                int interval = int.Parse(str.Split('/')[1]);
                while (true)
                {
                    c.Days[begin - 1] = 1;
                    if ((begin + interval) >= len)
                        break;
                    begin += interval;
                }
            }
            else if (str.Contains(","))
            {
                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Days[int.Parse(str.Split(',')[i]) - 1] = 1;
                }
            }
            else if (str.Contains("L"))
            {
                int i = str.Replace("L", "") == "" ? 0 : int.Parse(str.Replace("L", ""));
                c.Days[len - 1 - i] = 1;
            }
            else if (str.Contains("W"))
            {
                c.Days[len - 1] = 1;
            }
            else
            {
                c.Days[int.Parse(str) - 1] = 1;
            }
        }
        private static void Weeks(Cron c, string str, int len, DateTime now)
        {
            if (str == "*" || str == "?")
            {
                for (int i = 0; i < 7; i++)
                {
                    c.Weeks[i] = 1;
                }
            }
            else if (str.Contains("-"))
            {
                int begin = int.Parse(str.Split('-')[0]);
                int end = int.Parse(str.Split('-')[1]);
                for (int i = begin; i <= end; i++)
                {
                    c.Weeks[i - 1] = 1;
                }
            }
            else if (str.Contains(","))
            {
                for (int i = 0; i < str.Split(',').Length; i++)
                {
                    c.Weeks[int.Parse(str.Split(',')[i]) - 1] = 1;
                }
            }
            else if (str.Contains("L"))
            {
                int i = str.Replace("L", "") == "" ? 0 : int.Parse(str.Replace("L", ""));
                if (i == 0)
                {
                    c.Weeks[6] = 1;
                }
                else
                {
                    c.Weeks[i - 1] = 1;
                    c.Days[GetLastWeek(i, now) - 1] = 1;
                    return;
                }
            }
            else if (str.Contains("#"))
            {
                int i = int.Parse(str.Split('#')[0]);
                int j = int.Parse(str.Split('#')[1]);
                c.Weeks[i - 1] = 1;
                c.Days[GetWeek(i - 1, j, now)] = 1;
                return;
            }
            else
            {
                c.Weeks[int.Parse(str) - 1] = 1;
            }
            //week中初始化day，则说明day没要求
            for (int i = 0; i < len; i++)
            {
                c.Days[i] = 1;
            }
        }
        #endregion

        #region 方法

        private static bool IsExeOnce(string cron)
        {
            if (cron.Contains("-") || cron.Contains(",") || cron.Contains("/") || cron.Contains("*"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// 获取最后一个星期几的day
        /// </summary>
        /// <param name="i">星期几</param>
        /// <param name="now"></param>
        /// <returns></returns>
        private static int GetLastWeek(int i, DateTime now)
        {
            DateTime d = now.AddDays(1 - now.Day).Date.AddMonths(1).AddSeconds(-1);
            int DayOfWeek = ((((int)d.DayOfWeek) + 6) % 7) + 1;
            int a = DayOfWeek >= i ? DayOfWeek - i : 7 + DayOfWeek - i;
            return DateTime.DaysInMonth(now.Year, now.Month) - a;
        }
        /// <summary>
        /// 获取当月第几个星期几的day
        /// </summary>
        /// <param name="i">星期几</param>
        /// <param name="j">第几周</param>
        /// <param name="now"></param>
        /// <returns></returns>
        private static int GetWeek(int i, int j, DateTime now)
        {
            int day = 0;
            DateTime d = new DateTime(now.Year, now.Month, 1);
            int DayOfWeek = ((((int)d.DayOfWeek) + 6) % 7) + 1;
            if (i >= DayOfWeek)
            {
                day = (7 - DayOfWeek + 1) + 7 * (j - 2) + i;
            }
            else
            {
                day = (7 - DayOfWeek + 1) + 7 * (j - 1) + i;
            }
            return day;
        }
        #endregion

    }
}

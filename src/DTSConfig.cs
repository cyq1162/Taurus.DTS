using CYQ.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Taurus.Plugin.DistributedTask
{
    public static partial class DTSConfig
    {
        /// <summary>
        /// 队列名：以当前进程的临时唯一值为队列名。
        /// </summary>
        internal static readonly int pidName = Math.Abs(Guid.NewGuid().GetHashCode());

        /// <summary>
        /// 队列名：以项目为单位的队列名称，不同项目应该有不同的名称。
        /// 配置项：DTS.ProjectName ：TaurusProj
        /// </summary>
        public static string ProjectName
        {
            get
            {
                string projectName = AppConfig.GetApp("DTS.ProjectName");
                if (string.IsNullOrEmpty(projectName))
                {
                    Assembly ass = Assembly.GetEntryAssembly();
                    if (ass == null)
                    {
                        ass = GetEntryAssembly();
                    }
                    if (ass == null)
                    {
                        projectName = Environment.UserName;
                    }
                    else
                    {
                        projectName = ass.GetName().Name;
                    }
                    projectName = projectName.Replace(".", "").Replace("_", "");
                    AppConfig.SetApp("DTS.ProjectName", projectName);
                }
                return projectName;
            }
            set
            {
                AppConfig.SetApp("DTS.ProjectName", value);
            }
        }

        private static Assembly GetEntryAssembly()
        {
            string current = Assembly.GetExecutingAssembly().GetName().Name;
            Assembly[] assList = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> projs = new List<Assembly>();
            Assembly ctl = null;
            foreach (Assembly assembly in assList)
            {
                if (assembly.GlobalAssemblyCache || assembly.GetName().GetPublicKeyToken().Length > 0)
                {
                    continue;
                }
                else
                {
                    projs.Add(assembly);
                    if (ctl == null)
                    {
                        foreach (AssemblyName item in assembly.GetReferencedAssemblies())
                        {
                            if (current == item.Name)
                            {
                                ctl = assembly;
                                //引用了自身，要么是控制器，要么控制器本身就是主运行程序。
                                break;
                            }
                        }
                    }
                }
            }
            if (ctl != null)
            {
                current = ctl.GetName().Name;
                foreach (Assembly assembly in projs)
                {
                    foreach (AssemblyName item in assembly.GetReferencedAssemblies())
                    {
                        if (current == item.Name)
                        {
                            ctl = assembly;
                            //引用了自身，要么是控制器，要么控制器本身就是主运行程序。
                            break;
                        }
                    }
                }
            }
            return ctl;
        }


        /// <summary>
        /// Both：项目交换机：绑定所有项目队列
        /// </summary>
        internal static string ProjectExChange
        {
            get
            {
                return "DTS_Both_Project";
            }
        }
        /// <summary>
        /// Both：进程交换机：绑定所有进程队列
        /// </summary>
        internal static string ProcessExChange
        {
            get
            {
                return "DTS_Both_Process";
            }
        }
    }
}

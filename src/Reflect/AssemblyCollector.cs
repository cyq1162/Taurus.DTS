using System;
using System.Collections.Generic;
using System.Reflection;

namespace Taurus.Plugin.DistributedTask
{
    /// <summary>
    /// 搜索控制器的程序集【不缓存，因为只用1次】
    /// </summary>
    internal class AssemblyCollector
    {
        #region GetAssembly

        /// <summary>
        /// 获取引用自身的程序集列表
        /// </summary>
        public static List<Assembly> GetRefAssemblyList()
        {
            string current = Assembly.GetExecutingAssembly().GetName().Name;
            //获取所有程序集
            Assembly[] assList = AppDomain.CurrentDomain.GetAssemblies();
            List<Assembly> refAssemblyList = new List<Assembly>();
            foreach (Assembly assembly in assList)
            {
                if (assembly.GlobalAssemblyCache || assembly.GetName().GetPublicKeyToken().Length > 0)
                {
                    //过滤一下系统标准库
                    continue;
                }
                else
                {
                    //搜索引用自身的程序集
                    foreach (AssemblyName item in assembly.GetReferencedAssemblies())
                    {
                        if (current == item.Name)
                        {
                            //引用了自身
                            refAssemblyList.Add(assembly);
                            break;
                        }
                    }
                }
            }
            return refAssemblyList;
        }
        #endregion
    }
}

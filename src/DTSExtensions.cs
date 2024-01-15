using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Taurus.Plugin.DistributedTask;

namespace Microsoft.AspNetCore.Http
{
    public static partial class DTSExtensions
    {
        public static void AddTaurusDts(this IServiceCollection services)
        {
            services.AddHttpContext();
        }

        public static IApplicationBuilder AddTaurusDts(this IApplicationBuilder builder)
        {
            return UseTaurusDts(builder, DTSStartType.None);
        }
        public static IApplicationBuilder UseTaurusDts(this IApplicationBuilder builder, DTSStartType startType)
        {
            builder.UseHttpContext();
            switch (startType)
            {
                case DTSStartType.Client:
                    DTS.Client.Start();
                    break;
                case DTSStartType.Server:
                    DTS.Server.Start();
                    break;
                case DTSStartType.Both:
                    DTS.Start();
                    break;
            }
            return builder;
        }
    }
    public enum DTSStartType
    {
        /// <summary>
        /// 不设定，应用程序启动或重启时，不先启动数据扫描，由程序涉及调用相关函数时自动启动数据扫描。
        /// </summary>
        None,
        /// <summary>
        /// 启动时，进行客户端数据扫描。
        /// </summary>
        Client,
        /// <summary>
        /// 启动时，进行服务端数据扫描。
        /// </summary>
        Server,
        /// <summary>
        /// 启动时，对客户端和服务端都启动数据扫描。
        /// </summary>
        Both
    }
}

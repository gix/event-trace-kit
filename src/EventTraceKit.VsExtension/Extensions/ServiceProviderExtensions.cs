namespace EventTraceKit.VsExtension.Extensions
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;

    public static class ServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider provider)
            where T : class
        {
            return provider.GetService(typeof(T)) as T;
        }

        public static T GetService<TService, T>(this IServiceProvider provider)
            where T : class
        {
            return provider.GetService(typeof(TService)) as T;
        }

        public static async Task<T> GetServiceAsync<T>(this IAsyncServiceProvider provider)
            where T : class
        {
            return await provider.GetServiceAsync(typeof(T)) as T;
        }

        public static async Task<T> GetServiceAsync<TService, T>(this IAsyncServiceProvider provider)
            where T : class
        {
            return await provider.GetServiceAsync(typeof(TService)) as T;
        }
    }
}

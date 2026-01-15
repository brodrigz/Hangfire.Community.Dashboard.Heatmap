using System;
using System.Reflection;
using Hangfire.Dashboard;
using Hangfire.Community.Dashboard.Heatmap.Pages;

namespace Hangfire.Community.Dashboard.Heatmap
{
    public static class GlobalConfigurationExtension
    {
        public static string RouteBase = "/cron-heatmap";

        internal static string FileSuffix()
        {
            var version = typeof(GlobalConfigurationExtension).Assembly.GetName().Version;
            return $"{version.Major}_{version.Minor}_{version.Build}";
        }

        /// <summary>
        /// Adds the Cron Heatmap visualization page to Hangfire.
        /// </summary>
        /// <param name="config">The Hangfire global configuration.</param>
        /// <returns>The configuration instance for chaining.</returns>
        public static IGlobalConfiguration UseHeatmapPage(this IGlobalConfiguration config)
        {
            CreateHeatmapPage();
            AddApiRoutes();
            return config;
        }

        private static void AddApiRoutes()
        {
            DashboardRoutes.Routes.Add(
                $"{RouteBase}/api/recurring-jobs",
                new CronHeatmapApi()
            );
        }

        private static void CreateHeatmapPage()
        {
            DashboardRoutes.Routes.AddRazorPage(RouteBase, x => new CronHeatmap());

            NavigationMenu.Items.Add(page => new MenuItem(CronHeatmap.Title, page.Url.To(RouteBase))
            {
                Active = page.RequestPath == RouteBase || page.RequestPath.StartsWith($"{RouteBase}/")
            });
        }
    }
}

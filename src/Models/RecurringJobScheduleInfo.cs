using System;
using System.Collections.Generic;

namespace Hangfire.Community.Dashboard.Heatmap.Models
{
    internal class RecurringJobScheduleInfo
    {
        public string Id { get; set; }
        public string Cron { get; set; }
        public string Queue { get; set; }
        public string TimeZoneId { get; set; }
        public DateTime? NextExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public List<DateTime> Executions { get; set; }
    }
}

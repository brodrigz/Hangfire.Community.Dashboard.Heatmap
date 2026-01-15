using System.Collections.Generic;

namespace Hangfire.Community.Dashboard.Heatmap.Models
{
    internal class RecurringJobsResponse
    {
        public List<RecurringJobScheduleInfo> Jobs { get; set; }
    }
}

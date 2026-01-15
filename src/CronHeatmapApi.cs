using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Cronos;
using Hangfire.Community.Dashboard.Heatmap.Models;
using Hangfire.Dashboard;
using Hangfire.Storage;

namespace Hangfire.Community.Dashboard.Heatmap
{
    internal class CronHeatmapApi : IDashboardDispatcher
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public async Task Dispatch(DashboardContext context)
        {
            context.Response.ContentType = "application/json";

            try
            {
                //get client timezone offset from query param 
                var offsetMinutes = 0;
                var offsetParam = context.Request.GetQuery("tzOffset");
                if (!string.IsNullOrEmpty(offsetParam))
                {
                    int.TryParse(offsetParam, out offsetMinutes);
                }

                var storage = context.Storage;
                using (var connection = storage.GetConnection())
                {
                    var recurringJobs = connection.GetRecurringJobs();
                    var jobsData = GetJobScheduleData(recurringJobs, offsetMinutes);
                    var response = new RecurringJobsResponse { Jobs = jobsData };
                    var json = JsonSerializer.Serialize(response, JsonOptions);

                    var bytes = Encoding.UTF8.GetBytes(json);
                    await context.Response.Body.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new ErrorResponse { Error = ex.Message };
                var json = JsonSerializer.Serialize(errorResponse, JsonOptions);
                var bytes = Encoding.UTF8.GetBytes(json);
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        private List<RecurringJobScheduleInfo> GetJobScheduleData(List<RecurringJobDto> recurringJobs, int clientOffsetMinutes)
        {
            var result = new List<RecurringJobScheduleInfo>();
            var utcNow = DateTime.UtcNow;

            //get client timezone start and end of day in UTC
            var clientLocalNow = utcNow.AddMinutes(-clientOffsetMinutes);
            var clientLocalMidnight = new DateTime(clientLocalNow.Year, clientLocalNow.Month, clientLocalNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var startOfDayUtc = clientLocalMidnight.AddMinutes(clientOffsetMinutes);
            var endOfDayUtc = startOfDayUtc.AddDays(1);

            //calculate start of week for weekly heatmap
            var daysFromSunday = (int)clientLocalNow.DayOfWeek;
            var clientLocalWeekStart = clientLocalMidnight.AddDays(-daysFromSunday);
            var startOfWeekUtc = clientLocalWeekStart.AddMinutes(clientOffsetMinutes);
            var endOfWeekUtc = startOfWeekUtc.AddDays(7);

            foreach (var job in recurringJobs)
            {
                if (!job.NextExecution.HasValue) continue;

                if (!TryParseCron(job.Cron, out var cronExpression)) continue;

                var scheduleInfo = new RecurringJobScheduleInfo
                {
                    Id = job.Id,
                    Cron = job.Cron,
                    Queue = job.Queue ?? "default",
                    NextExecution = job.NextExecution,
                    LastExecution = job.LastExecution,
                    TimeZoneId = job.TimeZoneId ?? "UTC",
                    Executions = new List<DateTime>(),
                    WeeklyExecutions = new List<DateTime>()
                };

                //calculate all executions times for today
                var nextOccurrence = cronExpression.GetNextOccurrence(startOfDayUtc, TimeZoneInfo.Utc);
                while (nextOccurrence.HasValue && nextOccurrence.Value < endOfDayUtc)
                {
                    scheduleInfo.Executions.Add(nextOccurrence.Value);
                    nextOccurrence = cronExpression.GetNextOccurrence(nextOccurrence.Value, TimeZoneInfo.Utc);
                }

                //calculate all executions times for the week
                nextOccurrence = cronExpression.GetNextOccurrence(startOfWeekUtc, TimeZoneInfo.Utc);
                while (nextOccurrence.HasValue && nextOccurrence.Value < endOfWeekUtc)
                {
                    scheduleInfo.WeeklyExecutions.Add(nextOccurrence.Value);
                    nextOccurrence = cronExpression.GetNextOccurrence(nextOccurrence.Value, TimeZoneInfo.Utc);
                }

                result.Add(scheduleInfo);
            }

            return result;
        }

        private bool TryParseCron(string cronExpression, out CronExpression result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(cronExpression))
                return false;

            //try with seconds format first (6 fields)
            try
            {
                result = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);
                return true;
            }
            catch { }

            //try standard format (5 fields)
            try
            {
                result = CronExpression.Parse(cronExpression);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}

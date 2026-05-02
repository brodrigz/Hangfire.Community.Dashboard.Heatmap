using Hangfire;
using Hangfire.Community.Dashboard.Heatmap;
using Hangfire.MemoryStorage;
using TestApp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHangfire(configuration =>
{
    configuration
        .UseMemoryStorage()
        .UseHeatmapPage();
});
builder.Services.AddHangfireServer();

var app = builder.Build();

SeedRecurringJobs(app.Services.GetRequiredService<IRecurringJobManager>());

app.MapGet("/", () => Results.Redirect("/hangfire/cron-heatmap"));

app.UseAuthorization();

app.UseHangfireDashboard("/hangfire");

app.MapControllers();

app.Run();

static void SeedRecurringJobs(IRecurringJobManager recurringJobs)
{
    var schedules = new (string Id, string Cron)[]
    {
        ("every-minute", Cron.Minutely()),
        ("every-five-minutes", "*/5 * * * *"),
        ("every-ten-minutes", "*/10 * * * *"),
        ("every-fifteen-minutes", "*/15 * * * *"),
        ("twice-hourly", "7,37 * * * *"),
        ("hourly-on-the-hour", Cron.Hourly()),
        ("hourly-quarter-past", "15 * * * *"),
        ("hourly-half-past", "30 * * * *"),
        ("hourly-quarter-to", "45 * * * *"),
        ("business-hours-morning", "0,20,40 8-11 * * 1-5"),
        ("business-hours-afternoon", "5,25,45 13-17 * * 1-5"),
        ("overnight-watch", "0,30 0-5 * * *"),
        ("lunch-check", "0 12 * * 1-5"),
        ("evening-rollup", "10 18 * * 1-5"),
        ("late-evening-rollup", "50 22 * * *"),
        ("midnight-cleanup", Cron.Daily(0, 0)),
        ("daily-backup", Cron.Daily(2, 30)),
        ("daily-report", Cron.Daily(6, 15)),
        ("daily-digest", Cron.Daily(9, 45)),
        ("daily-sync", Cron.Daily(14, 5)),
        ("daily-reconciliation", Cron.Daily(19, 20)),
        ("weekday-standup", "30 9 * * 1-5"),
        ("weekday-closeout", "0 17 * * 1-5"),
        ("monday-planning", "0 10 * * 1"),
        ("wednesday-review", "30 15 * * 3"),
        ("friday-summary", "45 16 * * 5"),
        ("saturday-maintenance", "0 3 * * 6"),
        ("sunday-maintenance", "30 3 * * 0"),
        ("month-start", "0 7 1 * *"),
        ("month-end", "0 20 28-31 * *"),
        ("quarterly-audit", "0 8 1 1,4,7,10 *"),
        ("nightly-burst-1", "3 1 * * *"),
        ("nightly-burst-2", "8 1 * * *"),
        ("nightly-burst-3", "13 1 * * *"),
        ("morning-burst-1", "2 8 * * 1-5"),
        ("morning-burst-2", "4 8 * * 1-5"),
        ("morning-burst-3", "6 8 * * 1-5"),
        ("afternoon-burst-1", "11 15 * * 1-5"),
        ("afternoon-burst-2", "12 15 * * 1-5"),
        ("afternoon-burst-3", "13 15 * * 1-5")
    };

    foreach (var schedule in schedules)
    {
        recurringJobs.AddOrUpdate(
            schedule.Id,
            () => TestJobs.NoOp(schedule.Id),
            schedule.Cron);
    }
}

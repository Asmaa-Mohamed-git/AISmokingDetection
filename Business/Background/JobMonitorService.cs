using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Options;
using Microsoft.Extensions.Options;

namespace AISmokingDetection.Business.Background
{
    /// <summary>
    /// Periodically (1) retries submissions the AI service never accepted and
    /// (2) flags jobs that never received a final callback. Never assumes success.
    /// </summary>
    public class JobMonitorService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly AiServiceOptions _options;
        private readonly ILogger<JobMonitorService> _logger;

        public JobMonitorService(
            IServiceScopeFactory scopeFactory,
            IOptions<AiServiceOptions> options,
            ILogger<JobMonitorService> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(10, _options.MonitorIntervalSeconds));
            using var timer = new PeriodicTimer(interval);

            while (await WaitAsync(timer, stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var jobs = scope.ServiceProvider.GetRequiredService<IProcessingJobService>();

                    await jobs.RetryPendingSubmissionsAsync(stoppingToken);
                    await jobs.FlagStuckJobsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job monitor iteration failed.");
                }
            }
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
        {
            try { return await timer.WaitForNextTickAsync(ct); }
            catch (OperationCanceledException) { return false; }
        }
    }
}

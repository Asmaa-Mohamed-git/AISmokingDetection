using System.Net.Http.Json;
using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Model;
using AISmokingDetection.Options;
using Microsoft.Extensions.Options;

namespace AISmokingDetection.Business.Services
{
    /// <summary>
    /// Sends job metadata + an internal media reference to the AI service ("Option B" in the contract).
    /// Switch to a streamed multipart upload here if the teams agree on "Option A".
    /// </summary>
    public class AiJobSubmitter : IAiJobSubmitter
    {
        private readonly HttpClient _http;
        private readonly AiServiceOptions _options;
        private readonly ILogger<AiJobSubmitter> _logger;

        public AiJobSubmitter(HttpClient http, IOptions<AiServiceOptions> options, ILogger<AiJobSubmitter> logger)
        {
            _http = http;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<AiSubmitResult> SubmitAsync(ProcessingJob job, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
                return new AiSubmitResult(false, "AI service base URL is not configured.");

            var body = new
            {
                jobId = job.JobId,
                videoId = job.VideoId,
                cameraId = job.CameraId,
                capturedAt = job.CapturedAtUtc,
                schemaVersion = job.SchemaVersion,
                videoReference = job.VideoReference
            };

            try
            {
                using var response = await _http.PostAsJsonAsync(_options.SubmitPath, body, ct);

                // 202 Accepted = accepted for asynchronous processing (not completed).
                if (response.StatusCode == System.Net.HttpStatusCode.Accepted ||
                    response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return new AiSubmitResult(true, null);
                }

                var error = $"AI service responded with {(int)response.StatusCode}.";
                _logger.LogWarning("Job {JobId} submission rejected: {Error}", job.JobId, error);
                return new AiSubmitResult(false, error);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Job {JobId} submission failed.", job.JobId);
                return new AiSubmitResult(false, "AI service unreachable or timed out.");
            }
        }
    }
}

namespace AISmokingDetection.Options
{
    public class AiServiceOptions
    {
        public const string SectionName = "AiService";

        // ---- Backend B -> AI (outbound submission) ----
        public string BaseUrl { get; set; } = string.Empty;          // e.g. https://ai-worker.internal
        public string SubmitPath { get; set; } = "/api/v1/processing-jobs";
        public string OutboundApiKey { get; set; } = string.Empty;   // credential Backend B presents to AI
        public int TimeoutSeconds { get; set; } = 30;
        public string SchemaVersion { get; set; } = "1.0";

        // ---- AI -> Backend B (inbound callbacks) ----
        // Several keys are allowed so keys can be rotated without downtime.
        public string[] InboundApiKeys { get; set; } = Array.Empty<string>();

        // ---- Reliability ----
        public int MaxSubmissionAttempts { get; set; } = 5;
        public int StuckJobTimeoutMinutes { get; set; } = 60;
        public int MonitorIntervalSeconds { get; set; } = 60;
    }

    public class EvidenceOptions
    {
        public const string SectionName = "Evidence";

        // Root folder that evidence references are resolved against.
        // Replace IEvidenceStorage if the AI team owns evidence storage.
        public string RootPath { get; set; } = string.Empty;
    }
}

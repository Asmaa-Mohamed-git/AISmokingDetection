using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using AISmokingDetection.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AISmokingDetection.Security
{
    public static class AuthConstants
    {
        public const string ApiKeyScheme = "ApiKey";
        public const string ApiKeyHeader = "X-Api-Key";
        public const string AiServicePolicy = "AiService";
        public const string AiServiceRole = "ai-service";
    }

    /// <summary>
    /// Service-to-service authentication for AI callbacks. Kept separate from the staff JWT scheme.
    /// </summary>
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AiServiceOptions _aiOptions;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IOptions<AiServiceOptions> aiOptions)
            : base(options, logger, encoder)
        {
            _aiOptions = aiOptions.Value;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(AuthConstants.ApiKeyHeader, out var provided) ||
                string.IsNullOrWhiteSpace(provided))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var providedBytes = Encoding.UTF8.GetBytes(provided.ToString());

            // Compare against every configured key (supports rotation); constant-time per key.
            bool valid = false;
            foreach (var key in _aiOptions.InboundApiKeys.Where(k => !string.IsNullOrWhiteSpace(k)))
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);
                if (CryptographicOperations.FixedTimeEquals(providedBytes, keyBytes))
                    valid = true;
            }

            if (!valid)
                return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.Name, "ai-service"),
                    new Claim(ClaimTypes.Role, AuthConstants.AiServiceRole)
                },
                Scheme.Name);

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

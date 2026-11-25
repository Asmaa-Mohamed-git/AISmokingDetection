using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Options;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace AISmokingDetection.Business.Services
{
    /// <summary>
    /// Resolves opaque evidence references (e.g. "clips/EVENT-001.mp4") against a configured root folder.
    /// Protects against path traversal. Replace with a blob/S3/AI-team-backed implementation if needed.
    /// </summary>
    public class LocalFileEvidenceStorage : IEvidenceStorage
    {
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        private readonly string _root;

        public LocalFileEvidenceStorage(IOptions<EvidenceOptions> options)
        {
            var configured = options.Value.RootPath;
            _root = string.IsNullOrWhiteSpace(configured)
                ? string.Empty
                : Path.GetFullPath(configured);
        }

        public Task<EvidenceStream?> OpenReadAsync(string reference, CancellationToken ct = default)
        {
            if (_root.Length == 0 || string.IsNullOrWhiteSpace(reference))
                return Task.FromResult<EvidenceStream?>(null);

            var rootWithSeparator = _root.EndsWith(Path.DirectorySeparatorChar)
                ? _root
                : _root + Path.DirectorySeparatorChar;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(_root, reference));
            }
            catch (Exception)
            {
                return Task.FromResult<EvidenceStream?>(null);
            }

            // Reject anything that resolves outside the evidence root.
            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fullPath))
            {
                return Task.FromResult<EvidenceStream?>(null);
            }

            if (!ContentTypes.TryGetContentType(fullPath, out var contentType))
                contentType = "application/octet-stream";

            var stream = new FileStream(
                fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 81920, useAsync: true);

            return Task.FromResult<EvidenceStream?>(new EvidenceStream(stream, contentType));
        }
    }
}

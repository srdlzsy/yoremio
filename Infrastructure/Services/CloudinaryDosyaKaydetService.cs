using Application.Interfaces;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Infrastructure.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class CloudinaryDosyaKaydetService : IDosyaKaydetService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinaryOptions _options;
        private readonly ILogger<CloudinaryDosyaKaydetService> _logger;

        public CloudinaryDosyaKaydetService(
            IOptions<CloudinaryOptions> options,
            ILogger<CloudinaryDosyaKaydetService> logger)
        {
            _options = options.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_options.CloudName)
                || string.IsNullOrWhiteSpace(_options.ApiKey)
                || string.IsNullOrWhiteSpace(_options.ApiSecret))
            {
                throw new InvalidOperationException("Cloudinary ayarlari eksik. Cloudinary:CloudName, ApiKey ve ApiSecret zorunludur.");
            }

            _cloudinary = new Cloudinary(new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret));
            _cloudinary.Api.Secure = true;
        }

        public async Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
        {
            if (dosya == null || dosya.Length == 0)
            {
                throw new ArgumentException("Dosya gecersiz.", nameof(dosya));
            }

            if (string.IsNullOrWhiteSpace(dosya.FileName) || string.IsNullOrWhiteSpace(Path.GetExtension(dosya.FileName)))
            {
                throw new ArgumentException("Dosya uzantisi bulunamadi.", nameof(dosya));
            }

            var folder = BuildFolder(klasor);
            await using var stream = dosya.OpenReadStream();

            var uploadResult = dosya.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                ? await UploadVideoAsync(dosya.FileName, stream, folder)
                : await UploadImageAsync(dosya.FileName, stream, folder);

            if (uploadResult.Error != null)
            {
                _logger.LogWarning("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                throw new InvalidOperationException("Dosya Cloudinary'ye yuklenemedi.");
            }

            var secureUrl = uploadResult.SecureUrl?.ToString();
            if (string.IsNullOrWhiteSpace(secureUrl))
            {
                throw new InvalidOperationException("Cloudinary guvenli dosya URL'i donmedi.");
            }

            return secureUrl;
        }

        public async Task SilDosyaAsync(string url)
        {
            if (!TryParseCloudinaryPublicId(url, out var publicId, out var resourceType))
            {
                _logger.LogDebug("Cloudinary delete skipped for non-Cloudinary URL: {Url}", url);
                return;
            }

            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId)
            {
                ResourceType = resourceType
            });

            if (result.Error != null)
            {
                _logger.LogWarning("Cloudinary delete failed for {PublicId}: {Error}", publicId, result.Error.Message);
                throw new InvalidOperationException("Dosya Cloudinary'den silinemedi.");
            }
        }

        private async Task<UploadResult> UploadImageAsync(string fileName, Stream stream, string folder)
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            return await _cloudinary.UploadAsync(uploadParams);
        }

        private async Task<UploadResult> UploadVideoAsync(string fileName, Stream stream, string folder)
        {
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(fileName, stream),
                Folder = folder,
                UseFilename = false,
                UniqueFilename = true,
                Overwrite = false
            };

            return await _cloudinary.UploadAsync(uploadParams);
        }

        private string BuildFolder(string klasor)
        {
            var root = SanitizePathSegment(_options.UploadFolder);
            var safeSegments = klasor
                .Split(new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(SanitizePathSegment)
                .ToArray();

            if (safeSegments.Length == 0)
            {
                throw new ArgumentException("Klasor yolu gecersiz.", nameof(klasor));
            }

            return string.Join("/", new[] { root }.Concat(safeSegments));
        }

        private static string SanitizePathSegment(string segment)
        {
            var normalized = segment
                .Trim()
                .ToLowerInvariant()
                .Replace("ı", "i")
                .Replace("ö", "o")
                .Replace("ü", "u")
                .Replace("ş", "s")
                .Replace("ğ", "g")
                .Replace("ç", "c");

            normalized = Regex.Replace(normalized, @"\s+", "-");
            normalized = Regex.Replace(normalized, @"[^a-z0-9_-]", "-");
            normalized = Regex.Replace(normalized, @"-+", "-").Trim('-');

            if (string.IsNullOrWhiteSpace(normalized))
            {
                throw new ArgumentException("Klasor yolu gecersiz.");
            }

            return normalized;
        }

        private static bool TryParseCloudinaryPublicId(string url, out string publicId, out ResourceType resourceType)
        {
            publicId = string.Empty;
            resourceType = ResourceType.Image;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || !uri.Host.Contains("cloudinary.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var segments = uri.AbsolutePath
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Uri.UnescapeDataString)
                .ToArray();

            var uploadIndex = Array.FindIndex(segments, segment => string.Equals(segment, "upload", StringComparison.OrdinalIgnoreCase));
            if (uploadIndex < 1 || uploadIndex >= segments.Length - 1)
            {
                return false;
            }

            resourceType = string.Equals(segments[uploadIndex - 1], "video", StringComparison.OrdinalIgnoreCase)
                ? ResourceType.Video
                : ResourceType.Image;

            var publicIdSegments = segments.Skip(uploadIndex + 1).ToList();
            if (publicIdSegments.Count > 0
                && publicIdSegments[0].Length > 1
                && publicIdSegments[0][0] == 'v'
                && publicIdSegments[0].Skip(1).All(char.IsDigit))
            {
                publicIdSegments.RemoveAt(0);
            }

            if (publicIdSegments.Count == 0)
            {
                return false;
            }

            var lastSegment = publicIdSegments[^1];
            var extensionIndex = lastSegment.LastIndexOf('.');
            if (extensionIndex > 0)
            {
                publicIdSegments[^1] = lastSegment[..extensionIndex];
            }

            publicId = string.Join("/", publicIdSegments);
            return !string.IsNullOrWhiteSpace(publicId);
        }
    }
}

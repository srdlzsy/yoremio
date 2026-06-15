using Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services
{
    public class DosyaKaydetService : IDosyaKaydetService
    {
        private readonly IWebHostEnvironment _environment;

        public DosyaKaydetService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> KaydetDosyaAsync(IFormFile dosya, string klasor)
        {
            if (dosya == null || dosya.Length == 0)
            {
                throw new ArgumentException("Dosya geçerli değil.", nameof(dosya));
            }

            var extension = Path.GetExtension(dosya.FileName);
            if (string.IsNullOrWhiteSpace(extension))
            {
                throw new ArgumentException("Dosya uzantısı bulunamadı.", nameof(dosya));
            }

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var webRootFullPath = Path.GetFullPath(webRootPath);
            Directory.CreateDirectory(webRootFullPath);

            var safeSegments = klasor
                .Split(new[] { '/', '\\', Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(SanitizePathSegment)
                .ToArray();

            if (safeSegments.Length == 0)
            {
                throw new ArgumentException("Klasör yolu geçerli değil.", nameof(klasor));
            }

            var targetFolder = Path.GetFullPath(Path.Combine(new[] { webRootFullPath }.Concat(safeSegments).ToArray()));
            EnsurePathInsideRoot(webRootFullPath, targetFolder);
            Directory.CreateDirectory(targetFolder);

            var dosyaAdi = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var tamYol = Path.Combine(targetFolder, dosyaAdi);

            await using (var stream = new FileStream(tamYol, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await dosya.CopyToAsync(stream);
            }

            return "/" + string.Join("/", safeSegments.Append(dosyaAdi));
        }

        public Task SilDosyaAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                return Task.CompletedTask;
            }

            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            var webRootFullPath = Path.GetFullPath(webRootPath);
            var relativePath = url.TrimStart('/', '\\')
                .Replace('/', Path.DirectorySeparatorChar)
                .Replace('\\', Path.DirectorySeparatorChar);
            var targetPath = Path.GetFullPath(Path.Combine(webRootFullPath, relativePath));

            EnsurePathInsideRoot(webRootFullPath, targetPath);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            return Task.CompletedTask;
        }

        private static string SanitizePathSegment(string segment)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(segment
                .Where(character => !invalidChars.Contains(character))
                .ToArray())
                .Trim()
                .Trim('.');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                throw new ArgumentException("Klasör yolu geçerli değil.");
            }

            return sanitized;
        }

        private static void EnsurePathInsideRoot(string rootPath, string targetPath)
        {
            var normalizedRoot = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            var normalizedTarget = targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!normalizedTarget.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Dosya yolu uygulama kökünün dışında olamaz.");
            }
        }
    }
}

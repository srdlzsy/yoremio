using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Application.Validation
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class AllowedContentTypesAttribute : ValidationAttribute
    {
        private readonly string[] _allowedPrefixes;

        public AllowedContentTypesAttribute(params string[] allowedPrefixes)
        {
            _allowedPrefixes = allowedPrefixes;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IEnumerable<IFormFile> files)
                return ValidationResult.Success;

            foreach (var file in files)
            {
                if (_allowedPrefixes.All(prefix => !file.ContentType.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    return new ValidationResult(ErrorMessage);

                if (!HasAllowedExtensionAndSignature(file))
                    return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }

        private bool HasAllowedExtensionAndSignature(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (_allowedPrefixes.Any(prefix => prefix.Equals("image/", StringComparison.OrdinalIgnoreCase)))
            {
                return extension is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp"
                    && HasImageSignature(file);
            }

            if (_allowedPrefixes.Any(prefix => prefix.Equals("video/", StringComparison.OrdinalIgnoreCase)))
            {
                return extension is ".mp4" or ".webm" or ".mov"
                    && HasVideoSignature(file);
            }

            return true;
        }

        private static bool HasImageSignature(IFormFile file)
        {
            var header = ReadHeader(file, 16);
            return StartsWith(header, [0xFF, 0xD8, 0xFF])
                || StartsWith(header, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
                || StartsWith(header, "GIF87a"u8.ToArray())
                || StartsWith(header, "GIF89a"u8.ToArray())
                || IsWebp(header);
        }

        private static bool HasVideoSignature(IFormFile file)
        {
            var header = ReadHeader(file, 16);
            return StartsWith(header, [0x1A, 0x45, 0xDF, 0xA3])
                || HasMp4LikeSignature(header);
        }

        private static byte[] ReadHeader(IFormFile file, int length)
        {
            using var stream = file.OpenReadStream();
            var buffer = new byte[length];
            var read = stream.Read(buffer, 0, buffer.Length);
            return buffer.Take(read).ToArray();
        }

        private static bool StartsWith(byte[] value, byte[] prefix)
        {
            return value.Length >= prefix.Length && prefix.SequenceEqual(value.Take(prefix.Length));
        }

        private static bool IsWebp(byte[] header)
        {
            return header.Length >= 12
                && StartsWith(header, "RIFF"u8.ToArray())
                && header.Skip(8).Take(4).SequenceEqual("WEBP"u8.ToArray());
        }

        private static bool HasMp4LikeSignature(byte[] header)
        {
            return header.Length >= 12
                && header.Skip(4).Take(4).SequenceEqual("ftyp"u8.ToArray());
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly long _maxBytes;

        public MaxFileSizeAttribute(long maxBytes)
        {
            _maxBytes = maxBytes;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not IEnumerable<IFormFile> files)
                return ValidationResult.Success;

            foreach (var file in files)
            {
                if (file.Length > _maxBytes)
                    return new ValidationResult(ErrorMessage);
            }

            return ValidationResult.Success;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class PositiveDecimalAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is null)
                return ValidationResult.Success;

            if (value is decimal decimalValue && decimalValue > 0)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }
    }
}

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
            }

            return ValidationResult.Success;
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

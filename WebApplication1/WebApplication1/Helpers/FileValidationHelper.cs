using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace WebApplication1.Helpers
{
    public static class FileValidationHelper
    {
        private const long MaxFileSizeBytes = 12 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        public static (bool IsValid, string ErrorMessage) ValidateFile(IFormFile file, bool imagesOnly = false)
        {
            if (file == null || file.Length == 0)
            {
                return (false, "Please select a valid file.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return (false, "Fileee size exceeds the maximum allowed limit of 12 MB.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowed = imagesOnly ? new[] { ".jpg", ".jpeg", ".png" } : AllowedExtensions;

            if (!allowed.Contains(extension))
            {
                return (false, "Invalid file extension. Only .jpg, .jpeg, .png, and .pdf are allowed.");
            }

            return (true, string.Empty);
        }
    }
}
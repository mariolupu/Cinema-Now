using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Helpers;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(UserManager<IdentityUser> userManager, IWebHostEnvironment environment, ILogger<ProfileController> logger)
        {
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Avatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.AvatarUrl = GetAvatarUrl(user.Id);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Avatar(IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var validation = FileValidationHelper.ValidateFile(avatarFile, imagesOnly: true);
                if (!validation.IsValid)
                {
                    _logger.LogWarning("User {Email} failed to upload profile picture: {Error}", user.Email, validation.ErrorMessage);
                    TempData["Error"] = validation.ErrorMessage;
                    ViewBag.AvatarUrl = GetAvatarUrl(user.Id);
                    return View();
                }

                var avatarsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(avatarsFolder);

                foreach (var oldFile in Directory.GetFiles(avatarsFolder, $"{user.Id}.*"))
                {
                    System.IO.File.Delete(oldFile);
                }

                var extension = Path.GetExtension(avatarFile.FileName).ToLowerInvariant();
                var filePath = Path.Combine(avatarsFolder, $"{user.Id}{extension}");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                _logger.LogInformation("User {Email} successfully updated their profile picture.", user.Email);
                TempData["Success"] = "Profile picture updated successfully!";
            }
            else
            {
                TempData["Success"] = "Changes saved!";
            }

            return RedirectToAction(nameof(Avatar));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAvatar()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var avatarsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (Directory.Exists(avatarsFolder))
            {
                foreach (var oldFile in Directory.GetFiles(avatarsFolder, $"{user.Id}.*"))
                {
                    System.IO.File.Delete(oldFile);
                }
            }

            _logger.LogInformation("User {Email} removed their profile picture.", user.Email);
            TempData["Success"] = "Profile picture removed successfully!";
            return RedirectToAction(nameof(Avatar));
        }

        private string? GetAvatarUrl(string userId)
        {
            var avatarsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            if (!Directory.Exists(avatarsFolder)) return null;

            var existing = Directory.GetFiles(avatarsFolder, $"{userId}.*").FirstOrDefault();
            return existing != null ? $"/uploads/avatars/{Path.GetFileName(existing)}?v={DateTime.Now.Ticks}" : null;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadsController : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg"
    };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public UploadsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxBytes)]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] string folder = "questions")
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));

        if (file.Length > MaxBytes)
            return BadRequest(ApiResponse<object>.Fail($"File too large (max {MaxBytes / (1024 * 1024)} MB)."));

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail($"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}"));

        var safeFolder = string.Join("_", folder.Split(Path.GetInvalidFileNameChars()));
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadsRoot = Path.Combine(webRoot, "uploads", safeFolder);
        Directory.CreateDirectory(uploadsRoot);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(uploadsRoot, fileName);

        await using (var stream = System.IO.File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/{safeFolder}/{fileName}";
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{relativeUrl}";

        return Ok(ApiResponse<UploadResult>.Ok(new UploadResult
        {
            Url = relativeUrl,
            AbsoluteUrl = absoluteUrl,
            FileName = fileName,
            Size = file.Length
        }));
    }

    public class UploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string AbsoluteUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}

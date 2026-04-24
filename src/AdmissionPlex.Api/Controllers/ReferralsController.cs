using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReferralsController : ControllerBase
{
    private readonly IReferralService _referralService;

    public ReferralsController(IReferralService referralService) => _referralService = referralService;

    [HttpGet("my-code")]
    public async Task<IActionResult> GetMyCode()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var code = await _referralService.GenerateCodeAsync(userId);
        return Ok(ApiResponse<object>.Ok(new { Code = code }));
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyCode([FromBody] ApplyReferralDto dto)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var (success, error) = await _referralService.ApplyCodeAsync(userId, dto.Code);
        if (!success) return BadRequest(ApiResponse<object>.Fail(error!));
        return Ok(ApiResponse<object>.Ok(new { }, "Referral applied successfully."));
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var stats = await _referralService.GetStatsAsync(userId);
        return Ok(ApiResponse<object>.Ok(stats));
    }
}

public class ApplyReferralDto
{
    public string Code { get; set; } = "";
}

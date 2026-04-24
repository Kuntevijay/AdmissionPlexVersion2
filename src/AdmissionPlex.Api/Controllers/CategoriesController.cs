using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly IQuestionRepository _questionRepo;

    public CategoriesController(IQuestionRepository questionRepo)
    {
        _questionRepo = questionRepo;
    }

    /// <summary>
    /// Get all 10 interest categories (FA, PA, MT, ME, PI, SO, WN, RA, LU, OS)
    /// </summary>
    [HttpGet("interest-categories")]
    public async Task<IActionResult> GetInterestCategories()
    {
        var categories = await _questionRepo.GetAllInterestCategoriesAsync();
        return Ok(ApiResponse<object>.Ok(categories));
    }

    /// <summary>
    /// Get all 7 aptitude categories (SA, NC, MA, NA, VA, LA, SP)
    /// </summary>
    [HttpGet("aptitude-categories")]
    public async Task<IActionResult> GetAptitudeCategories()
    {
        var categories = await _questionRepo.GetAllAptitudeCategoriesAsync();
        return Ok(ApiResponse<object>.Ok(categories));
    }
}

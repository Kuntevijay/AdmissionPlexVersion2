using Microsoft.AspNetCore.Mvc;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public HealthController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var interestCount = await _unitOfWork.Questions.CountAsync();
        var response = ApiResponse<object>.Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Database = "Connected",
            QuestionsInBank = interestCount
        });
        return Ok(response);
    }
}

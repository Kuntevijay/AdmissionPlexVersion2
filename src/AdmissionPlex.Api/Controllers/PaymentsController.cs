using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AdmissionPlex.Core.Interfaces.Repositories;
using AdmissionPlex.Core.Interfaces.Services;
using AdmissionPlex.Shared.Common;

namespace AdmissionPlex.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ICCavenueService _ccavenue;
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;

    public PaymentsController(IPaymentService paymentService, ICCavenueService ccavenue, IUnitOfWork uow, IConfiguration config)
    {
        _paymentService = paymentService;
        _ccavenue = ccavenue;
        _uow = uow;
        _config = config;
    }

    [Authorize]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiatePaymentDto dto)
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var email = User.FindFirstValue(ClaimTypes.Email) ?? "";

        var payment = await _paymentService.InitiatePaymentAsync(userId, dto.Amount, dto.PaymentFor, dto.ReferenceId);

        var redirectUrl = _config["CCAvenue:RedirectUrl"] ?? $"{Request.Scheme}://{Request.Host}/api/payments/ccavenue/response";
        var cancelUrl = _config["CCAvenue:CancelUrl"] ?? $"{Request.Scheme}://{Request.Host}/api/payments/ccavenue/cancel";

        var formHtml = _ccavenue.BuildRequestForm(
            payment.OrderId, payment.Amount, payment.Currency,
            redirectUrl, cancelUrl, email, null);

        return Ok(ApiResponse<object>.Ok(new
        {
            payment.OrderId,
            payment.Amount,
            FormHtml = formHtml
        }));
    }

    [HttpPost("ccavenue/response")]
    [AllowAnonymous]
    public async Task<IActionResult> CcavenueResponse([FromForm] string encResponse)
    {
        try
        {
            var payment = await _paymentService.ProcessResponseAsync(encResponse);
            var clientUrl = _config["ClientUrl"] ?? "https://localhost:7002";
            var status = payment.Status.ToString().ToLower();
            return Redirect($"{clientUrl}/student/payment-result?status={status}&orderId={payment.OrderId}");
        }
        catch (Exception ex)
        {
            var clientUrl = _config["ClientUrl"] ?? "https://localhost:7002";
            return Redirect($"{clientUrl}/student/payment-result?status=error&message={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpPost("ccavenue/cancel")]
    [AllowAnonymous]
    public async Task<IActionResult> CcavenueCancel([FromForm] string encResponse)
    {
        try
        {
            await _paymentService.ProcessCancellationAsync(encResponse);
        }
        catch { }

        var clientUrl = _config["ClientUrl"] ?? "https://localhost:7002";
        return Redirect($"{clientUrl}/student/payment-result?status=cancelled");
    }

    [Authorize]
    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetByOrderId(string orderId)
    {
        var payment = await _uow.Payments.GetByOrderIdAsync(orderId);
        if (payment == null) return NotFound(ApiResponse<object>.Fail("Payment not found."));

        return Ok(ApiResponse<object>.Ok(new
        {
            payment.OrderId, payment.Amount, payment.Currency,
            Status = payment.Status.ToString(),
            PaymentFor = payment.PaymentFor.ToString(),
            payment.PaymentMode, payment.PaidAt, payment.StatusMessage
        }));
    }

    [Authorize]
    [HttpGet("my-payments")]
    public async Task<IActionResult> GetMyPayments()
    {
        var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
        var payments = await _uow.Payments.GetByUserIdAsync(userId);
        return Ok(ApiResponse<object>.Ok(payments.Select(p => new
        {
            p.OrderId, p.Amount, Status = p.Status.ToString(),
            PaymentFor = p.PaymentFor.ToString(), p.PaidAt, p.CreatedAt
        })));
    }
}

public class InitiatePaymentDto
{
    public decimal Amount { get; set; }
    public string PaymentFor { get; set; } = "";
    public long? ReferenceId { get; set; }
}

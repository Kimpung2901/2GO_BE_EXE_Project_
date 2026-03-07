using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/payos/webhook")]
[AllowAnonymous]
[EnableCors("WebhookPolicy")]
public class PayOSWebhookController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PayOSWebhookController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult> ReceiveWebhook(Webhook webhook, CancellationToken cancellationToken = default)
    {
        if (webhook == null)
        {
            return BadRequest("Dữ liệu webhook là bắt buộc");
        }

        try
        {
            var webhookData = await _paymentService.VerifyWebhookSignatureAsync(webhook, cancellationToken);
            
            // Handle PayOS test webhook for dashboard configuration
            if (webhookData.OrderCode == 123 && webhookData.Description == "VQRIO123" && webhookData.AccountNumber == "12345678")
            {
                return Ok(new { message = "Webhook processed successfully" });
            }
            
            // Process real webhook
            var result = await _paymentService.HandlePayOSWebhookAsync(webhook, cancellationToken);
            
            if (!result.Success)
            {
                return Ok(new { error = result.Message, message = "Đã nhận webhook nhưng xử lý thất bại" });
            }

            return Ok(new { message = "Webhook processed successfully", orderCode = webhookData.OrderCode });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PayOS Webhook Error] {ex.Message}");
            return Problem(ex.Message);
        }
    }
}




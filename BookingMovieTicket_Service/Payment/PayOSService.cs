using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Payment;

public class PayOSService : IPayOSService
{
    private readonly PayOSClient _client;
    private readonly ILogger<PayOSService> _logger;

    public PayOSService(IConfiguration configuration, ILogger<PayOSService> logger)
    {
        _logger = logger;

        var clientId = configuration["PayOS:ClientId"] ?? throw new ArgumentNullException("PayOS:ClientId is missing in configuration");
        var apiKey = configuration["PayOS:ApiKey"] ?? throw new ArgumentNullException("PayOS:ApiKey is missing in configuration");
        var checksumKey = configuration["PayOS:ChecksumKey"] ?? throw new ArgumentNullException("PayOS:ChecksumKey is missing in configuration");

        _client = new PayOSClient(clientId, apiKey, checksumKey);
    }

    /// <summary>
    /// Tạo liên kết thanh toán PayOS VietQR kèm mã QR và URL chuyển khoản
    /// </summary>
    public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string returnUrl,
        string cancelUrl,
        List<PaymentLinkItem>? items = null,
        string? buyerName = null,
        string? buyerEmail = null,
        string? buyerPhone = null)
    {
        try
        {
            var request = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = amount,
                Description = description.Length > 25 ? description.Substring(0, 25) : description,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                Items = items ?? new List<PaymentLinkItem>(),
                BuyerName = buyerName,
                BuyerEmail = buyerEmail,
                BuyerPhone = buyerPhone
            };

            var result = await _client.PaymentRequests.CreateAsync(request);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gọi API tạo liên kết thanh toán PayOS cho OrderCode={OrderCode}", orderCode);
            throw;
        }
    }

    /// <summary>
    /// Lấy thông tin trạng thái liên kết thanh toán trực tiếp từ PayOS
    /// </summary>
    public async Task<PaymentLink> GetPaymentLinkInformationAsync(long orderCode)
    {
        try
        {
            var result = await _client.PaymentRequests.GetAsync(orderCode);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi truy vấn thông tin thanh toán từ PayOS cho OrderCode={OrderCode}", orderCode);
            throw;
        }
    }

    /// <summary>
    /// Xác thực chữ ký webhook và lấy dữ liệu giao dịch từ PayOS
    /// </summary>
    public async Task<WebhookData> VerifyPaymentWebhookDataAsync(Webhook webhookBody)
    {
        try
        {
            var data = await _client.Webhooks.VerifyAsync(webhookBody);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xác thực dữ liệu Webhook từ PayOS.");
            throw;
        }
    }
}


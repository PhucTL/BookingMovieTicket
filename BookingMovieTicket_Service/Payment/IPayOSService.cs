using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Payment;

public interface IPayOSService
{
    /// <summary>
    /// Tạo liên kết thanh toán PayOS VietQR kèm mã QR và URL chuyển khoản
    /// </summary>
    Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(
        long orderCode,
        int amount,
        string description,
        string returnUrl,
        string cancelUrl,
        List<PaymentLinkItem>? items = null,
        string? buyerName = null,
        string? buyerEmail = null,
        string? buyerPhone = null);

    /// <summary>
    /// Lấy thông tin trạng thái liên kết thanh toán trực tiếp từ PayOS
    /// </summary>
    Task<PaymentLink> GetPaymentLinkInformationAsync(long orderCode);

    /// <summary>
    /// Xác thực chữ ký webhook và lấy dữ liệu giao dịch từ PayOS
    /// </summary>
    Task<WebhookData> VerifyPaymentWebhookDataAsync(Webhook webhookBody);
}


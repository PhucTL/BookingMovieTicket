using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otp, int expireMinutes = 5, string purpose = "xác thực tài khoản")
    {
        // Ghi log mã OTP để nhà phát triển luôn kiểm tra và test được ngay trên Console
        _logger.LogInformation("==================================================");
        _logger.LogInformation("[OTP NOTIFICATION] ({Purpose}) Email: {Email} | Mã OTP: {Otp} | Hết hạn sau: {ExpireMinutes} phút", purpose, toEmail, otp, expireMinutes);
        _logger.LogInformation("==================================================");
        Console.WriteLine($"\n[OTP NOTIFICATION] >>> Mã OTP ({purpose}) cho {toEmail} là: {otp} (Hiệu lực {expireMinutes} phút) <<<\n");

        var subject = $"[BookingMovieTicket] Mã xác thực OTP {purpose}: {otp}";
        var htmlBody = $@"
        <div style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;"">
            <h2 style=""color: #e50914; text-align: center;"">Booking Movie Ticket</h2>
            <hr style=""border: 0; border-top: 1px solid #eee;"">
            <p>Xin chào,</p>
            <p>Bạn đang thực hiện <strong>{purpose}</strong> tại <strong>Booking Movie Ticket</strong>. Đây là mã xác thực OTP của bạn:</p>
            <div style=""text-align: center; margin: 30px 0;"">
                <span style=""font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #1a73e8; background: #f1f3f4; padding: 12px 24px; border-radius: 6px; display: inline-block;"">
                    {otp}
                </span>
            </div>
            <p style=""color: #555;"">Mã OTP này có hiệu lực trong vòng <strong>{expireMinutes} phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai.</p>
            <p style=""color: #888; font-size: 13px;"">Nếu bạn không yêu cầu thao tác này, xin vui lòng bỏ qua email này.</p>
            <hr style=""border: 0; border-top: 1px solid #eee;"">
            <p style=""color: #aaa; font-size: 12px; text-align: center;"">© 2026 Booking Movie Ticket System. All rights reserved.</p>
        </div>";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
        var smtpPort = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var port) ? port : 587;
        var senderName = _configuration["EmailSettings:SenderName"] ?? "Booking Movie Ticket";
        var senderEmail = _configuration["EmailSettings:SenderEmail"];
        var appPassword = _configuration["EmailSettings:AppPassword"];

        // Nếu chưa cấu hình SenderEmail hoặc AppPassword, bỏ qua bước gửi SMTP thực tế để không crash ứng dụng khi dev
        if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword) || appPassword.Contains("your-app-password"))
        {
            _logger.LogWarning("[EMAIL WARNING] EmailSettings chưa được cấu hình đầy đủ trong appsettings.json. Mã OTP đã được in ra console.");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(new MailboxAddress(toEmail, toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            // Gmail dùng cổng 587 với StartTls
            await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("[EMAIL SUCCESS] Đã gửi email thành công tới {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EMAIL ERROR] Lỗi khi gửi email tới {ToEmail}: {Message}", toEmail, ex.Message);
            // Không ném Exception ra ngoài để luồng xử lý xác thực không bị gián đoạn, mã OTP vẫn có trên log
        }
    }
}


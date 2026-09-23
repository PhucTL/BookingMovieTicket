using System.Threading.Tasks;

namespace BookingMovieTicket_Service.Authentication.Email;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string otp, int expireMinutes = 5, string purpose = "xác thực tài khoản");
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}


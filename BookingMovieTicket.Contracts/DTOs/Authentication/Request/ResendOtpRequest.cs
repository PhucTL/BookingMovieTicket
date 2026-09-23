using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Request;

public class ResendOtpRequest
{
    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public string Email { get; set; } = string.Empty;
}


using System;
using System.Text.Json.Serialization;

namespace BookingMovieTicket.Contracts.DTOs.Authentication.Response;

public class LoginResponse
{
    [JsonPropertyOrder(1)]
    public string? Token { get; set; } = string.Empty;

    [JsonPropertyOrder(2)]
    public string? RefreshToken { get; set; }

    [JsonPropertyOrder(3)]
    public Guid Id { get; set; }

    [JsonPropertyOrder(4)]
    public string? Username { get; set; }

    [JsonPropertyOrder(5)]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyOrder(6)]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyOrder(7)]
    public short Role { get; set; }

    [JsonPropertyOrder(8)]
    public string RoleName => Enum.IsDefined(typeof(BookingMovieTicket.Contracts.Enums.UserRole), Role)
        ? ((BookingMovieTicket.Contracts.Enums.UserRole)Role).ToString()
        : Role.ToString();
}


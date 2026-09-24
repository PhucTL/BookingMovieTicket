namespace BookingMovieTicket.Contracts.Constants;

/// <summary>
/// Các hằng số Role dùng cho Attribute [Authorize(Roles = ...)]
/// </summary>
public static class RoleConstants
{
    public const string User = "User";
    public const string Staff = "Staff";

    /// <summary>
    /// Cho phép cả User và Staff
    /// </summary>
    public const string All = "User,Staff";
}


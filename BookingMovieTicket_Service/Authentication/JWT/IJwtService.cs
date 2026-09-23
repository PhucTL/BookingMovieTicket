using System;

namespace BookingMovieTicket_Service.Authentication.JWT;

public interface IJwtService
{
    string GenerateToken(Guid userId, string username, string email, short role);
}


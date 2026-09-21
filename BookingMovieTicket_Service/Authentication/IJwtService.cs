using System;

namespace BookingMovieTicket_Service.Authentication;

public interface IJwtService
{
    string GenerateToken(Guid userId, string username, string email, short role);
}


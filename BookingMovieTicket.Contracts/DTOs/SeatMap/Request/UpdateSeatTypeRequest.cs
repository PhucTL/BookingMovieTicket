using BookingMovieTicket.Contracts.Enums;
using System.ComponentModel.DataAnnotations;

namespace BookingMovieTicket.Contracts.DTOs.SeatMap.Request;

public class UpdateSeatTypeRequest
{
    [Required(ErrorMessage = "Vui lòng chọn loại ghế mới (SeatType).")]
    public SeatType SeatType { get; set; }
}


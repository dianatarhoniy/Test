using TestApbd.Models.DTOs;

namespace TestApbd.Services;

public interface IDbService
{
    Task<AppointmentDetailsDto> GetAppointmentByIdAsync(int appointmentId);
    Task AddAppointmentAsync(CreateAppointmentRequestDto request);
}
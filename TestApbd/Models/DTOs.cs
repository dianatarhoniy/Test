namespace TestApbd.Models.DTOs;

public class AppointmentDetailsDto
{
    public DateTime Date { get; set; }
    public PatientDto Patient { get; set; } = new();
    public DoctorDto Doctor { get; set; } = new();
    public List<AppointmentServiceDto> AppointmentServices { get; set; } = [];
}

public class PatientDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName  { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
}

public class DoctorDto
{
    public int DoctorId { get; set; }
    public string Pwz { get; set; } = string.Empty;
}

public class AppointmentServiceDto
{
    public string Name { get; set; } = string.Empty;
    public decimal ServiceFee { get; set; }
}
public class CreateAppointmentRequestDto
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string Pwz { get; set; } = string.Empty;
    public List<ServiceInputDto> Services { get; set; } = new();
}

public class ServiceInputDto
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal ServiceFee  { get; set; }
}
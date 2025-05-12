using System.Data.Common;
using TestApbd.Exceptions;
using TestApbd.Models.DTOs;
using Microsoft.Data.SqlClient;
using TestApbd.Services;

namespace TestApbd.Services;

public class DbService : IDbService
{
    private readonly string _connectionString;
    public DbService(IConfiguration cfg) =>
        _connectionString = cfg.GetConnectionString("Default") ?? string.Empty;
    
    public async Task<AppointmentDetailsDto> GetAppointmentByIdAsync(int appointmentId)
    {
        const string sql = """
            SELECT  a.date,
                    p.first_name, p.last_name, p.date_of_birth,
                    d.doctor_id, d.pwz,
                    s.name, aps.service_fee
            FROM Appointment            a
            JOIN Patient                p   ON p.patient_id  = a.patient_id
            JOIN Doctor                 d   ON d.doctor_id   = a.doctor_id
            LEFT JOIN Appointment_Service aps ON aps.appoitment_id = a.appoitment_id
            LEFT JOIN Service           s   ON s.service_id  = aps.service_id
            WHERE a.appoitment_id = @Id;
            """;

        await using SqlConnection conn = new(_connectionString);
        await using SqlCommand    cmd  = new(sql, conn);
        cmd.Parameters.AddWithValue("@Id", appointmentId);
        await conn.OpenAsync();

        var rdr = await cmd.ExecuteReaderAsync();
        AppointmentDetailsDto? dto = null;

        while (await rdr.ReadAsync())
        {
            dto ??= new AppointmentDetailsDto
            {
                Date = rdr.GetDateTime(0),
                Patient = new PatientDto
                {
                    FirstName   = rdr.GetString(1),
                    LastName    = rdr.GetString(2),
                    DateOfBirth = rdr.GetDateTime(3)
                },
                Doctor = new DoctorDto
                {
                    DoctorId = rdr.GetInt32(4),
                    Pwz      = rdr.GetString(5)
                },
                AppointmentServices = new List<AppointmentServiceDto>()
            };

            if (!await rdr.IsDBNullAsync(6))
            {
                dto!.AppointmentServices.Add(new()
                {
                    Name       = rdr.GetString(6),
                    ServiceFee = rdr.GetDecimal(7)
                });
            }
        }

        return dto ?? throw new NotFoundException("Appointment not found.");
    }
    
    public async Task AddAppointmentAsync(CreateAppointmentRequestDto req)
    {
        if (req.Services.Count == 0)
            throw new ArgumentException("At least one service is required.");

        await using SqlConnection conn = new(_connectionString);
        await conn.OpenAsync();
        DbTransaction tx = await conn.BeginTransactionAsync();

        try
        {
            await using SqlCommand cmd = new() { Connection = conn, Transaction = (SqlTransaction)tx };
            
            cmd.CommandText = "SELECT 1 FROM Appointment WHERE appoitment_id = @AppId";
            cmd.Parameters.AddWithValue("@AppId", req.AppointmentId);
            if (await cmd.ExecuteScalarAsync() is not null)
                throw new ConflictException($"Appointment {req.AppointmentId} already exists.");
            
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT 1 FROM Patient WHERE patient_id = @PatId";
            cmd.Parameters.AddWithValue("@PatId", req.PatientId);
            if (await cmd.ExecuteScalarAsync() is null)
                throw new NotFoundException($"Patient {req.PatientId} not found.");
            
            cmd.Parameters.Clear();
            cmd.CommandText = "SELECT doctor_id FROM Doctor WHERE pwz = @Pwz";
            cmd.Parameters.AddWithValue("@Pwz", req.Pwz);
            object? doctorIdObj = await cmd.ExecuteScalarAsync();
            if (doctorIdObj is null)
                throw new NotFoundException($"Doctor with PWZ {req.Pwz} not found.");
            int doctorId = (int)doctorIdObj;
            
            cmd.Parameters.Clear();
            cmd.CommandText = """
                INSERT INTO Appointment (appoitment_id, patient_id, doctor_id, date)
                VALUES (@AppId, @PatId, @DocId, SYSUTCDATETIME());
                """;
            cmd.Parameters.AddWithValue("@AppId", req.AppointmentId);
            cmd.Parameters.AddWithValue("@PatId", req.PatientId);
            cmd.Parameters.AddWithValue("@DocId", doctorId);
            await cmd.ExecuteNonQueryAsync();
            
            foreach (var s in req.Services)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "SELECT service_id FROM Service WHERE name = @Name";
                cmd.Parameters.AddWithValue("@Name", s.ServiceName);
                object? srvIdObj = await cmd.ExecuteScalarAsync();
                if (srvIdObj is null)
                    throw new NotFoundException($"Service '{s.ServiceName}' not found.");
                int srvId = (int)srvIdObj;

                cmd.Parameters.Clear();
                cmd.CommandText = """
                    INSERT INTO Appointment_Service (appoitment_id, service_id, service_fee)
                    VALUES (@AppId, @SrvId, @Fee);
                    """;
                cmd.Parameters.AddWithValue("@AppId", req.AppointmentId);
                cmd.Parameters.AddWithValue("@SrvId", srvId);
                cmd.Parameters.AddWithValue("@Fee", s.ServiceFee);
                await cmd.ExecuteNonQueryAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
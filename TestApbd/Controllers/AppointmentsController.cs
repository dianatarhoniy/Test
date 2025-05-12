using TestApbd.Exceptions;
using TestApbd.Models.DTOs;
using TestApbd.Services;
using Microsoft.AspNetCore.Mvc;

namespace TestApbd.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AppointmentsController : ControllerBase
{
    private readonly IDbService _db;
    public AppointmentsController(IDbService db) => _db = db;
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        try
        {
            var dto = await _db.GetAppointmentByIdAsync(id);
            return Ok(dto);
        }
        catch (NotFoundException e) { return NotFound(e.Message); }
    }
    
    [HttpPost]
    public async Task<IActionResult> AddAppointment(CreateAppointmentRequestDto dto)
    {
        if (!ModelState.IsValid || dto.Services.Count == 0)
            return BadRequest("Invalid payload – at least one service is required.");

        try
        {
            await _db.AddAppointmentAsync(dto);
            return CreatedAtAction(nameof(GetAppointment), new { id = dto.AppointmentId }, dto);
        }
        catch (ConflictException e)   { return Conflict(e.Message); }
        catch (NotFoundException e)   { return NotFound(e.Message); }
        catch (ArgumentException e)   { return BadRequest(e.Message); }
    }
}
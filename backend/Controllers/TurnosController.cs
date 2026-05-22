using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TurnosMedicos.Data;
using TurnosMedicos.Helpers;
using TurnosMedicos.Models;

namespace TurnosMedicos.Controllers;

[ApiController]
[Route("[controller]")]
public class TurnosController : ControllerBase
{
    private readonly AppDbContext _context;
    private const int MaxNoShowsBeforeBlock = 3;
    private static readonly TimeSpan BlockDuration = TimeSpan.FromDays(30);

    public TurnosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var turnos = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .ToListAsync();
        return Ok(turnos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var turno = await _context.Turnos
            .Include(t => t.Paciente)
            .Include(t => t.Medico)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (turno == null) return NotFound();
        return Ok(turno);
    }

    [HttpPost]
    public async Task<IActionResult> CrearTurno([FromBody] Turno turno)
    {
        var paciente = await _context.Pacientes.FindAsync(turno.PacienteId);
        if (paciente == null)
            return NotFound(new { mensaje = "Paciente no encontrado." });

        if (DesbloquearSiVencioBloqueo(paciente))
            await _context.SaveChangesAsync();

        if (paciente.Bloqueado)
            return BadRequest(new { mensaje = "El paciente se encuentra bloqueado para agendar turnos online." });

        var medicoExiste = await _context.Medicos.AnyAsync(m => m.Id == turno.MedicoId);
        if (!medicoExiste)
            return NotFound(new { mensaje = "Médico no encontrado." });

        var turnoConflicto = await _context.Turnos.AnyAsync(t =>
            t.MedicoId == turno.MedicoId &&
            t.FechaHora == turno.FechaHora &&
            t.Estado != EstadoTurno.Cancelado);
        if (turnoConflicto)
            return BadRequest(new { mensaje = "El médico ya tiene un turno en ese horario." });

        turno.FechaCreacion = DateTime.UtcNow;
        turno.Estado = EstadoTurno.Pendiente;
        _context.Turnos.Add(turno);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = turno.Id }, turno);
    }

    [HttpPut("{id}/cancelar")]
    public async Task<IActionResult> CancelarTurno(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        var mensajeError = await AplicarTransicionConPolitica(turno, EstadoTurno.Cancelado);
        if (mensajeError != null)
            return BadRequest(new { mensaje = mensajeError });

        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    [HttpPost("{id}/ausencia")]
    public async Task<IActionResult> MarcarAusencia(int id)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        var mensajeError = await AplicarTransicionConPolitica(turno, EstadoTurno.NoShow);
        if (mensajeError != null)
            return BadRequest(new { mensaje = mensajeError });

        await _context.SaveChangesAsync();
        return Ok(turno);
    }

    private async Task<string?> AplicarTransicionConPolitica(Turno turno, EstadoTurno estadoObjetivo)
    {
        if (turno.Estado == estadoObjetivo)
            return null;

        if (estadoObjetivo == EstadoTurno.NoShow && !turno.FechaHora.IsWithinNoShowWindow())
            return "La ausencia solo puede registrarse dentro de las 24 horas del turno.";

        var penalizaDespues = DeterminarPenalizacion(turno, estadoObjetivo);
        var penalizabaAntes = turno.PenalizaNoShow;

        turno.Estado = estadoObjetivo;
        turno.PenalizaNoShow = penalizaDespues.Penaliza;
        turno.TipoPenalizacionNoShow = penalizaDespues.Tipo;

        if (!penalizabaAntes && penalizaDespues.Penaliza)
            await AjustarNoShowPaciente(turno.PacienteId, 1);

        if (penalizabaAntes && !penalizaDespues.Penaliza)
            await AjustarNoShowPaciente(turno.PacienteId, -1);

        return null;
    }

    private static (bool Penaliza, TipoPenalizacionNoShow? Tipo) DeterminarPenalizacion(Turno turno, EstadoTurno estadoObjetivo)
    {
        if (estadoObjetivo == EstadoTurno.NoShow)
            return (true, TipoPenalizacionNoShow.Ausencia);

        if (estadoObjetivo == EstadoTurno.Cancelado && !turno.FechaHora.IsCancellable())
            return (true, TipoPenalizacionNoShow.CancelacionTardia);

        return (false, null);
    }

    private async Task AjustarNoShowPaciente(int? pacienteId, int delta)
    {
        if (pacienteId == null || delta == 0)
            return;

        var paciente = await _context.Pacientes.FindAsync(pacienteId.Value);
        if (paciente == null)
            return;

        paciente.NoShowCount = Math.Max(0, paciente.NoShowCount + delta);
        RecalcularBloqueoPorNoShow(paciente);
    }

    private static void RecalcularBloqueoPorNoShow(Paciente paciente)
    {
        if (paciente.NoShowCount >= MaxNoShowsBeforeBlock)
        {
            paciente.Bloqueado = true;
            paciente.FechaBloqueo ??= DateTime.UtcNow;
            return;
        }

        paciente.Bloqueado = false;
        paciente.FechaBloqueo = null;
    }

    private static bool DesbloquearSiVencioBloqueo(Paciente paciente)
    {
        if (!paciente.Bloqueado || paciente.FechaBloqueo == null)
            return false;

        var tiempoBloqueado = DateTime.UtcNow - paciente.FechaBloqueo.Value;
        if (tiempoBloqueado < BlockDuration)
            return false;

        paciente.Bloqueado = false;
        paciente.FechaBloqueo = null;
        paciente.NoShowCount = 0;
        return true;
    }

    [HttpPut("{id}/estado")]
    public async Task<IActionResult> ActualizarEstado(int id, [FromBody] ActualizarEstadoRequest request)
    {
        var turno = await _context.Turnos.FindAsync(id);
        if (turno == null) return NotFound();

        var mensajeError = await AplicarTransicionConPolitica(turno, request.Estado);
        if (mensajeError != null)
            return BadRequest(new { mensaje = mensajeError });

        await _context.SaveChangesAsync();
        return Ok(turno);
    }
}

public class ActualizarEstadoRequest
{
    public EstadoTurno Estado { get; set; }
}

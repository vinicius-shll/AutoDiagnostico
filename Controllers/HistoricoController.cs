using System.Security.Claims;
using AutoDiagnostico.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoDiagnostico.Controllers;

[Authorize]
public class HistoricoController : Controller
{
    private readonly AutoDiagnosticoContext _context;

    public HistoricoController(AutoDiagnosticoContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var idTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(idTexto, out var usuarioId);

        var consultas = await _context.Consultas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.DataHora)
            .ToListAsync();

        return View(consultas);
    }
}
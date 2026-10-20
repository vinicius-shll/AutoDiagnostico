using System.Security.Claims;
using AutoDiagnostico.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoDiagnostico.Controllers;

// Tela "Meu histórico": lista as consultas de quem está logado. Dona: Milena.
[Authorize]
public class HistoricoController : Controller
{
    private readonly AutoDiagnosticoContext _context;

    public HistoricoController(AutoDiagnosticoContext context)
    {
        _context = context;
    }

    // Endereço: /Historico
    public async Task<IActionResult> Index()
    {
        // Lê o Id do usuário que está no "crachá" (cookie) do login
        var idTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(idTexto, out var usuarioId);

        // Busca no banco só as consultas desse usuário, da mais nova para a mais antiga
        var consultas = await _context.Consultas
            .Where(c => c.UsuarioId == usuarioId)
            .OrderByDescending(c => c.DataHora)
            .ToListAsync();

        return View(consultas);
    }
}
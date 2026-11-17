using AutoDiagnostico.Data;
using AutoDiagnostico.Models;
using AutoDiagnostico.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace AutoDiagnostico.Controllers;

[Authorize]
public class ConsultaController : Controller
{
    private readonly IDiagnosticoService _diagnostico;
    private readonly IOficinaService _oficinas;
    private readonly AutoDiagnosticoContext _context;

    public ConsultaController(
        IDiagnosticoService diagnostico,
        IOficinaService oficinas,
        AutoDiagnosticoContext context)
    {
        _diagnostico = diagnostico;
        _oficinas = oficinas;
        _context = context;
    }

    [HttpGet]
    public IActionResult Criar()
    {
        return View(new ConsultaFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ConsultaFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var resultado = new ResultadoViewModel
        {
            Sintoma = form.Sintoma,
            Endereco = form.Endereco
        };

        try
        {
            resultado.Diagnostico = await _diagnostico.DiagnosticarAsync(form.Sintoma);
        }
        catch (ServicoExternoException ex)
        {
            resultado.AvisoDiagnostico = ex.Message;
        }

        try
        {
            var oficinas = await _oficinas.BuscarProximasAsync(PrepararEndereco(form.Endereco));
            resultado.Oficinas = DestaqueOficinas.Aplicar(oficinas, resultado.Diagnostico?.Especialidade);
        }
        catch (ServicoExternoException ex)
        {
            resultado.AvisoOficinas = ex.Message;
        }

        var idTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? usuarioId = null;
        if (int.TryParse(idTexto, out var id))
        {
            usuarioId = id;
        }

        var consulta = new Consulta
        {
            UsuarioId = usuarioId,
            Sintoma = form.Sintoma,
            Localizacao = form.Endereco,
            DataHora = DateTime.Now,
            Resumo = resultado.Diagnostico?.Resumo ?? string.Empty,
            PossiveisCausas = string.Join("; ", resultado.Diagnostico?.PossiveisCausas ?? new List<string>()),
            Gravidade = resultado.Diagnostico?.Gravidade ?? string.Empty,
            Especialidade = resultado.Diagnostico?.Especialidade ?? string.Empty
        };

        _context.Consultas.Add(consulta);
        await _context.SaveChangesAsync();

        return View("Resultado", resultado);
    }

    private static string PrepararEndereco(string endereco)
    {
        var texto = endereco.Trim();
        return Regex.IsMatch(texto, @"^\d{5}-?\d{3}$") ? texto + ", Brasil" : texto;
    }
}

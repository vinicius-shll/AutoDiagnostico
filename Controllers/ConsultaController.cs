using System.Security.Claims;
using AutoDiagnostico.Data;
using AutoDiagnostico.Models;
using AutoDiagnostico.Services;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

namespace AutoDiagnostico.Controllers;

// O "maestro" da consulta: recebe o formulário, pede o diagnóstico e as oficinas,
// salva no banco e mostra o resultado. Dono: Vinicius.
// Telas (Gabriel): Views/Consulta/Criar.cshtml e Views/Consulta/Resultado.cshtml.
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

    // GET /Consulta/Criar → mostra o formulário vazio
    [HttpGet]
    public IActionResult Criar()
    {
        return View(new ConsultaFormViewModel());
    }

    // POST /Consulta/Criar → recebe o formulário preenchido
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Criar(ConsultaFormViewModel form)
    {
        // 1. Formulário com erro? Volta para a mesma tela mostrando as mensagens
        if (!ModelState.IsValid)
        {
            return View(form);
        }

        var resultado = new ResultadoViewModel
        {
            Sintoma = form.Sintoma,
            Endereco = form.Endereco
        };

        // 2. Pede o diagnóstico. Se a IA falhar, guarda a mensagem e continua
        try
        {
            resultado.Diagnostico = await _diagnostico.DiagnosticarAsync(form.Sintoma);
        }
        catch (ServicoExternoException ex)
        {
            resultado.AvisoDiagnostico = ex.Message;
        }

        // 3. Pede as oficinas. Se a busca falhar, guarda a mensagem e continua
        try
        {
            resultado.Oficinas = await _oficinas.BuscarProximasAsync(PrepararEndereco(form.Endereco));
        }
        catch (ServicoExternoException ex)
        {
            resultado.AvisoOficinas = ex.Message;
        }

        // 4. Salva a consulta no banco, ligada a quem está logado (sem login, fica sem dono)
        //    É esse UsuarioId que a tela "Meu histórico" usa para achar as consultas
        var idTexto = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? usuarioId = int.TryParse(idTexto, out var id) ? id : null;

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

        // 5. Mostra a tela de resultado (do Gabriel)
        return View("Resultado", resultado);
    }

    // Se a pessoa digitou só um CEP (ex.: 80020-310), acrescenta ", Brasil" para a Geoapify entender melhor
    private static string PrepararEndereco(string endereco)
    {
        var texto = endereco.Trim();
        return Regex.IsMatch(texto, @"^\d{5}-?\d{3}$") ? texto + ", Brasil" : texto;
    }
}

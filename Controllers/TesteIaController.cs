using AutoDiagnostico.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoDiagnostico.Controllers;

[Authorize]
public class TesteIaController : Controller
{
    private readonly IDiagnosticoService _diagnostico;

    public TesteIaController(IDiagnosticoService diagnostico)
    {
        _diagnostico = diagnostico;
    }

    public async Task<IActionResult> Index(string sintoma = "barulho metálico quando eu freio")
    {
        try
        {
            var r = await _diagnostico.DiagnosticarAsync(sintoma);

            var texto = "SINTOMA: " + sintoma + "\n\n"
                      + "RESUMO: " + r.Resumo + "\n\n"
                      + "GRAVIDADE: " + r.Gravidade + "\n"
                      + "ESPECIALIDADE: " + r.Especialidade + "\n"
                      + "CAUSAS:\n- " + string.Join("\n- ", r.PossiveisCausas);

            return Content(texto, "text/plain; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Content("DEU ERRO: " + ex.Message, "text/plain; charset=utf-8");
        }
    }
}
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// CONTRATO do diagnóstico: recebe o sintoma e devolve o diagnóstico organizado.
/// Quem cumpre: GeminiDiagnosticoService (Milena).
/// Quem usa: ConsultaController (Vinicius).
/// Em caso de falha, quem cumpre o contrato lança ServicoExternoException.
/// </summary>
public interface IDiagnosticoService
{
    Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma);
}

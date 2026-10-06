using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// CONTRATO do diagnóstico: recebe o sintoma e devolve o diagnóstico organizado.
/// Quem cumpre: GeminiDiagnosticoService (real, Milena) e DiagnosticoFakeService (dublê).
/// Quem usa: ConsultaController (Vinicius), que não sabe qual dos dois está recebendo.
/// Em caso de falha, quem cumpre o contrato lança ServicoExternoException.
/// </summary>
public interface IDiagnosticoService
{
    Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma);
}

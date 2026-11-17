using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

public interface IDiagnosticoService
{
    Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma);
}

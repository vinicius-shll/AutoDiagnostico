using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

public interface IOficinaService
{
    Task<List<Oficina>> BuscarProximasAsync(string endereco);
}

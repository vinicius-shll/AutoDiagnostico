using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// CONTRATO da busca de oficinas: recebe endereço ou CEP e devolve as oficinas próximas,
/// da mais perto para a mais longe.
/// Quem cumpre: GeoapifyOficinaService (Kauan).
/// Quem usa: ConsultaController (Vinicius).
/// Lista vazia = nenhuma oficina no raio (não é erro).
/// Em caso de falha, quem cumpre o contrato lança ServicoExternoException.
/// </summary>
public interface IOficinaService
{
    Task<List<Oficina>> BuscarProximasAsync(string endereco);
}

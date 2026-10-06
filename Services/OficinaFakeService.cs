using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// DUBLÊ da busca de oficinas: devolve oficinas inventadas no centro de Curitiba,
/// sem chamar a internet. Os nomes têm "(teste)" para ninguém confundir com dados reais.
/// Ligado quando "ServicosFalsos:Oficinas" = true (padrão do appsettings.json).
/// Com "ServicosFalsos:SimularFalhaOficinas" = true, finge que a Geoapify caiu.
/// </summary>
public class OficinaFakeService : IOficinaService
{
    private readonly bool _simularFalha;

    public OficinaFakeService(IConfiguration config)
    {
        _simularFalha = config.GetValue<bool>("ServicosFalsos:SimularFalhaOficinas");
    }

    public async Task<List<Oficina>> BuscarProximasAsync(string endereco)
    {
        await Task.Delay(300);

        if (_simularFalha)
        {
            throw new ServicoExternoException(
                "Não conseguimos buscar oficinas agora. Seu diagnóstico continua válido; tente buscar de novo em instantes.");
        }

        var oficinas = new List<Oficina>
        {
            new() { Nome = "Centro Automotivo Exemplo (teste)",  Endereco = "Rua Exemplo A, 100 - Centro, Curitiba - PR",   DistanciaMetros = 450,  Latitude = -25.4290, Longitude = -49.2710 },
            new() { Nome = "Auto Elétrica Modelo (teste)",       Endereco = "Rua Exemplo B, 250 - Centro, Curitiba - PR",   DistanciaMetros = 900,  Latitude = -25.4320, Longitude = -49.2680 },
            new() { Nome = "Freios e Suspensão Teste (teste)",   Endereco = "Avenida Exemplo C, 1200 - Rebouças, Curitiba - PR", DistanciaMetros = 1800, Latitude = -25.4390, Longitude = -49.2650 },
            new() { Nome = "Pneus e Alinhamento Demo (teste)",   Endereco = "Rua Exemplo D, 75 - Batel, Curitiba - PR",     DistanciaMetros = 2600, Latitude = -25.4420, Longitude = -49.2860 }
        };

        return oficinas.OrderBy(o => o.DistanciaMetros).ToList();
    }
}

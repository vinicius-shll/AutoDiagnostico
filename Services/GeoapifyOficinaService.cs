using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// Service REAL da busca de oficinas, usando a API da Geoapify.
/// DONO: Kauan. Os passos estão nos cards do Trello:
///   - "GeoapifyOficinaService (parte 1): transformar endereço/CEP em coordenadas (Geocoding)"
///   - "GeoapifyOficinaService (parte 2): buscar oficinas próximas (Places)..."
///
/// Para usar este service na SUA máquina (sem mexer em arquivo):
///   dotnet user-secrets set "Geoapify:ApiKey" "SUA_CHAVE"
///   dotnet user-secrets set "ServicosFalsos:Oficinas" "false"
/// </summary>
public class GeoapifyOficinaService : IOficinaService
{
    private readonly HttpClient _http;           // já com timeout de 30 s (Program.cs)
    private readonly ILogger<GeoapifyOficinaService> _logger;
    private readonly string _apiKey;             // vem do user-secrets ("Geoapify:ApiKey")
    private readonly int _raioMetros;            // vem do appsettings ("Geoapify:RaioMetros")

    public GeoapifyOficinaService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeoapifyOficinaService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Geoapify:ApiKey"] ?? string.Empty;
        _raioMetros = config.GetValue("Geoapify:RaioMetros", 5000);
    }

    public Task<List<Oficina>> BuscarProximasAsync(string endereco)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Geoapify:ApiKey não configurada. Rode: dotnet user-secrets set \"Geoapify:ApiKey\" \"SUA_CHAVE\"");
            throw new ServicoExternoException("O serviço de busca de oficinas não está configurado.");
        }

        // TODO (Kauan, parte 1) — criar o método privado:
        //   private async Task<(double Lat, double Lon)> ObterCoordenadasAsync(string endereco)
        //   URL: https://api.geoapify.com/v1/geocode/search?text={Uri.EscapeDataString(endereco)}
        //        &filter=countrycode:br&lang=pt&format=json&limit=1&apiKey={_apiKey}
        //   Sem resultado → throw new ServicoExternoException("Não encontramos esse endereço. Confira e tente de novo.");
        //
        // TODO (Kauan, parte 2) — aqui no BuscarProximasAsync (trocar para async):
        //   var (lat, lon) = await ObterCoordenadasAsync(endereco);
        //   URL: https://api.geoapify.com/v2/places?categories=service.vehicle.car_repair
        //        &filter=circle:{lon},{lat},{_raioMetros}&bias=proximity:{lon},{lat}&limit=10&apiKey={_apiKey}
        //   ATENÇÃO: lon antes de lat, e números com ponto: lon.ToString(CultureInfo.InvariantCulture)
        //   Cada features[i].properties → new Oficina { Nome = name, Endereco = formatted,
        //        DistanciaMetros = distance, Latitude = lat, Longitude = lon }
        //   Devolver ordenado por DistanciaMetros.
        _logger.LogWarning("GeoapifyOficinaService ainda não implementado (raio configurado: {Raio} m).", _raioMetros);
        _ = _http; // remover esta linha quando o _http for usado de verdade
        throw new NotImplementedException(
            "GeoapifyOficinaService ainda não foi implementado (card do Kauan). " +
            "Para continuar usando o dublê: ServicosFalsos:Oficinas = true.");
    }
}

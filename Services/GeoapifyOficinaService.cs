using System.Globalization;
using System.Text.Json;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

// Service REAL da busca de oficinas, usando a Geoapify. Dono: Kauan.
public class GeoapifyOficinaService : IOficinaService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeoapifyOficinaService> _logger;
    private readonly string _apiKey;
    private readonly int _raioMetros;

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

    public async Task<List<Oficina>> BuscarProximasAsync(string endereco)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Geoapify:ApiKey não configurada (dotnet user-secrets set \"Geoapify:ApiKey\" \"SUA_CHAVE\").");
            throw new ServicoExternoException("O serviço de busca de oficinas não está configurado.");
        }

        try
        {
            // 1. Descobre o ponto no mapa (latitude e longitude) do endereço
            var (lat, lon) = await ObterCoordenadasAsync(endereco);
            _logger.LogInformation("O endereço '{Endereco}' fica em lat {Lat}, lon {Lon}", endereco, lat, lon);

            // 2. Escreve os números com PONTO decimal (o C# em português usaria vírgula e quebraria a URL)
            var latTexto = lat.ToString(CultureInfo.InvariantCulture);
            var lonTexto = lon.ToString(CultureInfo.InvariantCulture);

            // 3. Monta a busca: oficinas mecânicas num círculo ao redor do ponto (atenção: LON vem antes de LAT)
            var url = "https://api.geoapify.com/v2/places"
                    + "?categories=service.vehicle.repair.car"
                    + "&filter=circle:" + lonTexto + "," + latTexto + "," + _raioMetros
                    + "&bias=proximity:" + lonTexto + "," + latTexto
                    + "&limit=10"
                    + "&apiKey=" + _apiKey;

            // 4. Faz a busca
            var json = await _http.GetStringAsync(url);
            using var documento = JsonDocument.Parse(json);

            // 5. Cada item da lista "features" é uma oficina
            var oficinas = new List<Oficina>();
            foreach (var item in documento.RootElement.GetProperty("features").EnumerateArray())
            {
                var p = item.GetProperty("properties");

                oficinas.Add(new Oficina
                {
                    Nome = LerTexto(p, "name") ?? "Oficina mecânica",
                    Endereco = LerTexto(p, "formatted") ?? string.Empty,
                    DistanciaMetros = p.TryGetProperty("distance", out var distancia) ? (double?)distancia.GetDouble() : null,
                    Latitude = p.GetProperty("lat").GetDouble(),
                    Longitude = p.GetProperty("lon").GetDouble()
                });
            }

            // 6. Devolve da mais perto para a mais longe
            return oficinas.OrderBy(o => o.DistanciaMetros).ToList();
        }
        catch (ServicoExternoException)
        {
            // Já é o nosso erro "combinado", com mensagem amigável: só repassa
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "A Geoapify demorou demais para responder.");
            throw new ServicoExternoException("A busca de oficinas demorou demais. Tente novamente em instantes.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha ao chamar a Geoapify.");
            throw new ServicoExternoException("Não conseguimos buscar oficinas agora. Seu diagnóstico continua válido; tente buscar de novo em instantes.", ex);
        }
        catch (Exception ex) when (ex is JsonException
                                   || ex is KeyNotFoundException
                                   || ex is InvalidOperationException)
        {
            _logger.LogError(ex, "A resposta da Geoapify veio num formato inesperado.");
            throw new ServicoExternoException("Não conseguimos buscar oficinas agora. Tente novamente.", ex);
        }
    }

    // Transforma um endereço (ou CEP) em latitude e longitude
    private async Task<(double Lat, double Lon)> ObterCoordenadasAsync(string endereco)
    {
        // 1. Monta o endereço da API de Geocoding, só com resultados do Brasil
        var url = "https://api.geoapify.com/v1/geocode/search"
                + "?text=" + Uri.EscapeDataString(endereco)
                + "&filter=countrycode:br&lang=pt&format=json&limit=1"
                + "&apiKey=" + _apiKey;

        // 2. Faz a chamada e recebe o JSON
        var json = await _http.GetStringAsync(url);

        // 3. A resposta tem uma lista "results"; pegamos o primeiro item
        using var documento = JsonDocument.Parse(json);
        var resultados = documento.RootElement.GetProperty("results");

        if (resultados.GetArrayLength() == 0)
        {
            throw new ServicoExternoException("Não encontramos esse endereço. Confira e tente de novo.");
        }

        var primeiro = resultados[0];
        return (primeiro.GetProperty("lat").GetDouble(), primeiro.GetProperty("lon").GetDouble());
    }

    // Lê um campo de texto do JSON; se ele não existir, devolve null
    private static string? LerTexto(JsonElement objeto, string campo)
    {
        if (objeto.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String)
        {
            return valor.GetString();
        }

        return null;
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

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
            var (lat, lon) = await ObterCoordenadasAsync(endereco);
            _logger.LogInformation("O endereço '{Endereco}' fica em lat {Lat}, lon {Lon}", endereco, lat, lon);

            var latTexto = lat.ToString(CultureInfo.InvariantCulture);
            var lonTexto = lon.ToString(CultureInfo.InvariantCulture);

            var url = "https://api.geoapify.com/v2/places"
                    + "?categories=service.vehicle.repair.car"
                    + "&filter=circle:" + lonTexto + "," + latTexto + "," + _raioMetros
                    + "&bias=proximity:" + lonTexto + "," + latTexto
                    + "&limit=10"
                    + "&apiKey=" + _apiKey;

            var json = await _http.GetStringAsync(url);
            using var documento = JsonDocument.Parse(json);

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

            return oficinas.OrderBy(o => o.DistanciaMetros).ToList();
        }
        catch (ServicoExternoException)
        {
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

    private async Task<(double Lat, double Lon)> ObterCoordenadasAsync(string endereco)
    {
        var coordenadas = await GeocodificarAsync(endereco);

        if (coordenadas == null)
        {
            var cep = Regex.Match(endereco, @"\b(\d{5})-?(\d{3})\b");
            if (cep.Success)
            {
                var enderecoDoCep = await BuscarEnderecoDoCepAsync(cep.Groups[1].Value + cep.Groups[2].Value);
                if (enderecoDoCep != null)
                {
                    _logger.LogInformation("CEP {Cep} não achado na Geoapify; tentando '{Endereco}' (ViaCEP)", cep.Value, enderecoDoCep);
                    coordenadas = await GeocodificarAsync(enderecoDoCep);
                }
            }
        }

        if (coordenadas == null)
        {
            throw new ServicoExternoException("Não encontramos esse endereço. Confira e tente de novo.");
        }

        return coordenadas.Value;
    }

    private async Task<(double Lat, double Lon)?> GeocodificarAsync(string texto)
    {
        var url = "https://api.geoapify.com/v1/geocode/search"
                + "?text=" + Uri.EscapeDataString(texto)
                + "&filter=countrycode:br&lang=pt&format=json&limit=1"
                + "&apiKey=" + _apiKey;

        var json = await _http.GetStringAsync(url);

        using var documento = JsonDocument.Parse(json);
        var resultados = documento.RootElement.GetProperty("results");

        if (resultados.GetArrayLength() == 0)
        {
            return null;
        }

        var primeiro = resultados[0];
        return (primeiro.GetProperty("lat").GetDouble(), primeiro.GetProperty("lon").GetDouble());
    }

    private async Task<string?> BuscarEnderecoDoCepAsync(string cep)
    {
        try
        {
            var json = await _http.GetStringAsync("https://viacep.com.br/ws/" + cep + "/json/");
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;

            if (raiz.TryGetProperty("erro", out _))
            {
                return null;
            }

            var cidade = LerTexto(raiz, "localidade");
            var uf = LerTexto(raiz, "uf");
            if (string.IsNullOrWhiteSpace(cidade))
            {
                return null;
            }

            var partes = new[] { LerTexto(raiz, "logradouro"), LerTexto(raiz, "bairro"), cidade + " - " + uf, "Brasil" };
            return string.Join(", ", partes.Where(p => !string.IsNullOrWhiteSpace(p)));
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException || ex is JsonException)
        {
            _logger.LogWarning(ex, "Falha ao consultar o ViaCEP para o CEP {Cep}.", cep);
            return null;
        }
    }

    private static string? LerTexto(JsonElement objeto, string campo)
    {
        if (objeto.TryGetProperty(campo, out var valor) && valor.ValueKind == JsonValueKind.String)
        {
            return valor.GetString();
        }

        return null;
    }
}

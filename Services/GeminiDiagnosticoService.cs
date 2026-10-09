using System.Net.Http.Json;
using System.Text.Json;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

// Service REAL do diagnóstico, usando a IA do Google (Gemini). Dona: Milena.
public class GeminiDiagnosticoService : IDiagnosticoService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiDiagnosticoService> _logger;
    private readonly string _apiKey;
    private readonly string _modelo;

    public GeminiDiagnosticoService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeminiDiagnosticoService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        _modelo = config["Gemini:Modelo"] ?? "gemini-3.7-flash";
    }

    public async Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new ServicoExternoException("O serviço de diagnóstico não está configurado.");
        }

        // 1. Endereço da API do Gemini, com o modelo escolhido no appsettings.json
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent";

        // 2. A pergunta que vamos fazer para a IA
        var prompt = "Você é um mecânico experiente. Um motorista descreveu este problema no carro: \""
                      + sintoma + "\". Explique em poucas frases o que pode ser.";

        // 3. O "pacote" que o Gemini espera receber
        var corpo = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        // 4. Monta o pedido, coloca a chave no cabeçalho e envia
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, url);
        requisicao.Headers.Add("x-goog-api-key", _apiKey);
        requisicao.Content = JsonContent.Create(corpo);

        using var resposta = await _http.SendAsync(requisicao);
        var json = await resposta.Content.ReadAsStringAsync();

        if (!resposta.IsSuccessStatusCode)
        {
            _logger.LogError("O Gemini respondeu com erro {Status}: {Corpo}", (int)resposta.StatusCode, json);
            throw new ServicoExternoException("Não conseguimos gerar o diagnóstico agora. Tente novamente.");
        }

        // 5. Tira o texto de dentro da resposta: candidates[0].content.parts[0].text
        using var documento = JsonDocument.Parse(json);
        var texto = documento.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        _logger.LogInformation("Resposta do Gemini: {Texto}", texto);

        // 6. Por enquanto, o texto inteiro vai no Resumo (a parte 2 separa os campos)
        return new DiagnosticoResultado { Resumo = texto };
    }
}
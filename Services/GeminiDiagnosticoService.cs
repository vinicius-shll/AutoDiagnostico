using System.Net.Http.Json;
using System.Text.Json;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

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

        var modeloConfig = config["Gemini:Modelo"];
        _modelo = string.IsNullOrWhiteSpace(modeloConfig) ? "gemini-2.5-flash" : modeloConfig.Trim();
    }

    public async Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return CriarResultadoFallback("O serviço de diagnóstico não está configurado corretamente.");
        }

        var modelosParaTentar = new List<string>
{
    _modelo,                // Modelo vindo do appsettings/user-secrets
    "gemini-3.8-flash",     // Modelo atual recomendado pela Google
    "gemini-3.7-flash",     // Fallback rápido
    "gemini-3.5-flash-lite" // Fallback leve
}.Distinct().ToList();

        var prompt = string.Join("\n",
            "Você é um assistente de pré-diagnóstico automotivo. Você NÃO substitui um mecânico.",
            "Um motorista descreveu este problema no carro: \"" + sintoma + "\"",
            "Responda somente com um JSON com estes 4 campos:",
            "- resumo: 2 ou 3 frases simples explicando o que pode ser;",
            "- possiveisCausas: uma lista com 2 a 4 causas prováveis, em textos curtos;",
            "- gravidade: exatamente Baixa, Média ou Alta;",
            "- especialidade: exatamente um destes valores: " + string.Join(", ", Especialidades.Todas) + ".",
            "Se o texto não for sobre um problema de carro, use gravidade Baixa, especialidade Outros e peça mais detalhes no resumo.");

        var corpo = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };

        foreach (var modeloAtual in modelosParaTentar)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modeloAtual}:generateContent";

                using var requisicao = new HttpRequestMessage(HttpMethod.Post, url);
                requisicao.Headers.Add("x-goog-api-key", _apiKey);
                requisicao.Content = JsonContent.Create(corpo);

                using var resposta = await _http.SendAsync(requisicao);
                var json = await resposta.Content.ReadAsStringAsync();

                if ((int)resposta.StatusCode == 503 || resposta.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("Modelo {Modelo} indisponível (Erro {Status}). Tentando o próximo modelo...", modeloAtual, (int)resposta.StatusCode);
                    continue;
                }

                if (!resposta.IsSuccessStatusCode)
                {
                    _logger.LogError("Erro no modelo {Modelo} ({Status}): {Corpo}", modeloAtual, (int)resposta.StatusCode, json);
                    continue;
                }

                using var documento = JsonDocument.Parse(json);
                var texto = documento.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? string.Empty;

                var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var resultado = JsonSerializer.Deserialize<DiagnosticoResultado>(texto, opcoes);

                if (resultado != null && !string.IsNullOrWhiteSpace(resultado.Resumo))
                {
                    if (!Especialidades.Todas.Contains(resultado.Especialidade))
                    {
                        resultado.Especialidade = Especialidades.Outros;
                    }

                    _logger.LogInformation("Diagnóstico gerado com sucesso usando o modelo: {Modelo}", modeloAtual);
                    return resultado;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao processar a requisição com o modelo {Modelo}.", modeloAtual);
            }
        }

        return CriarResultadoFallback("Todos os servidores da IA estão temporariamente sobrecarregados. Por favor, tente novamente em alguns instantes.");
    }

    private static DiagnosticoResultado CriarResultadoFallback(string mensagemErro)
    {
        return new DiagnosticoResultado
        {
            Resumo = mensagemErro,
            PossiveisCausas = new List<string> { "Serviço indisponível temporariamente" },
            Gravidade = "Baixa",
            Especialidade = Especialidades.Outros
        };
    }
}
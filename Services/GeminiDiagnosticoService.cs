using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// Service REAL do diagnóstico, usando a API do Gemini.
/// DONA: Milena. Os passos estão nos cards do Trello:
///   - "GeminiDiagnosticoService (parte 1): fazer a chamada básica ao Gemini"
///   - "GeminiDiagnosticoService (parte 2): pedir resposta em JSON..."
///
/// Para usar este service na SUA máquina (sem mexer em arquivo):
///   dotnet user-secrets set "Gemini:ApiKey" "SUA_CHAVE"
///   dotnet user-secrets set "ServicosFalsos:Diagnostico" "false"
/// </summary>
public class GeminiDiagnosticoService : IDiagnosticoService
{
    private readonly HttpClient _http;           // o "navegador" do C#, já com timeout de 30 s (Program.cs)
    private readonly ILogger<GeminiDiagnosticoService> _logger;
    private readonly string _apiKey;             // vem do user-secrets ("Gemini:ApiKey")
    private readonly string _modelo;             // vem do appsettings ("Gemini:Modelo")

    public GeminiDiagnosticoService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeminiDiagnosticoService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        _modelo = config["Gemini:Modelo"] ?? "gemini-flash-latest";
    }

    public Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini:ApiKey não configurada. Rode: dotnet user-secrets set \"Gemini:ApiKey\" \"SUA_CHAVE\"");
            throw new ServicoExternoException("O serviço de diagnóstico não está configurado.");
        }

        // TODO (Milena, parte 1):
        //   1. Trocar a assinatura para "public async Task<DiagnosticoResultado> DiagnosticarAsync(...)".
        //   2. URL: $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent"
        //   3. Corpo: new { contents = new[] { new { parts = new[] { new { text = prompt } } } } }
        //   4. HttpRequestMessage(HttpMethod.Post, url) + header "x-goog-api-key" = _apiKey
        //      + Content = JsonContent.Create(corpo)   (using System.Net.Http.Json;)
        //   5. var resposta = await _http.SendAsync(requisicao);
        //   6. Texto em: candidates[0].content.parts[0].text   (using System.Text.Json;)
        //
        // TODO (Milena, parte 2):
        //   - Prompt pedindo JSON com resumo, possiveisCausas, gravidade e especialidade
        //     (especialidade só pode ser um valor de Especialidades.Todas).
        //   - generationConfig = new { responseMimeType = "application/json" }
        //   - JsonSerializer.Deserialize<DiagnosticoResultado>(texto,
        //         new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        _logger.LogWarning("GeminiDiagnosticoService ainda não implementado (modelo configurado: {Modelo}).", _modelo);
        _ = _http; // remover esta linha quando o _http for usado de verdade
        throw new NotImplementedException(
            "GeminiDiagnosticoService ainda não foi implementado (card da Milena). " +
            "Para continuar usando o dublê: ServicosFalsos:Diagnostico = true.");
    }
}

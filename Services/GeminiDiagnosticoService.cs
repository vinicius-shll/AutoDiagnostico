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
        _modelo = config["Gemini:Modelo"] ?? "gemini-flash-latest";
    }

    public async Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini:ApiKey não configurada (dotnet user-secrets set \"Gemini:ApiKey\" \"SUA_CHAVE\").");
            throw new ServicoExternoException("O serviço de diagnóstico não está configurado.");
        }

        try
        {
            // 1. Endereço da API do Gemini, com o modelo escolhido no appsettings.json
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelo}:generateContent";

            // 2. A pergunta, pedindo a resposta em JSON com 4 campos fixos
            var prompt = string.Join("\n",
                "Você é um assistente de pré-diagnóstico automotivo. Você NÃO substitui um mecânico.",
                "Um motorista descreveu este problema no carro: \"" + sintoma + "\"",
                "Responda somente com um JSON com estes 4 campos:",
                "- resumo: 2 ou 3 frases simples explicando o que pode ser;",
                "- possiveisCausas: uma lista com 2 a 4 causas prováveis, em textos curtos;",
                "- gravidade: exatamente Baixa, Média ou Alta;",
                "- especialidade: exatamente um destes valores: " + string.Join(", ", Especialidades.Todas) + ".",
                "Se o texto não for sobre um problema de carro, use gravidade Baixa, especialidade Outros e peça mais detalhes no resumo.");

            // 3. O "pacote" que o Gemini espera receber + o pedido de resposta em JSON
            var corpo = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                },
                generationConfig = new { responseMimeType = "application/json" }
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

                var mensagem = (int)resposta.StatusCode == 429
                    ? "Muitas consultas ao mesmo tempo. Espere um minuto e tente de novo."
                    : "Não conseguimos gerar o diagnóstico agora. Tente novamente.";
                throw new ServicoExternoException(mensagem);
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

            // 6. O texto é um JSON: transforma em DiagnosticoResultado
            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultado = JsonSerializer.Deserialize<DiagnosticoResultado>(texto, opcoes);

            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Resumo))
            {
                throw new ServicoExternoException("A IA respondeu num formato inesperado. Tente novamente.");
            }

            // 7. Se a IA inventar uma especialidade fora da lista, usa "Outros"
            if (!Especialidades.Todas.Contains(resultado.Especialidade))
            {
                resultado.Especialidade = Especialidades.Outros;
            }

            return resultado;
        }
        catch (ServicoExternoException)
        {
            // Já é o nosso erro "combinado", com mensagem amigável: só repassa
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "O Gemini demorou demais para responder.");
            throw new ServicoExternoException("A IA demorou para responder. Tente novamente em instantes.", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de conexão com o Gemini.");
            throw new ServicoExternoException("Não conseguimos falar com a IA agora. Verifique a internet e tente novamente.", ex);
        }
        catch (Exception ex) when (ex is JsonException
                                   || ex is KeyNotFoundException
                                   || ex is InvalidOperationException
                                   || ex is IndexOutOfRangeException)
        {
            _logger.LogError(ex, "A resposta do Gemini veio num formato inesperado.");
            throw new ServicoExternoException("A IA respondeu num formato inesperado. Tente novamente.", ex);
        }
    }
}
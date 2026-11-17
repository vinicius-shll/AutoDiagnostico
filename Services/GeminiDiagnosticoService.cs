using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

public class GeminiDiagnosticoService : IDiagnosticoService
{
    private const int TentativasPorModelo = 2;

    private static readonly TimeSpan TempoTotalMaximo = TimeSpan.FromSeconds(25);

    private static readonly TimeSpan TempoMaximoPorTentativa = TimeSpan.FromSeconds(8);

    private static readonly int[] ErrosPassageiros = { 429, 500, 502, 503, 504 };

    private static readonly TimeSpan PausaDoModeloPrincipal = TimeSpan.FromMinutes(5);

    private static long _principalPausadoAte;

    private readonly HttpClient _http;
    private readonly ILogger<GeminiDiagnosticoService> _logger;
    private readonly string _apiKey;
    private readonly List<string> _modelos;
    private readonly string _nivelRaciocinio;

    public GeminiDiagnosticoService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeminiDiagnosticoService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;

        _modelos = new[]
            {
                config["Gemini:Modelo"] ?? "gemini-3.5-flash",
                config["Gemini:ModeloReserva"] ?? "gemini-3.1-flash-lite"
            }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();

        _nivelRaciocinio = config["Gemini:NivelRaciocinio"] ?? string.Empty;
    }

    public async Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("Gemini:ApiKey não configurada (dotnet user-secrets set \"Gemini:ApiKey\" \"SUA_CHAVE\").");
            throw new ServicoExternoException("O serviço de diagnóstico não está configurado.");
        }

        var corpo = MontarCorpo(sintoma);
        var relogio = Stopwatch.StartNew();
        var mensagemParaUsuario = "Não conseguimos gerar o diagnóstico agora. Tente novamente em instantes.";

        foreach (var modelo in _modelos)
        {
            var ehUltimoModelo = modelo == _modelos[^1];
            var ehPrincipalComReserva = modelo == _modelos[0] && !ehUltimoModelo;

            if (ehPrincipalComReserva && DateTime.UtcNow.Ticks < Interlocked.Read(ref _principalPausadoAte))
            {
                _logger.LogInformation("Gemini: modelo {Modelo} pausado após falha recente; usando o de reserva.", modelo);
                continue;
            }

            for (var tentativa = 1; tentativa <= TentativasPorModelo; tentativa++)
            {
                var tempoRestante = TempoTotalMaximo - relogio.Elapsed;
                if (tempoRestante < TimeSpan.FromSeconds(2))
                {
                    _logger.LogWarning("Gemini: tempo total de {Segundos} s esgotado, desistindo.", TempoTotalMaximo.TotalSeconds);
                    throw new ServicoExternoException(mensagemParaUsuario);
                }

                var tempoDestaTentativa = ehUltimoModelo || tempoRestante < TempoMaximoPorTentativa
                    ? tempoRestante
                    : TempoMaximoPorTentativa;

                try
                {
                    var resultado = await ChamarModeloAsync(modelo, corpo, tempoDestaTentativa);
                    if (modelo != _modelos[0])
                    {
                        _logger.LogWarning("Gemini: diagnóstico gerado pelo modelo de reserva {Modelo}.", modelo);
                    }
                    return resultado;
                }
                catch (TentativaFalhou falha)
                {
                    mensagemParaUsuario = falha.MensagemParaUsuario;
                    _logger.LogWarning("Gemini: modelo {Modelo}, tentativa {Tentativa} falhou: {Motivo}",
                        modelo, tentativa, falha.Message);

                    if (!falha.ValeRepetir)
                    {
                        break;
                    }

                    if (falha.MelhorTrocarDeModelo && !ehUltimoModelo)
                    {
                        break;
                    }

                    if (tentativa < TentativasPorModelo)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(tentativa));
                    }
                }
            }

            if (ehPrincipalComReserva)
            {
                Interlocked.Exchange(ref _principalPausadoAte, DateTime.UtcNow.Add(PausaDoModeloPrincipal).Ticks);
                _logger.LogWarning("Gemini: modelo {Modelo} pausado por {Minutos} min.", modelo, PausaDoModeloPrincipal.TotalMinutes);
            }
        }

        throw new ServicoExternoException(mensagemParaUsuario);
    }

    private object MontarCorpo(string sintoma)
    {
        var prompt = string.Join("\n",
            "Você é um assistente de pré-diagnóstico automotivo. Você NÃO substitui um mecânico.",
            "Um motorista descreveu este problema no carro: \"" + sintoma + "\"",
            "Responda somente com um JSON com estes 4 campos:",
            "- resumo: 2 ou 3 frases simples explicando o que pode ser;",
            "- possiveisCausas: uma lista com 2 a 4 causas prováveis, em textos curtos;",
            "- gravidade: exatamente Baixa, Média ou Alta;",
            "- especialidade: exatamente um destes valores: " + string.Join(", ", Especialidades.Todas) + ".",
            "Se o texto não for sobre um problema de carro, use gravidade Baixa, especialidade Outros e peça mais detalhes no resumo.");

        var configuracao = new Dictionary<string, object>
        {
            ["responseMimeType"] = "application/json",
            ["responseSchema"] = new
            {
                type = "OBJECT",
                properties = new
                {
                    resumo = new { type = "STRING" },
                    possiveisCausas = new { type = "ARRAY", items = new { type = "STRING" }, minItems = 2, maxItems = 4 },
                    gravidade = new { type = "STRING", @enum = new[] { "Baixa", "Média", "Alta" } },
                    especialidade = new { type = "STRING", @enum = Especialidades.Todas }
                },
                required = new[] { "resumo", "possiveisCausas", "gravidade", "especialidade" }
            }
        };
        if (!string.IsNullOrWhiteSpace(_nivelRaciocinio))
        {
            configuracao["thinkingConfig"] = new { thinkingLevel = _nivelRaciocinio };
        }

        return new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = configuracao
        };
    }

    private async Task<DiagnosticoResultado> ChamarModeloAsync(string modelo, object corpo, TimeSpan tempoMaximo)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent";

        using var limite = new CancellationTokenSource(tempoMaximo);
        using var requisicao = new HttpRequestMessage(HttpMethod.Post, url);
        requisicao.Headers.Add("x-goog-api-key", _apiKey);
        requisicao.Content = JsonContent.Create(corpo);

        string json;
        int status;
        try
        {
            using var resposta = await _http.SendAsync(requisicao, limite.Token);
            json = await resposta.Content.ReadAsStringAsync(limite.Token);
            status = (int)resposta.StatusCode;
        }
        catch (OperationCanceledException)
        {
            throw new TentativaFalhou("demorou demais", valeRepetir: true,
                "A IA demorou para responder. Tente novamente em instantes.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Falha de conexão com o Gemini.");
            throw new ServicoExternoException(
                "Não conseguimos falar com a IA agora. Verifique a internet e tente novamente.", ex);
        }

        if (status != 200)
        {
            _logger.LogError("O Gemini ({Modelo}) respondeu com erro {Status}: {Corpo}", modelo, status, json);

            if (ErrosPassageiros.Contains(status))
            {
                throw new TentativaFalhou($"erro {status}", valeRepetir: true,
                    status == 429
                        ? "Muitas consultas ao mesmo tempo. Espere um minuto e tente de novo."
                        : "A IA está sobrecarregada agora. Tente novamente em instantes.",
                    melhorTrocarDeModelo: status == 503);
            }

            if (status == 404)
            {
                throw new TentativaFalhou("modelo não encontrado", valeRepetir: false,
                    "Não conseguimos gerar o diagnóstico agora. Tente novamente.");
            }

            throw new ServicoExternoException("Não conseguimos gerar o diagnóstico agora. Tente novamente.");
        }

        try
        {
            using var documento = JsonDocument.Parse(json);
            var texto = documento.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            _logger.LogInformation("Resposta do Gemini ({Modelo}): {Texto}", modelo, texto);

            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultado = JsonSerializer.Deserialize<DiagnosticoResultado>(texto, opcoes);

            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Resumo))
            {
                throw new JsonException("resposta sem resumo");
            }

            if (!Especialidades.Todas.Contains(resultado.Especialidade))
            {
                resultado.Especialidade = Especialidades.Outros;
            }

            return resultado;
        }
        catch (Exception ex) when (ex is JsonException
                                   || ex is KeyNotFoundException
                                   || ex is InvalidOperationException
                                   || ex is IndexOutOfRangeException)
        {
            throw new TentativaFalhou("formato inesperado: " + ex.Message, valeRepetir: true,
                "A IA respondeu num formato inesperado. Tente novamente.");
        }
    }

    private sealed class TentativaFalhou : Exception
    {
        public bool ValeRepetir { get; }
        public bool MelhorTrocarDeModelo { get; }
        public string MensagemParaUsuario { get; }

        public TentativaFalhou(string motivo, bool valeRepetir, string mensagemParaUsuario, bool melhorTrocarDeModelo = false) : base(motivo)
        {
            ValeRepetir = valeRepetir;
            MelhorTrocarDeModelo = melhorTrocarDeModelo;
            MensagemParaUsuario = mensagemParaUsuario;
        }
    }
}

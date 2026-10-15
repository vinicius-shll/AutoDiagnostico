using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

// Service REAL do diagnóstico, usando a IA do Google (Gemini). Dona: Milena.
// Resiliência (Vinicius, 15/10): quando o Gemini está sobrecarregado (erro 503 "high demand")
// ou limita as chamadas (429), tenta de novo e, se continuar falhando, usa o modelo de reserva.
public class GeminiDiagnosticoService : IDiagnosticoService
{
    // Quantas vezes tentar cada modelo antes de passar para o próximo
    private const int TentativasPorModelo = 2;

    // Tempo máximo somando TODAS as tentativas. Depois disso, desiste e mostra o aviso na tela.
    private static readonly TimeSpan TempoTotalMaximo = TimeSpan.FromSeconds(25);

    // Tempo máximo de UMA tentativa no modelo principal. Sobrecarregado, o Gemini às vezes "trava"
    // em vez de responder 503; este limite garante que sempre sobra tempo para o modelo de reserva.
    // (Uma resposta normal leva 1 a 3 s; medido em 15/10.)
    private static readonly TimeSpan TempoMaximoPorTentativa = TimeSpan.FromSeconds(8);

    // Erros do Gemini que costumam passar sozinhos: vale tentar de novo
    private static readonly int[] ErrosPassageiros = { 429, 500, 502, 503, 504 };

    private readonly HttpClient _http;
    private readonly ILogger<GeminiDiagnosticoService> _logger;
    private readonly string _apiKey;
    private readonly List<string> _modelos;

    public GeminiDiagnosticoService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeminiDiagnosticoService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;

        // Primeiro o modelo principal; se ele falhar, o de reserva (os dois vêm do appsettings.json)
        _modelos = new[]
            {
                config["Gemini:Modelo"] ?? "gemini-flash-latest",
                config["Gemini:ModeloReserva"] ?? "gemini-3.5-flash-lite"
            }
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .ToList();
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

            for (var tentativa = 1; tentativa <= TentativasPorModelo; tentativa++)
            {
                var tempoRestante = TempoTotalMaximo - relogio.Elapsed;
                if (tempoRestante < TimeSpan.FromSeconds(2))
                {
                    _logger.LogWarning("Gemini: tempo total de {Segundos} s esgotado, desistindo.", TempoTotalMaximo.TotalSeconds);
                    throw new ServicoExternoException(mensagemParaUsuario);
                }

                // O último modelo pode usar todo o tempo que sobrou; os outros têm teto por tentativa
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
                        break; // este modelo não vai funcionar: passa para o próximo
                    }

                    if (tentativa < TentativasPorModelo)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(tentativa)); // espera 1 s antes de tentar de novo
                    }
                }
            }
        }

        throw new ServicoExternoException(mensagemParaUsuario);
    }

    // Monta o "pacote" que o Gemini espera receber, pedindo a resposta em JSON com 4 campos fixos
    private static object MontarCorpo(string sintoma)
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

        return new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new { responseMimeType = "application/json" }
        };
    }

    // Uma chamada a um modelo. Devolve o diagnóstico ou lança TentativaFalhou dizendo se vale repetir.
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
            // Sem internet ou endereço inacessível: repetir não adianta
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
                        : "A IA está sobrecarregada agora. Tente novamente em instantes.");
            }

            if (status == 404)
            {
                // Modelo com nome errado ou desativado: tenta o próximo modelo
                throw new TentativaFalhou("modelo não encontrado", valeRepetir: false,
                    "Não conseguimos gerar o diagnóstico agora. Tente novamente.");
            }

            // 400, 401, 403...: chave inválida ou pedido errado. Repetir não adianta.
            throw new ServicoExternoException("Não conseguimos gerar o diagnóstico agora. Tente novamente.");
        }

        try
        {
            // Tira o texto de dentro da resposta: candidates[0].content.parts[0].text
            using var documento = JsonDocument.Parse(json);
            var texto = documento.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            _logger.LogInformation("Resposta do Gemini ({Modelo}): {Texto}", modelo, texto);

            // O texto é um JSON: transforma em DiagnosticoResultado
            var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var resultado = JsonSerializer.Deserialize<DiagnosticoResultado>(texto, opcoes);

            if (resultado == null || string.IsNullOrWhiteSpace(resultado.Resumo))
            {
                throw new JsonException("resposta sem resumo");
            }

            // Se a IA inventar uma especialidade fora da lista, usa "Outros"
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
            // A IA às vezes responde fora do formato; uma nova tentativa costuma resolver
            throw new TentativaFalhou("formato inesperado: " + ex.Message, valeRepetir: true,
                "A IA respondeu num formato inesperado. Tente novamente.");
        }
    }

    // Erro interno deste service: uma tentativa falhou. Nunca sai daqui; vira ServicoExternoException no fim.
    private sealed class TentativaFalhou : Exception
    {
        public bool ValeRepetir { get; }
        public string MensagemParaUsuario { get; }

        public TentativaFalhou(string motivo, bool valeRepetir, string mensagemParaUsuario) : base(motivo)
        {
            ValeRepetir = valeRepetir;
            MensagemParaUsuario = mensagemParaUsuario;
        }
    }
}

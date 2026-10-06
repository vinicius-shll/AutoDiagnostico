using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

/// <summary>
/// DUBLÊ do diagnóstico: devolve um diagnóstico inventado, sem chamar a internet.
/// Serve para:
///   - todo mundo trabalhar sem chave de API;
///   - plano B da defesa, se a internet cair.
/// Ligado quando "ServicosFalsos:Diagnostico" = true (padrão do appsettings.json).
/// Com "ServicosFalsos:SimularFalhaDiagnostico" = true, finge que a IA caiu.
/// </summary>
public class DiagnosticoFakeService : IDiagnosticoService
{
    private readonly bool _simularFalha;

    public DiagnosticoFakeService(IConfiguration config)
    {
        _simularFalha = config.GetValue<bool>("ServicosFalsos:SimularFalhaDiagnostico");
    }

    public async Task<DiagnosticoResultado> DiagnosticarAsync(string sintoma)
    {
        // Pequena espera para parecer uma chamada de verdade
        await Task.Delay(300);

        if (_simularFalha)
        {
            throw new ServicoExternoException(
                "Não conseguimos gerar o diagnóstico agora. Tente novamente em instantes.");
        }

        var texto = (sintoma ?? string.Empty).ToLowerInvariant();

        if (texto.Contains("bateria") || texto.Contains("luz") || texto.Contains("painel") || texto.Contains("partida"))
        {
            return new DiagnosticoResultado
            {
                Resumo = "[DADOS DE TESTE] O sintoma indica um problema no sistema elétrico, " +
                         "provavelmente na carga da bateria.",
                PossiveisCausas = new List<string>
                {
                    "Bateria no fim da vida útil",
                    "Alternador sem carregar corretamente",
                    "Algum componente consumindo energia com o carro desligado"
                },
                Gravidade = "Média",
                Especialidade = "Elétrica"
            };
        }

        if (texto.Contains("pneu") || texto.Contains("puxa") || texto.Contains("alinhamento"))
        {
            return new DiagnosticoResultado
            {
                Resumo = "[DADOS DE TESTE] O carro pode estar desalinhado ou com pneus gastos de forma irregular.",
                PossiveisCausas = new List<string>
                {
                    "Alinhamento fora do padrão",
                    "Calibragem diferente entre os pneus",
                    "Desgaste irregular dos pneus"
                },
                Gravidade = "Baixa",
                Especialidade = "Pneus e alinhamento"
            };
        }

        // Resposta padrão: freios
        return new DiagnosticoResultado
        {
            Resumo = "[DADOS DE TESTE] O ruído ao frear sugere desgaste no sistema de freios. " +
                     "Evite viagens longas até uma avaliação.",
            PossiveisCausas = new List<string>
            {
                "Pastilhas de freio gastas",
                "Disco de freio riscado ou empenado",
                "Falta de lubrificação nas pinças"
            },
            Gravidade = "Alta",
            Especialidade = "Freios"
        };
    }
}

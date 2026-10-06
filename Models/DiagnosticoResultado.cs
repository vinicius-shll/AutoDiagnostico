namespace AutoDiagnostico.Models;

/// <summary>
/// O diagnóstico devolvido pela IA, já organizado em campos.
/// Quem preenche: GeminiDiagnosticoService (Milena) ou o dublê DiagnosticoFakeService.
/// Quem usa: ConsultaController (Vinicius) e a tela de resultado (Gabriel).
/// </summary>
public class DiagnosticoResultado
{
    /// <summary>Explicação curta, em linguagem simples, do que pode estar acontecendo.</summary>
    public string Resumo { get; set; } = string.Empty;

    /// <summary>De 2 a 4 causas prováveis, em textos curtos.</summary>
    public List<string> PossiveisCausas { get; set; } = new();

    /// <summary>"Baixa", "Média" ou "Alta".</summary>
    public string Gravidade { get; set; } = string.Empty;

    /// <summary>Um dos valores de <see cref="Especialidades.Todas"/>.</summary>
    public string Especialidade { get; set; } = Especialidades.Outros;
}

namespace AutoDiagnostico.Models;

/// <summary>
/// Tudo o que a tela de resultado precisa para ser montada.
/// Quem monta: ConsultaController (Vinicius).
/// Tela: Views/Consulta/Resultado.cshtml (Gabriel).
/// </summary>
public class ResultadoViewModel
{
    public string Sintoma { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    /// <summary>Fica null quando a IA falhou. Nesse caso, AvisoDiagnostico vem preenchido.</summary>
    public DiagnosticoResultado? Diagnostico { get; set; }

    /// <summary>Lista vazia quando não há oficinas no raio ou quando a busca falhou.</summary>
    public List<Oficina> Oficinas { get; set; } = new();

    /// <summary>Mensagem amigável quando a IA falhou (vem da ServicoExternoException).</summary>
    public string? AvisoDiagnostico { get; set; }

    /// <summary>Mensagem amigável quando a busca de oficinas falhou.</summary>
    public string? AvisoOficinas { get; set; }
}

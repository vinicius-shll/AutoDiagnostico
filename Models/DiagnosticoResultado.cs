namespace AutoDiagnostico.Models;

public class DiagnosticoResultado
{
    public string Resumo { get; set; } = string.Empty;

    public List<string> PossiveisCausas { get; set; } = new();

    public string Gravidade { get; set; } = string.Empty;

    public string Especialidade { get; set; } = Especialidades.Outros;
}

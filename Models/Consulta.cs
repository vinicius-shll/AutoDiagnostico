namespace AutoDiagnostico.Models;

public class Consulta
{
    public int Id { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Sintoma { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Now;

    public string Resumo { get; set; } = string.Empty;
    public string PossiveisCausas { get; set; } = string.Empty;
    public string Gravidade { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
}

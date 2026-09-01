namespace AutoDiagnostico.Models;

public class Consulta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Sintoma { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Now;
}
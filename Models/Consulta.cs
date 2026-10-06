namespace AutoDiagnostico.Models;

public class Consulta
{
    public int Id { get; set; }

    // Opcional (int?) só até o login existir. Depois do card do Luan,
    // toda consulta nova sai com o Id do usuário logado.
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public string Sintoma { get; set; } = string.Empty;
    public string Localizacao { get; set; } = string.Empty;
    public DateTime DataHora { get; set; } = DateTime.Now;

    // Diagnóstico devolvido pela IA (ficam vazios se a IA falhar)
    public string Resumo { get; set; } = string.Empty;
    public string PossiveisCausas { get; set; } = string.Empty; // separadas por "; "
    public string Gravidade { get; set; } = string.Empty;
    public string Especialidade { get; set; } = string.Empty;
}

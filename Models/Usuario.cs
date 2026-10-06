namespace AutoDiagnostico.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // Senha "embaralhada" pelo PasswordHasher (card do Luan). Nunca a senha pura.
    public string SenhaHash { get; set; } = string.Empty;
}

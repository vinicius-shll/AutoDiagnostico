using System.ComponentModel.DataAnnotations;

namespace AutoDiagnostico.Models;

public class CadastroViewModel
{
    [Required(ErrorMessage = "Informe o seu nome.")]
    [StringLength(100, ErrorMessage = "O nome pode ter no máximo {1} caracteres. ")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o seu e-mail.")]
    [EmailAddress(ErrorMessage = "Digite um e-mail válido. ")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Crie uma senha.")]
    [MinLength(6, ErrorMessage = "A senha precisa ter pelo menos 6 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Required(ErrorMessage = "Repita a senha.")]
    [Compare(nameof(Senha), ErrorMessage = "As senhas não são iguais.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar senha")]
    public string ConfirmarSenha { get; set; } = string.Empty;
}
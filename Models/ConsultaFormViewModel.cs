using System.ComponentModel.DataAnnotations;

namespace AutoDiagnostico.Models;

/// <summary>
/// O que o formulário de relato envia para o ConsultaController.
/// Tela: Views/Consulta/Criar.cshtml (Gabriel).
/// As regras abaixo valem no navegador (antes de enviar) e no servidor (ModelState.IsValid).
/// </summary>
public class ConsultaFormViewModel
{
    /// <summary>O que o carro está fazendo, nas palavras do motorista.</summary>
    [Required(ErrorMessage = "Descreva o problema do carro.")]
    [StringLength(1000, MinimumLength = 15, ErrorMessage = "Descreva com pelo menos {2} e no máximo {1} caracteres.")]
    [Display(Name = "O que o carro está fazendo?")]
    public string Sintoma { get; set; } = string.Empty;

    /// <summary>Endereço ou CEP de onde o motorista está.</summary>
    [Required(ErrorMessage = "Informe um endereço ou CEP.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Informe um endereço ou CEP válido.")]
    [Display(Name = "Endereço ou CEP")]
    public string Endereco { get; set; } = string.Empty;
}

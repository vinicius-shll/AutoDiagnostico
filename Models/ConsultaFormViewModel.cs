using System.ComponentModel.DataAnnotations;

namespace AutoDiagnostico.Models;

public class ConsultaFormViewModel
{
    [Required(ErrorMessage = "Descreva o problema do carro.")]
    [StringLength(1000, MinimumLength = 15, ErrorMessage = "Descreva com pelo menos {2} e no máximo {1} caracteres.")]
    [Display(Name = "O que o carro está fazendo?")]
    public string Sintoma { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe um endereço ou CEP.")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Informe um endereço ou CEP válido.")]
    [Display(Name = "Endereço ou CEP")]
    public string Endereco { get; set; } = string.Empty;
}

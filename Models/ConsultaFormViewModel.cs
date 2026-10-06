namespace AutoDiagnostico.Models;

/// <summary>
/// O que o formulário de relato envia para o ConsultaController.
/// Tela: Views/Consulta/Criar.cshtml (Gabriel).
/// As regras de validação (tamanho mínimo, mensagens em português)
/// entram no card "Validações do formulário" (Vinicius).
/// </summary>
public class ConsultaFormViewModel
{
    /// <summary>O que o carro está fazendo, nas palavras do motorista.</summary>
    public string Sintoma { get; set; } = string.Empty;

    /// <summary>Endereço ou CEP de onde o motorista está.</summary>
    public string Endereco { get; set; } = string.Empty;
}

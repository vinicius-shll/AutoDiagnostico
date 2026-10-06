namespace AutoDiagnostico.Services;

/// <summary>
/// O erro "combinado" entre os services e o Controller.
/// Os services (Milena e Kauan) LANÇAM quando a API externa falha, com uma mensagem
/// em português que pode ser mostrada ao motorista.
/// O ConsultaController (Vinicius) CAPTURA e põe a mensagem no ResultadoViewModel.
/// A tela (Gabriel) MOSTRA.
/// Detalhes técnicos vão no parâmetro "causa" e no log, nunca na mensagem.
/// </summary>
public class ServicoExternoException : Exception
{
    public ServicoExternoException(string mensagemParaUsuario)
        : base(mensagemParaUsuario)
    {
    }

    public ServicoExternoException(string mensagemParaUsuario, Exception causa)
        : base(mensagemParaUsuario, causa)
    {
    }
}

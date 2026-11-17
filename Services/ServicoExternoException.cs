namespace AutoDiagnostico.Services;

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

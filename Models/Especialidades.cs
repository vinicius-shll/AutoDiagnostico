namespace AutoDiagnostico.Models;

/// <summary>
/// Lista FIXA de especialidades que a IA pode indicar.
/// A Milena usa no prompt (a IA só pode responder um destes valores)
/// e o Kauan usa para destacar as oficinas certas.
/// Se mudar algum nome aqui, avise os dois.
/// </summary>
public static class Especialidades
{
    public const string Outros = "Outros";

    public static readonly string[] Todas =
    {
        "Freios",
        "Suspensão e direção",
        "Motor",
        "Elétrica",
        "Arrefecimento",
        "Transmissão e embreagem",
        "Pneus e alinhamento",
        "Funilaria e pintura",
        "Ar-condicionado",
        Outros
    };
}

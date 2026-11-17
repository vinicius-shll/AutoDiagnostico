namespace AutoDiagnostico.Models;

public class ResultadoViewModel
{
    public string Sintoma { get; set; } = string.Empty;
    public string Endereco { get; set; } = string.Empty;

    public DiagnosticoResultado? Diagnostico { get; set; }

    public List<Oficina> Oficinas { get; set; } = new();

    public string? AvisoDiagnostico { get; set; }

    public string? AvisoOficinas { get; set; }
}

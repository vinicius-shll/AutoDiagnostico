namespace AutoDiagnostico.Models;

public class Oficina
{
    public string Nome { get; set; } = string.Empty;

    public string Endereco { get; set; } = string.Empty;

    public double? DistanciaMetros { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public bool Destaque { get; set; }
}

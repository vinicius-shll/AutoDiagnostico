namespace AutoDiagnostico.Models;

/// <summary>
/// Uma oficina encontrada perto do motorista.
/// Quem preenche: GeoapifyOficinaService (Kauan) ou o dublê OficinaFakeService.
/// Quem usa: a tela de resultado (Gabriel).
/// Não mude os nomes destes campos sem avisar o Gabriel e o Vinicius.
/// </summary>
public class Oficina
{
    public string Nome { get; set; } = string.Empty;

    /// <summary>Endereço pronto para exibir (campo "formatted" da Geoapify).</summary>
    public string Endereco { get; set; } = string.Empty;

    /// <summary>Distância em metros até o endereço informado. Pode vir vazia.</summary>
    public double? DistanciaMetros { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    /// <summary>
    /// true quando o nome da oficina combina com a especialidade indicada pela IA.
    /// Quem marca: DestaqueOficinas (card do Kauan).
    /// </summary>
    public bool Destaque { get; set; }
}

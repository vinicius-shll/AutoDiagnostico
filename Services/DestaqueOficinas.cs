using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

public static class DestaqueOficinas
{
    private static readonly Dictionary<string, string[]> PalavrasChave = new()
    {
        ["Freios"] = new[] { "freio" },
        ["Suspensão e direção"] = new[] { "suspens", "amortecedor", "direção", "direcao" },
        ["Motor"] = new[] { "motor", "retífica", "retifica" },
        ["Elétrica"] = new[] { "elétric", "eletric", "bateria" },
        ["Arrefecimento"] = new[] { "radiador", "arrefecimento" },
        ["Transmissão e embreagem"] = new[] { "câmbio", "cambio", "embreagem", "transmiss" },
        ["Pneus e alinhamento"] = new[] { "pneu", "borrachar", "alinhamento", "balanceamento" },
        ["Funilaria e pintura"] = new[] { "funilar", "pintura", "lataria", "martelinho" },
        ["Ar-condicionado"] = new[] { "ar condicionado", "ar-condicionado", "climatiza" }
    };

    public static List<Oficina> Aplicar(List<Oficina> oficinas, string? especialidade)
    {
        if (especialidade == null || !PalavrasChave.TryGetValue(especialidade, out var palavras))
        {
            return oficinas;
        }

        foreach (var oficina in oficinas)
        {
            var nome = oficina.Nome.ToLowerInvariant();
            oficina.Destaque = palavras.Any(palavra => nome.Contains(palavra));
        }

        return oficinas
            .OrderByDescending(o => o.Destaque)
            .ThenBy(o => o.DistanciaMetros)
            .ToList();
    }
}

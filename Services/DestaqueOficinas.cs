using AutoDiagnostico.Models;

namespace AutoDiagnostico.Services;

// Marca como "destaque" as oficinas cujo NOME combina com a especialidade indicada pela IA. Dono: Kauan.
// Motivo: a Geoapify só tem uma categoria para oficina de carro (service.vehicle.repair.car),
// então não dá para filtrar por especialidade na própria API.
public static class DestaqueOficinas
{
    // Para cada especialidade (nomes iguais aos de Models/Especialidades.cs),
    // as palavras que, se aparecerem no nome da oficina, indicam que ela é especialista
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
        // Sem especialidade conhecida (ou "Outros"): devolve a lista como veio
        if (especialidade == null || !PalavrasChave.TryGetValue(especialidade, out var palavras))
        {
            return oficinas;
        }

        foreach (var oficina in oficinas)
        {
            var nome = oficina.Nome.ToLowerInvariant();
            oficina.Destaque = palavras.Any(palavra => nome.Contains(palavra));
        }

        // Primeiro as que estão em destaque; depois, da mais perto para a mais longe
        return oficinas
            .OrderByDescending(o => o.Destaque)
            .ThenBy(o => o.DistanciaMetros)
            .ToList();
    }
}

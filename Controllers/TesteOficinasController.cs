using AutoDiagnostico.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoDiagnostico.Controllers;

// Página de TESTE do Kauan: chama a busca de oficinas sem precisar das telas.
// Abra no navegador: http://localhost:5034/TesteOficinas?endereco=Rua XV de Novembro, Curitiba&especialidade=Elétrica
public class TesteOficinasController : Controller
{
    private readonly IOficinaService _oficinas;

    public TesteOficinasController(IOficinaService oficinas)
    {
        _oficinas = oficinas;
    }

    public async Task<IActionResult> Index(string endereco = "Rua XV de Novembro, Curitiba", string? especialidade = null)
    {
        try
        {
            var lista = await _oficinas.BuscarProximasAsync(endereco);
            lista = DestaqueOficinas.Aplicar(lista, especialidade);

            var texto = "ENDEREÇO: " + endereco + "\n"
                      + "ESPECIALIDADE: " + (especialidade ?? "(nenhuma)") + "\n"
                      + "OFICINAS ENCONTRADAS: " + lista.Count + "\n\n";

            foreach (var o in lista)
            {
                var marca = o.Destaque ? "[DESTAQUE] " : "";
                texto += "- " + marca + o.Nome + " | " + o.Endereco + " | " + o.DistanciaMetros + " m\n";
            }

            return Content(texto, "text/plain; charset=utf-8");
        }
        catch (Exception ex)
        {
            return Content("DEU ERRO: " + ex.Message, "text/plain; charset=utf-8");
        }
    }
}

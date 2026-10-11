using AutoDiagnostico.Data;
using AutoDiagnostico.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoDiagnostico.Controllers;

public class ContaController: Controller
{
    private readonly AutoDiagnosticoContext _context;
    private readonly PasswordHasher<Usuario> _hasher = new();

    public ContaController(AutoDiagnosticoContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Cadastrar()
    {
        return View(new CadastroViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cadastrar(CadastroViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var email = vm.Email.Trim().ToLower();
        if(await _context.Usuarios.AnyAsync(u => u.Email == email))
        {
            ModelState.AddModelError(nameof(vm.Email), "Este e-mail já está cadastrado. ");
            return View(vm);
        }

        var usuario = new Usuario
        {
            Nome = vm.Nome.Trim(), Email = email
        };
        usuario.SenhaHash = _hasher.HashPassword(usuario, vm.Senha);

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        TempData["Mensagem"] = "Conta criada com sucesso!";
        return RedirectToAction("Index", "Home");
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        TempData["Mensagem"] = "Conta criada com sucesso! Agora é só entrar.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var email = vm.Email.Trim().ToLower();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);

        var senhaConfere = usuario != null && !string.IsNullOrEmpty(usuario.SenhaHash) && _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, vm.Senha) != PasswordVerificationResult.Failed;
    
        if (usuario == null || !senhaConfere)
        {
            ModelState.AddModelError(string.Empty, "E-mail ou senha inválidos.");
            return View(vm);
        }

        // cra chá
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email)
        };
        var identidade = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // IsPersistent: o login continua mesmo se o navegador for fechado (até o ExpireTimeSpan do Program.cs)
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidade),
            new AuthenticationProperties { IsPersistent = true });

        if(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Criar", "Consulta");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
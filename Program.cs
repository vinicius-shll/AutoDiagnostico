using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AutoDiagnostico.Data;
using AutoDiagnostico.Services;

// ATENÇÃO: este arquivo é só do Vinicius. Precisa mudar algo aqui? Peça a ele.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Banco de dados (SQL Server LocalDB via Entity Framework Core)
builder.Services.AddDbContext<AutoDiagnosticoContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AutoDiagnosticoContext")));

// Login por cookie (cards do Luan). Quem não está logado e tenta abrir
// uma página com [Authorize] é mandado para /Conta/Login.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Sair";
        options.AccessDeniedPath = "/Conta/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// DUBLÊ ou SERVICE REAL — escolhido pela seção "ServicosFalsos" do appsettings.json.
// Para mudar só na sua máquina: dotnet user-secrets set "ServicosFalsos:Diagnostico" "false"
var tempoMaximoChamada = TimeSpan.FromSeconds(30);

if (builder.Configuration.GetValue("ServicosFalsos:Diagnostico", true))
{
    builder.Services.AddScoped<IDiagnosticoService, DiagnosticoFakeService>();
}
else
{
    builder.Services.AddHttpClient<IDiagnosticoService, GeminiDiagnosticoService>(
        client => client.Timeout = tempoMaximoChamada);
}

if (builder.Configuration.GetValue("ServicosFalsos:Oficinas", true))
{
    builder.Services.AddScoped<IOficinaService, OficinaFakeService>();
}
else
{
    builder.Services.AddHttpClient<IOficinaService, GeoapifyOficinaService>(
        client => client.Timeout = tempoMaximoChamada);
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); // precisa vir ANTES do UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

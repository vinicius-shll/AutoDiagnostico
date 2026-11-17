using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AutoDiagnostico.Data;
using AutoDiagnostico.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionStringConfigurada = builder.Configuration.GetConnectionString("AutoDiagnosticoContext");
var hospedado = !builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(connectionStringConfigurada);
var connectionString = hospedado
    ? connectionStringConfigurada!
    : EscolherConnectionString(builder.Configuration);
builder.Services.AddDbContext<AutoDiagnosticoContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        if (hospedado)
        {
            sql.EnableRetryOnFailure();
        }
    }));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";
        options.LogoutPath = "/Conta/Sair";
        options.AccessDeniedPath = "/Conta/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

var tempoMaximoChamada = TimeSpan.FromSeconds(30);

builder.Services.AddHttpClient<IDiagnosticoService, GeminiDiagnosticoService>(
    client => client.Timeout = tempoMaximoChamada);

builder.Services.AddHttpClient<IOficinaService, GeoapifyOficinaService>(
    client => client.Timeout = tempoMaximoChamada);

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
{
    escopo.ServiceProvider.GetRequiredService<AutoDiagnosticoContext>().Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


static string EscolherConnectionString(IConfiguration config)
{
    const string resto = "Database=AutoDiagnostico;Trusted_Connection=True;TrustServerCertificate=True;";

    var candidatas = new List<string>();
    var configurada = config.GetConnectionString("AutoDiagnosticoContext");
    if (!string.IsNullOrWhiteSpace(configurada))
    {
        candidatas.Add(configurada);
    }
    candidatas.Add(@"Server=(localdb)\mssqllocaldb;" + resto);
    candidatas.Add(@"Server=localhost\SQLEXPRESS;" + resto);
    candidatas.Add("Server=localhost;" + resto);

    foreach (var candidata in candidatas.Distinct())
    {
        var teste = new SqlConnectionStringBuilder(candidata)
        {
            InitialCatalog = "master",
            ConnectTimeout = candidata.Contains("(localdb)", StringComparison.OrdinalIgnoreCase) ? 15 : 5
        };

        try
        {
            using var conexao = new SqlConnection(teste.ConnectionString);
            conexao.Open();
            Console.WriteLine($"Banco de dados: usando o SQL Server em {teste.DataSource}");
            return candidata;
        }
        catch (Exception)
        {
        }
    }

    throw new InvalidOperationException(
        "Nenhum SQL Server encontrado nesta máquina. Instale o SQL Server Express (qualquer tipo de instalação) " +
        "ou o LocalDB. Na hospedagem, configure a variável de ambiente ConnectionStrings__AutoDiagnosticoContext. " +
        "Se o seu SQL Server tem outro nome de instância, configure com: " +
        "dotnet user-secrets set \"ConnectionStrings:AutoDiagnosticoContext\" \"Server=SEU_SERVIDOR;" + resto + "\"");
}

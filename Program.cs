using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using AutoDiagnostico.Data;
using AutoDiagnostico.Services;

// ATENÇÃO: este arquivo é só do Vinicius. Precisa mudar algo aqui? Peça a ele.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Banco de dados (SQL Server via Entity Framework Core).
// Na máquina de cada um (Development): o site escolhe sozinho o SQL Server que existir
// (LocalDB ou SQLEXPRESS). Veja EscolherConnectionString no fim deste arquivo.
// Na hospedagem (Production): usa direto a connection string da variável de ambiente
// ConnectionStrings__AutoDiagnosticoContext, configurada no painel da hospedagem, sem testar nada local.
// (Sem essa variável, como no "dotnet ef migrations add", continua a escolha automática.)
var connectionStringConfigurada = builder.Configuration.GetConnectionString("AutoDiagnosticoContext");
var hospedado = !builder.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(connectionStringConfigurada);
var connectionString = hospedado
    ? connectionStringConfigurada!
    : EscolherConnectionString(builder.Configuration);
builder.Services.AddDbContext<AutoDiagnosticoContext>(options =>
    options.UseSqlServer(connectionString, sql =>
    {
        // Banco em outro servidor: uma queda rápida de conexão é tentada de novo antes de virar erro
        if (hospedado)
        {
            sql.EnableRetryOnFailure();
        }
    }));

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

// Services reais. As chaves ficam nos user-secrets de cada máquina:
// dotnet user-secrets set "Gemini:ApiKey" "SUA_CHAVE"
// dotnet user-secrets set "Geoapify:ApiKey" "SUA_CHAVE"
var tempoMaximoChamada = TimeSpan.FromSeconds(30);

builder.Services.AddHttpClient<IDiagnosticoService, GeminiDiagnosticoService>(
    client => client.Timeout = tempoMaximoChamada);

builder.Services.AddHttpClient<IOficinaService, GeoapifyOficinaService>(
    client => client.Timeout = tempoMaximoChamada);

var app = builder.Build();

// Cria o banco e aplica as migrations sozinho ao iniciar o site.
// Ninguém da equipe precisa rodar "dotnet ef database update".
// (Os comandos "dotnet ef" param no builder.Build() acima, então esta parte não roda durante eles.)
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

app.UseAuthentication(); // precisa vir ANTES do UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


// Testa, em ordem, os jeitos mais comuns de ter SQL Server no Windows e devolve o primeiro que responder:
//   1. A connection string configurada (appsettings.json ou user-secrets — quem tem instância com outro nome usa user-secrets)
//   2. LocalDB              — vem com o Visual Studio ou com a opção "LocalDB" do instalador do SQL Server Express
//   3. localhost\SQLEXPRESS — o que o instalador do SQL Server Express cria na instalação "Básica"
//   4. localhost            — SQL Server instalado como instância padrão
// O teste conecta no banco "master", que sempre existe; o banco AutoDiagnostico é criado pelo Database.Migrate() ao iniciar.
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
            // O LocalDB pode demorar alguns segundos para "acordar" na primeira vez
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
            // Este não existe nesta máquina: tenta o próximo
        }
    }

    throw new InvalidOperationException(
        "Nenhum SQL Server encontrado nesta máquina. Instale o SQL Server Express (qualquer tipo de instalação) " +
        "ou o LocalDB. Na hospedagem, configure a variável de ambiente ConnectionStrings__AutoDiagnosticoContext. " +
        "Se o seu SQL Server tem outro nome de instância, configure com: " +
        "dotnet user-secrets set \"ConnectionStrings:AutoDiagnosticoContext\" \"Server=SEU_SERVIDOR;" + resto + "\"");
}

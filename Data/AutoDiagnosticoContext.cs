using Microsoft.EntityFrameworkCore;
using AutoDiagnostico.Models;

namespace AutoDiagnostico.Data;

public class AutoDiagnosticoContext : DbContext
{
    public AutoDiagnosticoContext(DbContextOptions<AutoDiagnosticoContext> options)
        : base(options)
    {
    }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Consulta> Consultas { get; set; }
}
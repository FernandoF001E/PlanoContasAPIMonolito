using Microsoft.EntityFrameworkCore;
using PlanoContasApi.Models;

namespace PlanoContasApi.Data
{
	/// <summary>
	/// AppContext
	/// </summary>
	public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
		/// <summary>
		/// Tabela Contas do banco de dados
		/// </summary>
		public DbSet<Conta> Contas { get; set; }
    }
}
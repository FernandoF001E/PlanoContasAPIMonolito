using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace PlanoContasApi.Models
{
	/// <summary>
	/// Model/Entidade Contas
	/// </summary>
	public class Conta
    {
		/// <summary>
		/// Propriedade Id
		/// </summary>
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }
		/// <summary>
		/// /// Propriedade Codigo
		/// </summary>
		public string? Codigo { get; set; }
		/// <summary>
		/// /// Propriedade Nome
		/// </summary>
		public string? Nome { get; set; }
		/// <summary>
		/// /// Propriedade Tipo
		/// </summary>
		public string? Tipo { get; set; }
		/// <summary>
		/// /// Propriedade AceitaLancamentos
		/// </summary>
		public bool AceitaLancamentos { get; set; }
    }
}
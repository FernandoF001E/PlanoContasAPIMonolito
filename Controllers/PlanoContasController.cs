using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PlanoContasApi.Data;
using PlanoContasApi.Models;

namespace PlanoContasApi.Controllers
{
	/// <summary>
	/// Controlador responsável por operações no plano de contas.
	/// </summary>
	[ApiController]
    [Route("api/[controller]")]
    public class PlanoContasController : ControllerBase
    {
        private readonly AppDbContext _context;

		/// <summary>
		/// Controlador responsável por operações no plano de contas.
		/// </summary>
		public PlanoContasController(AppDbContext context)
        {
            _context = context;
        }

		/// <summary>
		/// Lista todas as contas.
		/// </summary>
		[HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Conta>), 200)]
        public async Task<ActionResult<IEnumerable<Conta>>> GetContas()
        {
            return await _context.Contas.ToListAsync();
        }

		/// <summary>
		/// Lista uma determinada conta
		/// </summary>
		[HttpGet("{id}")]
        [ProducesResponseType(typeof(Conta), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Conta>> GetConta(int id)
        {
            var conta = await _context.Contas.FindAsync(id);
            if (conta == null)
                return NotFound();
            return conta;
        }

		/// <summary>
		/// Cria uma nova conta
		/// </summary>
		[HttpPost]
        [ProducesResponseType(typeof(Conta), 201)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<Conta>> CreateConta(Conta conta)
        {
            if (_context.Contas.Any(c => c.Codigo == conta.Codigo))
                return BadRequest("Já existe uma conta com este código.");

            var codigoPai = ObterCodigoPai(conta.Codigo);
            Conta? contaPai = null;
            if (!string.IsNullOrEmpty(codigoPai))
            {
                contaPai = await _context.Contas.FirstOrDefaultAsync(c => c.Codigo == codigoPai);
                if (contaPai == null)
                    return BadRequest($"Conta pai com código '{codigoPai}' não encontrada.");

                if (contaPai.AceitaLancamentos)
                    return BadRequest("A conta pai aceita lançamentos e, portanto, não pode ter contas filhas.");

                if (conta.Tipo != contaPai.Tipo)
                    return BadRequest("A conta deve ser do mesmo tipo da conta pai.");
            }

            _context.Contas.Add(conta);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConta), new { id = conta.Id }, conta);
        }

		/// <summary>
		/// Altera uma conta
		/// </summary>
		/// 
		[HttpPut("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> UpdateConta(int id, Conta conta)
        {
            if (id != conta.Id)
                return BadRequest("ID da URL não corresponde ao ID da conta.");

            var contaExistente = await _context.Contas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (contaExistente == null)
                return NotFound();

            // Verifica duplicidade de código (caso tenha sido alterado)
            if (_context.Contas.Any(c => c.Codigo == conta.Codigo && c.Id != id))
                return BadRequest("Já existe uma conta com este código.");

            var codigoPai = ObterCodigoPai(conta.Codigo);
            Conta? contaPai = null;
            if (!string.IsNullOrEmpty(codigoPai))
            {
                contaPai = await _context.Contas.FirstOrDefaultAsync(c => c.Codigo == codigoPai);
                if (contaPai == null)
                    return BadRequest($"Conta pai com código '{codigoPai}' não encontrada.");

                if (contaPai.AceitaLancamentos)
                    return BadRequest("A conta pai aceita lançamentos e, portanto, não pode ter contas filhas.");

                if (conta.Tipo != contaPai.Tipo)
                    return BadRequest("A conta deve ser do mesmo tipo da conta pai.");
            }

            _context.Entry(conta).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

		/// <summary>
		/// Apaga uma conta
		/// </summary>
		[HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteConta(int id)
        {
            var conta = await _context.Contas.FindAsync(id);
            if (conta == null)
                return NotFound();

            _context.Contas.Remove(conta);
            await _context.SaveChangesAsync();
            return NoContent();
        }

		/// <summary>
		/// Sugere novo codigo para uma nova conta
		/// </summary>
		[HttpGet("sugerir-proximo-codigo/{codigoPai}")]
		[ProducesResponseType(204)]
		[ProducesResponseType(404)]
		public async Task<ActionResult<string>> SugerirProximoCodigo(string codigoPai)
		{
			const int LIMITE_MAXIMO = 999;

			var todasContas = await _context.Contas.ToListAsync();

			foreach (var c in todasContas)
			{
				var pai = ObterCodigoPai(c.Codigo);
				Console.WriteLine($"Conta: {c.Codigo} => Pai: {pai}");
			}

			var filhosDiretos = todasContas
	            .Where(c => ObterCodigoPai(c.Codigo) == codigoPai) 
	            .ToList();

			Console.WriteLine($"paiAtual: {codigoPai}, filhosDiretos.Count: {filhosDiretos.Count}");

			var ultimosNumeros = filhosDiretos
				.Select(c =>
				{
					var partes = c.Codigo.Split('.');
					return int.TryParse(partes.Last(), out var n) ? n : -1;
				})
				.Where(n => n > 0)
				.ToList();

			var proximoNumero = (ultimosNumeros.Count > 0 ? ultimosNumeros.Max() + 1 : 1);

			if (proximoNumero > LIMITE_MAXIMO)
			{
				var novoPai = ObterCodigoPai(codigoPai);
				if (string.IsNullOrEmpty(novoPai))
					return BadRequest("Limite máximo de códigos atingido para este nível e não há um pai acima.");

				todasContas = await _context.Contas.ToListAsync();

				var filhosNovoPai = todasContas
				.Where(c => ObterCodigoPai(c.Codigo) == novoPai)
				.ToList();

				var ultimosNovoPai = filhosNovoPai
					.Select(c =>
					{
						var partes = c.Codigo.Split('.');
						return int.TryParse(partes.Last(), out var n) ? n : -1;
					})
					.Where(n => n > 0)
					.ToList();

				var novoNumero = (ultimosNovoPai.Count > 0 ? ultimosNovoPai.Max() + 1 : 1);

				if (novoNumero > LIMITE_MAXIMO)
					return BadRequest("Limite máximo de códigos atingido em todos os níveis.");

				return Ok(new
				{
					NovoPai = novoPai,
					CodigoSugerido = $"{novoPai}.{novoNumero}"
				});
			}

			return Ok(new
			{
				CodigoSugerido = $"{codigoPai}.{proximoNumero}"
			});
		}

		private string? ObterCodigoPai(string codigo)
		{
			var partes = codigo.Split('.');
			if (partes.Length <= 1) return null;
			return string.Join('.', partes.Take(partes.Length - 1));
		}
	}
}
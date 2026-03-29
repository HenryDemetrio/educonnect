using EduConnect.API.Data;
using EduConnect.API.DTOs;
using EduConnect.API.Entities;
using EduConnect.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EduConnect.API.Controllers
{
    [ApiController]
    [Route("professores")]
    public class ProfessoresController : ControllerBase
    {
        private readonly EduConnectContext _ctx;

        public ProfessoresController(EduConnectContext ctx)
        {
            _ctx = ctx;
        }

        // POST /professores  (ADMIN)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateProfessorRequest req,
            [FromServices] AccessProvisioningService accessSvc)
        {
            if (string.IsNullOrWhiteSpace(req.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            if (string.IsNullOrWhiteSpace(req.EmailContato) || !req.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            if (string.IsNullOrWhiteSpace(req.Registro))
                return BadRequest(new { message = "Registro é obrigatório." });

            var registroExiste = await _ctx.Professores.AnyAsync(p => p.Registro == req.Registro);
            if (registroExiste)
                return Conflict(new { message = "Registro já cadastrado." });

            var emailInstitucional = await accessSvc.GenerateInstitutionalEmailFromNameAsync(req.Nome);

            var emailExiste = await _ctx.Usuarios.AnyAsync(u => u.Email == emailInstitucional);
            if (emailExiste)
                return Conflict(new { message = "Não foi possível gerar email institucional único." });

            var usuario = new Usuario
            {
                Nome = req.Nome.Trim(),
                Email = emailInstitucional,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(accessSvc.GenerateTempPassword(24)),
                Role = "Professor"
            };

            _ctx.Usuarios.Add(usuario);
            await _ctx.SaveChangesAsync();

            var professor = new Professor
            {
                UsuarioId = usuario.Id,
                Registro = req.Registro.Trim(),
                EmailContato = req.EmailContato.Trim()
            };

            _ctx.Professores.Add(professor);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = professor.Id }, new { id = professor.Id });
        }

        // GET /professores  (ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProfessorResponse>>> GetAll()
        {
            var dados = await _ctx.Professores
                .Include(p => p.Usuario)
                .Select(p => new ProfessorResponse
                {
                    Id = p.Id,
                    Nome = p.Usuario!.Nome,
                    EmailInstitucional = p.Usuario.Email,
                    EmailContato = p.EmailContato,
                    Registro = p.Registro!
                })
                .ToListAsync();

            return Ok(dados);
        }

        // GET /professores/{id}  (ADMIN)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProfessorResponse>> GetById(int id)
        {
            var p = await _ctx.Professores
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (p == null)
                return NotFound(new { message = "Professor não encontrado." });

            return Ok(new ProfessorResponse
            {
                Id = p.Id,
                Nome = p.Usuario!.Nome,
                EmailInstitucional = p.Usuario.Email,
                EmailContato = p.EmailContato,
                Registro = p.Registro!
            });
        }

        // PUT /professores/{id}  (ADMIN)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProfessorRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            if (string.IsNullOrWhiteSpace(req.EmailContato) || !req.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            if (string.IsNullOrWhiteSpace(req.Registro))
                return BadRequest(new { message = "Registro é obrigatório." });

            var professor = await _ctx.Professores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (professor == null)
                return NotFound(new { message = "Professor não encontrado." });

            var registroExiste = await _ctx.Professores.AnyAsync(p => p.Registro == req.Registro && p.Id != id);
            if (registroExiste)
                return Conflict(new { message = "Registro já cadastrado para outro professor." });

            professor.Usuario!.Nome = req.Nome.Trim();
            professor.Registro = req.Registro.Trim();
            professor.EmailContato = req.EmailContato.Trim();

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // POST /professores/{id}/gerar-acesso (ADMIN)
        [HttpPost("{id:int}/gerar-acesso")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarAcesso(
            int id,
            [FromServices] AccessProvisioningService accessSvc,
            [FromServices] PowerAutomateService paSvc)
        {
            var professor = await _ctx.Professores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (professor == null)
                return NotFound(new { message = "Professor não encontrado." });

            if (string.IsNullOrWhiteSpace(professor.EmailContato) || !professor.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            var senhaTemp = accessSvc.GenerateTempPassword(10);
            professor.Usuario!.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp);

            await _ctx.SaveChangesAsync();

            await paSvc.SendProvisioningEmailAsync(new
            {
                tipo = "Professor",
                nome = professor.Usuario.Nome,
                emailContato = professor.EmailContato,
                emailInstitucional = professor.Usuario.Email,
                senhaTemporaria = senhaTemp
            });

            return Ok(new { message = "Acesso gerado e envio solicitado." });
        }

        // DELETE /professores/{id}  (ADMIN)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var professor = await _ctx.Professores
                .Include(p => p.Usuario)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (professor == null)
                return NotFound(new { message = "Professor não encontrado." });

            var vinculado = await _ctx.TurmaDisciplinas.AnyAsync(td => td.ProfessorId == id);
            if (vinculado)
                return Conflict(new { message = "Não é possível excluir professor vinculado a disciplinas." });

            _ctx.Usuarios.Remove(professor.Usuario!);
            await _ctx.SaveChangesAsync();
            return NoContent();
        }
    }
}

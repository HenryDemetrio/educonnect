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
    [Route("alunos")]
    public class AlunosController : ControllerBase
    {
        private readonly EduConnectContext _ctx;

        public AlunosController(EduConnectContext ctx)
        {
            _ctx = ctx;
        }

        // POST /alunos  (ADMIN) - cria aluno + usuario com email institucional gerado
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            [FromBody] CreateAlunoRequest req,
            [FromServices] AccessProvisioningService accessSvc)
        {
            if (string.IsNullOrWhiteSpace(req.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            if (string.IsNullOrWhiteSpace(req.EmailContato) || !req.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            if (string.IsNullOrWhiteSpace(req.RA))
                return BadRequest(new { message = "RA é obrigatório." });

            // duplicidade de RA
            var raExiste = await _ctx.Alunos.AnyAsync(a => a.RA == req.RA);
            if (raExiste)
                return Conflict(new { message = "RA já cadastrado." });

            // gera email institucional pelo nome (nome.sobrenome@educonnect.com)
            var emailInstitucional = await accessSvc.GenerateInstitutionalEmailFromNameAsync(req.Nome);

            // duplicidade (deveria ser impossível por causa do generator, mas garante)
            var emailExiste = await _ctx.Usuarios.AnyAsync(u => u.Email == emailInstitucional);
            if (emailExiste)
                return Conflict(new { message = "Não foi possível gerar email institucional único." });

            // cria Usuario (senha placeholder - só vira válida quando gerar-acesso)
            var usuario = new Usuario
            {
                Nome = req.Nome.Trim(),
                Email = emailInstitucional,
                SenhaHash = BCrypt.Net.BCrypt.HashPassword(accessSvc.GenerateTempPassword(24)),
                Role = "Aluno"
            };

            _ctx.Usuarios.Add(usuario);
            await _ctx.SaveChangesAsync();

            var aluno = new Aluno
            {
                UsuarioId = usuario.Id,
                RA = req.RA.Trim(),
                EmailContato = req.EmailContato.Trim()
            };

            _ctx.Alunos.Add(aluno);
            await _ctx.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = aluno.Id }, new { id = aluno.Id });
        }

        // GET /alunos  (ADMIN)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<AlunoResponse>>> GetAll()
        {
            var dados = await _ctx.Alunos
                .Include(a => a.Usuario)
                .Select(a => new AlunoResponse
                {
                    Id = a.Id,
                    Nome = a.Usuario!.Nome,
                    EmailInstitucional = a.Usuario.Email,
                    EmailContato = a.EmailContato,
                    RA = a.RA
                })
                .ToListAsync();

            return Ok(dados);
        }

        // GET /alunos/{id}  (ADMIN)
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<AlunoResponse>> GetById(int id)
        {
            var a = await _ctx.Alunos
                .Include(x => x.Usuario)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (a == null)
                return NotFound(new { message = "Aluno não encontrado." });

            return Ok(new AlunoResponse
            {
                Id = a.Id,
                Nome = a.Usuario!.Nome,
                EmailInstitucional = a.Usuario.Email,
                EmailContato = a.EmailContato,
                RA = a.RA
            });
        }

        // PUT /alunos/{id}  (ADMIN) - atualiza dados do aluno (sem mexer em senha/email institucional)
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAlunoRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Nome))
                return BadRequest(new { message = "Nome é obrigatório." });

            if (string.IsNullOrWhiteSpace(req.EmailContato) || !req.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            if (string.IsNullOrWhiteSpace(req.RA))
                return BadRequest(new { message = "RA é obrigatório." });

            var aluno = await _ctx.Alunos
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
                return NotFound(new { message = "Aluno não encontrado." });

            // checar duplicidade de RA (ignorando o próprio)
            var raExiste = await _ctx.Alunos.AnyAsync(a => a.RA == req.RA && a.Id != id);
            if (raExiste)
                return Conflict(new { message = "RA já cadastrado para outro aluno." });

            aluno.Usuario!.Nome = req.Nome.Trim();
            aluno.RA = req.RA.Trim();
            aluno.EmailContato = req.EmailContato.Trim();

            await _ctx.SaveChangesAsync();
            return NoContent();
        }

        // POST /alunos/{id}/gerar-acesso (ADMIN) - gera senha temp + envia email via Power Automate
        [HttpPost("{id:int}/gerar-acesso")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GerarAcesso(
            int id,
            [FromServices] AccessProvisioningService accessSvc,
            [FromServices] PowerAutomateService paSvc)
        {
            var aluno = await _ctx.Alunos
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
                return NotFound(new { message = "Aluno não encontrado." });

            if (string.IsNullOrWhiteSpace(aluno.EmailContato) || !aluno.EmailContato.Contains("@"))
                return BadRequest(new { message = "Email de contato inválido." });

            var senhaTemp = accessSvc.GenerateTempPassword(10);
            aluno.Usuario!.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemp);

            await _ctx.SaveChangesAsync();

            // Para DEV/demo: você quer ver a senha no log do flow
            await paSvc.SendProvisioningEmailAsync(new
            {
                tipo = "Aluno",
                nome = aluno.Usuario.Nome,
                emailContato = aluno.EmailContato,
                emailInstitucional = aluno.Usuario.Email,
                senhaTemporaria = senhaTemp
            });

            return Ok(new { message = "Acesso gerado e envio solicitado." });
        }

        // DELETE /alunos/{id}  (ADMIN)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var aluno = await _ctx.Alunos
                .Include(a => a.Usuario)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
                return NotFound(new { message = "Aluno não encontrado." });

            _ctx.Usuarios.Remove(aluno.Usuario!);
            await _ctx.SaveChangesAsync();

            return NoContent();
        }
    }
}

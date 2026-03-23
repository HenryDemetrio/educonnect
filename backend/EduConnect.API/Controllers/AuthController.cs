using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EduConnect.API.Auth;
using EduConnect.API.Data;
using EduConnect.API.DTOs;
using EduConnect.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EduConnect.API.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly EduConnectContext _ctx;
        private readonly JwtSettings _jwt;
        private readonly PowerAutomateService _pa;

        public AuthController(EduConnectContext ctx, IOptions<JwtSettings> jwt, PowerAutomateService pa)
        {
            _ctx = ctx;
            _jwt = jwt.Value;
            _pa = pa;
        }

        // POST /auth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var user = _ctx.Usuarios.FirstOrDefault(u => u.Email.ToLower() == email);
            if (user == null) return Unauthorized(new { message = "Credenciais inválidas" });

            var ok = BCrypt.Net.BCrypt.Verify(req.Senha, user.SenhaHash);
            if (!ok) return Unauthorized(new { message = "Credenciais inválidas" });

            var token = TokenService.Generate(user.Id.ToString(), user.Nome, user.Role, _jwt);

            return Ok(new LoginResponse
            {
                Id = user.Id,
                Nome = user.Nome,
                Email = user.Email,
                Role = user.Role,
                Token = token
            });
        }

        // ✅ POST /auth/forgot-password
        // Dispara Power Automate com senha temporária (mesmo Flow do provisioning)
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            // resposta genérica (não vaza se email existe)
            const string okMsg = "Se esse e-mail existir, enviamos instruções de recuperação. Verifique sua caixa de entrada.";

            if (req == null || string.IsNullOrWhiteSpace(req.Email))
                return Ok(new { message = okMsg });

            var email = req.Email.Trim().ToLowerInvariant();

            // tenta achar pelo email de login (institucional)
            var user = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            // se não achou, tenta achar pelo emailContato de aluno e pegar o usuario
            if (user == null)
            {
                var aluno = await _ctx.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.EmailContato.ToLower() == email);
                if (aluno != null)
                    user = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Id == aluno.UsuarioId);
            }

            // se não achou, retorna genérico
            if (user == null)
                return Ok(new { message = okMsg });

            // gera senha temporária (simples, suficiente pro projeto)
            var senhaTemporaria = $"Edu@{Random.Shared.Next(100000, 999999)}";
            user.SenhaHash = BCrypt.Net.BCrypt.HashPassword(senhaTemporaria);
            await _ctx.SaveChangesAsync();

            // descobre emailContato (se existir)
            string emailContato = user.Email; // fallback
            if (user.Role == "Aluno")
            {
                var aluno = await _ctx.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.UsuarioId == user.Id);
                if (!string.IsNullOrWhiteSpace(aluno?.EmailContato))
                    emailContato = aluno.EmailContato.Trim();
            }

            // dispara Power Automate (mesmo schema)
            try
            {
                await _pa.SendProvisioningEmailAsync(new
                {
                    tipo = "RecuperacaoSenha",
                    nome = user.Nome,
                    emailContato = emailContato,
                    emailInstitucional = user.Email,
                    senhaTemporaria = senhaTemporaria
                });
            }
            catch
            {
                // não quebra o fluxo pro usuário (evita enumerar e evita travar UX)
                // se quiser, você pode logar a exception num logger depois
            }

            return Ok(new { message = okMsg });
        }

        [Authorize]
        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { message = "Informe a senha atual e a nova senha." });

            if (req.NewPassword.Length < 8)
                return BadRequest(new { message = "A nova senha precisa ter pelo menos 8 caracteres." });

            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                return Unauthorized(new { message = "Token inválido (sub)." });

            var usuario = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Id == userId);
            if (usuario == null) return Unauthorized(new { message = "Usuário não encontrado." });

            var ok = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, usuario.SenhaHash);
            if (!ok) return BadRequest(new { message = "Senha atual incorreta." });

            usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

            await _ctx.SaveChangesAsync();
            return Ok(new { message = "Senha alterada com sucesso." });
        }

        // GET /auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                return Unauthorized(new { message = "Token inválido (sub)." });

            var user = await _ctx.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return Unauthorized(new { message = "Usuário não encontrado." });

            var aluno = await _ctx.Alunos.AsNoTracking().FirstOrDefaultAsync(a => a.UsuarioId == userId);
            var professor = await _ctx.Professores.AsNoTracking().FirstOrDefaultAsync(p => p.UsuarioId == userId);

            return Ok(new AuthMeResponse
            {
                UserId = user.Id,
                Nome = user.Nome,
                Email = user.Email,
                Role = user.Role,
                AlunoId = aluno?.Id,
                RA = aluno?.RA,
                ProfessorId = professor?.Id,
                Registro = professor?.Registro
            });
        }
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = "";
    }
}
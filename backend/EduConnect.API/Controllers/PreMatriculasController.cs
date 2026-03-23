using EduConnect.API.Data;
using EduConnect.API.DTOs;
using EduConnect.API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace EduConnect.API.Controllers;

[ApiController]
[Route("pre-matriculas")]
public class PreMatriculasController : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedDocExts = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg" };

    private static bool UploadValido(IFormFile file)
    {
        if (file == null || file.Length <= 0 || file.Length > MaxUploadBytes) return false;
        var ext = Path.GetExtension(file.FileName);
        return AllowedDocExts.Contains(ext);
    }

    private readonly EduConnectContext _ctx;
    private readonly IWebHostEnvironment _env;

    public PreMatriculasController(EduConnectContext ctx, IWebHostEnvironment env)
    {
        _ctx = ctx;
        _env = env;
    }

    // ETAPA 1
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreatePreMatriculaRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Nome) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Telefone) ||
            string.IsNullOrWhiteSpace(req.Endereco) ||
            string.IsNullOrWhiteSpace(req.DataNasc))
            return BadRequest(new { message = "Preencha todos os campos obrigatórios." });

        var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy" };

        if (!DateTime.TryParseExact(req.DataNasc, formats,
                CultureInfo.GetCultureInfo("pt-BR"),
                DateTimeStyles.None, out var dataNasc))
        {
            return BadRequest(new { message = "Data de nascimento inválida." });
        }

        var pre = new PreMatricula
        {
            Nome = req.Nome.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            Telefone = req.Telefone.Trim(),
            Endereco = req.Endereco.Trim(),
            DataNascimento = dataNasc,
            Status = "INICIADA",
            CriadaEmUtc = DateTime.UtcNow
        };

        _ctx.PreMatriculas.Add(pre);
        await _ctx.SaveChangesAsync();

        return Ok(new { id = pre.Id });
    }

    // ETAPA 2: documentos
    [HttpPost("{id:int}/documentos")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadDocumentos(int id, [FromForm] UploadPreMatriculaDocumentosRequest req)
    {
        var pre = await _ctx.PreMatriculas.FirstOrDefaultAsync(x => x.Id == id);
        if (pre == null) return NotFound(new { message = "Pré-matrícula não encontrada." });

        if (req.RgCpf == null || req.Escolaridade == null)
            return BadRequest(new { message = "Envie RG/CPF e Escolaridade." });

        if (!UploadValido(req.RgCpf) || !UploadValido(req.Escolaridade))
            return BadRequest(new { message = "Arquivos inválidos. Envie PDF/JPG/PNG com até 10 MB." });

        var baseDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "pre-matriculas", id.ToString());
        Directory.CreateDirectory(baseDir);

        async Task<string> SaveFile(IFormFile f, string name)
        {
            var ext = Path.GetExtension(f.FileName);
            var fileName = $"{name}{ext}";
            var abs = Path.Combine(baseDir, fileName);
            await using var fs = System.IO.File.Create(abs);
            await f.CopyToAsync(fs);
            return $"/uploads/pre-matriculas/{id}/{fileName}";
        }

        pre.RgCpfUrl = await SaveFile(req.RgCpf, "rg_cpf");
        pre.EscolaridadeUrl = await SaveFile(req.Escolaridade, "escolaridade");
        pre.Status = "DOCUMENTOS_OK";

        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Documentos enviados." });
    }

    // ETAPA 3: comprovante pagamento
    [HttpPost("{id:int}/pagamento")]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPagamento(int id, [FromForm] UploadPreMatriculaPagamentoRequest req)
    {
        var pre = await _ctx.PreMatriculas.FirstOrDefaultAsync(x => x.Id == id);
        if (pre == null) return NotFound(new { message = "Pré-matrícula não encontrada." });

        if (req.Comprovante == null)
            return BadRequest(new { message = "Envie o comprovante." });

        if (!UploadValido(req.Comprovante))
            return BadRequest(new { message = "Arquivo inválido. Envie PDF/JPG/PNG com até 10 MB." });

        var baseDir = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "pre-matriculas", id.ToString());
        Directory.CreateDirectory(baseDir);

        var ext = Path.GetExtension(req.Comprovante.FileName);
        var fileName = $"comprovante{ext}";
        var abs = Path.Combine(baseDir, fileName);

        await using (var fs = System.IO.File.Create(abs))
            await req.Comprovante.CopyToAsync(fs);

        pre.ComprovantePagamentoUrl = $"/uploads/pre-matriculas/{id}/{fileName}";
        pre.Status = "PENDENTE_ADMIN";

        await _ctx.SaveChangesAsync();
        return Ok(new { message = "Pagamento enviado. Aguardando aprovação." });
    }
}
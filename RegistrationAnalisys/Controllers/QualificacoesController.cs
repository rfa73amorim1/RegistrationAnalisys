using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RegistrationAnalisys.Application.DTOs;
using RegistrationAnalisys.Application.Services;

namespace RegistrationAnalisys.Controllers;

[ApiController]
[Route("qualificacoes")]
public sealed class QualificacoesController : ControllerBase
{
    private static readonly Regex NonDigitsRegex = new("\\D", RegexOptions.Compiled);
    private readonly IQualificacaoService _qualificacaoService;

    public QualificacoesController(IQualificacaoService qualificacaoService)
    {
        _qualificacaoService = qualificacaoService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(QualificacaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QualificacaoResponse>> Post(
        [FromBody] QualificacaoRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Cnpj))
        {
            return BadRequest(new { message = "CNPJ e obrigatorio." });
        }

        var cnpjNormalizado = NonDigitsRegex.Replace(request.Cnpj, string.Empty);

        if (cnpjNormalizado.Length != 14)
        {
            return BadRequest(new { message = "CNPJ invalido. Informe 14 digitos." });
        }

        if (request.ValorOperacao <= 0)
        {
            return BadRequest(new { message = "valorOperacao deve ser maior que zero." });
        }

        if (request.PrazoOprecaoDias <= 0)
        {
            return BadRequest(new { message = "prazoOprecaoDias deve ser maior que zero." });
        }

        if (string.IsNullOrWhiteSpace(request.PoliticaId))
        {
            return BadRequest(new { message = "politicaId e obrigatorio." });
        }

        if (string.IsNullOrWhiteSpace(request.Papel))
        {
            request.Papel = "CLIENTE";
        }

        request.Papel = request.Papel.Trim().ToUpperInvariant();

        if (request.Papel is not ("CLIENTE" or "FORNECEDOR"))
        {
            return BadRequest(new { message = "papel invalido. Informe CLIENTE ou FORNECEDOR." });
        }

        if (request.RelacionamentoNovo && request.DiasAtrasoInterno90d.HasValue)
        {
            return BadRequest(new { message = "diasAtrasoInterno90d nao deve ser enviado para relacionamento novo." });
        }

        if (!request.RelacionamentoNovo && !request.DiasAtrasoInterno90d.HasValue)
        {
            return BadRequest(new { message = "diasAtrasoInterno90d e obrigatorio para relacionamento existente." });
        }

        if (request.DiasAtrasoInterno90d is < 0)
        {
            return BadRequest(new { message = "diasAtrasoInterno90d nao pode ser negativo." });
        }

        request.Cnpj = cnpjNormalizado;

        try
        {
            var result = await _qualificacaoService.QualificarAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

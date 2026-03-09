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
        [FromQuery] bool includeExplanation = true,
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

        var result = await _qualificacaoService.QualificarAsync(cnpjNormalizado, includeExplanation, cancellationToken);
        return Ok(result);
    }
}

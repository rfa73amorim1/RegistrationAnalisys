using RegistrationAnalisys.Application.DTOs;
using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Application.Services;

public sealed class QualificacaoService : IQualificacaoService
{
    private readonly ISerasaSource _serasaSource;
    private readonly ICertidoesSource _certidoesSource;
    private readonly IExplicadorQualificacao _explicador;

    public QualificacaoService(ISerasaSource serasaSource, ICertidoesSource certidoesSource, IExplicadorQualificacao explicador)
    {
        _serasaSource = serasaSource;
        _certidoesSource = certidoesSource;
        _explicador = explicador;
    }

    public async Task<QualificacaoResponse> QualificarAsync(string cnpj, bool includeExplanation = true, CancellationToken cancellationToken = default)
    {
        var serasa = await _serasaSource.ConsultarAsync(cnpj, cancellationToken);
        var certidoes = await _certidoesSource.ConsultarAsync(cnpj, cancellationToken);

        var consolidado = new ConsolidadoData
        {
            Serasa = serasa,
            Certidoes = certidoes
        };

        var evidencias = MontarEvidencias(consolidado);
        var pendencias = MontarPendencias(consolidado.Certidoes);
        var decisaoFinal = CalcularDecisao(consolidado.Serasa.Score, pendencias.Any(), evidencias);

        return new QualificacaoResponse
        {
            Cnpj = cnpj,
            DecisaoFinal = decisaoFinal.ToString(),
            ScoreFinanceiro = consolidado.Serasa.Score,
            Evidencias = evidencias,
            Pendencias = pendencias,
            ExplicacaoAgente = includeExplanation
                ? _explicador.GerarExplicacao(decisaoFinal, consolidado.Serasa.Score, evidencias, pendencias)
                : null
        };
    }

    private static DecisaoFinal CalcularDecisao(decimal score, bool possuiPendenciasCertidoes, List<string> evidencias)
    {
        if (possuiPendenciasCertidoes)
        {
            evidencias.Add("Existe certidao em status PENDENTE ou IRREGULAR.");
            return DecisaoFinal.REPROVADO;
        }

        if (score >= 8.0m)
        {
            return DecisaoFinal.APROVADO;
        }

        if (score >= 6.0m)
        {
            return DecisaoFinal.APROVADO_COM_RESSALVAS;
        }

        return DecisaoFinal.REPROVADO;
    }

    private static List<string> MontarEvidencias(ConsolidadoData consolidado)
    {
        return new List<string>
        {
            $"Score Serasa: {consolidado.Serasa.Score:0.0} (faixa {consolidado.Serasa.Faixa}).",
            $"Endividamento informado: {consolidado.Serasa.Endividamento:0.##}%.",
            $"Quantidade de atrasos: {consolidado.Serasa.Atrasos}."
        };
    }

    private static List<string> MontarPendencias(CertidoesData certidoes)
    {
        return certidoes
            .ToPairs()
            .Where(item => item.Value.Equals("PENDENTE", StringComparison.OrdinalIgnoreCase)
                || item.Value.Equals("IRREGULAR", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"Certidao {item.Key} em status {item.Value}.")
            .ToList();
    }
}

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
        var explicacao = includeExplanation
            ? _explicador.GerarExplicacao(decisaoFinal, consolidado.Serasa.Score, evidencias, pendencias)
            : null;

        return new QualificacaoResponse
        {
            Cnpj = cnpj,
            DecisaoFinal = decisaoFinal.ToString(),
            ScoreFinanceiro = consolidado.Serasa.Score,
            ResultadoCnds = MontarResultadoCnds(consolidado.Certidoes),
            Evidencias = evidencias,
            Pendencias = pendencias,
            ExplicacaoAgente = explicacao is null
                ? null
                : new ExplicacaoAgenteDto
                {
                    Resumo = explicacao.Resumo,
                    Fundamentos = explicacao.Fundamentos,
                    Recomendacoes = explicacao.Recomendacoes
                }
        };
    }

    private static DecisaoFinal CalcularDecisao(decimal score, bool possuiPendenciasCertidoes, List<string> evidencias)
    {
        if (possuiPendenciasCertidoes)
        {
            evidencias.Add("Existe CND em status POSITIVA (com restricao).");
            return DecisaoFinal.REPROVADO;
        }

        if (score >= 800m)
        {
            return DecisaoFinal.APROVADO;
        }

        if (score >= 600m)
        {
            return DecisaoFinal.APROVADO_COM_RESSALVAS;
        }

        return DecisaoFinal.REPROVADO;
    }

    private static List<string> MontarEvidencias(ConsolidadoData consolidado)
    {
        return new List<string>
        {
            $"Score Serasa: {consolidado.Serasa.Score:0} (faixa {consolidado.Serasa.Faixa}).",
            $"Endividamento informado: {consolidado.Serasa.Endividamento:0.##}%.",
            $"Quantidade de atrasos: {consolidado.Serasa.Atrasos}."
        };
    }

    private static List<string> MontarPendencias(CertidoesData certidoes)
    {
        return certidoes
            .ToPairs()
            .Where(item => item.Value.Equals("POSITIVA", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"CND {item.Key} em status POSITIVA (com restricao).")
            .ToList();
    }

    private static Dictionary<string, string> MontarResultadoCnds(CertidoesData certidoes)
    {
        return certidoes
            .ToPairs()
            .ToDictionary(item => item.Key, item => ClassificarCnd(item.Value));
    }

    private static string ClassificarCnd(string status)
    {
        return status.Equals("NEGATIVA", StringComparison.OrdinalIgnoreCase)
            ? "NEGATIVA_SEM_RESTRICAO"
            : "POSITIVA_COM_RESTRICAO";
    }
}

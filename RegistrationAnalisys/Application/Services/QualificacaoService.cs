using RegistrationAnalisys.Application.DTOs;
using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;
using RegistrationAnalisys.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace RegistrationAnalisys.Application.Services;

public sealed class QualificacaoService : IQualificacaoService
{
    private readonly ISerasaSource _serasaSource;
    private readonly ICertidoesSource _certidoesSource;
    private readonly IExplicadorQualificacao _explicador;
    private readonly Dictionary<string, PoliticaNegocio> _politicas;

    public QualificacaoService(
        ISerasaSource serasaSource,
        ICertidoesSource certidoesSource,
        IExplicadorQualificacao explicador,
        IOptions<PoliticasNegocioOptions> politicasOptions)
    {
        _serasaSource = serasaSource;
        _certidoesSource = certidoesSource;
        _explicador = explicador;
        _politicas = ConstruirPoliticas(politicasOptions.Value);
    }

    public async Task<QualificacaoResponse> QualificarAsync(QualificacaoRequest request, CancellationToken cancellationToken = default)
    {
        var serasa = await _serasaSource.ConsultarAsync(request.Cnpj, cancellationToken);
        var certidoes = await _certidoesSource.ConsultarAsync(request.Cnpj, cancellationToken);

        var consolidado = new ConsolidadoData
        {
            Serasa = serasa,
            Certidoes = certidoes
        };

        var politica = ObterPolitica(request.PoliticaId);
        var evidencias = MontarEvidencias(consolidado, request, politica);
        var pendencias = MontarPendencias(consolidado.Certidoes);
        var decisaoFinal = CalcularDecisao(consolidado.Serasa.Score, pendencias.Any(), request, politica);
        var explicacao = await _explicador.GerarExplicacaoAsync(decisaoFinal, consolidado.Serasa.Score, evidencias, pendencias, cancellationToken);
        var acaoComercial = MontarAcaoComercial(decisaoFinal, request, politica);
        var motivosPrincipais = MontarMotivosPrincipais(consolidado.Serasa, pendencias, request);

        return new QualificacaoResponse
        {
            RecomendacaoAgente = MontarRecomendacaoAgente(explicacao.Resumo, acaoComercial, decisaoFinal),
            AcaoComercial = acaoComercial,
            MotivosPrincipais = motivosPrincipais
        };
    }

    private static DecisaoFinal CalcularDecisao(decimal score, bool possuiPendenciasCertidoes, QualificacaoRequest request, PoliticaNegocio politica)
    {
        if (possuiPendenciasCertidoes && politica.BloqueiaComRestricaoAtiva)
        {
            return DecisaoFinal.REPROVADO;
        }

        if (!request.ClienteNovo && (request.DiasAtrasoInterno90d ?? 0) > politica.MaxDiasAtrasoInterno)
        {
            return DecisaoFinal.REPROVADO;
        }

        if (score >= politica.ScoreMinAprovar)
        {
            return DecisaoFinal.APROVADO;
        }

        if (score >= politica.ScoreMinRessalva)
        {
            return DecisaoFinal.APROVADO_COM_RESSALVAS;
        }

        return DecisaoFinal.REPROVADO;
    }

    private static List<string> MontarEvidencias(ConsolidadoData consolidado, QualificacaoRequest request, PoliticaNegocio politica)
    {
        return new List<string>
        {
            $"Score Serasa: {consolidado.Serasa.Score:0} (faixa {consolidado.Serasa.Faixa}).",
            $"Endividamento informado: {consolidado.Serasa.Endividamento:0.##}%.",
            $"Quantidade de atrasos: {consolidado.Serasa.Atrasos}.",
            $"Valor do pedido: {request.ValorPedido:0.##}.",
            $"Prazo desejado: {request.PrazoDesejadoDias} dias.",
            $"Politica aplicada: {politica.Id}."
        };
    }

    private static AcaoComercialDto MontarAcaoComercial(DecisaoFinal decisaoFinal, QualificacaoRequest request, PoliticaNegocio politica)
    {
        if (decisaoFinal == DecisaoFinal.APROVADO)
        {
            return new AcaoComercialDto
            {
                Decisao = decisaoFinal.ToString(),
                LimiteCreditoSugerido = politica.LimiteBase,
                PrazoMaximoDias = Math.Min(request.PrazoDesejadoDias, politica.PrazoMaximoAprovadoDias),
                EntradaMinimaPercentual = 0,
                VendaSomenteAVista = false
            };
        }

        if (decisaoFinal == DecisaoFinal.APROVADO_COM_RESSALVAS)
        {
            return new AcaoComercialDto
            {
                Decisao = decisaoFinal.ToString(),
                LimiteCreditoSugerido = Math.Min(request.ValorPedido, politica.LimiteBase * 0.5m),
                PrazoMaximoDias = Math.Min(request.PrazoDesejadoDias, politica.PrazoMaximoRessalvaDias),
                EntradaMinimaPercentual = politica.EntradaMinimaRessalvaPercentual,
                VendaSomenteAVista = false
            };
        }

        return new AcaoComercialDto
        {
            Decisao = decisaoFinal.ToString(),
            LimiteCreditoSugerido = 0,
            PrazoMaximoDias = 0,
            EntradaMinimaPercentual = 100,
            VendaSomenteAVista = true
        };
    }

    private static List<string> MontarMotivosPrincipais(SerasaData serasa, List<string> pendencias, QualificacaoRequest request)
    {
        var motivos = new List<string>
        {
            $"Score Serasa em faixa {serasa.Faixa} ({serasa.Score:0})."
        };

        if (!request.ClienteNovo)
        {
            motivos.Add($"Atraso interno em 90 dias: {request.DiasAtrasoInterno90d ?? 0} dia(s).");
        }

        if (pendencias.Count > 0)
        {
            motivos.Add(pendencias[0]);
        }

        return motivos.Take(3).ToList();
    }

    private static string MontarRecomendacaoAgente(string resumoExplicacao, AcaoComercialDto acaoComercial, DecisaoFinal decisaoFinal)
    {
        if (!string.IsNullOrWhiteSpace(resumoExplicacao))
        {
            return resumoExplicacao;
        }

        return decisaoFinal switch
        {
            DecisaoFinal.APROVADO => "Recomendo seguir com a venda nas condicoes padrao, mantendo monitoramento da carteira.",
            DecisaoFinal.APROVADO_COM_RESSALVAS => $"Recomendo vender com cautela: limite sugerido de {acaoComercial.LimiteCreditoSugerido:0.##}, prazo de ate {acaoComercial.PrazoMaximoDias} dias e entrada minima de {acaoComercial.EntradaMinimaPercentual}%.",
            _ => "Recomendo nao liberar credito a prazo neste momento e ofertar apenas venda a vista ou antecipada."
        };
    }

    private PoliticaNegocio ObterPolitica(string politicaId)
    {
        if (_politicas.TryGetValue(politicaId, out var politica))
        {
            return politica;
        }

        return _politicas["B2B_BENS_CONSUMO_PADRAO_V1"];
    }

    private static List<string> MontarPendencias(CertidoesData certidoes)
    {
        return certidoes
            .ToPairs()
            .Where(item => item.Value.Equals("POSITIVA", StringComparison.OrdinalIgnoreCase))
            .Select(item => $"CND {item.Key} em status POSITIVA (com restricao).")
            .ToList();
    }

    private sealed record PoliticaNegocio(
        string Id,
        decimal ScoreMinAprovar,
        decimal ScoreMinRessalva,
        int MaxDiasAtrasoInterno,
        decimal LimiteBase,
        int PrazoMaximoAprovadoDias,
        int PrazoMaximoRessalvaDias,
        int EntradaMinimaRessalvaPercentual,
        bool BloqueiaComRestricaoAtiva);

    private static Dictionary<string, PoliticaNegocio> ConstruirPoliticas(PoliticasNegocioOptions options)
    {
        var configuradas = options.Itens
            .Where(item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(item => new PoliticaNegocio(
                item.Id,
                item.ScoreMinAprovar,
                item.ScoreMinRessalva,
                item.MaxDiasAtrasoInterno,
                item.LimiteBase,
                item.PrazoMaximoAprovadoDias,
                item.PrazoMaximoRessalvaDias,
                item.EntradaMinimaRessalvaPercentual,
                item.BloqueiaComRestricaoAtiva))
            .ToDictionary(item => item.Id, StringComparer.OrdinalIgnoreCase);

        if (configuradas.Count > 0)
        {
            return configuradas;
        }

        return new Dictionary<string, PoliticaNegocio>(StringComparer.OrdinalIgnoreCase)
        {
            ["B2B_ALIMENTOS_CONSERVADORA_V1"] = new PoliticaNegocio(
                "B2B_ALIMENTOS_CONSERVADORA_V1",
                780m,
                620m,
                5,
                20000m,
                21,
                14,
                25,
                true),
            ["B2B_BENS_CONSUMO_PADRAO_V1"] = new PoliticaNegocio(
                "B2B_BENS_CONSUMO_PADRAO_V1",
                740m,
                580m,
                10,
                30000m,
                28,
                21,
                20,
                true)
        };
    }
}

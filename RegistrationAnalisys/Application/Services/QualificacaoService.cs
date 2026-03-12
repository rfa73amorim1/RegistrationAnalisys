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
        var tipoPapel = ParseTipoPapel(request.Papel);
        var serasa = await _serasaSource.ConsultarAsync(request.Cnpj, cancellationToken);
        var certidoes = await _certidoesSource.ConsultarAsync(request.Cnpj, cancellationToken);

        var consolidado = new ConsolidadoData
        {
            Serasa = serasa,
            Certidoes = certidoes
        };

        var politica = ObterPolitica(request.PoliticaId, tipoPapel);
        var evidencias = MontarEvidencias(consolidado, request, politica, tipoPapel);
        var pendencias = MontarPendencias(consolidado.Certidoes);
        var decisaoFinal = CalcularDecisao(consolidado.Serasa.Score, pendencias.Any(), request, politica);
        var explicacao = await _explicador.GerarExplicacaoAsync(decisaoFinal, tipoPapel, consolidado.Serasa.Score, evidencias, pendencias, cancellationToken);
        var acaoComercial = tipoPapel == TipoPapel.CLIENTE ? MontarAcaoComercial(decisaoFinal, request, politica) : null;
        var acaoOnboardingFornecedor = tipoPapel == TipoPapel.FORNECEDOR
            ? MontarAcaoOnboardingFornecedor(decisaoFinal, pendencias, explicacao)
            : null;
        var motivosPrincipais = MontarMotivosPrincipais(consolidado.Serasa, pendencias, request);

        return new QualificacaoResponse
        {
            Decisao = decisaoFinal.ToString(),
            RecomendacaoAgente = MontarRecomendacaoAgente(explicacao.Resumo, acaoComercial, decisaoFinal, tipoPapel, pendencias),
            AcaoComercial = acaoComercial,
            AcaoOnboardingFornecedor = acaoOnboardingFornecedor,
            MotivosPrincipais = motivosPrincipais
        };
    }

    private static DecisaoFinal CalcularDecisao(decimal score, bool possuiPendenciasCertidoes, QualificacaoRequest request, PoliticaNegocio politica)
    {
        if (possuiPendenciasCertidoes && politica.BloqueiaComRestricaoAtiva)
        {
            return DecisaoFinal.REPROVADO;
        }

        if (!request.RelacionamentoNovo && politica.MaxDiasAtrasoInterno.HasValue && (request.DiasAtrasoInterno90d ?? 0) > politica.MaxDiasAtrasoInterno.Value)
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

    private static List<string> MontarEvidencias(ConsolidadoData consolidado, QualificacaoRequest request, PoliticaNegocio politica, TipoPapel tipoPapel)
    {
        var evidencias = new List<string>
        {
            $"Score Serasa: {consolidado.Serasa.Score:0} (faixa {consolidado.Serasa.Faixa}).",
            $"Endividamento informado: {consolidado.Serasa.Endividamento:0.##}%.",
            $"Quantidade de atrasos: {consolidado.Serasa.Atrasos}.",
            $"Politica aplicada: {politica.Id}."
        };

        if (tipoPapel == TipoPapel.CLIENTE)
        {
            evidencias.Add($"Valor da operacao: {request.ValorOperacao:0.##}.");
            evidencias.Add($"Prazo da operacao: {request.PrazoOprecaoDias} dias.");
        }

        return evidencias;
    }

    private static AcaoComercialDto MontarAcaoComercial(DecisaoFinal decisaoFinal, QualificacaoRequest request, PoliticaNegocio politica)
    {
        var limiteBase = politica.LimiteBase ?? 0m;
        var prazoAprovado = politica.PrazoMaximoAprovadoDias ?? 0;
        var prazoRessalva = politica.PrazoMaximoRessalvaDias ?? 0;
        var entradaRessalva = politica.EntradaMinimaRessalvaPercentual ?? 0;

        if (decisaoFinal == DecisaoFinal.APROVADO)
        {
            return new AcaoComercialDto
            {
                Decisao = decisaoFinal.ToString(),
                LimiteCreditoSugerido = limiteBase,
                PrazoMaximoDias = Math.Min(request.PrazoOprecaoDias, prazoAprovado),
                EntradaMinimaPercentual = 0,
                VendaSomenteAVista = false
            };
        }

        if (decisaoFinal == DecisaoFinal.APROVADO_COM_RESSALVAS)
        {
            return new AcaoComercialDto
            {
                Decisao = decisaoFinal.ToString(),
                LimiteCreditoSugerido = Math.Min(request.ValorOperacao, limiteBase * 0.5m),
                PrazoMaximoDias = Math.Min(request.PrazoOprecaoDias, prazoRessalva),
                EntradaMinimaPercentual = entradaRessalva,
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

    private static AcaoOnboardingFornecedorDto MontarAcaoOnboardingFornecedor(
        DecisaoFinal decisaoFinal,
        List<string> pendencias,
        ExplicacaoQualificacao explicacao)
    {
        var baseAcao = decisaoFinal switch
        {
            DecisaoFinal.APROVADO => new AcaoOnboardingFornecedorDto
            {
                HabilitarCadastro = true,
                AcaoRecomendada = "Habilitar fornecedor com monitoramento padrao de compliance.",
                Pendencias = new List<string>(),
                NivelRisco = "BAIXO"
            },
            DecisaoFinal.APROVADO_COM_RESSALVAS => new AcaoOnboardingFornecedorDto
            {
                HabilitarCadastro = true,
                AcaoRecomendada = pendencias.Count > 0
                    ? "Habilitar fornecedor com ressalvas e revisao de pendencias."
                    : "Habilitar fornecedor com ressalvas e monitoramento reforcado de compliance.",
                Pendencias = pendencias,
                NivelRisco = "MEDIO"
            },
            _ => new AcaoOnboardingFornecedorDto
            {
                HabilitarCadastro = false,
                AcaoRecomendada = "Nao habilitar fornecedor ate regularizacao das pendencias.",
                Pendencias = pendencias,
                NivelRisco = "ALTO"
            }
        };

        var sugestaoIa = SelecionarSugestaoIaFornecedor(explicacao, pendencias);
        if (!string.IsNullOrWhiteSpace(sugestaoIa))
        {
            baseAcao.AcaoRecomendada = sugestaoIa;
        }

        return baseAcao;
    }

    private static string? SelecionarSugestaoIaFornecedor(ExplicacaoQualificacao explicacao, IReadOnlyCollection<string> pendencias)
    {
        var sugestao = explicacao.Recomendacoes.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();
        if (string.IsNullOrWhiteSpace(sugestao))
        {
            return null;
        }

        if (pendencias.Count == 0 && ContemTermosDePendencia(sugestao))
        {
            return null;
        }

        return sugestao;
    }

    private static List<string> MontarMotivosPrincipais(SerasaData serasa, List<string> pendencias, QualificacaoRequest request)
    {
        var motivos = new List<string>
        {
            $"Score Serasa em faixa {serasa.Faixa} ({serasa.Score:0})."
        };

        if (!request.RelacionamentoNovo)
        {
            motivos.Add($"Atraso interno em 90 dias: {request.DiasAtrasoInterno90d ?? 0} dia(s).");
        }

        if (pendencias.Count > 0)
        {
            motivos.Add(pendencias[0]);
        }

        return motivos.Take(3).ToList();
    }

    private static string MontarRecomendacaoAgente(
        string resumoExplicacao,
        AcaoComercialDto? acaoComercial,
        DecisaoFinal decisaoFinal,
        TipoPapel tipoPapel,
        IReadOnlyCollection<string> pendencias)
    {
        if (tipoPapel == TipoPapel.FORNECEDOR && !string.IsNullOrWhiteSpace(resumoExplicacao))
        {
            var possuiPendencias = pendencias.Count > 0;
            if (!possuiPendencias && ContemTermosDePendencia(resumoExplicacao))
            {
                return decisaoFinal switch
                {
                    DecisaoFinal.APROVADO => "Recomendo habilitar o fornecedor com monitoramento padrao de compliance.",
                    DecisaoFinal.APROVADO_COM_RESSALVAS => "Recomendo habilitar o fornecedor com ressalvas e monitoramento reforcado de compliance.",
                    _ => "Recomendo nao habilitar o fornecedor neste momento devido ao risco identificado."
                };
            }
        }

        if (!string.IsNullOrWhiteSpace(resumoExplicacao))
        {
            return resumoExplicacao;
        }

        if (tipoPapel == TipoPapel.FORNECEDOR)
        {
            return decisaoFinal switch
            {
                DecisaoFinal.APROVADO => "Recomendo habilitar o fornecedor com monitoramento padrao de compliance.",
                DecisaoFinal.APROVADO_COM_RESSALVAS => "Recomendo habilitar o fornecedor com ressalvas, acompanhando pendencias e compliance.",
                _ => "Recomendo nao habilitar o fornecedor neste momento devido ao risco identificado."
            };
        }

        if (acaoComercial is null)
        {
            return "Recomendo revisar a operacao comercial com base nas regras definidas.";
        }

        return decisaoFinal switch
        {
            DecisaoFinal.APROVADO => "Recomendo seguir com a venda nas condicoes padrao, mantendo monitoramento da carteira.",
            DecisaoFinal.APROVADO_COM_RESSALVAS => $"Recomendo vender com cautela: limite sugerido de {acaoComercial.LimiteCreditoSugerido:0.##}, prazo de ate {acaoComercial.PrazoMaximoDias} dias e entrada minima de {acaoComercial.EntradaMinimaPercentual}%.",
            _ => "Recomendo nao liberar credito a prazo neste momento e ofertar apenas venda a vista ou antecipada."
        };
    }

    private static bool ContemTermosDePendencia(string texto)
    {
        var normalizado = texto.Trim().ToLowerInvariant();
        return normalizado.Contains("pendenc")
            || normalizado.Contains("restric")
            || normalizado.Contains("regulariz");
    }

    private PoliticaNegocio ObterPolitica(string politicaId, TipoPapel tipoPapelSolicitado)
    {
        if (!_politicas.TryGetValue(politicaId, out var politica))
        {
            throw new ArgumentException($"politicaId '{politicaId}' nao encontrada.");
        }

        if (politica.TipoPapel != tipoPapelSolicitado)
        {
            throw new ArgumentException($"Politica informada e do tipo {politica.TipoPapel}, mas papel solicitado e {tipoPapelSolicitado}.");
        }

        return politica;
    }

    private static TipoPapel ParseTipoPapel(string? valor)
    {
        if (Enum.TryParse<TipoPapel>(valor, ignoreCase: true, out var tipoPapel))
        {
            return tipoPapel;
        }

        return TipoPapel.CLIENTE;
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
        TipoPapel TipoPapel,
        decimal ScoreMinAprovar,
        decimal ScoreMinRessalva,
        int? MaxDiasAtrasoInterno,
        decimal? LimiteBase,
        int? PrazoMaximoAprovadoDias,
        int? PrazoMaximoRessalvaDias,
        int? EntradaMinimaRessalvaPercentual,
        bool BloqueiaComRestricaoAtiva);

    private static Dictionary<string, PoliticaNegocio> ConstruirPoliticas(PoliticasNegocioOptions options)
    {
        var configuradas = new Dictionary<string, PoliticaNegocio>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in options.Itens.Where(item => !string.IsNullOrWhiteSpace(item.Id)))
        {
            if (!Enum.TryParse<TipoPapel>(item.TipoPapel, ignoreCase: true, out var tipoPapel))
            {
                throw new InvalidOperationException($"Politica '{item.Id}' com TipoPapel invalido. Use CLIENTE ou FORNECEDOR.");
            }

            if (item.ScoreMinAprovar <= 0 || item.ScoreMinRessalva <= 0 || item.ScoreMinAprovar < item.ScoreMinRessalva)
            {
                throw new InvalidOperationException($"Politica '{item.Id}' com faixas de score invalidas.");
            }

            if (tipoPapel == TipoPapel.CLIENTE)
            {
                if (!item.LimiteBase.HasValue || !item.PrazoMaximoAprovadoDias.HasValue || !item.PrazoMaximoRessalvaDias.HasValue || !item.EntradaMinimaRessalvaPercentual.HasValue)
                {
                    throw new InvalidOperationException($"Politica '{item.Id}' do tipo CLIENTE exige campos comerciais preenchidos.");
                }
            }
            else
            {
                if (item.LimiteBase.HasValue || item.PrazoMaximoAprovadoDias.HasValue || item.PrazoMaximoRessalvaDias.HasValue || item.EntradaMinimaRessalvaPercentual.HasValue)
                {
                    throw new InvalidOperationException($"Politica '{item.Id}' do tipo FORNECEDOR nao deve conter campos comerciais.");
                }
            }

            configuradas[item.Id] = new PoliticaNegocio(
                item.Id,
                tipoPapel,
                item.ScoreMinAprovar,
                item.ScoreMinRessalva,
                item.MaxDiasAtrasoInterno,
                item.LimiteBase,
                item.PrazoMaximoAprovadoDias,
                item.PrazoMaximoRessalvaDias,
                item.EntradaMinimaRessalvaPercentual,
                item.BloqueiaComRestricaoAtiva);
        }

        if (configuradas.Count > 0)
        {
            return configuradas;
        }

        return new Dictionary<string, PoliticaNegocio>(StringComparer.OrdinalIgnoreCase)
        {
            ["B2B_ALIMENTOS_CONSERVADORA_V1"] = new PoliticaNegocio(
                "B2B_ALIMENTOS_CONSERVADORA_V1",
                TipoPapel.CLIENTE,
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
                TipoPapel.CLIENTE,
                740m,
                580m,
                10,
                30000m,
                28,
                21,
                20,
                true),
            ["FORN_ALIMENTOS_CONSERVADORA_V1"] = new PoliticaNegocio(
                "FORN_ALIMENTOS_CONSERVADORA_V1",
                TipoPapel.FORNECEDOR,
                780m,
                620m,
                5,
                null,
                null,
                null,
                null,
                true)
        };
    }
}

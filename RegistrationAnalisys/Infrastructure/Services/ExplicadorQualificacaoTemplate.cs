using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class ExplicadorQualificacaoTemplate : IExplicadorQualificacao
{
    public Task<ExplicacaoQualificacao> GerarExplicacaoAsync(
        DecisaoFinal decisaoFinal,
        TipoPapel tipoPapel,
        decimal scoreFinanceiro,
        IReadOnlyCollection<string> evidencias,
        IReadOnlyCollection<string> pendencias,
        CancellationToken cancellationToken = default)
    {
        var seed = BuildSeed(decisaoFinal, scoreFinanceiro, evidencias.Count, pendencias.Count);
        var resumo = EscolherFraseAbertura(decisaoFinal, tipoPapel, scoreFinanceiro, seed);
        var fundamentos = MontarFundamentos(decisaoFinal, scoreFinanceiro, evidencias, pendencias);
        var recomendacoes = MontarRecomendacoes(decisaoFinal, tipoPapel, seed);

        var frases = new List<string> { resumo };
        if (fundamentos.Count > 0)
        {
            frases.Add(string.Join(" ", fundamentos));
        }

        if (recomendacoes.Count > 0)
        {
            frases.Add(string.Join(" ", recomendacoes));
        }

        frases.Add("A avaliacao considera apenas os dados tecnicos retornados pelas fontes mockadas.");

        var result = new ExplicacaoQualificacao
        {
            Resumo = resumo,
            Fundamentos = fundamentos,
            Recomendacoes = recomendacoes,
            Origem = "FALLBACK_TEMPLATE"
        };

        return Task.FromResult(result);
    }

    private static List<string> MontarFundamentos(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, IReadOnlyCollection<string> evidencias, IReadOnlyCollection<string> pendencias)
    {
        if (decisaoFinal == DecisaoFinal.REPROVADO)
        {
            var fundamentos = new List<string>();

            if (pendencias.Count > 0)
            {
                fundamentos.Add($"Foram identificadas pendencias: {string.Join("; ", pendencias)}.");
            }

            if (scoreFinanceiro < 800m)
            {
                fundamentos.Add($"O score financeiro ({scoreFinanceiro:0}) esta abaixo do patamar considerado aceitavel (800).");
            }

            if (fundamentos.Count > 0)
            {
                return fundamentos;
            }
        }

        if (pendencias.Count > 0)
        {
            return new List<string>
            {
                $"Foram identificadas pendencias: {string.Join("; ", pendencias)}."
            };
        }

        if (evidencias.Count > 0)
        {
            return new List<string>
            {
                $"Evidencias principais: {string.Join("; ", evidencias.Take(2))}."
            };
        }

        return new List<string>();
    }

    private static int BuildSeed(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, int evidenciasCount, int pendenciasCount)
    {
        return (int)decisaoFinal + (int)(scoreFinanceiro * 10) + (evidenciasCount * 7) + (pendenciasCount * 11);
    }

    private static string EscolherFraseAbertura(DecisaoFinal decisaoFinal, TipoPapel tipoPapel, decimal scoreFinanceiro, int seed)
    {
        if (tipoPapel == TipoPapel.FORNECEDOR)
        {
            var opcoesFornecedor = decisaoFinal switch
            {
                DecisaoFinal.APROVADO => new[]
                {
                    $"Recomenda-se habilitar o cadastro do fornecedor com monitoramento regular de compliance.",
                    $"A analise de risco indica habilitacao do fornecedor com score financeiro {scoreFinanceiro:0}."
                },
                DecisaoFinal.APROVADO_COM_RESSALVAS => new[]
                {
                    "Recomenda-se habilitar o fornecedor com ressalvas e acompanhamento de pendencias de compliance.",
                    $"A analise sustenta habilitacao com ressalvas para onboarding do fornecedor (score {scoreFinanceiro:0})."
                },
                _ => new[]
                {
                    "Recomenda-se nao habilitar o cadastro do fornecedor neste momento por risco elevado.",
                    "A analise de onboarding indica bloqueio temporario do fornecedor no cenario atual."
                }
            };

            return opcoesFornecedor[Math.Abs(seed) % opcoesFornecedor.Length];
        }

        var opcoes = decisaoFinal switch
        {
            DecisaoFinal.APROVADO => new[]
            {
                $"Recomenda-se aprovar a operacao nas condicoes padrao, com score financeiro {scoreFinanceiro:0}.",
                $"A analise comercial indica APROVADO para a venda com score financeiro {scoreFinanceiro:0}."
            },
            DecisaoFinal.APROVADO_COM_RESSALVAS => new[]
            {
                $"Recomenda-se aprovar com ressalvas, adotando limite e prazo mais conservadores para esta venda.",
                $"A analise comercial sustenta APROVADO_COM_RESSALVAS com score financeiro {scoreFinanceiro:0}."
            },
            _ => new[]
            {
                $"Recomenda-se nao aprovar credito a prazo nesta operacao com score financeiro {scoreFinanceiro:0}.",
                $"A analise comercial indica REPROVADO para venda a prazo no cenario atual."
            }
        };

        return opcoes[Math.Abs(seed) % opcoes.Length];
    }

    private static List<string> MontarRecomendacoes(DecisaoFinal decisaoFinal, TipoPapel tipoPapel, int seed)
    {
        if (tipoPapel == TipoPapel.FORNECEDOR)
        {
            if (decisaoFinal == DecisaoFinal.APROVADO_COM_RESSALVAS)
            {
                var opcoesFornecedor = new[]
                {
                    "Manter cadastro ativo com revisao documental periodica e acompanhamento de restricoes.",
                    "Habilitar com ressalvas e exigir regularizacao de pendencias apontadas no onboarding.",
                    "Aplicar monitoramento reforcado de compliance durante o periodo inicial de homologacao."
                };

                return new List<string> { opcoesFornecedor[Math.Abs(seed) % opcoesFornecedor.Length] };
            }

            if (decisaoFinal == DecisaoFinal.REPROVADO)
            {
                return new List<string>
                {
                    "Suspender a habilitacao ate a regularizacao das pendencias e reavaliar em nova consulta."
                };
            }

            return new List<string>();
        }

        if (decisaoFinal == DecisaoFinal.APROVADO_COM_RESSALVAS)
        {
            var opcoes = new[]
            {
                "Aplicar entrada minima e reduzir prazo para mitigar risco nesta venda.",
                "Liberar credito com limite reduzido e acompanhamento da carteira apos a venda.",
                "Condicionar a aprovacao a condicoes comerciais mais conservadoras."
            };

            return new List<string> { opcoes[Math.Abs(seed) % opcoes.Length] };
        }

        if (decisaoFinal == DecisaoFinal.REPROVADO)
        {
            return new List<string>
            {
                "Para esta operacao, priorizar venda a vista ou pagamento antecipado."
            };
        }

        return new List<string>();
    }
}

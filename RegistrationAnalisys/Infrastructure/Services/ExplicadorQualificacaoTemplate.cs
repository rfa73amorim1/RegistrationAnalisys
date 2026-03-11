using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class ExplicadorQualificacaoTemplate : IExplicadorQualificacao
{
    public Task<ExplicacaoQualificacao> GerarExplicacaoAsync(
        DecisaoFinal decisaoFinal,
        decimal scoreFinanceiro,
        IReadOnlyCollection<string> evidencias,
        IReadOnlyCollection<string> pendencias,
        CancellationToken cancellationToken = default)
    {
        var seed = BuildSeed(decisaoFinal, scoreFinanceiro, evidencias.Count, pendencias.Count);
        var resumo = EscolherFraseAbertura(decisaoFinal, scoreFinanceiro, seed);
        var fundamentos = MontarFundamentos(decisaoFinal, scoreFinanceiro, evidencias, pendencias);
        var recomendacoes = MontarRecomendacoes(decisaoFinal, seed);

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
            Recomendacoes = recomendacoes
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

    private static string EscolherFraseAbertura(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, int seed)
    {
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

    private static List<string> MontarRecomendacoes(DecisaoFinal decisaoFinal, int seed)
    {
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

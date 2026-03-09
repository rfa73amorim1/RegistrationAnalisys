using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class ExplicadorQualificacaoTemplate : IExplicadorQualificacao
{
    public ExplicacaoQualificacao GerarExplicacao(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, IReadOnlyCollection<string> evidencias, IReadOnlyCollection<string> pendencias)
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

        return new ExplicacaoQualificacao
        {
            Resumo = resumo,
            Fundamentos = fundamentos,
            Recomendacoes = recomendacoes
        };
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
                $"A decisao final foi APROVADO com score financeiro {scoreFinanceiro:0}.",
                $"Com score financeiro {scoreFinanceiro:0}, o resultado da analise e APROVADO."
            },
            DecisaoFinal.APROVADO_COM_RESSALVAS => new[]
            {
                $"A empresa foi classificada como APROVADO_COM_RESSALVAS com score financeiro {scoreFinanceiro:0}.",
                $"O score financeiro {scoreFinanceiro:0} sustenta APROVADO_COM_RESSALVAS, com necessidade de monitoramento."
            },
            _ => new[]
            {
                $"A decisao final foi REPROVADO com score financeiro {scoreFinanceiro:0}.",
                $"Com score financeiro {scoreFinanceiro:0}, o enquadramento final ficou em REPROVADO."
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
                "Como mitigacao, considere reduzir o prazo medio de recebimento e priorizar operacoes com adiantamento a vista.",
                "Recomenda-se ajustar a politica comercial para encurtar prazos de recebimento e ampliar incentivos para pagamento a vista.",
                "Para reduzir risco, vale reforcar garantias e concentrar vendas em condicoes com menor prazo financeiro."
            };

            return new List<string> { opcoes[Math.Abs(seed) % opcoes.Length] };
        }

        if (decisaoFinal == DecisaoFinal.REPROVADO)
        {
            return new List<string>
            {
                "E aconselhavel regularizar as pendencias apontadas antes de submeter uma nova analise."
            };
        }

        return new List<string>();
    }
}

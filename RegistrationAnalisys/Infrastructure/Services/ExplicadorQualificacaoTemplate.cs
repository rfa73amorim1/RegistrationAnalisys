using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class ExplicadorQualificacaoTemplate : IExplicadorQualificacao
{
    public string GerarExplicacao(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, IReadOnlyCollection<string> evidencias, IReadOnlyCollection<string> pendencias)
    {
        var frases = new List<string>
        {
            $"A decisao final foi {decisaoFinal} com score financeiro {scoreFinanceiro:0.0}."
        };

        if (pendencias.Count > 0)
        {
            frases.Add($"Foram identificadas pendencias: {string.Join("; ", pendencias)}.");
        }
        else if (evidencias.Count > 0)
        {
            frases.Add($"Evidencias principais: {string.Join("; ", evidencias.Take(2))}.");
        }

        frases.Add("A avaliacao considera apenas os dados tecnicos retornados pelas fontes mockadas.");
        return string.Join(" ", frases.Take(3));
    }
}

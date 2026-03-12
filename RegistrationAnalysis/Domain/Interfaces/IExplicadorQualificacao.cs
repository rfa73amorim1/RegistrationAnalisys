using RegistrationAnalysis.Domain.Enums;
using RegistrationAnalysis.Domain.Models;

namespace RegistrationAnalysis.Domain.Interfaces;

public interface IExplicadorQualificacao
{
    Task<ExplicacaoQualificacao> GerarExplicacaoAsync(
        DecisaoFinal decisaoFinal,
        TipoPapel tipoPapel,
        decimal scoreFinanceiro,
        IReadOnlyCollection<string> evidencias,
        IReadOnlyCollection<string> pendencias,
        CancellationToken cancellationToken = default);
}

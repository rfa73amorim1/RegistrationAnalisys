using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Domain.Interfaces;

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

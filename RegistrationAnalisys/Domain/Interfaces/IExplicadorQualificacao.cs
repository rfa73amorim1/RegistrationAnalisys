using RegistrationAnalisys.Domain.Enums;

namespace RegistrationAnalisys.Domain.Interfaces;

public interface IExplicadorQualificacao
{
    string GerarExplicacao(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, IReadOnlyCollection<string> evidencias, IReadOnlyCollection<string> pendencias);
}

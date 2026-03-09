using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Domain.Interfaces;

public interface IExplicadorQualificacao
{
    ExplicacaoQualificacao GerarExplicacao(DecisaoFinal decisaoFinal, decimal scoreFinanceiro, IReadOnlyCollection<string> evidencias, IReadOnlyCollection<string> pendencias);
}

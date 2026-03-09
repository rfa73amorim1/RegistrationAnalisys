using RegistrationAnalisys.Application.DTOs;

namespace RegistrationAnalisys.Application.Services;

public interface IQualificacaoService
{
    Task<QualificacaoResponse> QualificarAsync(string cnpj, bool includeExplanation = true, CancellationToken cancellationToken = default);
}

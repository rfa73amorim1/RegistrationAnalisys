using RegistrationAnalisys.Application.DTOs;

namespace RegistrationAnalisys.Application.Services;

public interface IQualificacaoService
{
    Task<QualificacaoResponse> QualificarAsync(QualificacaoRequest request, CancellationToken cancellationToken = default);
}

using RegistrationAnalysis.Application.DTOs;

namespace RegistrationAnalysis.Application.Services;

public interface IQualificacaoService
{
    Task<QualificacaoResponse> QualificarAsync(QualificacaoRequest request, CancellationToken cancellationToken = default);
}

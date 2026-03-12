using RegistrationAnalysis.Domain.Models;

namespace RegistrationAnalysis.Domain.Interfaces;

public interface ICertidoesSource
{
    Task<CertidoesData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}

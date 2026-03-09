using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Domain.Interfaces;

public interface ICertidoesSource
{
    Task<CertidoesData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}

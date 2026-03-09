using RegistrationAnalisys.Domain.Models;

namespace RegistrationAnalisys.Domain.Interfaces;

public interface ISerasaSource
{
    Task<SerasaData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}

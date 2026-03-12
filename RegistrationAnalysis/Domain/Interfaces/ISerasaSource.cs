using RegistrationAnalysis.Domain.Models;

namespace RegistrationAnalysis.Domain.Interfaces;

public interface ISerasaSource
{
    Task<SerasaData> ConsultarAsync(string cnpj, CancellationToken cancellationToken = default);
}

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RegistrationAnalisys.Domain.Enums;
using RegistrationAnalisys.Domain.Interfaces;
using RegistrationAnalisys.Domain.Models;
using RegistrationAnalisys.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace RegistrationAnalisys.Infrastructure.Services;

public sealed class ExplicadorQualificacaoAzure : IExplicadorQualificacao
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AzureOpenAIOptions> _options;
    private readonly ExplicadorQualificacaoTemplate _fallback;
    private readonly ILogger<ExplicadorQualificacaoAzure> _logger;

    public ExplicadorQualificacaoAzure(
        IHttpClientFactory httpClientFactory,
        IOptions<AzureOpenAIOptions> options,
        ExplicadorQualificacaoTemplate fallback,
        ILogger<ExplicadorQualificacaoAzure> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<ExplicacaoQualificacao> GerarExplicacaoAsync(
        DecisaoFinal decisaoFinal,
        decimal scoreFinanceiro,
        IReadOnlyCollection<string> evidencias,
        IReadOnlyCollection<string> pendencias,
        CancellationToken cancellationToken = default)
    {
        var config = _options.Value;
        if (!config.Enabled || string.IsNullOrWhiteSpace(config.Endpoint) || string.IsNullOrWhiteSpace(config.ApiKey) || string.IsNullOrWhiteSpace(config.DeploymentName))
        {
            return await _fallback.GerarExplicacaoAsync(decisaoFinal, scoreFinanceiro, evidencias, pendencias, cancellationToken);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("AzureOpenAI");
            var endpoint = config.Endpoint.TrimEnd('/');
            var url = $"{endpoint}/openai/deployments/{config.DeploymentName}/chat/completions?api-version={config.ApiVersion}";

            var requestPayload = new
            {
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "Voce gera recomendacoes comerciais de credito B2B em portugues do Brasil. O foco e orientar o time de vendas sobre aprovar, aprovar com ressalvas ou reprovar a prazo. Nao oriente o consultado a melhorar score. Responda exclusivamente em JSON valido com as chaves: resumo, fundamentos (array), recomendacoes (array). Nao invente fatos fora das evidencias e pendencias recebidas."
                    },
                    new
                    {
                        role = "user",
                        content = BuildUserPrompt(decisaoFinal, scoreFinanceiro, evidencias, pendencias)
                    }
                },
                temperature = config.Temperature,
                max_tokens = config.MaxOutputTokens
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Add("api-key", config.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestPayload), Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure OpenAI retornou status {StatusCode}. Fallback para template.", response.StatusCode);
                return await _fallback.GerarExplicacaoAsync(decisaoFinal, scoreFinanceiro, evidencias, pendencias, cancellationToken);
            }

            var completion = JsonSerializer.Deserialize<AzureChatCompletionResponse>(responseBody, JsonOptions);
            var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Resposta do Azure OpenAI sem conteudo. Fallback para template.");
                return await _fallback.GerarExplicacaoAsync(decisaoFinal, scoreFinanceiro, evidencias, pendencias, cancellationToken);
            }

            var explicacao = TryParseExplicacao(content);
            if (explicacao is not null)
            {
                return explicacao;
            }

            _logger.LogWarning("Nao foi possivel interpretar JSON do Azure OpenAI. Fallback para template.");
            return await _fallback.GerarExplicacaoAsync(decisaoFinal, scoreFinanceiro, evidencias, pendencias, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar Azure OpenAI. Fallback para template.");
            return await _fallback.GerarExplicacaoAsync(decisaoFinal, scoreFinanceiro, evidencias, pendencias, cancellationToken);
        }
    }

    private static string BuildUserPrompt(
        DecisaoFinal decisaoFinal,
        decimal scoreFinanceiro,
        IReadOnlyCollection<string> evidencias,
        IReadOnlyCollection<string> pendencias)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Dados da qualificacao:");
        sb.AppendLine($"- decisaoFinal: {decisaoFinal}");
        sb.AppendLine($"- scoreFinanceiro: {scoreFinanceiro:0}");
        sb.AppendLine($"- evidencias: {string.Join(" | ", evidencias)}");
        sb.AppendLine($"- pendencias: {string.Join(" | ", pendencias)}");
        sb.AppendLine();
        sb.AppendLine("Gere JSON com:");
        sb.AppendLine("{");
        sb.AppendLine("  \"resumo\": \"string\",");
        sb.AppendLine("  \"fundamentos\": [\"string\"],");
        sb.AppendLine("  \"recomendacoes\": [\"string\"]");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static ExplicacaoQualificacao? TryParseExplicacao(string rawContent)
    {
        var cleaned = rawContent.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Replace("```", string.Empty, StringComparison.OrdinalIgnoreCase)
                             .Trim();
        }

        var parsed = JsonSerializer.Deserialize<ExplicacaoLlmOutput>(cleaned, JsonOptions);
        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Resumo))
        {
            return null;
        }

        return new ExplicacaoQualificacao
        {
            Resumo = parsed.Resumo,
            Fundamentos = parsed.Fundamentos ?? new List<string>(),
            Recomendacoes = parsed.Recomendacoes ?? new List<string>()
        };
    }

    private sealed class AzureChatCompletionResponse
    {
        public List<Choice>? Choices { get; set; }
    }

    private sealed class Choice
    {
        public Message? Message { get; set; }
    }

    private sealed class Message
    {
        public string? Content { get; set; }
    }

    private sealed class ExplicacaoLlmOutput
    {
        public string Resumo { get; set; } = string.Empty;
        public List<string>? Fundamentos { get; set; }
        public List<string>? Recomendacoes { get; set; }
    }
}

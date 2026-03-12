namespace RegistrationAnalysis.Infrastructure.Options;

public sealed class AzureOpenAIOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-10-21";
    public int MaxOutputTokens { get; set; } = 280;
    public decimal Temperature { get; set; } = 0.2m;
}

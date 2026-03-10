# Setup rapido de segredos (time)

Este guia e para qualquer dev do time rodar o projeto sem salvar chave no codigo.

## 1) Pre-requisitos

- Ter acesso ao repositorio.
- Ter .NET SDK instalado.
- Ter estes 3 dados do Azure OpenAI:
	- Endpoint
	- ApiKey
	- DeploymentName

## 2) Configurar com user-secrets (recomendado)

Na raiz do repositorio, rode um comando por vez:

```bash
dotnet user-secrets init --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:Enabled" "true" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://SEU-ENDPOINT.openai.azure.com/" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "SUA_KEY_TEMPORARIA" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini" --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

Opcional (normalmente ja vem por default no projeto):

```bash
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-10-21" --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

Validar se salvou:

```bash
dotnet user-secrets list --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

## 3) Rodar e testar

```bash
dotnet restore
dotnet run --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

No Postman:

- `POST https://localhost:7202/qualificacoes?includeExplanation=true`

## 4) Opcao B: variaveis de ambiente

### PowerShell (sessao atual)

```powershell
$env:AzureOpenAI__Enabled="true"
$env:AzureOpenAI__Endpoint="https://SEU-ENDPOINT.openai.azure.com/"
$env:AzureOpenAI__ApiKey="SUA_CHAVE"
$env:AzureOpenAI__DeploymentName="gpt-4o-mini"
$env:AzureOpenAI__ApiVersion="2024-10-21"
```

### PowerShell (persistente)

```powershell
setx AzureOpenAI__Enabled "true"
setx AzureOpenAI__Endpoint "https://SEU-ENDPOINT.openai.azure.com/"
setx AzureOpenAI__ApiKey "SUA_CHAVE"
setx AzureOpenAI__DeploymentName "gpt-4o-mini"
setx AzureOpenAI__ApiVersion "2024-10-21"
```

## 5) Boas praticas

- Nunca commitar chaves reais.
- Se uma chave for exposta, rotacione no Azure imediatamente.
- Para producao, prefira Azure Key Vault / App Service Configuration.

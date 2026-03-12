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
dotnet user-secrets init --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:Enabled" "true" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://SEU-ENDPOINT.openai.azure.com/" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "SUA_KEY_TEMPORARIA" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini" --project RegistrationAnalysis/RegistrationAnalysis.csproj
```

Opcional (normalmente ja vem por default no projeto):

```bash
dotnet user-secrets set "AzureOpenAI:ApiVersion" "2024-10-21" --project RegistrationAnalysis/RegistrationAnalysis.csproj
```

Validar se salvou:

```bash
dotnet user-secrets list --project RegistrationAnalysis/RegistrationAnalysis.csproj
```

## 3) Rodar e testar

```bash
dotnet restore
dotnet run --project RegistrationAnalysis/RegistrationAnalysis.csproj
```

No Postman:

- Importar `RegistrationAnalysis.postman_collection.v2.json`
- Definir `baseUrl` como `http://localhost:5099`
- Testar `POST /qualificacoes`

Request minima valida (exemplo):

```json
{
	"cnpj": "12.345.678/0001-91",
	"valorOperacao": 12000,
	"prazoOprecaoDias": 21,
	"relacionamentoNovo": true,
	"politicaId": "B2B_BENS_CONSUMO_PADRAO_V1"
}
```

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

# RegistrationAnalisys MVP

API Web em .NET 6 para qualificacao de CNPJ com foco em decisao comercial B2B.

Importante: este projeto usa dados simulados (mocks) para PoC. Nao ha consulta real a bureaus externos neste MVP.

## Resumo do produto (PoC)

1. Recebe dados do pedido + contexto do cliente.
2. Aplica uma politica de negocio (segmentada por `politicaId`).
3. Retorna resposta enxuta para o comercial:
   - `recomendacaoAgente`
   - `acaoComercial`
   - `motivosPrincipais`

## Como rodar

```bash
dotnet restore
dotnet run --project RegistrationAnalisys/RegistrationAnalisys.csproj --urls http://localhost:5099
```

Base URL sugerida para Postman:

- `http://localhost:5099`

Swagger foi desabilitado para uso direto com Postman.

## Endpoint

- `POST /qualificacoes`

## Contrato da API

### Request

```json
{
  "cnpj": "12.345.678/0001-91",
  "valorPedido": 12000.00,
  "prazoDesejadoDias": 21,
  "clienteNovo": true,
  "politicaId": "B2B_BENS_CONSUMO_PADRAO_V1"
}
```

Campos:

1. `cnpj` (obrigatorio)
2. `valorPedido` (obrigatorio, > 0)
3. `prazoDesejadoDias` (obrigatorio, > 0)
4. `clienteNovo` (obrigatorio)
5. `diasAtrasoInterno90d` (obrigatorio apenas se `clienteNovo = false`)
6. `politicaId` (obrigatorio)

### Response

```json
{
  "recomendacaoAgente": "Recomenda-se aprovar com ressalvas, adotando limite e prazo mais conservadores para esta venda.",
  "acaoComercial": {
    "decisao": "APROVADO_COM_RESSALVAS",
    "limiteCreditoSugerido": 10000.0,
    "prazoMaximoDias": 14,
    "entradaMinimaPercentual": 25,
    "vendaSomenteAVista": false
  },
  "motivosPrincipais": [
    "Score Serasa em faixa B (640)."
  ]
}
```

## Politicas de negocio (PoC)

As politicas agora sao configuradas no `appsettings.json`, sem necessidade de alterar codigo.

Secao:

- `PoliticasNegocio:Itens`

Politicas atuais:

1. `B2B_ALIMENTOS_CONSERVADORA_V1`
2. `B2B_BENS_CONSUMO_PADRAO_V1`

Cada politica define:

1. `ScoreMinAprovar`
2. `ScoreMinRessalva`
3. `MaxDiasAtrasoInterno`
4. `LimiteBase`
5. `PrazoMaximoAprovadoDias`
6. `PrazoMaximoRessalvaDias`
7. `EntradaMinimaRessalvaPercentual`
8. `BloqueiaComRestricaoAtiva`

## Cenarios de mock

Para facilitar testes, o ultimo digito do CNPJ define os dados mockados:

1. Final `1`: `serasa-ok` + `certidoes-ok`
2. Final `2`: `serasa-ressalva` + `certidoes-ok`
3. Final `3`: `serasa-reprovado` + `certidoes-positiva`

## Collection Postman

Use a collection nova:

- `RegistrationAnalisys.postman_collection.v2.json`

Ela ja inclui cenarios:

1. APROVADO
2. APROVADO_COM_RESSALVAS
3. REPROVADO
4. Erro de validacao (400)

Variavel da collection:

- `baseUrl = http://localhost:5099`

## Regras principais de decisao

1. Se existir CND `POSITIVA` e a politica bloquear restricao ativa => `REPROVADO`
2. Se cliente existente tiver atraso interno acima do limite da politica => `REPROVADO`
3. Se `score >= ScoreMinAprovar` => `APROVADO`
4. Se `score >= ScoreMinRessalva` => `APROVADO_COM_RESSALVAS`
5. Caso contrario => `REPROVADO`

## Explicacao com Azure OpenAI (opcional)

O projeto pode gerar `recomendacaoAgente` usando Azure OpenAI.

Se faltar configuracao, usa fallback local automaticamente (sem quebrar API).

Passo a passo rapido:

```bash
dotnet user-secrets init --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:Enabled" "true" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://SEU-ENDPOINT.openai.azure.com/" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "SUA_KEY_TEMPORARIA" --project RegistrationAnalisys/RegistrationAnalisys.csproj
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini" --project RegistrationAnalisys/RegistrationAnalisys.csproj
```

## Onboarding do time

Para configurar segredos com seguranca em novas maquinas:

1. Guia completo: `docs/setup-secrets.md`
2. Exemplo de variaveis: `.env.example`


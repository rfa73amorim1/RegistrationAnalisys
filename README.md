# RegistrationAnalysis MVP

API Web em .NET 6 para qualificacao de CNPJ com foco em decisao comercial B2B.

Importante: este projeto usa dados simulados (mocks) para PoC. Nao ha consulta real a bureaus externos neste MVP.

## Resumo do produto (PoC)

1. Recebe dados da operacao + contexto do relacionamento.
2. Aplica uma politica de negocio (segmentada por `politicaId`).
3. Retorna nucleo comum de decisao:
  - `decisao`
  - `recomendacaoAgente`
  - `motivosPrincipais`
4. Retorna bloco especifico por papel:
  - CLIENTE: `acaoComercial`
  - FORNECEDOR: `acaoOnboardingFornecedor`

## Como rodar

```bash
dotnet restore
dotnet run --project RegistrationAnalysis/RegistrationAnalysis.csproj --urls http://localhost:5099
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
  "papel": "CLIENTE",
  "valorOperacao": 12000.00,
  "prazoOprecaoDias": 21,
  "relacionamentoNovo": true,
  "politicaId": "B2B_BENS_CONSUMO_PADRAO_V1"
}
```

Campos:

1. `cnpj` (obrigatorio)
2. `papel` (opcional: `CLIENTE` ou `FORNECEDOR`; default = `CLIENTE` para retrocompatibilidade)
3. `valorOperacao` (obrigatorio, > 0)
4. `prazoOprecaoDias` (obrigatorio, > 0)
5. `relacionamentoNovo` (obrigatorio)
6. `diasAtrasoInterno90d` (obrigatorio apenas se `relacionamentoNovo = false`)
7. `politicaId` (obrigatorio)

### Response - CLIENTE

```json
{
  "decisao": "APROVADO_COM_RESSALVAS",
  "recomendacaoAgente": "Recomenda-se aprovar com ressalvas, adotando limite e prazo mais conservadores para esta venda.",
  "acaoComercial": {
    "decisao": "APROVADO_COM_RESSALVAS",
    "limiteCreditoSugerido": 10000.0,
    "prazoMaximoDias": 14,
    "entradaMinimaPercentual": 25,
    "vendaSomenteAVista": false
  },
  "acaoOnboardingFornecedor": null,
  "motivosPrincipais": [
    "Score Serasa em faixa B (640)."
  ]
}
```

### Response - FORNECEDOR

```json
{
  "decisao": "APROVADO_COM_RESSALVAS",
  "recomendacaoAgente": "Recomenda-se habilitar o fornecedor com ressalvas e acompanhamento de pendencias de compliance.",
  "acaoOnboardingFornecedor": {
    "habilitarCadastro": true,
    "acaoRecomendada": "Habilitar fornecedor com ressalvas e revisao de pendencias.",
    "pendencias": [
      "CND estadual em status POSITIVA (com restricao)."
    ],
    "nivelRisco": "MEDIO"
  },
  "acaoComercial": null,
  "motivosPrincipais": [
    "Score Serasa em faixa B (640)."
  ]
}
```

Observacao de contrato:

1. O bloco nao aplicavel por papel e retornado como `null`.
2. Nao ha campo misto (string/objeto) nesses blocos.

## Politicas de negocio (PoC)

As politicas agora sao configuradas no `appsettings.json`, sem necessidade de alterar codigo.

Secao:

- `PoliticasNegocio:Itens`

Politicas atuais:

1. `B2B_ALIMENTOS_CONSERVADORA_V1`
2. `B2B_BENS_CONSUMO_PADRAO_V1`
3. `FORN_ALIMENTOS_CONSERVADORA_V1`

Cada item agora define obrigatoriamente `TipoPapel`:

1. `CLIENTE`
2. `FORNECEDOR`

### Exemplo de politica CLIENTE

```json
{
  "Id": "B2B_ALIMENTOS_CONSERVADORA_V1",
  "TipoPapel": "CLIENTE",
  "ScoreMinAprovar": 780,
  "ScoreMinRessalva": 620,
  "MaxDiasAtrasoInterno": 5,
  "LimiteBase": 20000,
  "PrazoMaximoAprovadoDias": 21,
  "PrazoMaximoRessalvaDias": 14,
  "EntradaMinimaRessalvaPercentual": 25,
  "BloqueiaComRestricaoAtiva": true
}
```

### Exemplo de politica FORNECEDOR (sem campos comerciais)

```json
{
  "Id": "FORN_ALIMENTOS_CONSERVADORA_V1",
  "TipoPapel": "FORNECEDOR",
  "ScoreMinAprovar": 780,
  "ScoreMinRessalva": 620,
  "MaxDiasAtrasoInterno": 5,
  "BloqueiaComRestricaoAtiva": true
}
```

Validacao condicional:

1. Se `TipoPapel = CLIENTE`, campos comerciais sao obrigatorios.
2. Se `TipoPapel = FORNECEDOR`, campos comerciais nao devem ser enviados.

### Compatibilidade entre request e politica

1. O endpoint valida se `papel` da request e compativel com o `TipoPapel` da politica.
2. Em incompatibilidade, retorna 400 com mensagem clara.

Exemplo:

```text
Politica informada e do tipo CLIENTE, mas papel solicitado e FORNECEDOR.
```

## Requests de exemplo por papel

### CLIENTE

```json
{
  "cnpj": "12.345.678/0001-91",
  "papel": "CLIENTE",
  "valorOperacao": 12000.00,
  "prazoOprecaoDias": 21,
  "relacionamentoNovo": true,
  "politicaId": "B2B_BENS_CONSUMO_PADRAO_V1"
}
```

### FORNECEDOR

```json
{
  "cnpj": "12.345.678/0001-92",
  "papel": "FORNECEDOR",
  "valorOperacao": 12000.00,
  "prazoOprecaoDias": 21,
  "relacionamentoNovo": true,
  "politicaId": "FORN_ALIMENTOS_CONSERVADORA_V1"
}
```

Observacao: para manter retrocompatibilidade, requests antigas sem `papel` continuam funcionando como `CLIENTE`.

## Cenarios de mock

Para facilitar testes, o ultimo digito do CNPJ define os dados mockados:

1. Final `1`: `serasa-ok` + `certidoes-ok`
2. Final `2`: `serasa-ressalva` + `certidoes-ok`
3. Final `3`: `serasa-reprovado` + `certidoes-positiva`

## Collection Postman

Para testes de fluxo antigo (sem papel), use:

- `RegistrationAnalysis.postman_collection.v2.json`

Para testes da Opcao B (CLIENTE/FORNECEDOR), use:

- `RegistrationAnalysis.postman_collection.v3.json`

A v3 inclui cenarios:

1. APROVADO
2. APROVADO_COM_RESSALVAS
3. FORNECEDOR
4. Erro de validacao de compatibilidade (400)

Variavel da collection:

- `baseUrl = http://localhost:5099`

## Regras principais de decisao

1. Se existir CND `POSITIVA` e a politica bloquear restricao ativa => `REPROVADO`
2. Se cliente existente tiver atraso interno acima do limite da politica => `REPROVADO`
3. Se `score >= ScoreMinAprovar` => `APROVADO`
4. Se `score >= ScoreMinRessalva` => `APROVADO_COM_RESSALVAS`
5. Caso contrario => `REPROVADO`

Importante: a IA apenas explica/fundamenta a decisao. As regras deterministicas continuam decidindo o resultado.

## Explicacao com Azure OpenAI (opcional)

O projeto pode gerar `recomendacaoAgente` usando Azure OpenAI.

Se faltar configuracao, usa fallback local automaticamente (sem quebrar API).

Passo a passo rapido:

```bash
dotnet user-secrets init --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:Enabled" "true" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://SEU-ENDPOINT.openai.azure.com/" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:ApiKey" "SUA_KEY_TEMPORARIA" --project RegistrationAnalysis/RegistrationAnalysis.csproj
dotnet user-secrets set "AzureOpenAI:DeploymentName" "gpt-4o-mini" --project RegistrationAnalysis/RegistrationAnalysis.csproj
```

## Onboarding do time

Para configurar segredos com seguranca em novas maquinas:

1. Guia completo: `docs/setup-secrets.md`
2. Exemplo de variaveis: `.env.example`


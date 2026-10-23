# DotCruz.Notifications.Delivery.Lambda

> [!WARNING]
> **PROJETO DESCONTINUADO / ARQUIVADO (AWS EXIT - ADR-014)**
> Este componente AWS Lambda foi formalmente substituído pelo **`DotCruz.Notifications.Worker`**, rodando em container Docker on-premises na rede `dotcruz_net` e consumindo mensagens via **RabbitMQ + MassTransit**.
> Para detalhes completos da migração e arquitetura, consulte a `ADR-014-notifications-rabbitmq-worker-aws-exit` no `DotCruz.Vault` e o projeto `DotCruz.Notifications.Worker` em `DotCruz.Notifications`.

Este repositório contém o antigo worker de entrega de notificações *serverless* para o ecossistema **DotCruz**. Mantido exclusivamente para fins de histórico e consulta de implementação anterior.

---

## 🛡️ Decisões de Arquitetura & Segurança

Este projeto foi desenhado com o objetivo de ser um worker de alto desempenho e baixo custo, capaz de processar envios de notificações (E-mail, SMS, Push) utilizando as seguintes decisões técnicas:

### 1. Compilação Native AOT (`PublishAot`)
*   **Decisão**: O projeto compila diretamente para um executável nativo do sistema operacional (`<PublishAot>true</PublishAot>` no arquivo [DotCruz.Notifications.Delivery.Lambda.csproj](./src/DotCruz.Notifications.Delivery.Lambda/DotCruz.Notifications.Delivery.Lambda.csproj)), executado em um runtime customizado `provided.al2023`.
*   **Motivo**: Runtimes padrão do .NET na AWS Lambda sofrem com latência de inicialização a frio (*cold start*) e maior uso de memória. O Native AOT elimina a necessidade de compilação JIT em tempo de execução, garantindo inicialização imediata e baixo consumo de memória (limitado a 256MB no arquivo [aws-lambda-tools-defaults.json](./src/DotCruz.Notifications.Delivery.Lambda/aws-lambda-tools-defaults.json)).
*   **Serialização AOT**: Utiliza serialização baseada em geradores de código via `SourceGeneratorLambdaJsonSerializer` e `LambdaJsonSerializerContext` para assegurar compatibilidade com o corte de código (*trimming*) do AOT.

### 2. Integração com Fila SQS
*   **Decisão**: A Lambda é disparada assincronamente por eventos do Amazon SQS.
*   **Motivo**: O SQS funciona como um amortecedor e fila de tentativas automáticas, garantindo alta resiliência e tolerância a falhas caso ocorra instabilidade externa nos envios.
*   **Handler**: O arquivo [FunctionHandlerService.cs](./src/DotCruz.Notifications.Delivery.Lambda/FunctionHandlerService.cs) recebe lotes de mensagens, desserializa o corpo de cada registro e despacha comandos individuais via MediatR.

### 3. Recuperação Dinâmica de SMTP via AWS SSM
*   **Decisão**: As credenciais SMTP específicas de cada cliente (*tenant*) não são salvas em variáveis de ambiente nem no código. Elas são buscadas em tempo de execução no AWS Systems Manager (SSM) Parameter Store.
*   **Motivo**: Suporte multi-inquilino (*multi-tenant*). Permite adicionar ou alterar as credenciais de SMTP de um cliente sem necessidade de atualizar ou reinstalar o código da Lambda.
*   **Cache local**: Para otimizar a performance e reduzir custos com chamadas à API da AWS, o [SmtpConfigProvider.cs](./src/DotCruz.Notifications.Delivery.Lambda/Services/SmtpConfigProvider.cs) faz cache das credenciais obtidas em memória por 5 minutos.

### 4. Estratégias de Envio Isoladas (Strategy Pattern)
*   **Decisão**: Os envios usam o padrão *Strategy* integrado ao manipulador de comandos do MediatR ([ProcessNotificationCommandHandler.cs](./src/DotCruz.Notifications.Delivery.Lambda/UseCases/ProcessNotification/ProcessNotificationCommandHandler.cs)).
*   **Motivo**: Facilita a expansão e inclusão de novos canais de envio (ex: WhatsApp, Discord) no futuro. O manipulador decide dinamicamente a estratégia de envio correta (E-mail, SMS ou Push) baseando-se no tipo da notificação recebida no payload.

### 5. Loopback de Status (Callback)
*   **Decisão**: Após o sucesso ou falha da tentativa de entrega, o worker realiza uma chamada HTTP de volta à API principal rodando na VPS.
*   **Motivo**: Atualiza o banco de dados principal de notificações com o estado real de entrega e registra o log detalhado de erro em caso de falhas.

---

## 🛠️ Tecnologias Utilizadas

*   **.NET 10.0** (C#)
*   **AWS Lambda & Amazon SQS**
*   **AWS Systems Manager (SSM) Parameter Store**
*   **MailKit**: Envio de e-mails via SMTP.
*   **MediatR**: Despacho de comandos em memória.
*   **Native AOT**: Compilação nativa altamente otimizada para serverless.

---

## 🚀 Configuração e Execução Local

### Pré-requisitos
*   SDK do .NET 10 instalado.
*   AWS CLI configurado com credenciais de acesso para ler parâmetros do SSM (se for testar integrado à nuvem).

### Variáveis de Ambiente
O worker espera que as seguintes variáveis estejam definidas no ambiente de execução:

| Variável | Descrição |
| :--- | :--- |
| `NOTIFICATIONS_API_URL` | Endereço base HTTP da API de Notificações principal (VPS). |
| `NOTIFICATIONS_API_KEY` | Chave de API de segurança usada para autorizar os callbacks de retorno na API principal. |
| `PARAMETER_PATH` | Caminho de busca no AWS SSM formatado com a chave do inquilino (ex: `/dotcruz/tenants/{0}/smtp-config`). |

### Executando os Testes
A pasta de testes possui validações de unidade e integração. Execute-os usando:
```bash
dotnet test
```

---

## 🔄 Deploy Contínuo (CI/CD)

O deploy é gerenciado automaticamente pelo GitHub Actions através do workflow [deploy.yml](./.github/workflows/deploy.yml).

Sempre que um commit é integrado na branch `main`:
1.  Prepara o ambiente do SDK do .NET.
2.  Instala a ferramenta de linha de comando `Amazon.Lambda.Tools`.
3.  Configura as credenciais AWS a partir dos segredos criptografados do GitHub.
4.  Realiza a publicação e atualização da função com base nas definições do arquivo [aws-lambda-tools-defaults.json](./src/DotCruz.Notifications.Delivery.Lambda/aws-lambda-tools-defaults.json):
    ```bash
    dotnet lambda deploy-function notification-delivery-handler-prod
    ```

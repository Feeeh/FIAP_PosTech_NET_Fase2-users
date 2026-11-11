# FCG — UsersAPI

Serviço de Identidade e Acesso da Fase 2. Mantém usuários no banco `fcg_users`,
autentica com BCrypt, emite JWT HS256 e publica `UserCreatedEvent` para Notifications.
As camadas Domain, Application, Infrastructure e API preservam o contexto da Fase 1.

## Endpoints

| Método e rota | Acesso / resultado |
|---|---|
| POST /api/usuarios | Anônimo; 201; senha inválida 400; e-mail duplicado 409 |
| POST /api/autenticacao/login | Anônimo; JWT com sub, email, role, jti; falha 401 |
| GET /api/usuarios/eu | Autenticado; perfil sem senha ou hash |
| GET /api/usuarios | Administrador; lista paginada |
| PATCH /api/usuarios/{usuarioId}/perfil | Administrador; promoção; 204 |
| POST /api/usuarios/{usuarioId}/desativacao | Administrador; desativação; 204 |
| GET /saude | Anônimo; saúde do processo HTTP |

Login permite 10 tentativas e cadastro 5 por minuto, por IP; excesso retorna 429.
Use `Authorization: Bearer <token>` nos endpoints protegidos. A senha precisa de
8 caracteres, letra, número e caractere especial. Contas desativadas não autenticam.
`X-Correlation-Id` é devolvido na resposta e acompanha logs, outbox e AMQP.

## Configuração

`appsettings.json` contém somente padrões não secretos. Variáveis usam `__`.

| Variável | Finalidade / padrão | Origem |
|---|---|---|
| ASPNETCORE_ENVIRONMENT | Production no cluster; Development local | ConfigMap |
| Swagger__Habilitado | true no cluster; false por padrão; Development sempre habilita | ConfigMap |
| ConnectionStrings__SqlServer | SQL Server, banco fcg_users; conexão completa em Secret | Secret |
| RabbitMq__Host | rabbitmq no cluster; localhost local | ConfigMap |
| RabbitMq__Porta | 5672 | ConfigMap |
| RabbitMq__Usuario | TROCAR_USUARIO | Secret |
| RabbitMq__Senha | TROCAR_SENHA | Secret |
| RabbitMq__Exchange | fcg.events | ConfigMap |
| RabbitMq__ExchangeDeMensagensMortas | fcg.events.dlx | ConfigMap |
| RabbitMq__NomeDaAplicacao | users-api; nome da conexão e AMQP AppId | Configuração do serviço |
| Jwt__ChaveSecreta | Chave HS256 com pelo menos 32 bytes UTF-8; obrigatória | Secret |
| Jwt__Emissor | fcg-users-api | ConfigMap |
| Jwt__Audiencia | fcg-clientes | ConfigMap |
| Jwt__MinutosDeValidade | 60 | ConfigMap |
| AdministradorInicial__Nome | Nome inicial; padrão Administrador FCG | Secret |
| AdministradorInicial__Email | E-mail inicial; sem e-mail/senha não semeia | Secret |
| AdministradorInicial__Senha | Senha inicial sujeita à mesma política do cadastro | Secret |

## Executar localmente

Requer SDK .NET 10, SQL Server e RabbitMQ 4. Configure valores próprios nos placeholders:

```powershell
dotnet user-secrets set "ConnectionStrings:SqlServer" "Server=(localdb)\MSSQLLocalDB;Database=fcg_users;Integrated Security=true;TrustServerCertificate=true" --project src/Fcg.Users.API
dotnet user-secrets set "RabbitMq:Usuario" "TROCAR_USUARIO" --project src/Fcg.Users.API
dotnet user-secrets set "RabbitMq:Senha" "TROCAR_SENHA" --project src/Fcg.Users.API
dotnet user-secrets set "Jwt:ChaveSecreta" "TROCAR_CHAVE_COM_PELO_MENOS_32_BYTES" --project src/Fcg.Users.API
dotnet user-secrets set "AdministradorInicial:Nome" "TROCAR_NOME" --project src/Fcg.Users.API
dotnet user-secrets set "AdministradorInicial:Email" "TROCAR_EMAIL" --project src/Fcg.Users.API
dotnet user-secrets set "AdministradorInicial:Senha" "TROCAR_SENHA" --project src/Fcg.Users.API
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/Fcg.Users.API --urls http://localhost:8081
```

Swagger fica em `/swagger`. A inicialização rejeita chave JWT ausente ou curta.
As migrations são aplicadas antes do administrador inicial e antes de servir HTTP.
SQL Server e RabbitMQ têm retries de aproximadamente 60 segundos. O cliente RabbitMQ
recupera conexão automaticamente. Se o e-mail inicial já existe, nada é alterado.
O administrador passa pela UnidadeDeTrabalho e também produz o evento de cadastro.

## Testes e CI

```powershell
dotnet restore Fcg.Users.slnx
dotnet build Fcg.Users.slnx -c Release
dotnet test Fcg.Users.slnx -c Release
dotnet ef migrations has-pending-model-changes --project src/Fcg.Users.Infrastructure
dotnet list package --vulnerable --include-transitive
```

Os testes usam xUnit, NSubstitute e coverlet conforme a Fase 1. Persistência usa LocalDB
`MSSQLLocalDB` no Windows; `FCG_TEST_SQLSERVER` substitui a conexão e é obrigatório fora
do Windows. Cada teste cria e remove seu banco; a conta precisa dessas permissões.
São verificados Identity, casos de uso, cadastro com exatamente uma linha na outbox,
campos JSON e correlação, além de rollback de usuário e evento após violação única.
CI usa SQL Server 2022, restore/build/test e Docker; configure o secret GitHub
`FCG_TEST_SQLSERVER_PASSWORD`. Não exige RabbitMQ no CI. EF Core e Pkcs são 10.0.13;
a referência direta a Pkcs corrige a dependência transitiva vulnerável do cliente SQL.

## Docker e Kubernetes

```powershell
docker build -t fcg/users-api:1.0 .
```

A imagem usa SDK/ASP.NET 10, restore separado por projeto, porta 8080 e usuário não root.
A execução completa fica no repositório irmão `fcg-orchestration`. No Docker Desktop
Kubernetes use kubeadm para compartilhar as imagens locais. Crie namespace `fcg` e
infraestrutura na orquestração e gere os Secrets pelo script de lá antes de aplicar:

```powershell
kubectl apply -f k8s/
kubectl get pods -n fcg -l app=users-api
```

Acesse `http://localhost:30081`. Deployment/Service `users-api`, ConfigMap
`users-api-config`, Secret `users-api-secret`; NodePort 30081, porta 80 → 8080.
`secret.yaml.example` é modelo; `secret.yaml` real é ignorado pelo Git.
StartupProbe permite 120 segundos; readiness/liveness consultam `/saude`.
Recursos: 100m/128Mi até 500m/512Mi; execução sem root e sem elevação de privilégios.
TLS pertence ao ingress; o processo atende HTTP interno sem redirecionamento.

## Kit de mensageria

A referência aprovada está em `../fcg-payments-api`. Foram copiados, alterando somente
namespace, os arquivos de base comum, publicação/outbox, `Contratos/RotasDosEventos.cs`
e `Persistencia/MigradorDoBanco.cs` indicados na seção homônima do README de Payments.
Users não possui consumidor nem fila própria.

`Usuario.Cadastrar` levanta `UsuarioCadastrado`, incluindo nome e e-mail. A
UnidadeDeTrabalho recolhe os eventos, mapeia somente esse fato para `UserCreatedEvent`
e salva `Usuarios` e `MensagensDeSaida` num único SaveChanges. Os demais fatos internos
não geram mensagens. A violação de índice único continua virando `ExcecaoDeConflito`.

O JSON possui exatamente `eventId`, `occurredAt`, `userId`, `name`, `email`. O dispatcher
consulta até 50 linhas a cada 2 segundos, publica em `fcg.events` com rota `user.created`,
confirms e mandatory. Type é `UserCreatedEvent`, AppId é `users-api`, MessageId é eventId
e CorrelationId vem do HTTP. Só marca enviada após confirmação; nack, return ou timeout
mantêm a linha pendente. Notifications declara sua fila e a vincula à rota.
Sem consumidor vinculado o evento permanece pendente e será tentado novamente.

## Limitações conhecidas

Entrega pelo menos uma vez: queda entre publicação e marcação ou múltiplas réplicas
podem repetir mensagens. Não há expurgo da outbox. `/saude` indica processo iniciado,
sem consultar dependências a cada probe. O rate limit é local a cada réplica.
Um JWT já emitido permanece válido até expirar mesmo após a conta ser desativada;
a desativação impede novos logins, conforme a Fase 1.
A conta SQL `sa` compartilhada é uma limitação aceita do desafio; produção precisa
logins por serviço com menor privilégio. Não há TLS no processo, revogação de tokens,
cluster RabbitMQ de alta disponibilidade ou envio de e-mail neste serviço.

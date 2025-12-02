# ECommerce Solution

## 📋 Índice

- [Introdução](#introdução)
- [Arquitetura e Componentes](#arquitetura-e-componentes)
- [Ferramentas e Tecnologias](#ferramentas-e-tecnologias)
- [APIs da Solução](#apis-da-solução)
- [Instalação e Configuração](#instalação-e-configuração)
- [Como Executar](#como-executar)
- [Testes Unitários](#testes-unitários)
- [Conclusão](#conclusão)

---

## 🎯 Introdução

**ECommerceSolution** é uma aplicação de e-commerce distribuída construída usando arquitetura de microsserviços. A solução implementa os principais domínios de um sistema de comércio eletrônico: **Autenticação**, **Gestão de Estoque** e **Vendas**, todos orquestrados através de um **API Gateway** centralizado.

### Objetivos

- **Modularidade**: Cada serviço é independente e possui responsabilidades bem definidas
- **Escalabilidade**: Arquitetura preparada para crescimento horizontal
- **Segurança**: Autenticação e autorização baseadas em JWT com controle de roles
- **Comunicação Assíncrona**: Integração entre serviços via RabbitMQ para operações críticas
- **Testabilidade**: Cobertura abrangente de testes unitários em todos os componentes

### Propósito

Esta solução serve como um exemplo prático de implementação de microsserviços em .NET, demonstrando:
- Padrões de arquitetura moderna (Repository, Service Layer, DTOs)
- Comunicação síncrona (HTTP/REST) e assíncrona (Message Queue)
- Autenticação e autorização distribuída
- Testes unitários com mocks e injeção de dependências
- API Gateway para roteamento e segurança centralizada

---

## 🏗️ Arquitetura e Componentes

A solução é composta por 7 projetos principais organizados em 4 domínios:

### 1. **Auth (Autenticação)**
- **Auth.API**: API responsável pelo gerenciamento de usuários e autenticação
- **Auth.Tests**: Testes unitários para o serviço de autenticação

**Responsabilidades:**
- Cadastro, atualização, listagem e exclusão de usuários
- Login com geração de tokens JWT
- Validação de credenciais e roles (Admin, Sales, Stock)

### 2. **Stock (Estoque)**
- **Stock.API**: API de gerenciamento de produtos e estoque
- **Stock.Tests**: Testes unitários para o serviço de estoque

**Responsabilidades:**
- CRUD completo de produtos (nome, descrição, preço, quantidade em estoque)
- Atualização automática de estoque via mensageria (RabbitMQ)
- Worker assíncrono (`UpdateStockWorker`) que consome eventos de atualização de estoque

### 3. **Sales (Vendas)**
- **Sales.API**: API de gerenciamento de pedidos de venda
- **Sales.Tests**: Testes unitários para o serviço de vendas

**Responsabilidades:**
- Criação e gerenciamento de pedidos
- Integração com Stock.API para validação de produtos
- Publicação de eventos de atualização de estoque no RabbitMQ
- Cálculo automático de valores totais

### 4. **API Gateway**
- **Gateway.Ocelot**: Gateway unificado usando Ocelot

**Responsabilidades:**
- Roteamento centralizado de todas as requisições
- Validação de autenticação JWT
- Controle de acesso baseado em roles
- Agregação de endpoints dos microsserviços

### 5. **Contracts**
- **Contracts**: Biblioteca compartilhada com contratos de eventos (DTOs de mensageria)

**Responsabilidades:**
- Define `UpdateStockEvent` usado na comunicação entre Sales e Stock

### Como Funcionam em Conjunto

```mermaid
graph TB
    Client[Cliente/Frontend] --> Gateway[API Gateway :5000]
    
    Gateway -->|JWT Auth| Auth[Auth.API :5202]
    Gateway -->|JWT Auth + Roles| Sales[Sales.API :5244]
    Gateway -->|JWT Auth + Roles| Stock[Stock.API :5268]
    
    Auth -->|SQL Server| AuthDB[(Auth DB)]
    Sales -->|SQL Server| SalesDB[(Sales DB)]
    Stock -->|SQL Server| StockDB[(Stock DB)]
    
    Sales -->|HTTP Request| Stock
    Sales -->|Publish Event| RabbitMQ[RabbitMQ]
    RabbitMQ -->|Consume Event| Worker[UpdateStockWorker]
    Worker -->|Update Stock| Stock
    
    style Gateway fill:#f9f,stroke:#333,stroke-width:2px
    style RabbitMQ fill:#ff9,stroke:#333,stroke-width:2px
```

**Fluxo de Pedido:**
1. Cliente se autentica no **Auth.API** e recebe um token JWT
2. Cliente faz requisições através do **Gateway** usando o token
3. Gateway valida o token e roteia para o serviço apropriado
4. **Sales.API** recebe uma requisição de criação de pedido
5. Sales.API faz uma chamada HTTP para **Stock.API** para validar produtos e preços
6. Sales.API cria o pedido e publica um evento `UpdateStockEvent` no **RabbitMQ**
7. **UpdateStockWorker** (em Stock.API) consome o evento e atualiza o estoque automaticamente

---

## 🛠️ Ferramentas e Tecnologias

### Framework e Runtime
| Ferramenta | Versão |
|-----------|--------|
| .NET SDK | 9.0 |
| C# | 13.0 |
| ASP.NET Core | 9.0 |

### Banco de Dados
| Ferramenta | Versão |
|-----------|--------|
| Microsoft SQL Server | (Compatível com EF Core 9.0.9) |
| Entity Framework Core | 9.0.9 |

### Mensageria
| Ferramenta | Versão |
|-----------|--------|
| RabbitMQ | (Compatível com Client 7.1.2) |
| RabbitMQ.Client | 7.1.2 |

### API Gateway
| Ferramenta | Versão |
|-----------|--------|
| Ocelot | 24.0.1 |

### Autenticação
| Ferramenta | Versão |
|-----------|--------|
| JWT Bearer Authentication | 9.0.0 |

### Documentação de APIs
| Ferramenta | Versão |
|-----------|--------|
| Swashbuckle (Swagger) | 9.0.5 - 9.0.6 |

### Health Checks
| Ferramenta | Versão |
|-----------|--------|
| AspNetCore.HealthChecks.SqlServer | 9.0.0 |

### Testes
| Ferramenta | Versão |
|-----------|--------|
| xUnit | 2.9.2 |
| Moq | 4.20.72 |
| Microsoft.NET.Test.Sdk | 17.12.0 |
| coverlet.collector | 6.0.2 |
| EF Core InMemory | 9.0.0 |

---

## 📡 APIs da Solução

### 🔐 Auth.API (Porta 5202)

**Descrição:** Serviço de autenticação e gerenciamento de usuários.

**Endpoints Principais:**

| Método | Endpoint | Descrição | Autenticação | Roles |
|--------|----------|-----------|--------------|-------|
| POST | `/api/User/Login` | Autenticação de usuário e geração de JWT | Não | - |
| GET | `/api/User/ObterTodos` | Lista todos os usuários (paginado) | Sim | Admin |
| GET | `/api/User/ObterPorId/{id}` | Obtém um usuário por ID | Sim | Admin |
| POST | `/api/User/Cadastrar` | Cadastra um novo usuário | Sim | Admin |
| PUT | `/api/User/Atualizar/{id}` | Atualiza um usuário existente | Sim | Admin |
| DELETE | `/api/User/Excluir/{id}` | Exclui um usuário | Sim | Admin |

**Funcionalidades:**
- Validação de email e senha com formatação adequada
- Hash de senhas para segurança
- Geração de tokens JWT com claims de roles
- Suporte a três roles: Admin, Sales e Stock
- Paginação e ordenação de resultados

**Configuração Necessária:**
- `ConnectionStrings:StandardConnection`: String de conexão com SQL Server
- `Jwt:SecretKey`: Chave secreta para assinatura de tokens JWT (mínimo 32 caracteres)

---

### 📦 Stock.API (Porta 5268)

**Descrição:** Serviço de gerenciamento de produtos e controle de estoque.

**Endpoints Principais:**

| Método | Endpoint | Descrição | Autenticação | Roles |
|--------|----------|-----------|--------------|-------|
| GET | `/api/Produto/ObterTodos` | Lista todos os produtos (paginado, filtros) | Sim | Todos |
| GET | `/api/Produto/ObterPorId/{id}` | Obtém um produto por ID | Sim | Todos |
| POST | `/api/Produto/Cadastrar` | Cadastra um novo produto | Sim | Admin, Stock |
| PUT | `/api/Produto/Atualizar/{id}` | Atualiza um produto existente | Sim | Admin, Stock |
| DELETE | `/api/Produto/Excluir/{id}` | Exclui um produto | Sim | Admin, Stock |

**Funcionalidades:**
- CRUD completo de produtos
- Validação de campos (nome, preço, quantidade)
- Paginação com suporte a filtros (preço mínimo/máximo, ordenação)
- Ordenação por nome ou preço
- Worker assíncrono `UpdateStockWorker` que consome mensagens da fila `update_stock_queue` no RabbitMQ
- Atualização automática de estoque baseada em eventos de pedidos

**Configuração Necessária:**
- `ConnectionStrings:StandardConnection`: String de conexão com SQL Server
- `ConnectionStrings:RabbitMQ`: String de conexão com RabbitMQ (ex: `amqp://guest:guest@localhost:5672`)
- `Jwt:SecretKey`: Chave secreta para validação de tokens JWT

**Background Workers:**
- **UpdateStockWorker**: Consome eventos `UpdateStockEvent` e atualiza o estoque dos produtos automaticamente

---

### 🛒 Sales.API (Porta 5244)

**Descrição:** Serviço de gerenciamento de pedidos de venda.

**Endpoints Principais:**

| Método | Endpoint | Descrição | Autenticação | Roles |
|--------|----------|-----------|--------------|-------|
| GET | `/api/Pedido/ObterTodos` | Lista todos os pedidos (paginado, filtros) | Sim | Admin, Sales |
| GET | `/api/Pedido/ObterPorId/{id}` | Obtém um pedido por ID | Sim | Admin, Sales |
| POST | `/api/Pedido/Cadastrar` | Cria um novo pedido | Sim | Admin, Sales |
| DELETE | `/api/Pedido/Excluir/{id}` | Exclui um pedido | Sim | Admin, Sales |

**Funcionalidades:**
- Criação de pedidos com múltiplos itens
- Validação de IDs de produtos e quantidades
- Integração HTTP com Stock.API para obter informações de produtos
- Cálculo automático do valor total do pedido
- Publicação de eventos `UpdateStockEvent` no RabbitMQ para atualização assíncrona de estoque
- Paginação e filtros de valor total (min/max)
- Ordenação por valor total

**Configuração Necessária:**
- `ConnectionStrings:StandardConnection`: String de conexão com SQL Server
- `ConnectionStrings:RabbitMQ`: String de conexão com RabbitMQ
- `Jwt:SecretKey`: Chave secreta para validação de tokens JWT
- Comunicação HTTP com Stock.API (configurado internamente)

**Integrações:**
- **HTTP**: Chama Stock.API para validar produtos e obter preços
- **RabbitMQ**: Publica eventos de atualização de estoque após criação de pedidos

---

### 🚪 Gateway.Ocelot (Porta 5000)

**Descrição:** API Gateway que centraliza o acesso a todos os microsserviços.

**Rotas Configuradas:**

| Upstream (Gateway) | Downstream (Serviço) | Métodos | Autenticação | Roles |
|-------------------|---------------------|---------|--------------|-------|
| `/api/users/login` | Auth.API `/api/User/Login` | POST | Não | - |
| `/api/users/{everything}` | Auth.API `/api/User/{everything}` | GET | Sim | Todos |
| `/api/users/{everything}` | Auth.API `/api/User/{everything}` | POST, PUT, DELETE | Sim | Admin |
| `/api/pedidos/{everything}` | Sales.API `/api/Pedido/{everything}` | GET, POST, DELETE | Sim | Admin, Sales |
| `/api/produtos/{everything}` | Stock.API `/api/Produto/{everything}` | GET | Sim | Todos |
| `/api/produtos/{everything}` | Stock.API `/api/Produto/{everything}` | POST, PUT, DELETE | Sim | Admin, Stock |
| `/api/users` | Auth.API `/` | GET | Não | - |
| `/api/vendas` | Sales.API `/` | GET | Não | - |
| `/api/estoque` | Stock.API `/` | GET | Não | - |

**Funcionalidades:**
- Roteamento centralizado de requisições
- Validação de JWT em rotas protegidas
- Controle de acesso baseado em roles (Admin, Sales, Stock)
- Agregação de endpoints de múltiplos serviços
- URLs amigáveis e padronizadas

**Configuração Necessária:**
- `Jwt:SecretKey`: Chave secreta para validação de tokens JWT (deve ser a mesma usada pelos outros serviços)
- Configuração de rotas no arquivo `ocelot.json`

---

## 🚀 Instalação e Configuração

### Pré-requisitos

1. **.NET SDK 9.0** instalado
2. **SQL Server** (LocalDB ou instância completa)
3. **RabbitMQ** instalado e em execução
4. **Git** para clonar o repositório

### Passos de Instalação

#### 1. Clonar o Repositório

```bash
git clone <url-do-repositorio>
cd ECommerceSolution
```

#### 2. Configurar Strings de Conexão

Cada API possui um arquivo `appsettings.Development.example.json`. Copie-o para `appsettings.Development.json` e configure:

**Para Auth.API, Sales.API e Stock.API:**

```bash
# No diretório de cada API (Auth.API, Sales.API, Stock.API):
cd src\Auth\Auth.API
copy appsettings.Development.example.json appsettings.Development.json

cd ..\..\..\Sales\Sales.API
copy appsettings.Development.example.json appsettings.Development.json

cd ..\..\..\Stock\Stock.API
copy appsettings.Development.example.json appsettings.Development.json
```

**Edite cada `appsettings.Development.json`:**

```json
{
  "ConnectionStrings": {
    "StandardConnection": "Server=(localdb)\\mssqllocaldb;Database=<NomeDB>;Trusted_Connection=True;MultipleActiveResultSets=true",
    "RabbitMQ": "amqp://guest:guest@localhost:5672"
  },
  "Jwt": {
    "SecretKey": "YourSecretKeyForJWTTokenGenerationAndValidationMinimum32CharactersLong"
  }
}
```

> **Importante:** Use a mesma chave `Jwt:SecretKey` em **todos** os serviços (Auth, Sales, Stock, Gateway).

**Para Gateway.Ocelot:**

```bash
cd src\ApiGateway\Gateway.Ocelot
copy appsettings.Development.example.json appsettings.Development.json
```

Configure apenas a chave JWT:

```json
{
  "Jwt": {
    "SecretKey": "YourSecretKeyForJWTTokenGenerationAndValidationMinimum32CharactersLong"
  }
}
```

#### 3. Restaurar Dependências

```bash
# Na raiz do projeto
dotnet restore
```

#### 4. Criar os Bancos de Dados

Cada API possui um arquivo `docker-compose.yml` que pode ser usado para iniciar uma instância local do SQL Server em container Docker. Esta é a maneira recomendada para desenvolvimento local.

##### Opção 1: Usando Docker Compose (Recomendado)

**1. Iniciar as instâncias SQL Server:**

Cada API tem sua própria instância de SQL Server rodando em portas diferentes:

**Auth.API** (Porta 1435):
```bash
cd src\Auth\Auth.API
docker-compose up -d
cd ..\..\..
```

**Sales.API** (Porta 1434):
```bash
cd src\Sales\Sales.API
docker-compose up -d
cd ..\..\..
```

**Stock.API** (Porta 1433):
```bash
cd src\Stock\Stock.API
docker-compose up -d
cd ..\..\..
```

> **Nota:** Cada container SQL Server usa:
> - **Usuário**: `sa`
> - **Senha**: `SuaSenhaAqui`
> - **Imagem**: `mcr.microsoft.com/mssql/server:2022-latest`

**2. Atualizar as strings de conexão:**

Após iniciar os containers, atualize os arquivos `appsettings.Development.json` de cada API com as strings de conexão apropriadas:

```json
{
  "ConnectionStrings": {
    "StandardConnection": "Server=localhost,<PORTA>;Database=<NomeDB>;User Id=sa;Password=SuaSenhaAqui;TrustServerCertificate=True;",
    "RabbitMQ": "amqp://guest:guest@localhost:5672"
  }
}
```

Onde `<PORTA>` é:
- **Auth.API**: `1435` → Database: `AuthDB`
- **Sales.API**: `1434` → Database: `SalesDB`
- **Stock.API**: `1433` → Database: `StockDB`

**3. Executar as migrations:**

Após configurar as strings de conexão, execute as migrations:

**Auth.API:**
```bash
cd src\Auth\Auth.API
dotnet ef database update
cd ..\..\..
```

**Sales.API:**
```bash
cd src\Sales\Sales.API
dotnet ef database update
cd ..\..\..
```

**Stock.API:**
```bash
cd src\Stock\Stock.API
dotnet ef database update
cd ..\..\..
```

**4. Gerenciando os containers:**

```bash
# Listar containers em execução
docker ps

# Parar todos os containers SQL Server
docker stop users_db sales_db stock_db

# Remover containers (dados serão mantidos nos volumes)
docker rm users_db sales_db stock_db

# Remover volumes (CUIDADO: isso apaga os dados!)
docker volume prune
```

##### Opção 2: Usando SQL Server Local ou LocalDB

Se preferir usar uma instalação local do SQL Server ou LocalDB ao invés de Docker:

```json
{
  "ConnectionStrings": {
    "StandardConnection": "Server=(localdb)\\mssqllocaldb;Database=<NomeDB>;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

Execute apenas as migrations como mostrado acima.

#### 5. Iniciar RabbitMQ

A solução possui um arquivo `docker-compose.rabbitmq.yml` localizado em `src/Common/Docker/` para iniciar o RabbitMQ facilmente.

##### Opção 1: Usando Docker Compose (Recomendado)

**1. Iniciar o RabbitMQ:**

```bash
# A partir da raiz do projeto
cd src\Common\Docker
docker-compose -f docker-compose.rabbitmq.yml up -d
cd ..\..\..
```

> **Nota:** O container RabbitMQ usa:
> - **Nome do container**: `rabbitmq_bus`
> - **Porta AMQP**: `5672` (comunicação com as APIs)
> - **Porta Management UI**: `15672` (interface web)
> - **Usuário padrão**: `guest`
> - **Senha padrão**: `guest`
> - **Imagem**: `rabbitmq:3-management`

**2. Verificar se o RabbitMQ está rodando:**

Acesse a interface de gerenciamento:
- **Management UI**: http://localhost:15672
- **Login**: guest / guest

**3. Comandos úteis:**

```bash
# Verificar status
docker ps | grep rabbitmq

# Ver logs do RabbitMQ
docker logs rabbitmq_bus

# Parar o RabbitMQ
docker stop rabbitmq_bus

# Reiniciar o RabbitMQ
docker restart rabbitmq_bus

# Remover o container (dados serão mantidos no volume)
docker rm rabbitmq_bus

# Remover o volume (CUIDADO: isso apaga as filas e dados!)
docker volume rm docker_rabbitmq_data
```

##### Opção 2: Usando Instalação Local

Se preferir usar uma instalação local do RabbitMQ ao invés de Docker, certifique-se de que:
- O serviço RabbitMQ está em execução na porta `5672`
- O plugin de management está habilitado: `rabbitmq-plugins enable rabbitmq_management`
- As credenciais padrão (guest/guest) estão configuradas

---

## ▶️ Como Executar

### Opção 1: Executar Todos os Serviços Individualmente

Abra **4 terminais** diferentes e execute cada serviço:

**Terminal 1 - Auth.API:**
```bash
cd src\Auth\Auth.API
dotnet run
```

**Terminal 2 - Stock.API:**
```bash
cd src\Stock\Stock.API
dotnet run
```

**Terminal 3 - Sales.API:**
```bash
cd src\Sales\Sales.API
dotnet run
```

**Terminal 4 - Gateway.Ocelot:**
```bash
cd src\ApiGateway\Gateway.Ocelot
dotnet run
```

### Opção 2: Build de Toda a Solução

```bash
# Na raiz do projeto
dotnet build
```

### Verificar se os Serviços Estão Rodando

- **Auth.API**: http://localhost:5202/swagger
- **Stock.API**: http://localhost:5268/swagger
- **Sales.API**: http://localhost:5244/swagger
- **Gateway**: http://localhost:5000

### Testar o Fluxo Completo

1. **Criar um usuário Admin** via Auth.API (ou usar o seed)
2. **Fazer login** via `/api/users/login` e obter o token JWT
3. **Cadastrar produtos** via Stock.API usando o token
4. **Criar um pedido** via Sales.API usando o token
5. **Verificar** que o estoque foi atualizado automaticamente

**Exemplo de requisição via Gateway:**

```bash
# 1. Login (sem autenticação) com usuário seed
POST http://localhost:5000/api/users/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "12345678"
}

# 2. Listar produtos (com token)
GET http://localhost:5000/api/produtos/ObterTodos
Authorization: Bearer <seu-token-jwt>
```

---

## 🧪 Testes Unitários

### Estrutura de Testes

A solução possui **3 projetos de testes** com cobertura abrangente:

#### 1. **Stock.Tests**
Testa todos os componentes do serviço de estoque:

- **ProdutoRepositoryTests**: Testa operações de banco de dados com EF Core InMemory
  - CRUD de produtos
  - Paginação e filtros
  - Ordenação
  
- **ProdutoServiceTests**: Testa a camada de serviço com mocks
  - Lógica de negócio
  - Validação de regras
  - Atualização de estoque
  
- **ProdutoControllerTests**: Testa a camada de controller com mocks
  - Validação de entrada
  - Códigos HTTP corretos
  - Tratamento de erros
  
- **UpdateStockWorkerTests**: Testa o worker de mensageria
  - Processamento de mensagens do RabbitMQ
  - Tratamento de erros de desserialização
  - Confirmação/rejeição de mensagens

#### 2. **Sales.Tests**
Testa todos os componentes do serviço de vendas:

- **PedidoRepositoryTests**: Testa operações de repositório com EF Core InMemory
- **ItemPedidoRepositoryTests**: Testa operações de itens de pedido
- **PedidoServiceTests**: Testa lógica de negócio de pedidos
  - Integração mockada com Stock.API
  - Publicação de eventos no RabbitMQ
- **PedidoControllerTests**: Testa endpoints da API
- **HttpAuthHandlerTests**: Testa o handler de autenticação para chamadas HTTP entre serviços

#### 3. **Auth.Tests**
Testa todos os componentes do serviço de autenticação:

- **UserRepositoryTests**: Testa operações de repositório de usuários
- **UserServiceTests**: Testa lógica de negócio de autenticação
  - Validação de credenciais
  - Geração de tokens JWT
  - Hash de senhas
- **UserControllerTests**: Testa endpoints da API de usuários

### Tecnologias de Teste Utilizadas

- **xUnit**: Framework de testes
- **Moq**: Biblioteca de mocking para simular dependências
- **EF Core InMemory**: Banco de dados em memória para testes de repositório
- **Coverlet**: Coleta de cobertura de código

### Como Executar os Testes

**Executar todos os testes da solução:**
```bash
# Na raiz do projeto
dotnet test
```

**Executar testes de um projeto específico:**
```bash
# Stock.Tests
dotnet test src\Stock\Stock.Tests\Stock.Tests.csproj

# Sales.Tests
dotnet test src\Sales\Sales.Tests\Sales.Tests.csproj

# Auth.Tests
dotnet test src\Auth\Auth.Tests\Auth.Tests.csproj
```

**Executar com detalhes verbosos:**
```bash
dotnet test --verbosity detailed
```

**Executar com cobertura de código:**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

Todos os testes foram implementados seguindo as melhores práticas:
- Uso de **AAA pattern** (Arrange, Act, Assert)
- **Isolamento completo** com mocks
- **Testes independentes** que podem rodar em qualquer ordem
- **Nomenclatura clara** que descreve o cenário testado

---

## 📝 Conclusão

### Estado Atual da Solução

A **ECommerceSolution** está em um estado funcional e completo, com os seguintes pontos fortes:

✅ **Arquitetura Sólida:**
- Separação clara de responsabilidades entre microsserviços
- Padrões de design bem implementados (Repository, Service Layer, DTOs)
- Comunicação assíncrona para operações críticas

✅ **Segurança Implementada:**
- Autenticação JWT funcionando corretamente
- Controle de acesso baseado em roles (Admin, Sales, Stock)
- API Gateway como ponto único de entrada

✅ **Testes Abrangentes:**
- Cobertura completa de testes unitários em todos os serviços
- Testes de controllers, services, repositories e workers
- Total de testes implementados com sucesso

✅ **Mensageria Assíncrona:**
- Integração RabbitMQ funcionando
- Worker consumindo eventos e atualizando estoque automaticamente
- Tratamento de erros e acknowledgments

✅ **Documentação:**
- Swagger/OpenAPI em cada serviço
- README completo com instruções claras

### Pontos de Melhoria para o Futuro

#### 1. **Observabilidade e Monitoramento**
- [ ] Implementar **distributed tracing** com OpenTelemetry ou Jaeger
- [ ] Adicionar **métricas** com Prometheus e Grafana
- [ ] Implementar **logging centralizado** com Elasticsearch, Logstash e Kibana (ELK Stack)
- [ ] Configurar **alertas** para falhas críticas

#### 2. **Resiliência e Confiabilidade**
- [ ] Adicionar **circuit breakers** usando Polly para chamadas HTTP entre serviços
- [ ] Implementar **retry policies** com backoff exponencial
- [ ] Configurar **health checks** mais robustos (incluindo dependências externas)
- [ ] Implementar **saga pattern** para transações distribuídas complexas

#### 3. **Containerização e Orquestração**
- [ ] Criar **Dockerfiles** para cada serviço
- [ ] Criar **docker-compose.yml** para execução local completa
- [ ] Preparar manifestos **Kubernetes** para deploy em produção
- [ ] Configurar **CI/CD pipelines** (GitHub Actions, Azure DevOps)

#### 4. **Performance e Escalabilidade**
- [ ] Adicionar **caching** com Redis para consultas frequentes
- [ ] Implementar **paginação assíncrona** (IAsyncEnumerable)
- [ ] Otimizar queries do Entity Framework (evitar N+1)
- [ ] Configurar **load balancing** para múltiplas instâncias

#### 5. **Testes e Qualidade**
- [ ] Adicionar **testes de integração** end-to-end
- [ ] Implementar **testes de carga** com k6 ou JMeter
- [ ] Aumentar cobertura de código para +90%
- [ ] Adicionar **testes de contrato** com Pact para comunicação entre serviços

#### 6. **Segurança Avançada**
- [ ] Implementar **OAuth2/OpenID Connect** com IdentityServer
- [ ] Adicionar **rate limiting** no API Gateway
- [ ] Configurar **CORS** adequadamente para produção
- [ ] Implementar **API versioning**
- [ ] Adicionar **auditoria de ações** (quem fez o quê e quando)

#### 7. **Funcionalidades de Negócio**
- [ ] Adicionar **cancelamento de pedidos** com rollback de estoque
- [ ] Implementar **notificações** por email/SMS
- [ ] Adicionar **carrinho de compras** persistente
- [ ] Implementar **pagamentos** (integração com gateway de pagamento)
- [ ] Adicionar **histórico de pedidos** por cliente
- [ ] Implementar **relatórios** de vendas e estoque

#### 8. **DevEx (Developer Experience)**
- [ ] Criar **scripts de inicialização** automatizados
- [ ] Adicionar **seed de dados** para ambiente de desenvolvimento
- [ ] Criar coleções de requisições http para testes manuais

#### 9. **Conformidade e Governança**
- [ ] Implementar **LGPD/GDPR compliance** para dados de usuários
- [ ] Adicionar **políticas de retenção de dados**
- [ ] Implementar **logs de auditoria** para conformidade
- [ ] Documentar **SLAs** (Service Level Agreements)

---

## 📄 Licença

Este projeto é um exemplo educacional desenvolvido durante o Bootcamp Avanade - Back-end com .NET e IA em parceria com a DIO.

## 🤝 Contribuindo

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests.

---

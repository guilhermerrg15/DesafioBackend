# 🏍️ API - Sistema de Gerenciamento de Aluguel de Motos

API RESTful desenvolvida em .NET 9.0 para gerenciar aluguel de motos e entregadores, com sistema de mensageria para notificações.

## 📋 Índice

- [Requisitos](#requisitos)
- [Tecnologias](#tecnologias)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Como Executar](#como-executar)
- [Início Rápido](#início-rápido)
- [Endpoints da API](#endpoints-da-api)
- [Exemplos de Uso](#exemplos-de-uso)
- [Configuração](#configuração)
- [Banco de Dados](#banco-de-dados)
- [Sistema de Mensageria](#sistema-de-mensageria)
- [Swagger](#swagger)
- [Regras de Negócio](#regras-de-negócio)
- [Testes Unitários](#testes-unitários)
- [Troubleshooting](#troubleshooting)

---

## 🔧 Requisitos

- Docker e Docker Compose
- .NET 9.0 SDK (para desenvolvimento local e migrations)
- Git

---

## 🛠️ Tecnologias

- **.NET 9.0** - Framework principal
- **C#** - Linguagem de programação
- **PostgreSQL 15** - Banco de dados
- **Entity Framework Core** - ORM
- **RabbitMQ** - Sistema de mensageria
- **Docker** - Containerização
- **Swagger/OpenAPI** - Documentação da API
- **xUnit** - Framework de testes

---

## 📁 Estrutura do Projeto

```
Projeto/
├── Api/                    # Camada de API (Endpoints)
├── Core/                   # Camada de Domínio (Entidades, DTOs, Services)
│   ├── Entities/           # Entidades do domínio
│   ├── DTO/                # Data Transfer Objects
│   └── Services/           # Interfaces de serviços
├── Infrastructure/         # Camada de Infraestrutura
│   ├── Data/               # DbContext e Migrations
│   └── Services/            # Implementações de serviços
├── Tests/                  # Testes unitários
└── docker-compose.yml      # Configuração Docker
```

---

## 🚀 Como Executar

### Opção 1: Docker Compose (Recomendado)

1. **Clone o repositório** (se ainda não tiver):
```bash
git clone <seu-repositorio>
cd Projeto
```

2. **Inicie os containers**:
```bash
docker-compose up -d --build
```

3. **Aguarde os serviços iniciarem** (cerca de 10-15 segundos)

4. **Aplique as migrations** (primeira vez):
```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --connection "Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=apppass"
```

5. **Acesse a API**:
   - API: http://localhost:5001
   - Swagger: http://localhost:5001/swagger
   - RabbitMQ Management: http://localhost:15672

### Opção 2: Desenvolvimento Local

1. **Configure o banco de dados**:
   - Certifique-se de que o PostgreSQL está rodando
   - Atualize a connection string em `appsettings.json`

2. **Restaure as dependências**:
```bash
dotnet restore
```

3. **Aplique as migrations**:
```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj
```

4. **Execute a aplicação**:
```bash
cd Api
dotnet run
```

---

## ⚡ Início Rápido (5 minutos)

### 1. Subir a aplicação
```bash
docker-compose up -d --build
```

### 2. Aplicar migrations (primeira vez)
```bash
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Api/Api.csproj --connection "Host=localhost;Port=5432;Database=appdb;Username=appuser;Password=apppass"
```

### 3. Acessar
- **Swagger**: http://localhost:5001/swagger
- **API**: http://localhost:5001

### 4. Criar uma Locação Completa

1. **Cadastrar Moto**
   ```bash
   POST /motos
   {
     "ano": 2024,
     "modelo": "Honda CB 600F",
     "placa": "ABC1234"
   }
   ```

2. **Cadastrar Entregador**
   ```bash
   POST /entregadores
   {
     "nome": "João Silva",
     "cnpj": "12345678000190",
     "dataNascimento": "1990-05-15T00:00:00Z",
     "numeroCnh": "12345678901",
     "tipoCnh": "A"
   }
   ```

3. **Upload CNH** (opcional, mas recomendado)
   ```bash
   POST /entregadores/{id}/cnh
   Form-data: file (PNG ou BMP)
   ```

4. **Criar Locação**
   ```bash
   POST /locacoes
   {
     "motoId": 123456,
     "entregadorId": 789012,
     "planoDias": 7
   }
   ```

5. **Devolver Locação**
   ```bash
   PUT /locacoes/{id}/devolucao
   {
     "dataTerminoReal": "2024-11-15T00:00:00Z"
   }
   ```

---

## 📡 Endpoints da API

### 🏍️ Moto

#### POST /motos
Cadastra uma nova moto.

**Request Body:**
```json
{
  "ano": 2024,
  "modelo": "Honda CB 600F",
  "placa": "ABC1234"
}
```

**Response:** `201 Created`
```json
{
  "id": 123456,
  "ano": 2024,
  "modelo": "Honda CB 600F",
  "placa": "ABC1234"
}
```

**Validações:**
- Placa deve ser única
- ID gerado automaticamente baseado na placa (hash)
- Publica evento "moto cadastrada" no RabbitMQ

---

#### GET /motos
Lista todas as motos ou filtra por placa.

**Query Parameters:**
- `placa` (opcional): Filtra motos pela placa

**Exemplos:**
- `GET /motos` - Lista todas
- `GET /motos?placa=ABC` - Filtra por placa contendo "ABC"

**Response:** `200 OK`
```json
[
  {
    "id": 123456,
    "ano": 2024,
    "modelo": "Honda CB 600F",
    "placa": "ABC1234"
  }
]
```

---

#### GET /motos/{id}
Busca uma moto específica por ID.

**Response:** `200 OK` ou `404 Not Found`

---

#### PUT /motos/{id}/placa
Atualiza a placa de uma moto.

**Request Body:**
```json
{
  "placa": "XYZ9876"
}
```

**Response:** `200 OK` ou `404 Not Found` ou `409 Conflict` (se placa já existe)

---

#### DELETE /motos/{id}
Remove uma moto.

**Validações:**
- Não permite remover se houver locações ativas

**Response:** `204 No Content` ou `404 Not Found` ou `409 Conflict`

---

### 👤 Entregador

#### POST /entregadores
Cadastra um novo entregador.

**Request Body:**
```json
{
  "nome": "João Silva",
  "cnpj": "12345678000190",
  "dataNascimento": "1990-05-15T00:00:00Z",
  "numeroCnh": "12345678901",
  "tipoCnh": "A"
}
```

**Tipos CNH válidos:** `A`, `B`, `AB` ou `A+B`

**Validações:**
- CNPJ deve ser único
- Número CNH deve ser único
- Tipo CNH deve ser A, B, AB ou A+B
- ID gerado automaticamente baseado no CNPJ (hash)

**Response:** `201 Created`

---

#### POST /entregadores/{id}/cnh
Faz upload da foto da CNH do entregador.

**Request:** `multipart/form-data`
- Campo: `file` (arquivo PNG ou BMP)

**Validações:**
- Entregador deve existir
- Entregador deve ter CNH tipo A, AB ou A+B (para alugar motos)
- Arquivo deve ser PNG ou BMP

**Response:** `200 OK`
```json
{
  "message": "CNH salva com sucesso.",
  "entregadorId": 123456,
  "cnhUrl": "123456_cnh.png"
}
```

---

### 📦 Locação

#### POST /locacoes
Cria uma nova locação de moto.

**Request Body:**
```json
{
  "motoId": 123456,
  "entregadorId": 789012,
  "planoDias": 7
}
```

**Planos disponíveis:**
- `7 dias` - R$ 30,00/dia (Total: R$ 210,00)
- `15 dias` - R$ 28,00/dia (Total: R$ 420,00)
- `30 dias` - R$ 22,00/dia (Total: R$ 660,00)
- `45 dias` - R$ 20,00/dia (Total: R$ 900,00)
- `50 dias` - R$ 18,00/dia (Total: R$ 900,00)

**Validações:**
- Entregador deve ter CNH tipo A, AB ou A+B
- Moto não pode estar locada
- Data início = primeiro dia após criação (meia-noite do dia seguinte)
- Data término prevista = data início + plano dias

**Response:** `201 Created`

---

#### PUT /locacoes/{id}/devolucao
Registra a devolução de uma locação.

**Request Body:**
```json
{
  "dataTerminoReal": "2024-11-15T00:00:00Z"
}
```

**Cálculos automáticos:**

**Devolução Antecipada:**
- Plano 7 dias: Multa de 20% sobre diárias não usadas
- Plano 15 dias: Multa de 40% sobre diárias não usadas
- Outros planos: Desconto das diárias não usadas

**Devolução Atrasada:**
- R$ 50,00 por cada dia adicional

**Response:** `200 OK`
```json
{
  "locacao": { ... },
  "valorTotalCalculado": 210.00,
  "mensagem": "Locação devolvida com sucesso."
}
```

---

#### GET /locacoes
Lista todas as locações.

**Response:** `200 OK`

---

#### GET /locacoes/{id}
Busca uma locação específica por ID.

**Response:** `200 OK` ou `404 Not Found`

---

## 💡 Exemplos de Uso

### Exemplo Completo: Fluxo de Locação

```bash
# 1. Cadastrar uma moto
curl -X POST http://localhost:5001/motos \
  -H "Content-Type: application/json" \
  -d '{
    "ano": 2024,
    "modelo": "Honda CB 600F",
    "placa": "ABC1234"
  }'

# 2. Cadastrar um entregador
curl -X POST http://localhost:5001/entregadores \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "João Silva",
    "cnpj": "12345678000190",
    "dataNascimento": "1990-05-15T00:00:00Z",
    "numeroCnh": "12345678901",
    "tipoCnh": "A"
  }'

# 3. Fazer upload da CNH
curl -X POST http://localhost:5001/entregadores/{entregadorId}/cnh \
  -F "file=@caminho/para/cnh.png"

# 4. Criar uma locação
curl -X POST http://localhost:5001/locacoes \
  -H "Content-Type: application/json" \
  -d '{
    "motoId": 123456,
    "entregadorId": 789012,
    "planoDias": 7
  }'

# 5. Devolver a locação
curl -X PUT http://localhost:5001/locacoes/{locacaoId}/devolucao \
  -H "Content-Type: application/json" \
  -d '{
    "dataTerminoReal": "2024-11-10T00:00:00Z"
  }'
```

---

## ⚙️ Configuração

### Variáveis de Ambiente

As configurações podem ser definidas no `docker-compose.yml` ou `appsettings.json`:

**PostgreSQL:**
- `POSTGRES_USER`: appuser
- `POSTGRES_PASSWORD`: apppass
- `POSTGRES_DB`: appdb

**RabbitMQ:**
- `RABBITMQ_DEFAULT_USER`: admin
- `RABBITMQ_DEFAULT_PASS`: secure_pass_2024!
- `RABBITMQ_CONNECTION`: amqp://admin:secure_pass_2024!@rabbitmq:5672/

### Portas

- **API**: 5001
- **PostgreSQL**: 5432
- **RabbitMQ AMQP**: 5672
- **RabbitMQ Management**: 15672

### Credenciais Importantes

**PostgreSQL:**
- Host: `localhost`
- Port: `5432`
- Database: `appdb`
- User: `appuser`
- Password: `apppass`

**RabbitMQ Management:**
- URL: http://localhost:15672
- User: `admin`
- Password: `secure_pass_2024!`

---

## 🗄️ Banco de Dados

### Tabelas

- **Motos**: Armazena informações das motocicletas
- **Entregadores**: Armazena dados dos entregadores
- **Locacoes**: Armazena as locações realizadas
- **Notificacoes**: Armazena notificações de motos de 2024

### Visualizar o Banco

#### Opção 1: psql (Terminal)

```bash
# Acessar o psql interativo
docker-compose exec postgres_db psql -U appuser -d appdb

# Comandos úteis no psql:
\dt                    # Listar todas as tabelas
\d "Motos"             # Descrever estrutura de uma tabela
SELECT * FROM "Motos";  # Ver dados de uma tabela
\q                     # Sair do psql
```

#### Opção 2: Executar comandos SQL diretamente

```bash
# Listar tabelas
docker-compose exec postgres_db psql -U appuser -d appdb -c "\dt"

# Ver dados
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT * FROM \"Motos\";"

# Ver estrutura
docker-compose exec postgres_db psql -U appuser -d appdb -c "\d \"Entregadores\""
```

#### Opção 3: Ferramentas Gráficas

**pgAdmin** (Recomendado)
- Download: https://www.pgadmin.org/download/
- Host: `localhost`, Port: `5432`, Database: `appdb`, Username: `appuser`, Password: `apppass`

**DBeaver** (Gratuito e Multiplataforma)
- Download: https://dbeaver.io/download/
- Configuração: Driver PostgreSQL, Host: `localhost`, Port: `5432`, Database: `appdb`, Username: `appuser`, Password: `apppass`

**TablePlus** (macOS/Windows)
- Download: https://tableplus.com/
- Type: PostgreSQL, Host: `localhost`, Port: `5432`, Database: `appdb`, Username: `appuser`, Password: `apppass`

---

## 📨 Sistema de Mensageria

### RabbitMQ

A aplicação utiliza RabbitMQ para publicar eventos e processar notificações de forma assíncrona.

**Acesso ao Management UI:**
- URL: http://localhost:15672
- Usuário: `admin`
- Senha: `secure_pass_2024!`

### Como Funciona

1. **Quando uma moto é cadastrada** → Um evento é publicado no RabbitMQ
2. **Um consumidor escuta esse evento** → Processa a mensagem
3. **Se a moto for de 2024** → Cria automaticamente uma notificação no banco de dados

### Fluxo Completo

```
Cliente → POST /motos → API salva moto → Publica evento "moto.cadastrada" → RabbitMQ → 
Consumer processa → Se Ano == 2024 → Cria Notificação no banco
```

### Componentes

- **Publisher**: `RabbitMQMessageService.cs` - Publica eventos quando motos são cadastradas
- **Consumer**: `Program.cs` - Processa mensagens e cria notificações para motos de 2024
- **Exchange**: `app_exchange` (tipo: topic) - Roteia mensagens
- **Queue**: `moto_cadastrada_queue` - Armazena mensagens

### Eventos Publicados

**moto.cadastrada**
- Publicado quando uma moto é cadastrada
- Contém: MotoId, Ano, Modelo, Placa, DataCadastro

### Como Visualizar e Verificar

#### 1. RabbitMQ Management UI

**Acesso:**
- URL: http://localhost:15672
- Usuário: `admin`
- Senha: `secure_pass_2024!`

**O que verificar:**

- **Connections**: Deve ver 2 conexões ativas (Publisher e Consumer)
- **Channels**: Deve ver 2 canais
- **Exchanges**: Procure por `app_exchange` (tipo: topic, Durable)
- **Queues**: Procure por `moto_cadastrada_queue` (deve ter 1 Consumer)
- **Mensagens**: Clique na fila → "Get messages" para ver o conteúdo JSON

#### 2. Logs da API

```bash
docker-compose logs api | grep -i "rabbitmq\|consumidor\|notificação"
```

**Logs esperados:**
```
RabbitMQ consumer started. Waiting for messages...
Notification created for 2024 moto: ABCD123
```

#### 3. Banco de Dados

```bash
# Verificar notificações criadas
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT * FROM \"Notificacoes\" ORDER BY \"DataNotificacao\" DESC;"

# Verificar motos de 2024
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT \"Id\", \"Ano\", \"Placa\", \"Modelo\" FROM \"Motos\" WHERE \"Ano\" = 2024;"
```

### Como Testar

#### Teste 1: Cadastrar Moto de 2024

1. Acesse o Swagger: http://localhost:5001/swagger
2. Endpoint: `POST /motos`
3. Body:
```json
{
  "ano": 2024,
  "modelo": "Honda CB 600F",
  "placa": "XYZ2024"
}
```

4. O que deve acontecer:
   - ✅ Moto cadastrada (resposta 201)
   - ✅ Mensagem publicada no RabbitMQ
   - ✅ Consumidor processa a mensagem
   - ✅ Notificação criada no banco

5. Verificar:
   - RabbitMQ Management: Ver mensagem na fila
   - Banco: `SELECT * FROM "Notificacoes" WHERE "AnoMoto" = 2024;`

#### Teste 2: Cadastrar Moto de Outro Ano

1. Endpoint: `POST /motos`
2. Body:
```json
{
  "ano": 2023,
  "modelo": "Yamaha MT-07",
  "placa": "ABC2023"
}
```

3. O que deve acontecer:
   - ✅ Moto cadastrada
   - ✅ Mensagem publicada no RabbitMQ
   - ✅ Consumidor processa, mas **NÃO cria notificação** (ano != 2024)

---

## 📚 Swagger

A documentação interativa da API está disponível em:

**http://localhost:5001/swagger**

No Swagger você pode:
- Ver todos os endpoints
- Testar requisições diretamente
- Ver exemplos de request/response
- Entender os modelos de dados

---

## 🔍 Regras de Negócio

### Moto
- ✅ Placa deve ser única
- ✅ Não pode ser removida se tiver locações ativas
- ✅ Ao ser cadastrada, publica evento no RabbitMQ
- ✅ ID gerado automaticamente baseado na placa (hash)

### Entregador
- ✅ CNPJ deve ser único
- ✅ Número CNH deve ser único
- ✅ Tipo CNH: A, B, AB ou A+B
- ✅ Apenas CNH tipo A, AB ou A+B podem alugar motos
- ✅ Foto CNH: PNG ou BMP apenas
- ✅ ID gerado automaticamente baseado no CNPJ (hash)

### Locação
- ✅ Data início = primeiro dia após criação (meia-noite do dia seguinte)
- ✅ Planos: 7, 15, 30, 45 ou 50 dias
- ✅ Valores por plano:
  - 7d: R$ 30,00/dia (Total: R$ 210,00)
  - 15d: R$ 28,00/dia (Total: R$ 420,00)
  - 30d: R$ 22,00/dia (Total: R$ 660,00)
  - 45d: R$ 20,00/dia (Total: R$ 900,00)
  - 50d: R$ 18,00/dia (Total: R$ 900,00)
- ✅ Multas (devolução antecipada):
  - 7 dias: 20% sobre diárias não usadas
  - 15 dias: 40% sobre diárias não usadas
  - Outros planos: Desconto das diárias não usadas
- ✅ Diárias adicionais (devolução atrasada): R$ 50,00/dia

### Validações Importantes

- ✅ Placa de moto deve ser única
- ✅ CNPJ de entregador deve ser único
- ✅ Número CNH deve ser único
- ✅ Tipo CNH: A, B, AB ou A+B (apenas A, AB e A+B podem alugar motos)
- ✅ Não pode remover moto com locação ativa
- ✅ Não pode locar moto já locada
- ✅ Foto CNH: apenas PNG ou BMP

---

## 🧪 Testes Unitários

O projeto inclui testes unitários abrangentes cobrindo as funcionalidades principais.

### Executar Testes

```bash
dotnet test Tests/Tests.csproj
```

### Cobertura de Testes

**68 testes** cobrindo:

1. **ID Generation Tests** (`IdGeneratorTests.cs`)
   - Geração de ID para entregadores (baseado em CNPJ)
   - Geração de ID para motos (baseado em placa)
   - Consistência e unicidade de IDs

2. **CNH Validation Tests** (`CnhValidationTests.cs`)
   - Validação de tipos CNH válidos (A, B, AB, A+B)
   - Validação de tipos CNH inválidos
   - Validação case-insensitive
   - Validação para aluguel de motos

3. **Rental Calculation Tests** (`LocacaoCalculoTests.cs`)
   - Cálculo de valores totais por plano
   - Cálculo de multas para devolução antecipada
   - Cálculo de diárias adicionais para devolução atrasada
   - Cálculo de datas de início e término

4. **Plan Validation Tests** (`PlanoValidationTests.cs`)
   - Validação de planos válidos (7, 15, 30, 45, 50 dias)
   - Validação de planos inválidos
   - Validação de valores por dia

5. **Date Calculation Tests** (`DataCalculationTests.cs`)
   - Cálculo de data de início (meia-noite do dia seguinte)
   - Cálculo de data de término prevista
   - Cálculo de dias não usados
   - Cálculo de dias de atraso

---

## 🐛 Troubleshooting

### API não inicia
- Verifique se o PostgreSQL está rodando: `docker-compose ps`
- Verifique os logs: `docker-compose logs api`

### Erro de conexão com banco
- Certifique-se de que as migrations foram aplicadas
- Verifique a connection string no `docker-compose.yml`

### RabbitMQ não conecta
- Verifique se o container está rodando: `docker-compose ps rabbitmq`
- Verifique as credenciais no `docker-compose.yml`
- Verifique os logs: `docker-compose logs rabbitmq`

### Erro ao fazer upload de CNH
- Certifique-se de que o diretório `storage` existe
- Verifique permissões do diretório

### Mensagens não estão sendo processadas
- Verifique se o consumer está rodando: `docker-compose logs api | grep "RabbitMQ consumer started"`
- Verifique conexão com RabbitMQ: `docker-compose logs api | grep -i "rabbitmq\|connection"`
- Verifique se a fila existe no RabbitMQ Management UI

### Notificações não estão sendo criadas
- Verifique se a moto é de 2024
- Verifique se o consumidor processou a mensagem: `docker-compose logs api | grep "Notification created"`
- Verifique erros no processamento: `docker-compose logs api | grep -i "erro\|exception"`

### RabbitMQ Management não abre
- Verifique se o container está rodando: `docker-compose ps rabbitmq`
- Verifique se a porta está mapeada: `docker-compose ps | grep 15672`
- Verifique as credenciais: Usuário: `admin`, Senha: `secure_pass_2024!`

---

## 📝 Notas Importantes

1. **Primeira execução**: Sempre execute as migrations antes de usar a API
2. **Dados de teste**: Use o Swagger para criar dados de teste facilmente
3. **Logs**: Os logs do consumidor RabbitMQ aparecem no console da API
4. **Storage**: Arquivos de CNH são salvos em `./storage` (mapeado para `/app/storage` no container)
5. **IDs**: IDs são gerados automaticamente baseados em hash (CNPJ para entregadores, placa para motos)
6. **Colisões**: Se houver colisão de hash, o sistema adiciona um sufixo numérico automaticamente

---

## 📞 Suporte

Para mais informações sobre os endpoints, consulte:
- Swagger: http://localhost:5001/swagger
- Swagger de referência: https://app.swaggerhub.com/apis-docs/App/app_backend/1.0.0

---

## 📄 Licença

Este projeto foi desenvolvido como parte de um desafio técnico.

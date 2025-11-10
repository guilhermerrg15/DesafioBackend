# API - Sistema de Gerenciamento de Aluguel de Motos

API REST desenvolvida em .NET 9.0 para gerenciar aluguel de motos e entregadores, com sistema de mensageria para notificações.

---

## Tecnologias

- .NET 9.0 - Framework principal
- C# - Linguagem de programação
- PostgreSQL - Banco de dados
- Entity Framework Core - ORM
- RabbitMQ - Sistema de mensageria
- Docker - Containerização
- Swagger/OpenAPI - Documentação da API
- xUnit - Framework de testes unitários

---
Como Utilizar a Aplicação

### 1. Clone o repositório:
```bash
git clone <seu-repositorio>
cd Projeto
```

### 2. Subir a aplicação

Usando Makefile
```bash
make up
```

Usando Docker Compose diretamente
```bash
docker-compose up -d --build
```

### 3. Aplicar migrations (primeira vez)

Usando Makefile
```bash
make migrate
```

Comando direto
```bash
dotnet ef database update --project Mottu.Infrastructure/Mottu.Infrastructure.csproj --startup-project Mottu.Api/Mottu.Api.csproj --connection "Host=localhost;Port=5432;Database=mottudb;Username=mottuuser;Password=mottupass"
```

### 4. Acessar
- Swagger: http://localhost:5001/swagger
- API: http://localhost:5001

---

## Comandos Makefile

O projeto inclui um Makefile com comandos úteis para facilitar o desenvolvimento e uso da aplicação.

### Comandos de Testes
```bash
make test              # Executa todos os testes
make test-unit         # Executa apenas testes unitários
make test-integration  # Executa apenas testes de integração
make test-verbose      # Executa testes com saída detalhada
```

### Comandos Docker
```bash
make up                # Inicia todos os containers (aguarda serviços ficarem prontos)
make down              # Para todos os containers
make restart           # Reinicia todos os containers
make logs              # Mostra logs de todos os containers
make logs-api          # Mostra logs apenas da API
make logs-db           # Mostra logs apenas do PostgreSQL
make logs-rabbitmq     # Mostra logs apenas do RabbitMQ
```

### Outros Comandos
```bash
make build             # Compila a solução
make migrate           # Aplica migrations no banco de dados
make clean             # Limpa artefatos de build e para containers
make help              # Mostra todos os comandos disponíveis
```

Nota: O comando `make up` garante que os serviços sejam iniciados na ordem correta (PostgreSQL e RabbitMQ primeiro, depois a API), evitando problemas de conexão.

### 5. Criar uma Locação Completa

1. Cadastrar Moto
   ```bash
   POST /motos
   {
     "ano": 2024,
     "modelo": "Honda CB 600F",
     "placa": "ABC1234"
   }
   ```

2. Cadastrar Entregador
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

3. Upload CNH (opcional, mas recomendado)
   ```bash
   POST /entregadores/{id}/cnh
   Form-data: file (PNG ou BMP)
   ```

4. Criar Locação
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

## Endpoints da API

### Moto

#### POST /motos
Cadastra uma nova moto

Request Body:
```json
{
  "ano": 2024,
  "modelo": "Honda CB 600F",
  "placa": "ABC1234"
}
```

Response: `201 Created`
```json
{
  "id": 123456,
  "ano": 2024,
  "modelo": "Honda CB 600F",
  "placa": "ABC1234"
}
```

Validações:
- Placa deve ser única
- ID gerado automaticamente baseado na placa utilizando hash
- Publica evento "moto cadastrada" no RabbitMQ

---

#### GET /motos
Lista todas as motos ou filtra por placa

Query Parameters:
- `placa` (opcional): Filtra motos pela placa

Exemplos:
- `GET /motos` - Lista todas
- `GET /motos?placa=ABC` - Filtra por placa contendo "ABC"

Response: `200 OK`
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
Busca uma moto específica por ID

Response: `200 OK` ou `404 Not Found`

---

#### PUT /motos/{id}/placa
Atualiza a placa de uma moto

Request Body:
```json
{
  "placa": "XYZ9876"
}
```

Response: `200 OK` ou `404 Not Found` ou `409 Conflict` (se placa já existe)

---

#### DELETE /motos/{id}
Remove uma moto

Validações:
- Não permite remover se houver locações ativas

Response: `204 No Content` ou `404 Not Found` ou `409 Conflict`

---

### Entregador

#### POST /entregadores
Cadastra um novo entregador

Request Body:
```json
{
  "nome": "João Silva",
  "cnpj": "12345678000190",
  "dataNascimento": "1990-05-15T00:00:00Z",
  "numeroCnh": "12345678901",
  "tipoCnh": "A"
}
```

Tipos CNH válidos: `A`, `B` ou `A+B`

Validações:
- CNPJ deve ser único
- Número CNH deve ser único
- Tipo CNH deve ser A, B ou A+B
- ID gerado automaticamente baseado no CNPJ (hash)

Response: `201 Created`

---

#### POST /entregadores/{id}/cnh
Faz upload da foto da CNH do entregador

Request:
- Campo: `file` (arquivo PNG ou BMP)

Validações:
- Entregador deve existir
- Entregador deve ter CNH tipo A ou A+B (para alugar motos)
- Arquivo deve ser PNG ou BMP

Response: `200 OK`
```json
{
  "message": "CNH salva com sucesso.",
  "entregadorId": 123456,
  "cnhUrl": "123456_cnh.png"
}
```

---

### Locação

#### POST /locacoes
Cria uma nova locação de moto

Request Body:
```json
{
  "motoId": 123456,
  "entregadorId": 789012,
  "planoDias": 7
}
```

Planos disponíveis:
- `7 dias` - R$ 30,00/dia (Total: R$ 210,00)
- `15 dias` - R$ 28,00/dia (Total: R$ 420,00)
- `30 dias` - R$ 22,00/dia (Total: R$ 660,00)
- `45 dias` - R$ 20,00/dia (Total: R$ 900,00)
- `50 dias` - R$ 18,00/dia (Total: R$ 900,00)

Validações:
- Entregador deve ter CNH tipo A ou A+B
- Moto não pode estar locada
- Data início = primeiro dia após criação (meia-noite do dia seguinte)
- Data término prevista = data início + plano dias

Response: `201 Created`

---

#### PUT /locacoes/{id}/devolucao
Registra a devolução de uma locação

Request Body:
```json
{
  "dataTerminoReal": "2024-11-15T00:00:00Z"
}
```

Cálculos automáticos:

Devolução Antecipada:
- Plano 7 dias: Multa de 20% sobre diárias não usadas
- Plano 15 dias: Multa de 40% sobre diárias não usadas
- Outros planos: Desconto das diárias não usadas

Devolução Atrasada:
- R$ 50,00 por cada dia adicional

Response: `200 OK`
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

Response: `200 OK`

---

#### GET /locacoes/{id}
Busca uma locação específica por ID.

Response: `200 OK` ou `404 Not Found`

---

### Exemplo Completo de Fluxo de Locação com curl (pelo terminal)

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

## Configuração

### Variáveis de Ambiente

As configurações podem ser definidas no `docker-compose.yml` ou `appsettings.json`:

PostgreSQL:
- `POSTGRES_USER`: appuser
- `POSTGRES_PASSWORD`: apppass
- `POSTGRES_DB`: appdb

RabbitMQ:
- `RABBITMQ_DEFAULT_USER`: admin
- `RABBITMQ_DEFAULT_PASS`: secure_pass_2024!
- `RABBITMQ_CONNECTION`: amqp://admin:secure_pass_2024!@rabbitmq:5672/

### Portas

- API: 5001
- PostgreSQL: 5432
- RabbitMQ AMQP: 5672
- RabbitMQ Management: 15672

### Credenciais Importantes

PostgreSQL:
- Host: `localhost`
- Port: `5432`
- Database: `appdb`
- User: `appuser`
- Password: `apppass`

---

## Banco de Dados

### Tabelas

- Motos: Armazena informações das motocicletas
- Entregadores: Armazena dados dos entregadores
- Locacoes: Armazena as locações realizadas
- Notificacoes: Armazena notificações de motos de 2024

### Visualizar o Banco (via Terminal)

```bash
# Listar tabelas
docker-compose exec postgres_db psql -U appuser -d appdb -c "\dt"

# Ver dados
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT * FROM \"Motos\";"

# Ver estrutura
docker-compose exec postgres_db psql -U appuser -d appdb -c "\d \"Entregadores\""
```

#### Outra Opção

pgAdmin
- Download: https://www.pgadmin.org/download/
- Host: `localhost`, Port: `5432`, Database: `appdb`, Username: `appuser`, Password: `apppass`

---

## Sistema de Mensageria

A aplicação utiliza o RabbitMQ para publicar eventos e processar notificações de forma assíncrona

Acesso:
- URL: http://localhost:15672
- Usuário: `admin`
- Senha: `secure_pass_2024!`

### Como Funciona

1. Quando uma moto é cadastrada um evento é publicado no RabbitMQ
2. Um consumidor escuta esse evento e processa a mensagem
3. Se a moto for de 2024 cria automaticamente uma notificação no banco de dados

### Fluxo Completo

```
Cliente → POST /motos → API salva moto → Publica evento "moto.cadastrada" → RabbitMQ → 
Consumer processa → Se Ano == 2024 → Cria Notificação no banco
```

### Eventos Publicados

moto.cadastrada
- Publicado quando uma moto é cadastrada
- Contém: MotoId, Ano, Modelo, Placa, DataCadastro


#### Logs da API

```bash
docker-compose logs api | grep -i "rabbitmq\|consumidor\|notificação"
```

Logs esperados:
```
RabbitMQ consumer started. Waiting for messages...
Notification created for 2024 moto: ABCD123
```

#### Banco de Dados

```bash
# Verificar notificações criadas
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT * FROM \"Notificacoes\" ORDER BY \"DataNotificacao\" DESC;"

# Verificar motos de 2024
docker-compose exec postgres_db psql -U appuser -d appdb -c "SELECT \"Id\", \"Ano\", \"Placa\", \"Modelo\" FROM \"Motos\" WHERE \"Ano\" = 2024;"
```
### Testes

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
   - Moto cadastrada (resposta 201)
   - Mensagem publicada no RabbitMQ
   - Consumidor processa a mensagem
   - Notificação criada no banco

5. Verificar:
   - RabbitMQ: Ver mensagem na fila
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
   - Moto cadastrada
   - Mensagem publicada no RabbitMQ
   - Consumidor processa, mas não cria notificação (ano != 2024)

---

## Swagger

A documentação interativa da API está disponível em:

**http://localhost:5001/swagger**

No Swagger você pode:
- Ver todos os endpoints
- Testar requisições diretamente
- Ver exemplos de request/response

---

## Testes Unitários

### Executar Testes

Usando Makefile
```bash
make test              # Executa todos os testes
make test-unit         # Executa apenas testes unitários
make test-integration  # Executa apenas testes de integração
make test-verbose      # Executa testes com saída detalhada
```

Comando direto
```bash
dotnet test Mottu.Tests/Mottu.Tests.csproj
```

### Cobertura de Testes

1. ID Generation Tests (`IdGeneratorTests.cs`)
   - Geração de ID para entregadores (baseado em CNPJ)
   - Geração de ID para motos (baseado em placa)
   - Consistência e unicidade de IDs

2. CNH Validation Tests (`CnhValidationTests.cs`)
   - Validação de tipos CNH válidos (A, B, A+B)
   - Validação de tipos CNH inválidos
   - Validação case-insensitive
   - Validação para aluguel de motos

3. Rental Calculation Tests (`LocacaoCalculoTests.cs`)
   - Cálculo de valores totais por plano
   - Cálculo de multas para devolução antecipada
   - Cálculo de diárias adicionais para devolução atrasada
   - Cálculo de datas de início e término

4. Plan Validation Tests (`PlanoValidationTests.cs`)
   - Validação de planos válidos (7, 15, 30, 45, 50 dias)
   - Validação de planos inválidos
   - Validação de valores por dia

5. Date Calculation Tests (`DataCalculationTests.cs`)
   - Cálculo de data de início (meia-noite do dia seguinte)
   - Cálculo de data de término prevista
   - Cálculo de dias não usados
   - Cálculo de dias de atraso

---

## Verificação de Problemas

### API não inicia
- Verifique se o PostgreSQL está rodando: `docker-compose ps`
- Verifique os logs: `docker-compose logs api`

### Erro de conexão com banco
- Verifique se as migrations foram aplicadas
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
- Verifique se a fila existe no RabbitMQ

### Notificações não estão sendo criadas
- Verifique se a moto é de 2024
- Verifique se o consumidor processou a mensagem: `docker-compose logs api | grep "Notification created"`
- Verifique erros no processamento: `docker-compose logs api | grep -i "erro\|exception"`

### RabbitMQ não abre
- Verifique se o container está rodando: `docker-compose ps rabbitmq`
- Verifique se a porta está mapeada: `docker-compose ps | grep 15672`
- Verifique as credenciais (usuário e senha)

---

- Swagger: http://localhost:5001/swagger
- Swagger de referência: https://app.swaggerhub.com/apis-docs/App/app_backend/1.0.0

---

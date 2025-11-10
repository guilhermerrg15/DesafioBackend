# Guia de Apresentação da Aplicação

Este documento explica como funciona cada parte da aplicação para que você possa apresentá-la com confiança.

---

## 📋 Índice

1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Estrutura do Projeto](#2-estrutura-do-projeto)
3. [Fluxo de Dados](#3-fluxo-de-dados)
4. [Sistema de Mensageria (RabbitMQ)](#4-sistema-de-mensageria-rabbitmq)
5. [Geração de IDs](#5-geração-de-ids)
6. [Endpoints e Regras de Negócio](#6-endpoints-e-regras-de-negócio)
7. [Banco de Dados](#7-banco-de-dados)
8. [Testes](#8-testes)
9. [Docker e Deploy](#9-docker-e-deploy)

---

### Arquitetura em Camadas

A aplicação segue uma arquitetura em camadas:

```
Camada de Apresentação
- Recebe requisições HTTP
- Valida dados de entrada
- Chama serviços


Camada de Infraestrutura
- Acesso a dados (EF Core)
- Serviços externos (RabbitMQ)
- Implementações concretas


Camada de Domínio
- Entidades de negócio
- DTOs (Data Transfer Objects)
- Interfaces/Contratos

```

### Por que essa arquitetura?

- Cada camada tem uma função específica
- Fácil de testar cada camada isoladamente
- Mudanças em uma camada não afetam outras

---

## Estrutura do Projeto

### Mottu.Core (Camada de Domínio)

- Contém as regras de negócio e estruturas de dados.

Componentes principais:

1. Entities (`Mottu.Core/Entities/`)
   - `Moto.cs`: Representa uma motocicleta
   - `Entregador.cs`: Representa um entregador
   - `Locacao.cs`: Representa um aluguel
   - `Notificacao.cs`: Representa uma notificação

2. DTOs (`Mottu.Core/DTO/`)
   - `MotoCadastroDto.cs`: Dados para cadastrar moto
   - `EntregadorCadastroDto.cs`: Dados para cadastrar entregador
   - `LocacaoCriacaoDto.cs`: Dados para criar locação
   - `LocacaoDevolucaoDto.cs`: Dados para devolver locação

3. Interfaces (`Mottu.Core/Services/`)
   - `IMessageService.cs`: Contrato para serviço de mensageria
   - Permite trocar implementação sem mudar código que usa

### Mottu.Infrastructure (Camada de Infraestrutura)

-Implementa acesso a dados e serviços externos.

Componentes principais:

1. Data Access (`Mottu.Infrastructure/Data/`)
   - `MottuDbContext.cs`: Contexto do Entity Framework Core
     - Gerencia conexão com PostgreSQL
     - Define relacionamentos entre entidades
     - Configura índices únicos (CNPJ, Placa, CNH)

2. Migrations (`Mottu.Infrastructure/Migrations/`)
   - Histórico de mudanças no banco de dados
   - Permite versionar o "schema" do banco

3. **Services** (`Mottu.Infrastructure/Services/`)
   - `RabbitMQMessageService.cs`: Implementação do serviço de mensageria
     - Conecta ao RabbitMQ
     - Publica mensagens no exchange
     - Gerencia filas

### Mottu.Api (Camada de Apresentação)

-Expõe a API REST e orquestra as operações.

Componentes principais:

1. Program.cs: Arquivo principal
   - Configura serviços (DI, EF Core, RabbitMQ)
   - Define endpoints REST
   - Configura pipeline HTTP (Swagger, HTTPS)
   - Inicia consumer do RabbitMQ

2. Endpoints: 11 endpoints REST
   - Moto: 5 endpoints (POST, GET, GET/{id}, PUT/{id}/placa, DELETE/{id})
   - Entregador: 2 endpoints (POST, POST/{id}/cnh)
   - Locação: 4 endpoints (POST, GET, GET/{id}, PUT/{id}/devolucao)

### Mottu.Tests (Camada de Testes)

-Garantir qualidade e funcionamento correto.

Tipos de testes:

1. Testes Unitários: Testam lógica isolada
   - Geração de IDs
   - Validações de CNH
   - Cálculos de valores
   - Cálculos de datas

2. Testes de Integração: Testam fluxos completos
   - Endpoints HTTP
   - Interação com banco de dados
   - Fluxos de negócio completos

---

## 3. Fluxo de Dados

### Fluxo Típico: Cadastrar uma Moto

```
1. Cliente faz requisição HTTP
   POST /motos
   {
     "ano": 2024,
     "modelo": "Honda CB 600F",
     "placa": "ABC1234"
   }

2. Program.cs recebe no endpoint MapPost("/motos")
   
3. Validações:
   - Verifica se placa já existe
   - Gera ID baseado na placa com hash
   - Resolve colisões de hash se necessário
   
4. Cria entidade Moto
   
5. Salva no banco via Entity Framework Core
   db.Motos.Add(newMoto)
   db.SaveChangesAsync()
   
6. Publica evento no RabbitMQ
   messageService.PublishMotoCadastradaAsync(...)
   
7. Retorna resposta HTTP 201 Created
   {
     "id": 123456,
     "ano": 2024,
     "modelo": "Honda CB 600F",
     "placa": "ABC1234"
   }
```

### Fluxo: Consumer do RabbitMQ

```
1. Consumer está sempre escutando a fila "moto_cadastrada_queue"
   
2. Quando uma mensagem chega:
   - Deserializa JSON para MotoRegisteredEvent
   - Verifica se Ano == 2024
   
3. Se for 2024:
   - Cria entidade Notificacao
   - Salva no banco de dados
   - Log: "Notification created for 2024 moto: ABC1234"
   
4. Confirma processamento (ACK)
   - Remove mensagem da fila
   - Se houver erro, faz NACK e reenvia para fila
```

---

## 4. Sistema de Mensageria (RabbitMQ)

### Componentes

1. Exchange (`mottu_exchange`)
   - Tipo: Topic
   - Função: Roteia mensagens para filas baseado em routing key

2. Queue (`moto_cadastrada_queue`)
   - Durable: Sim (sobrevive a reinicializações)
   - Função: Armazena mensagens até serem processadas

3. Routing Key (`moto.cadastrada`)
   - Função: Identifica o tipo de evento
   - Permite filtrar mensagens por tipo

4. Binding
   - Conecta fila ao exchange com routing key específica


### Implementação

Publisher (`RabbitMQMessageService.cs`):
- Conecta ao RabbitMQ na inicialização
- Cria exchange, queue e binding
- Serializa objeto para JSON
- Publica mensagem com routing key

Consumer (`Program.cs`):
- Roda em background (Task.Run)
- Escuta fila continuamente
- Processa mensagens assincronamente
- Cria notificações no banco se necessário

---

## 5. Geração de IDs

- Hash pode gerar mesmo ID para CNPJs/Placas diferentes.

- Incrementa ID até encontrar um disponível


### Vantagens

- Mesmo CNPJ/Placa sempre gera mesmo ID
- Não precisa consultar banco para gerar ID

---


## 6. Banco de Dados

### Estrutura das Tabelas

#### Motos
```sql
CREATE TABLE "Motos" (
    "Id" INTEGER PRIMARY KEY,
    "Ano" INTEGER NOT NULL,
    "Modelo" TEXT NOT NULL,
    "Placa" TEXT NOT NULL UNIQUE
);
```

#### Entregadores
```sql
CREATE TABLE "Entregadores" (
    "Id" INTEGER PRIMARY KEY,
    "Nome" TEXT NOT NULL,
    "Cnpj" TEXT NOT NULL UNIQUE, 
    "DataNascimento" TIMESTAMP NOT NULL,
    "NumeroCnh" TEXT NOT NULL UNIQUE,
    "TipoCnh" TEXT NOT NULL,
    "ImagemCnhUrl" TEXT
);
```

#### Locacoes
```sql
CREATE TABLE "Locacoes" (
    "Id" INTEGER PRIMARY KEY,
    "MotoId" INTEGER NOT NULL,
    "EntregadorId" INTEGER NOT NULL,
    "PlanoDias" INTEGER NOT NULL,
    "DataInicio" TIMESTAMP NOT NULL,
    "DataTerminoPrevista" TIMESTAMP NOT NULL,
    "DataTerminoReal" TIMESTAMP NULL,
    "ValorTotal" DECIMAL NOT NULL,
    FOREIGN KEY ("MotoId") REFERENCES "Motos"("Id")
);
```

#### Notificacoes
```sql
CREATE TABLE "Notificacoes" (
    "Id" INTEGER PRIMARY KEY,
    "MotoId" INTEGER NOT NULL,
    "AnoMoto" INTEGER NOT NULL,
    "Mensagem" TEXT NOT NULL,
    "DataNotificacao" TIMESTAMP NOT NULL
);
```

### Entity Framework Core

Configurações importantes (`MottuDbContext.cs`):

1. Índices Únicos:
   - Placa (Motos)
   - CNPJ (Entregadores)
   - Número CNH (Entregadores)

2. Relacionamentos:
   - Locacao → Moto (muitos para um)
   - Locacao → Entregador (uitos para um)

3. Migrations:
   - Histórico de mudanças no schema

---

## 8. Testes

87 testes no total:
- 68 testes unitários
- 19 testes de integração

### Testes Unitários

Testam lógica isolada:

1. IdGeneratorTests.cs
   - Geração de IDs para entregadores (CNPJ)
   - Geração de IDs para motos (Placa)
   - Consistência (mesmo input = mesmo output)
   - Ignora formatação (CNPJ com/sem pontos)

2. CnhValidationTests.cs
   - Tipos CNH válidos (A, B, A+B)
   - Tipos CNH inválidos
   - Case-insensitive
   - Validação para aluguel de motos

3. LocacaoCalculoTests.cs
   - Cálculo de valores totais por plano
   - Cálculo de multas (7 dias: 20%, 15 dias: 40%)
   - Cálculo de diárias adicionais (R$ 50/dia)
   - Cálculo de devolução no prazo

4. PlanoValidationTests.cs
   - Planos válidos (7, 15, 30, 45, 50)
   - Planos inválidos
   - Valores corretos por plano

5. DataCalculationTests.cs
   - Data início (meia-noite do dia seguinte)
   - Data término prevista
   - Dias não usados
   - Dias de atraso

### Testes de Integração

Testam fluxos completos, incluindo HTTP e banco de dados:

1. MotoEndpointsTests.cs
   - POST /motos (criar, conflito de placa)
   - GET /motos (listar, filtrar)
   - GET /motos/{id} (buscar, não encontrado)
   - PUT /motos/{id}/placa (atualizar)
   - DELETE /motos/{id} (remover, conflito com locação)

2. EntregadorEndpointsTests.cs
   - POST /entregadores (criar, conflito CNPJ, CNH inválida, aceita A+B)
   - POST /entregadores/{id}/cnh (upload)

3. LocacaoEndpointsTests.cs
   - POST /locacoes (criar, plano inválido, CNH inválida, moto já locada)
   - PUT /locacoes/{id}/devolucao (calcular multa)
   - GET /locacoes (listar)

- xUnit: Framework de testes
- Microsoft.AspNetCore.Mvc.Testing: Testa API HTTP
- EntityFrameworkCore.InMemory: Banco em memória para testes

---

## 9. Docker e Deploy

### Docker Compose

3 serviços (containers):

1. **postgres_db** (PostgreSQL)
   - Imagem: postgres:15-alpine
   - Porta: 5432
   - Volume persistente para dados

2. **rabbitmq** (RabbitMQ)
   - Imagem: rabbitmq:3-management-alpine
   - Portas: 5672 (AMQP), 15672 (Management UI)
   - Volume persistente para dados

3. **mottu_api** (API .NET)
   - Build a partir de Dockerfile
   - Porta: 5001 (mapeada para 8080 interno)
   - Depende de postgres_db e rabbitmq
   - Volume para storage (fotos CNH)

### Dockerfile

1. (build): Compila aplicação
2. runtime: Imagem final leve (apenas runtime)

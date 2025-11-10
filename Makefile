.PHONY: help test test-unit test-integration test-verbose build up down restart logs logs-api logs-db logs-rabbitmq clean migrate

# Default target
help:
	@echo "Available commands:"
	@echo "  make test              - Run all tests"
	@echo "  make test-unit         - Run only unit tests"
	@echo "  make test-integration  - Run only integration tests"
	@echo "  make test-verbose      - Run tests with verbose output"
	@echo "  make build             - Build the solution"
	@echo "  make up                - Start all Docker containers"
	@echo "  make down              - Stop all Docker containers"
	@echo "  make restart           - Restart all Docker containers"
	@echo "  make logs              - Show logs from all containers"
	@echo "  make logs-api          - Show logs from API container"
	@echo "  make logs-db           - Show logs from PostgreSQL container"
	@echo "  make logs-rabbitmq     - Show logs from RabbitMQ container"
	@echo "  make migrate           - Apply database migrations"
	@echo "  make clean             - Clean build artifacts and stop containers"

# Test commands
test:
	@echo "Running all tests..."
	dotnet test Mottu.Tests/Mottu.Tests.csproj --verbosity minimal

test-unit:
	@echo "Running unit tests..."
	dotnet test Mottu.Tests/Mottu.Tests.csproj --filter "FullyQualifiedName!~IntegrationTests" --verbosity minimal

test-integration:
	@echo "Running integration tests..."
	dotnet test Mottu.Tests/Mottu.Tests.csproj --filter "FullyQualifiedName~IntegrationTests" --verbosity minimal

test-verbose:
	@echo "Running all tests with verbose output..."
	dotnet test Mottu.Tests/Mottu.Tests.csproj --verbosity normal

# Build commands
build:
	@echo "Building solution..."
	dotnet build

# Docker commands
up:
	@echo "Starting Docker containers..."
	@echo "Step 1: Starting PostgreSQL and RabbitMQ..."
	docker-compose up -d postgres_db rabbitmq
	@echo "Waiting for services to be ready..."
	@sleep 5
	@echo "Step 2: Starting API..."
	docker-compose up -d mottu_api
	@echo "Waiting for API to initialize..."
	@sleep 3
	@echo "Services started! Checking status..."
	@docker-compose ps
	@echo ""
	@echo "Swagger should be available at: http://localhost:5001/swagger"

down:
	@echo "Stopping Docker containers..."
	docker-compose down

restart:
	@echo "Restarting Docker containers..."
	docker-compose restart

# Logs commands
logs:
	docker-compose logs -f

logs-api:
	docker-compose logs -f mottu_api

logs-db:
	docker-compose logs -f postgres_db

logs-rabbitmq:
	docker-compose logs -f rabbitmq

# Database commands
migrate:
	@echo "Applying database migrations..."
	dotnet ef database update --project Mottu.Infrastructure/Mottu.Infrastructure.csproj --startup-project Mottu.Api/Mottu.Api.csproj --connection "Host=localhost;Port=5432;Database=mottudb;Username=mottuuser;Password=mottupass"

# Clean commands
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean
	@echo "Stopping containers..."
	docker-compose down
	@echo "Done!"


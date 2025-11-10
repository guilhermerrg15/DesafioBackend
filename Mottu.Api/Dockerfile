# Estágio de Build: Utiliza o SDK para compilar o código
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia o arquivo de solução e os arquivos .csproj para restaurar dependências
# O uso de wildcards ajuda o Docker a cachear a camada de restore
COPY *.sln .
COPY Mottu.Api/*.csproj Mottu.Api/
COPY Mottu.Core/*.csproj Mottu.Core/
COPY Mottu.Infrastructure/*.csproj Mottu.Infrastructure/

# Restaura as dependências
RUN dotnet restore

# Copia todo o código-fonte
COPY . .

# Publica a aplicação
RUN dotnet publish "Mottu.Api/Mottu.Api.csproj" -c Release -o /app/publish

# Estágio Final: Utiliza a imagem de runtime, menor e mais segura
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copia os arquivos publicados do estágio de build
COPY --from=build /app/publish .

# =========================================================
# SOLUÇÃO PARA O ERRO 500 (Permissão de escrita)
# 1. Cria o diretório 'storage' dentro do container.
# 2. Define permissão de escrita total (chmod 777) para o diretório
#    garantindo que o usuário não-root (padrão) consiga salvar arquivos.
RUN mkdir -p /app/storage && chmod 777 /app/storage
# =========================================================

# Define o ponto de entrada para a aplicação
ENTRYPOINT ["dotnet", "Mottu.Api.dll"]
# Compila com o SDK; copia o projeto antes do código para reutilizar o restore.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /codigo
COPY src/Fcg.Users.Domain/Fcg.Users.Domain.csproj src/Fcg.Users.Domain/
COPY src/Fcg.Users.Application/Fcg.Users.Application.csproj src/Fcg.Users.Application/
COPY src/Fcg.Users.Infrastructure/Fcg.Users.Infrastructure.csproj src/Fcg.Users.Infrastructure/
COPY src/Fcg.Users.API/Fcg.Users.API.csproj src/Fcg.Users.API/
RUN dotnet restore src/Fcg.Users.API/Fcg.Users.API.csproj

# Publica somente os arquivos necessários para executar a aplicação.
COPY src/ src/
RUN dotnet publish src/Fcg.Users.API/Fcg.Users.API.csproj -c Release --no-restore -o /publicado

# A imagem final contém o runtime, sem SDK nem código-fonte.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publicado .

# Usa o usuário sem privilégios fornecido pela imagem .NET (porta padrão 8080).
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Fcg.Users.API.dll"]

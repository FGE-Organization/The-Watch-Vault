FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["The-Watch-Vault.csproj", "./"]
RUN dotnet restore "The-Watch-Vault.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "The-Watch-Vault.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "The-Watch-Vault.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "The-Watch-Vault.dll"]

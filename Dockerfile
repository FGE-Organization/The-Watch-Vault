# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["The-Watch-Vault.csproj", "./"]
RUN dotnet restore "The-Watch-Vault.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "The-Watch-Vault.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "The-Watch-Vault.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Render expects the app to listen on a port provided by the $PORT env var.
# ASP.NET Core 8+ automatically respects the PORT env var if configured.
ENV ASPNETCORE_URLS=http://+:10000

# Expose the port
EXPOSE 10000

ENTRYPOINT ["dotnet", "The-Watch-Vault.dll"]

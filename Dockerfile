# ── Stage 1: Build ───────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Копируем csproj отдельно для кеширования restore
COPY LibNode.Api.csproj ./
RUN dotnet restore

# Копируем остальное и собираем
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ─────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Копируем собранные артефакты
COPY --from=build /app/publish .

# Порт, на котором слушает Kestrel
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "LibNode.Api.dll"]

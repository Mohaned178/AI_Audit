FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["AIUsageGuard.slnx", "./"]
COPY ["src/AIUsageGuard.Api/AIUsageGuard.Api.csproj", "src/AIUsageGuard.Api/"]
COPY ["src/AIUsageGuard.Application/AIUsageGuard.Application.csproj", "src/AIUsageGuard.Application/"]
COPY ["src/AIUsageGuard.Domain/AIUsageGuard.Domain.csproj", "src/AIUsageGuard.Domain/"]
COPY ["src/AIUsageGuard.Infrastructure/AIUsageGuard.Infrastructure.csproj", "src/AIUsageGuard.Infrastructure/"]

RUN dotnet restore "src/AIUsageGuard.Api/AIUsageGuard.Api.csproj"

COPY . .
RUN dotnet publish "src/AIUsageGuard.Api/AIUsageGuard.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "AIUsageGuard.Api.dll"]

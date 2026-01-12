# Enterprise API - .NET Core 8 Web API

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/EnterpriseApi.WebApi/EnterpriseApi.WebApi.csproj", "src/EnterpriseApi.WebApi/"]
COPY ["src/EnterpriseApi.Application/EnterpriseApi.Application.csproj", "src/EnterpriseApi.Application/"]
COPY ["src/EnterpriseApi.Domain/EnterpriseApi.Domain.csproj", "src/EnterpriseApi.Domain/"]
COPY ["src/EnterpriseApi.Infrastructure/EnterpriseApi.Infrastructure.csproj", "src/EnterpriseApi.Infrastructure/"]

RUN dotnet restore "src/EnterpriseApi.WebApi/EnterpriseApi.WebApi.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/EnterpriseApi.WebApi"
RUN dotnet build "EnterpriseApi.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EnterpriseApi.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseApi.WebApi.dll"]

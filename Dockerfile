# Enterprise API - .NET Core 8 Web API

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/Enterprise.WebApi/Enterprise.WebApi.csproj", "src/Enterprise.WebApi/"]
COPY ["src/Enterprise.Application/Enterprise.Application.csproj", "src/Enterprise.Application/"]
COPY ["src/Enterprise.Domain/Enterprise.Domain.csproj", "src/Enterprise.Domain/"]
COPY ["src/Enterprise.Infrastructure/Enterprise.Infrastructure.csproj", "src/Enterprise.Infrastructure/"]

RUN dotnet restore "src/Enterprise.WebApi/Enterprise.WebApi.csproj"

# Copy all source files
COPY . .

# Build the application
WORKDIR "/src/src/Enterprise.WebApi"
RUN dotnet build "Enterprise.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Enterprise.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Enterprise.WebApi.dll"]

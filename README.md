# Enterprise API - .NET Core 8 Web API

An enterprise-ready ASP.NET Core 8 Web API implementing **Clean Architecture** with **CQRS pattern**, following best practices and modern development patterns.

## 🏗️ Architecture

This project follows **Clean Architecture** with **CQRS (Command Query Responsibility Segregation)** using MediatR:

```
Enterprise/
├── src/
│   ├── EnterpriseApi.Domain/          # Domain entities and interfaces
│   ├── EnterpriseApi.Application/     # CQRS Commands, Queries, Handlers
│   │   └── Features/
│   │       └── Products/
│   │           ├── Commands/          # Write operations
│   │           └── Queries/           # Read operations
│   ├── EnterpriseApi.Infrastructure/  # Data access, EF Core, repositories
│   └── EnterpriseApi.WebApi/          # API controllers, middleware
├── docs/
│   └── CQRS-ARCHITECTURE.md           # Detailed architecture guide
├── .github/
│   └── copilot-instructions.md
├── Dockerfile
├── docker-compose.yml
└── EnterpriseApi.sln
```

### Dependency Flow

```
API Layer → Application Layer + Infrastructure Layer → Domain Layer
```

## ✨ Features

### Architecture Patterns

- **Clean Architecture** - Domain, Application, Infrastructure, and Presentation layers
- **CQRS Pattern** - Separate read and write operations using MediatR
- **Repository Pattern** - Data access abstraction
- **Unit of Work Pattern** - Transaction management

### Technical Stack

- **Entity Framework Core 8** - Database access with SQL Server
- **MediatR** - CQRS and mediator pattern implementation
- **AutoMapper** - Object-to-object mapping
- **FluentValidation** - Input validation with command validators
- **JWT Authentication** - Secure API endpoints
- **Swagger/OpenAPI** - API documentation and testing
- **Serilog** - Structured logging to console and files
- **Global Error Handling** - Centralized exception middleware
- **Health Checks** - Monitoring endpoint
- **CORS Support** - Cross-origin resource sharing
- **Docker Support** - Containerization with Docker and Docker Compose

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server/sql-server-downloads) or SQL Server LocalDB
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (optional)

### Installation

1. **Clone the repository**

   ```bash
   git clone <your-repo-url>
   cd Enterprise
   ```

2. **Update the connection string**

   Edit `src/EnterpriseApi.WebApi/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseApiDb;Trusted_Connection=true;MultipleActiveResultSets=true"
   }
   ```

3. **Install dependencies**

   ```bash
   dotnet restore
   ```

4. **Apply database migrations**

   ```bash
   dotnet ef migrations add InitialCreate --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
   dotnet ef database update --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
   ```

5. **Build the solution**

   ```bash
   dotnet build
   ```

6. **Run the application**

   ```bash
   cd src/EnterpriseApi.WebApi
   dotnet run
   ```

7. **Access the API**
   - Swagger UI: `https://localhost:5001` or `http://localhost:5000`
   - Health Check: `https://localhost:5001/health`

## 🐳 Docker

### Using Docker Compose

1. **Build and run with Docker Compose**

   ```bash
   docker-compose up -d
   ```

2. **Access the API**
   - API: `http://localhost:5000`
   - SQL Server: `localhost:1433`

3. **Stop the containers**

   ```bash
   docker-compose down
   ```

### Using Dockerfile

```bash
docker build -t enterprise-api .
docker run -p 5000:80 enterprise-api
```

## 📡 API Endpoints

### Products

- `GET /api/products` - Get all products
- `GET /api/products/{id}` - Get product by ID
- `GET /api/products/category/{category}` - Get products by category
- `POST /api/products` - Create a new product
- `PUT /api/products/{id}` - Update a product
- `DELETE /api/products/{id}` - Delete a product

### Health

- `GET /api/health` - Health check endpoint
- `GET /health` - Application health check

## 🔧 Configuration

### JWT Settings

Configure JWT in `appsettings.json`:

```json
"JwtSettings": {
  "SecretKey": "YourSuperSecretKeyForJWTTokenGeneration123456",
  "Issuer": "EnterpriseAPI",
  "Audience": "EnterpriseAPIUsers",
  "ExpirationInMinutes": 60
}
```

### Database

The project uses Entity Framework Core with SQL Server. To use a different database:

1. Install the appropriate EF Core provider
2. Update the connection string in `appsettings.json`
3. Modify `DependencyInjection.cs` in Infrastructure layer

### Logging

Logs are written to:

- Console (all environments)
- Files in `logs/` directory (rotating daily)

## 🧪 Testing

### Using Swagger

1. Run the application
2. Navigate to `https://localhost:5001`
3. Use the Swagger UI to test endpoints

### Example Product Creation

```json
{
  "name": "Sample Product",
  "description": "This is a sample product",
  "price": 29.99,
  "stock": 100,
  "category": "Electronics",
  "sku": "PROD-001"
}
```

## 📦 Project Structure

### Domain Layer

Contains enterprise business rules and entities:

- `Entities/` - Domain entities (Product)
- `Interfaces/` - Repository interfaces
- `Common/` - Base entity and shared domain logic

### Application Layer

Contains application business rules:

- `DTOs/` - Data Transfer Objects
- `Interfaces/` - Service interfaces
- `Services/` - Service implementations
- `Mappings/` - AutoMapper profiles
- `Validators/` - FluentValidation validators

### Infrastructure Layer

Contains external concerns:

- `Persistence/` - EF Core DbContext
- `Repositories/` - Repository implementations

### Web API Layer

Contains API controllers and middleware:

- `Controllers/` - API endpoints
- `Middleware/` - Error handling middleware
- `Common/` - API response models

## 🛠️ Development

### Adding a New Entity

1. Create entity in `Domain/Entities/`
2. Add DbSet to `ApplicationDbContext`
3. Create DTOs in `Application/DTOs/`
4. Add validators in `Application/Validators/`
5. Create service interface and implementation
6. Add controller in `WebApi/Controllers/`
7. Create and apply migration

### Creating Migrations

```bash
dotnet ef migrations add MigrationName --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
dotnet ef database update --project src/EnterpriseApi.Infrastructure --startup-project src/EnterpriseApi.WebApi
```

## 📝 Best Practices Implemented

- ✅ Clean Architecture
- ✅ SOLID Principles
- ✅ Repository Pattern
- ✅ Unit of Work Pattern
- ✅ Dependency Injection
- ✅ Async/Await throughout
- ✅ Global exception handling
- ✅ Request validation
- ✅ Structured logging
- ✅ API versioning ready
- ✅ Soft delete pattern
- ✅ Docker support

## 🔐 Security

- JWT Bearer token authentication configured
- HTTPS redirection enabled
- Input validation with FluentValidation
- Secure connection strings in configuration

## 📄 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## 📞 Support

For issues and questions, please open an issue in the repository.

---

**Built with ❤️ using .NET 8**

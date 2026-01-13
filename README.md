# Enterprise API - .NET Core 8 Web API

An enterprise-ready ASP.NET Core 8 Web API implementing **Clean Architecture** with **CQRS pattern**, featuring **MediatR pipeline behaviors**, comprehensive validation, logging, and best practices.

> **✨ Recently Enhanced** - Performance optimizations, security hardening, API versioning, and enhanced health checks. See [IMPROVEMENTS-APPLIED.md](docs/IMPROVEMENTS-APPLIED.md) for details.

## 🏗️ Architecture

This project follows **Clean Architecture** with **CQRS (Command Query Responsibility Segregation)** using MediatR:

```
Enterprise/
├── src/
│   ├── Enterprise.Domain/          # Domain entities and interfaces
│   ├── Enterprise.Application/     # CQRS Commands, Queries, Handlers
│   │   ├── Features/
│   │   │   └── Products/
│   │   │       ├── Commands/          # Write operations (Create, Update, Delete)
│   │   │       └── Queries/           # Read operations (Get, GetAll, etc.)
│   │   └── Common/
│   │       ├── Behaviors/             # MediatR pipeline behaviors
│   │       ├── Exceptions/            # Custom exceptions
│   │       └── Models/                # Shared models (Pagination, etc.)
│   ├── Enterprise.Infrastructure/  # Data access, EF Core, repositories
│   └── Enterprise.WebApi/          # API controllers, middleware
├── tests/
│   └── Enterprise.Application.Tests/ # Unit tests
├── docs/
│   └── CQRS-ARCHITECTURE.md           # Detailed architecture guide
├── .github/
│   └── copilot-instructions.md
├── Dockerfile
├── docker-compose.yml
└── Enterprise.sln
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
- **Pipeline Behaviors** - Cross-cutting concerns (validation, logging, performance monitoring)

### MediatR Pipeline Behaviors

The application implements three key pipeline behaviors that execute in order for every command/query:

1. **Logging Behavior** - Logs all requests with execution time
2. **Validation Behavior** - Automatic FluentValidation before handler execution
3. **Performance Behavior** - Monitors and logs slow requests (>500ms)

```csharp
Request → Logging → Validation → Performance → Handler → Response
```

### Technical Stack

- **Entity Framework Core 8** - Database access with SQL Server
- **MediatR 14** - CQRS and mediator pattern with pipeline behaviors
- **AutoMapper** - Object-to-object mapping
- **FluentValidation** - Automatic input validation via pipeline behavior
- **JWT Authentication** - Secure API endpoints
- **Swagger/OpenAPI** - API documentation and testing
- **Serilog** - Structured logging to console and files
- **Global Error Handling** - Centralized exception middleware
- **Health Checks** - Monitoring endpoint
- **CORS Support** - Cross-origin resource sharing
- **Docker Support** - Containerization with Docker and Docker Compose
- **Pagination Support** - Built-in pagination for list queries

### Testing

- **xUnit** - Testing framework
- **Moq** - Mocking library
- **FluentAssertions** - Expressive assertions
- **Unit Tests** - Command handlers, query handlers, and pipeline behaviors

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

   Edit `src/Enterprise.WebApi/appsettings.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EnterpriseDb;Trusted_Connection=true;MultipleActiveResultSets=true"
   }
   ```

3. **Install dependencies**

   ```bash
   dotnet restore
   ```

4. **Apply database migrations**

   ```bash
   dotnet ef migrations add InitialCreate --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
   dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
   ```

5. **Seed the database (optional but recommended)**

   ```bash
   cd src/Enterprise.DataSeeder
   dotnet run -- seed
   ```

   This creates default users and sample data. See [DataSeeder CLI documentation](docs/DATASEEDER-CLI.md) for more options.

   **Default test users:**
   - `admin` / `Admin@123` (Admin role)
   - `manager` / `Manager@123` (Manager role)
   - `user` / `User@123` (User role)

6. **Build the solution**

   ```bash
   dotnet build
   ```

7. **Run the application**

   ```bash
   cd src/Enterprise.WebApi
   dotnet run
   ```

8. **Access the API**
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

> **Note:** All endpoints now use API versioning. Use `/api/v1/` prefix instead of `/api/`. See [API Versioning](#api-versioning) section.

### Products

- `GET /api/v1/products` - Get all products (cached 60s)
- `GET /api/v1/products/paginated?pageNumber=1&pageSize=10` - Get paginated products (cached 60s)
- `GET /api/v1/products/{id}` - Get product by ID
- `GET /api/v1/products/category/{category}` - Get products by category (cached 60s)
- `POST /api/v1/products` - Create a new product
- `PUT /api/v1/products/{id}` - Update a product
- `DELETE /api/v1/products/{id}` - Delete a product

### Authentication

- `POST /api/v1/auth/register` - Register a new user
- `POST /api/v1/auth/login` - Login and get JWT token
- `POST /api/v1/auth/refresh` - Refresh access token

### Health

- `GET /health` - Basic health check
- `GET /health/ready` - Detailed health check with database status
- `GET /health/live` - Liveness probe

## 🔧 Configuration

### JWT Settings

⚠️ **Security Warning:** Never commit secrets to source control!

For **development**, use User Secrets:

```bash
cd src/Enterprise.WebApi
dotnet user-secrets init
dotnet user-secrets set "JwtSettings:SecretKey" "YourSecretKeyHere_Min32Characters!"
```

For **production**, use environment variables or Azure Key Vault.

See [SECURITY-CONFIGURATION.md](docs/SECURITY-CONFIGURATION.md) for detailed setup instructions.

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

### Run Unit Tests

```bash
dotnet test
```

The test project includes examples of:

- Testing command handlers (CreateProductCommandHandler)
- Testing query handlers (GetProductByIdQueryHandler)
- Testing pipeline behaviors (ValidationBehavior)
- Using Moq for mocking dependencies
- Using FluentAssertions for expressive assertions

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
dotnet ef migrations add MigrationName --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
dotnet ef database update --project src/Enterprise.Infrastructure --startup-project src/Enterprise.WebApi
```

## 📝 Best Practices Implemented

### Architecture & Design

- ✅ Clean Architecture with clear separation of concerns
- ✅ CQRS Pattern with MediatR
- ✅ SOLID Principles
- ✅ Repository Pattern with Unit of Work
- ✅ Dependency Injection throughout

### Performance & Optimization

- ✅ AsNoTracking() for read-only queries (30-40% performance gain)
- ✅ Response caching on GET endpoints
- ✅ Async/Await throughout
- ✅ Pagination support for list queries

### Security & Hardening

- ✅ JWT Bearer token authentication
- ✅ HTTPS redirection with HSTS
- ✅ Security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- ✅ User Secrets for development
- ✅ Environment-based configuration
- ✅ Production-ready CORS policy

### Monitoring & Observability

- ✅ Enhanced health checks with database connectivity
- ✅ Structured logging with Serilog
- ✅ Performance monitoring (slow request detection >500ms)
- ✅ Kubernetes readiness/liveness probes

### API Design

- ✅ API versioning with URL segments (`/api/v1/`)
- ✅ RESTful endpoints
- ✅ Swagger/OpenAPI documentation
- ✅ Global exception handling with RFC 7807 Problem Details

### Code Quality

- ✅ FluentValidation with automatic pipeline validation
- ✅ MediatR pipeline behaviors (Logging, Validation, Performance)
- ✅ Custom exceptions (NotFoundException, ValidationException)
- ✅ Unit tests with xUnit, Moq, and FluentAssertions
- ✅ Soft delete pattern

### DevOps

- ✅ Docker support with multi-stage builds
- ✅ Docker Compose for local development
- ✅ Database migration support

## 🔐 Security

- JWT Bearer token authentication with configurable expiration
- HTTPS redirection enabled with HSTS in production
- Security headers to prevent common attacks
- Input validation with FluentValidation via pipeline behavior
- Secure secrets management (User Secrets, Environment Variables, Key Vault)
- Production-ready CORS policy with origin whitelisting

See [SECURITY-CONFIGURATION.md](docs/SECURITY-CONFIGURATION.md) for security setup guide.

## 🚀 Performance Features

- **Query Optimization:** AsNoTracking() on all read operations
- **Response Caching:** 60-second cache on GET endpoints
- **Async Operations:** Non-blocking I/O throughout
- **Pagination:** Efficient data retrieval for large datasets
- **Performance Monitoring:** Automatic detection of slow queries

## 📚 Documentation

- [CQRS Architecture Guide](docs/CQRS-ARCHITECTURE.md) - Detailed architecture documentation
- [Security Configuration](docs/SECURITY-CONFIGURATION.md) - JWT and secrets management
- [Improvements Applied](docs/IMPROVEMENTS-APPLIED.md) - Recent enhancements and optimizations
- [Authentication Guide](AUTHENTICATION.md) - Authentication and authorization setup

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

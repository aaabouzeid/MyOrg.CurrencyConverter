# MyOrg.CurrencyConverter

A production-ready Currency Converter API built with .NET 10, featuring JWT authentication, PostgreSQL database, Redis caching, and comprehensive API documentation via Swagger.

## Features

- 🔐 **JWT Authentication** - Secure API access with Microsoft Identity
- 👥 **Role-Based Access Control** - Admin, Manager, and User roles with permissions
- ⏱️ **API Rate Limiting** - Protect against abuse with configurable throttling
- 📊 **OpenTelemetry Observability** - Distributed tracing with Client IP, User ID, and HTTP details
- 💰 **Currency Conversion** - Real-time exchange rates from multiple providers
- 🗄️ **PostgreSQL Database** - Persistent storage for users and data
- ⚡ **Redis Caching** - High-performance caching layer
- 📋 **Swagger UI** - Interactive API documentation
- 🔄 **Resilience Policies** - Retry and circuit breaker patterns with Polly
- 📝 **Structured Logging** - Comprehensive logging with Serilog
- 🐳 **Docker Support** - Full containerization with Docker Compose

## Quick Start with Docker (Recommended)

The fastest way to run the application with all dependencies (PostgreSQL + Redis + API):

```bash
# Start all services (migrations apply automatically)
docker-compose up -d

# View logs to see migration progress
docker-compose logs -f api
```

**That's it!** The database migrations are applied automatically on startup.

**Access Points:**
- **Web UI**: http://localhost:3000 (React application)
- **Swagger UI**: http://localhost:8080/swagger
- **API**: http://localhost:8080
- **PostgreSQL**: localhost:5432 (user: postgres, password: postgres_password_change_me)
- **Redis**: localhost:6379

**Default Login Credentials:**
- Email: `admin@admin.com`
- Password: `P@ssw0rd1234`

**Stop Services:**
```bash
docker-compose stop              # Stop containers
docker-compose down              # Remove containers
docker-compose down -v           # Remove containers and volumes
```

## Manual Setup

### Prerequisites

- .NET 10 SDK
- PostgreSQL 16+
- Redis (optional, for caching)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MyOrg.CurrencyConverter
   ```

2. **Configure Database**

   Update `appsettings.json` with your PostgreSQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=currencyconverter;Username=postgres;Password=yourpassword"
   }
   ```

3. **Apply Database Migrations**
   ```bash
   cd src/MyOrg.CurrencyConverter.API
   dotnet ef database update
   ```

4. **Run the Application**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**

   Navigate to: http://localhost:5000/swagger (or your configured port)

## API Authentication

The API uses JWT Bearer token authentication:

1. **Register a new user**: `POST /register`
2. **Login**: `POST /login` - Returns JWT token
3. **Authenticate in Swagger**:
   - Click the "Authorize" 🔓 button
   - Enter your JWT token
   - All subsequent requests will include the token

## Configuration

Key configuration sections in `appsettings.json`:

### Database
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=currencyconverter;Username=postgres;Password=yourpassword"
}
```

### JWT Settings
```json
"JwtSettings": {
  "SecretKey": "YourSecretKeyMustBeAtLeast32CharactersLong",
  "ExpirationMinutes": 60
}
```

### Cache (Redis)
```json
"Cache": {
  "Enabled": true,
  "ConnectionString": "localhost:6379",
  "DefaultTtlMinutes": 60
}
```

## Project Structure

```
MyOrg.CurrencyConverter/
├── src/
│   ├── MyOrg.CurrencyConverter.API/        # Main API project
│   └── CurrencyConverter.Web/              # React TypeScript web application
├── test/
│   └── MyOrg.CurrencyConverter.UnitTests/  # Unit tests
├── docker-compose.yml                      # Docker development setup
├── docker-compose.prod.yml                 # Docker production setup
├── .env.example                            # Environment variables template
└── README.md                               # This file
```

## Development

### Running Tests
```bash
dotnet test
```

### Building
```bash
dotnet build
```

### Creating Migrations
```bash
cd src/MyOrg.CurrencyConverter.API
dotnet ef migrations add YourMigrationName
dotnet ef database update
```

### Auto-Apply Migrations on Startup

**Enabled by default in Docker!** Migrations are automatically applied when the application starts in Docker environments.

To enable auto-migration for local development, add this to `appsettings.Development.json`:
```json
{
  "AutoMigrate": true
}
```

To disable auto-migration in Docker, remove or set to false:
```yaml
# In docker-compose.yml
environment:
  - AutoMigrate=false
```

## Docker Deployment

### Development
```bash
# Start all services (migrations apply automatically)
docker-compose up -d

# View logs
docker-compose logs -f
```

### Production

1. **Configure environment variables**
   ```bash
   cp .env.example .env
   # Edit .env and update all passwords and secrets
   ```

2. **Deploy with production configuration**
   ```bash
   # Start all services (migrations apply automatically)
   docker-compose -f docker-compose.prod.yml up -d

   # View logs
   docker-compose -f docker-compose.prod.yml logs -f
   ```

3. **Production checklist:**
   - ✅ Update all passwords in `.env` file
   - ✅ Change JWT secret key (minimum 32 characters)
   - ✅ Configure SSL/TLS certificates
   - ✅ Set up monitoring and logging
   - ✅ Implement automated PostgreSQL backups
   - ✅ Review resource limits in `docker-compose.prod.yml`

### Useful Docker Commands
```bash
# View logs
docker-compose logs -f api
docker-compose logs -f postgres

# Check service status
docker-compose ps

# Access PostgreSQL
docker-compose exec postgres psql -U postgres -d currencyconverter

# Access Redis CLI
docker-compose exec redis redis-cli

# Backup PostgreSQL
docker-compose exec postgres pg_dump -U postgres currencyconverter > backup.sql

# Restart services
docker-compose restart api
```

## Documentation

- API Documentation: Available via Swagger UI at `/swagger`
- Docker: See Docker Deployment section above

## Technologies

- **.NET 10** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM for database access
- **PostgreSQL** - Primary database
- **Redis** - Caching layer
- **Npgsql** - PostgreSQL provider for EF Core
- **Microsoft Identity** - Authentication and authorization
- **Polly** - Resilience and transient fault handling
- **Serilog** - Structured logging
- **Swashbuckle** - Swagger/OpenAPI documentation
- **FluentValidation** - Input validation

## License

[Your License Here]
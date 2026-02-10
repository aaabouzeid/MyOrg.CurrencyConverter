# Currency Converter

A production-ready Currency Converter API built with .NET 10, featuring JWT authentication, PostgreSQL database, Redis caching, and comprehensive API documentation via Swagger.

## Features

- 💰 **Currency Conversion** - Real-time exchange rates from multiple providers
- 🔐 **JWT Authentication** - Secure API access with Microsoft Identity
- 👥 **Role-Based Access Control** - Admin, Manager, and User roles with permissions
- ⏱️ **API Rate Limiting** - Protect against abuse with configurable throttling
- 📊 **OpenTelemetry Observability** - Distributed tracing with Client IP, User ID, and HTTP details
- 🗄️ **PostgreSQL Database** - Persistent storage for users identity
- ⚡ **Redis Caching** - High-performance caching layer
- 📋 **Swagger UI** - Interactive API documentation
- 🔄 **Resilience Policies** - Retry and circuit breaker patterns with Polly
- 📝 **Structured Logging** - Comprehensive logging with Serilog
- 🐳 **Docker Support** - Full containerization with Docker Compose

## Architecture Overview

### System Architecture

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│  React Frontend │─────▶│  ASP.NET Core API│─────▶│  PostgreSQL DB  │
│  (TypeScript)   │      │   (.NET 10)      │      │   (Identity)    │
└─────────────────┘      └──────────────────┘      └─────────────────┘
                                  │
                                  │
                         ┌────────┴────────┐
                         ▼                 ▼
                  ┌─────────────┐   ┌─────────────┐
                  │    Redis    │   │  External   │
                  │   (Cache)   │   │  Currency   │
                  │             │   │    APIs     │
                  └─────────────┘   └─────────────┘
```

### Layered Architecture

**Clean Architecture Pattern:**
- **API Layer**: Controllers, middleware, configuration
- **Core Layer**: Interfaces, DTOs, models, validators
- **Infrastructure Layer**: Data access, external services, caching
- **Services Layer**: Business logic, orchestration

### Key Design Patterns

1. **Factory Pattern**: Currency provider selection (easy to add new providers)
2. **Decorator Pattern**: Non-invasive caching layer
3. **Repository Pattern**: Data access abstraction
4. **Dependency Injection**: Loosely coupled components

### API Versioning

- **Current Version**: v1.0
- **URL Format**: `/api/v1/{controller}/{action}`
- **Versioning Strategy**: URL segment versioning
- **Future-Ready**: Easy to add v2, v3 without breaking existing clients

## Quick Start with Docker (Recommended)

### Development Environment
```bash
# Clone the repository
git clone <repository-url>
cd MyOrg.CurrencyConverter

# Start all services (API, Frontend, PostgreSQL, Redis)
docker-compose up -d

# View logs to see migration progress
docker-compose logs -f api
```

**Access Points:**
- **Web UI**: http://localhost:3000 (React application)
- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **PostgreSQL**: localhost:5432 (user: postgres, password: postgres_password_change_me)
- **Redis**: localhost:6379

**Default Login Credentials:**
- Email: `admin@admin.com`
- Password: `P@ssw0rd1234`

### Test Environment
```bash
# Start test environment (separate containers on different ports)
docker-compose -f docker-compose.test.yml up -d

# Access Points:
# - Web UI: http://localhost:3001
# - API: http://localhost:8082
# - Swagger: http://localhost:8082/swagger
```

**Stop Services:**
```bash
docker-compose stop              # Stop containers
docker-compose down              # Remove containers
docker-compose down -v           # Remove containers and volumes (clean slate)
```

## Manual Setup

### Prerequisites

**Backend:**
- .NET 10 SDK
- PostgreSQL 16+
- Redis 7+ (optional, for caching)

**Frontend:**
- Node.js 20+
- npm or yarn

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd MyOrg.CurrencyConverter
   ```

2. **Configure Database**

   Update `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=currencyconverter;Username=postgres;Password=yourpassword"
     }
   }
   ```

3. **Apply Database Migrations**
   ```bash
   cd src/MyOrg.CurrencyConverter.API
   dotnet ef database update
   ```

4. **Configure Settings** (Optional)

   Update `appsettings.json` for:
   - JWT secret key (production)
   - Redis connection string
   - Rate limiting
   - CORS allowed origins

5. **Run the API**
   ```bash
   dotnet run --project src/MyOrg.CurrencyConverter.API
   ```

6. **Access Swagger UI**

   Navigate to: http://localhost:5000/swagger

### Frontend Setup

1. **Navigate to frontend directory**
   ```bash
   cd src/CurrencyConverter.Web
   ```

2. **Install dependencies**
   ```bash
   npm install
   ```

3. **Configure API URL**

   Create `.env` file:
   ```env
   VITE_API_BASE_URL=http://localhost:5000
   ```

4. **Run development server**
   ```bash
   npm run dev
   ```

5. **Access the app**

   Navigate to: http://localhost:3000

6. **Build for production**
   ```bash
   npm run build
   # Output: dist/ folder
   ```

## API Endpoints

### Authentication (Identity API)
- `POST /register` - Register new user
- `POST /login` - Login and receive JWT token
- `POST /refresh` - Refresh access token
- `POST /logout` - Logout (client-side token removal)

### Currency Operations (v1) 🔐 Requires Authentication
- `GET /api/v1/currency/latest/{baseCurrency}` - Get all latest exchange rates
- `GET /api/v1/currency/convert?from={from}&to={to}&amount={amount}` - Convert currency
- `GET /api/v1/currency/rate?from={from}&to={to}` - Get specific exchange rate
- `GET /api/v1/currency/historical?baseCurrency={base}&startDate={start}&endDate={end}&pageNumber={page}&pageSize={size}` - Get historical rates (paginated)

### Role Management (Admin Only) 👑
- `GET /api/v1/role` - Get all roles
- `GET /api/v1/role/{userId}` - Get user's roles
- `POST /api/v1/role/assign` - Assign role to user
- `DELETE /api/v1/role/remove` - Remove role from user

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
│   ├── MyOrg.CurrencyConverter.API/          # ASP.NET Core Web API (.NET 10)
│   │   ├── Controllers/                      # API endpoints (v1)
│   │   ├── Core/                             # Domain layer (clean architecture)
│   │   │   ├── DTOs/                         # Data Transfer Objects
│   │   │   ├── Interfaces/                   # Abstractions
│   │   │   ├── Models/                       # Domain models
│   │   │   ├── Validators/                   # FluentValidation validators
│   │   │   └── Enums/                        # Enumerations
│   │   ├── Infrastructure/                   # External concerns
│   │   │   ├── Data/                         # EF Core, DbContext
│   │   │   ├── Providers/                    # Currency API providers
│   │   │   ├── Caching/                      # Redis caching
│   │   │   └── Factories/                    # Factory implementations
│   │   ├── Services/                         # Business logic
│   │   ├── Program.cs                        # App entry point & configuration
│   │
│   └── CurrencyConverter.Web/                # React TypeScript Frontend
│       ├── src/
│       │   ├── components/                   # React components
│       │   ├── contexts/                     # React contexts (auth)
│       │   ├── hooks/                        # Custom hooks (useCurrencies)
│       │   ├── services/                     # API client (axios)
│       │   └── App.tsx                       # Main app component
│       └── package.json                      # Dependencies
│
├── test/
│   ├── MyOrg.CurrencyConverter.UnitTests/    # Unit tests (30+ tests)
│   │
│   └── MyOrg.CurrencyConverter.IntegrationTests/  # Integration tests (17 tests)
│
├── docker-compose.yml                        # Development environment
├── docker-compose.test.yml                   # Test environment
└── README.md                                 # This file
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

## AI Usage Explanation

This project was developed with significant AI assistance using **Claude Code** and **Claude Sonnet 4.5**:

### How was AI used?
AI was used for code generation by providing clear context and constraints. It was also used to generate test code and to support iterative refactoring.

### What decisions did you validate or change manually?
Each step was manually reviewed to ensure the feature was implemented correctly. Manual changes were mainly applied to fine-grained configurations and in cases where the model did not produce an optimal solution, in order to maintain the development flow.

### What did you not blindly accept from AI?
No output was accepted blindly. All generated code was reviewed, and feedback was provided to refine or correct the code before final acceptance.

### Assumptions

1. **Currency Data Source**
   - Using Frankfurter API (free, no authentication required)
   - Historical data limited to API's retention period

2. **Caching**
   - Exchange rates cached for 30 minutes (production)
   - Assumes rates don't change frequently enough to cause issues
   - Cache invalidation is time-based, not event-driven

4. **Scalability**
   - Load balancing not configured (can be added later)

### Trade-offs


1. **Frontend State Management**
   - ✅ **Pro**: Simple useState/useEffect, minimal dependencies
   - ❌ **Con**: No global state management (Redux, Zustand)
   - **Decision**: Simplicity for current scope, easy to upgrade

2. **Docker Multi-Stage Builds**
   - ✅ **Pro**: Smaller production images
   - ❌ **Con**: Longer build times
   - **Decision**: Optimized production images for deployment

## Potential Future Improvements

1. **Multiple Currency Providers**
   - Add Fixer.io, ExchangeRate-API support
   - Automatic failover between providers
   - Provider health checks

2. **Frontend Features**
   - Mobile-responsive design improvements

3. **Testing**
   - E2E tests with Playwright
   - Performance testing

4. **Monitoring & Observability**
   - Integrate Jaeger/Zipkin for distributed tracing
   - Prometheus metrics

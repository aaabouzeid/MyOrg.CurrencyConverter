# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Currency Provider Configuration
ENV CurrencyProvider__FrankfurterApiBaseUrl=https://api.frankfurter.app

# Currency Restrictions Configuration
ENV CurrencyRestrictions__RestrictedCurrencies__0=TRY
ENV CurrencyRestrictions__RestrictedCurrencies__1=PLN
ENV CurrencyRestrictions__RestrictedCurrencies__2=THB
ENV CurrencyRestrictions__RestrictedCurrencies__3=MXN

# Resilience Policies Configuration
ENV ResiliencePolicies__RetryPolicy__RetryCount=3
ENV ResiliencePolicies__CircuitBreakerPolicy__HandledEventsAllowedBeforeBreaking=5
ENV ResiliencePolicies__CircuitBreakerPolicy__DurationOfBreakInSeconds=30


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/MyOrg.CurrencyConverter.API/MyOrg.CurrencyConverter.API.csproj", "src/MyOrg.CurrencyConverter.API/"]
RUN dotnet restore "./src/MyOrg.CurrencyConverter.API/MyOrg.CurrencyConverter.API.csproj"
COPY . .
WORKDIR "/src/src/MyOrg.CurrencyConverter.API"
RUN dotnet build "./MyOrg.CurrencyConverter.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./MyOrg.CurrencyConverter.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyOrg.CurrencyConverter.API.dll"]
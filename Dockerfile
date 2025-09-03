# 🐳 Multi-stage Dockerfile for Shopping Online API
# Optimized for production deployment

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj và restore dependencies trước (để cache layer này)
COPY ["ShoppingOnline.API/ShoppingOnline.API.csproj", "ShoppingOnline.API/"]
RUN dotnet restore "ShoppingOnline.API/ShoppingOnline.API.csproj"

# Copy source code và build
COPY . .
WORKDIR "/src/ShoppingOnline.API"
RUN dotnet build "ShoppingOnline.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ShoppingOnline.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Tạo non-root user cho security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Expose port
EXPOSE 8080

# Environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Start application
ENTRYPOINT ["dotnet", "ShoppingOnline.API.dll"]

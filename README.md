# 🛒 Shopping Online - Fashion E-commerce Backend API

![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)
![Entity Framework](https://img.shields.io/badge/Entity%20Framework-9.0.8-green.svg)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-orange.svg)
![JWT](https://img.shields.io/badge/Authentication-JWT-red.svg)

## 📖 Tổng Quan Dự Án

**Shopping Online** là hệ thống backend REST API cho website bán quần áo thời trang, được xây dựng để phục vụ khoảng **1000 người dùng/ngày**. Hệ thống cung cấp các API đầy đủ cho việc quản lý sản phẩm, đơn hàng, thanh toán, và hỗ trợ khách hàng.

### 🎯 Mục Tiêu Chính
- Xây dựng API RESTful hiệu năng cao với khả năng mở rộng
- Đảm bảo bảo mật với JWT Authentication và phân quyền Role-based
- Quản lý toàn diện quy trình e-commerce từ sản phẩm đến giao hàng
- Hỗ trợ real-time chat và báo cáo thống kê

---

## 🏗️ Kiến Trúc Hệ Thống

### Technology Stack
```
┌─────────────────┬──────────────────────────────────────┐
│ Layer           │ Technology                           │
├─────────────────┼──────────────────────────────────────┤
│ API Layer       │ ASP.NET Core 8.0 Web API           │
│ Authentication  │ JWT Bearer Token + Role-based Auth   │
│ ORM             │ Entity Framework Core 9.0.8         │
│ Database        │ SQL Server 2019+                     │
│ Documentation   │ Swagger/OpenAPI 3.0                  │
│ Validation      │ FluentValidation                     │
│ Password Hash   │ BCrypt.Net                           │
│ Logging         │ Serilog + File/Console Sinks         │
│ Caching         │ In-Memory Cache + Redis (Optional)    │
│ Testing         │ xUnit + Moq                          │
│ CI/CD           │ GitHub Actions + Docker              │
└─────────────────┴──────────────────────────────────────┘
```

### 🗂️ Cấu Trúc Database
```
Users ──┐
        ├── Roles (Admin, ProductManager, OrderManager, Account, Shipper, Customer)
        ├── Carts ──── CartItems ──── Products ──── Categories
        ├── Orders ──── OrderItems                ├── ProductVariants
        ├── Reviews                               └── ProductImages
        ├── ChatConversations ──── ChatMessages
        ├── Complaints
        ├── Payments
        ├── Shipping
        └── Reports
```

---

## 🚀 Setup Dự Án - Hướng Dẫn Chi Tiết

### ⚡ Yêu Cầu Hệ Thống
```
✅ .NET 8.0 SDK hoặc cao hơn
✅ SQL Server 2019+ hoặc SQL Server Express LocalDB
✅ Visual Studio 2022 / VS Code / JetBrains Rider
✅ Git (version control)
✅ Postman/Thunder Client (để test API)
```

### 📥 **BƯỚC 1: Tải Source Code**
```bash
# Clone repository
git clone https://github.com/your-username/shopping-online-api.git
cd shopping-online-api

# Hoặc download ZIP và extract
```

### 🗄️ **BƯỚC 2: Setup Database SQL Server**

#### **Option A: SQL Server Express LocalDB (Khuyến nghị cho Development)**
```bash
# Cài đặt SQL Server Express LocalDB (nếu chưa có)
# Download từ: https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

# Kiểm tra LocalDB đã cài đặt chưa
sqllocaldb info

# Tạo instance mới (nếu cần)
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

#### **Option B: SQL Server đầy đủ**
```sql
-- Kết nối SQL Server Management Studio
-- Tạo database mới
CREATE DATABASE ShoppingDB;

-- Hoặc sử dụng T-SQL command
sqlcmd -S localhost -Q "CREATE DATABASE ShoppingDB"
```

#### **Option C: Docker SQL Server (Cross-platform)**
```bash
# Pull và chạy SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Password123" -p 1433:1433 --name sqlserver2022 -d mcr.microsoft.com/mssql/server:2022-latest

# Kiểm tra container đang chạy
docker ps

# Test connection
docker exec -it sqlserver2022 /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "YourStrong@Password123" -Q "SELECT @@VERSION"
```

### ⚙️ **BƯỚC 3: Cấu Hình Connection String**

Mở file `ShoppingOnline.API/appsettings.json` và cập nhật:

```json
{
  "ConnectionStrings": {
    // LocalDB (Windows only)
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ShoppingDB;Trusted_Connection=True;TrustServerCertificate=True;",
    
    // SQL Server Express
    // "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=ShoppingDB;Trusted_Connection=True;TrustServerCertificate=True;",
    
    // SQL Server với username/password
    // "DefaultConnection": "Server=localhost;Database=ShoppingDB;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;",
    
    // Docker SQL Server
    // "DefaultConnection": "Server=localhost,1433;Database=ShoppingDB;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "ThisIsMySecretKeyForShoppingOnlineAPI_2024_DoNotShareWithAnyone",
    "Issuer": "ShoppingOnlineAPI",
    "Audience": "ShoppingOnlineClient"
  },
  "Security": {
    "BcryptWorkFactor": 4
  },
  "Performance": {
    "EnableAsyncLogging": true,
    "MaxConcurrentUsers": 1000,
    "CacheTimeout": 300
  }
}
```

### 📦 **BƯỚC 4: Cài Đặt Dependencies**
```bash
# Di chuyển vào thư mục API
cd ShoppingOnline.API

# Restore tất cả NuGet packages
dotnet restore

# Kiểm tra các packages đã được cài đặt
dotnet list package
```

### 🏗️ **BƯỚC 5: Tạo Database & Tables**

#### **Option A: Sử dụng Entity Framework Migrations (Khuyến nghị)**
```bash
# Cài đặt EF Core tools (nếu chưa có)
dotnet tool install --global dotnet-ef

# Kiểm tra EF tools
dotnet ef --version

# Tạo migration đầu tiên (nếu chưa có)
dotnet ef migrations add InitialCreate

# Áp dụng migration để tạo database và tables
dotnet ef database update

# Kiểm tra database đã được tạo
dotnet ef database list
```

#### **Option B: Chạy script SQL thủ công**
```sql
-- Connect vào SQL Server và chạy script tạo database
USE master;
GO

-- Tạo database nếu chưa có
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ShoppingDB')
BEGIN
    CREATE DATABASE ShoppingDB;
END
GO

USE ShoppingDB;
GO

-- Các tables sẽ được tạo tự động khi chạy ứng dụng lần đầu
```

### 🌱 **BƯỚC 6: Seed Data (Dữ liệu khởi tạo)**

Dữ liệu Roles sẽ được tự động tạo khi chạy ứng dụng, nhưng bạn có thể thêm thủ công:

```sql
-- Kết nối vào ShoppingDB và chạy script
USE ShoppingDB;
GO

-- Tạo Roles cơ bản (sẽ tự động tạo khi chạy app)
INSERT INTO Roles (RoleName, Description) VALUES 
('Admin', 'Quản trị viên hệ thống'),
('ProductManager', 'Quản lý sản phẩm'),
('OrderManager', 'Quản lý đơn hàng'),
('Account', 'Kế toán'),
('Shipper', 'Nhân viên giao hàng'),
('Customer', 'Khách hàng');

-- Tạo user Admin mặc định (tùy chọn)
-- Password: Admin123! (đã hash với BCrypt)
INSERT INTO Users (Username, Email, Password, RoleId, IsActive, CreatedAt) 
VALUES ('admin', 'admin@shoppingonline.com', '$2a$04$rWGgj4iWpE1t7gBmUhqUce7P8nOK.jF9G0R3QQHVrX8ZhCu6R6pVK', 1, 1, GETDATE());
```

### ▶️ **BƯỚC 7: Chạy Ứng Dụng**

```bash
# Option 1: Chạy bình thường
dotnet run

# Option 2: Chạy với hot reload (tự động restart khi có thay đổi)
dotnet watch run

# Option 3: Chạy với profile cụ thể
dotnet run --launch-profile https

# Kiểm tra ứng dụng đang chạy
curl https://localhost:7177/health
```

**Kết quả mong đợi:**
```
✅ API chạy tại: https://localhost:7177
✅ Swagger UI: https://localhost:7177/swagger
✅ Health check: https://localhost:7177/health
```

### 🧪 **BƯỚC 8: Test API hoạt động**

#### **Test với Swagger UI:**
1. Mở trình duyệt: `https://localhost:7177/swagger`
2. Test endpoint `/api/Roles` (GET) - không cần authentication
3. Test endpoint `/api/Categories` (GET) - không cần authentication

#### **Test với curl:**
```bash
# Test health check
curl -k https://localhost:7177/health

# Test get roles
curl -k https://localhost:7177/api/Roles

# Test register user mới
curl -k -X POST https://localhost:7177/api/Users \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test123!",
    "phone": "0123456789",
    "roleId": 6
  }'

# Test login
curl -k -X POST https://localhost:7177/api/Users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### 📊 **BƯỚC 9: Xác Nhận Setup Thành Công**

Kiểm tra các điều sau để đảm bảo setup hoàn tất:

#### ✅ **Database Check:**
```sql
-- Kết nối vào ShoppingDB và kiểm tra
USE ShoppingDB;

-- Kiểm tra tất cả tables đã được tạo
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';

-- Kết quả mong đợi: 18 tables
-- Carts, CartItems, Categories, ChatConversations, ChatMessages, Complaints, 
-- OrderItems, Orders, Payments, ProductImages, Products, ProductVariants, 
-- Reports, Reviews, Roles, Shippings, Users, __EFMigrationsHistory
```

#### ✅ **API Endpoints Check:**
```bash
# Lấy danh sách tất cả endpoints
curl -k https://localhost:7177/swagger/v1/swagger.json | jq '.paths | keys'
```

#### ✅ **Performance Check:**
```bash
# Test response time
curl -w "@curl-format.txt" -o /dev/null -s https://localhost:7177/api/Roles

# Tạo file curl-format.txt:
echo "Response Time: %{time_total}s\nHTTP Code: %{http_code}\n" > curl-format.txt
```

### 🚨 **Troubleshooting - Xử lý lỗi thường gặp**

#### **❌ Lỗi: "Unable to connect to database"**
```bash
# Kiểm tra SQL Server đang chạy
sc query MSSQLSERVER
# Hoặc cho LocalDB
sqllocaldb info MSSQLLocalDB

# Test connection string
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"
```

#### **❌ Lỗi: "A connection was successfully established with the server, but then an error occurred during the login process"**
```json
// Thêm TrustServerCertificate=True vào connection string
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ShoppingDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

#### **❌ Lỗi: "The certificate chain was issued by an authority that is not trusted"**
```bash
# Thêm certificate exception cho localhost
dotnet dev-certs https --trust
```

#### **❌ Lỗi: "There is already an object named 'Roles' in the database"**
```bash
# Drop database và tạo lại
dotnet ef database drop --force
dotnet ef database update
```

### 🎯 **Các Bước Tùy Chọn (Optional)**

#### **Setup IDE Extensions:**
```
Visual Studio Code:
- C# for Visual Studio Code
- REST Client
- Thunder Client
- GitLens

Visual Studio 2022:
- Entity Framework Power Tools
- Postman for VS
```

#### **Setup Git Hooks:**
```bash
# Pre-commit hook để chạy tests
echo "dotnet test" > .git/hooks/pre-commit
chmod +x .git/hooks/pre-commit
```

#### **Setup Environment Variables:**
```bash
# Windows
setx ASPNETCORE_ENVIRONMENT Development
setx ConnectionStrings__DefaultConnection "Server=(localdb)\MSSQLLocalDB;Database=ShoppingDB;Trusted_Connection=True;TrustServerCertificate=True;"

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Server=localhost;Database=ShoppingDB;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;"
```

---

## 🎉 **HOÀN TẤT SETUP!**

Nếu tất cả các bước trên đã hoàn thành thành công, bạn đã có:

✅ **Backend API** chạy tại `https://localhost:7177`  
✅ **Database** với 18 tables đầy đủ  
✅ **Authentication** với JWT token  
✅ **Swagger Documentation** tại `/swagger`  
✅ **18 Controllers** với 80+ endpoints  
✅ **Role-based Authorization** (6 roles)  
✅ **Performance Optimization** (1000+ concurrent users)  

**Bước tiếp theo:** Tạo frontend application để kết nối với API!

---

## 👥 Phân Quyền Người Dùng

### 🔐 Chi Tiết Roles và Chức Năng

| Role | Mô Tả | Quyền Hạn Chi Tiết |
|------|-------|-------------------|
| **Admin** | Quản trị viên toàn quyền | • Quản lý tất cả users và roles<br>• Xem toàn bộ báo cáo hệ thống<br>• Cấu hình hệ thống<br>• Xóa dữ liệu nhạy cảm |
| **ProductManager** | Quản lý sản phẩm | • CRUD sản phẩm, danh mục<br>• Quản lý kho (stock)<br>• Thiết lập khuyến mãi<br>• Quản lý hình ảnh sản phẩm |
| **OrderManager** | Quản lý đơn hàng | • Xem và cập nhật trạng thái đơn hàng<br>• Gán shipper cho đơn hàng<br>• Xử lý khiếu nại<br>• Báo cáo đơn hàng |
| **Account** | Kế toán viên | • Xử lý thanh toán<br>• Tạo hóa đơn<br>• Báo cáo doanh thu<br>• Quản lý khuyến mãi |
| **Shipper** | Nhân viên giao hàng | • Xem đơn hàng được gán<br>• Cập nhật trạng thái giao hàng<br>• Chat với khách hàng<br>• Báo cáo giao hàng |
| **Customer** | Khách hàng | • Đăng ký/đăng nhập<br>• Quản lý giỏ hàng<br>• Đặt hàng và thanh toán<br>• Đánh giá sản phẩm<br>• Chat hỗ trợ |

### 📋 Matrix Phân Quyền API

| Endpoint | Admin | ProductManager | OrderManager | Account | Shipper | Customer |
|----------|-------|----------------|--------------|---------|---------|----------|
| `/api/Users/**` | ✅ | ❌ | ❌ | ❌ | ❌ | 👤 |
| `/api/Categories/**` | ✅ | ✅ | ❌ | ❌ | ❌ | 👁️ |
| `/api/Products/**` | ✅ | ✅ | 👁️ | 👁️ | 👁️ | 👁️ |
| `/api/Orders/**` | ✅ | 👁️ | ✅ | ✅ | 📦 | 👤 |
| `/api/Cart/**` | ✅ | ❌ | ❌ | ❌ | ❌ | 👤 |
| `/api/Payments/**` | ✅ | ❌ | 👁️ | ✅ | ❌ | 👤 |
| `/api/Reports/**` | ✅ | 📊 | 📊 | 📊 | 📊 | ❌ |

**Chú thích:**
- ✅ Full Access - 👁️ Read Only - 👤 Own Data Only - 📦 Assigned Orders - 📊 Role-specific Reports - ❌ No Access

---

## 🎯 API Endpoints Chính

### 🔐 Authentication
```http
POST /api/Users/login           # Đăng nhập
POST /api/Users                 # Đăng ký
GET  /api/Users/Roles           # Lấy danh sách roles
```

### 🏷️ Categories & Products
```http
GET    /api/Categories                    # Lấy tất cả danh mục
POST   /api/Categories                    # Tạo danh mục mới [ProductManager+]
PUT    /api/Categories/{id}               # Cập nhật danh mục [ProductManager+]
DELETE /api/Categories/{id}               # Xóa danh mục [Admin]
GET    /api/Categories/{id}/products      # Sản phẩm theo danh mục

GET    /api/Products                      # Lấy tất cả sản phẩm
POST   /api/Products                      # Tạo sản phẩm [ProductManager+]
PUT    /api/Products/{id}                 # Cập nhật sản phẩm [ProductManager+]
DELETE /api/Products/{id}                 # Xóa sản phẩm [ProductManager+]
```

### 🛒 Shopping Cart
```http
GET    /api/Cart                          # Lấy giỏ hàng [Customer]
POST   /api/Cart/items                    # Thêm vào giỏ hàng [Customer]
PUT    /api/Cart/items/{id}               # Cập nhật số lượng [Customer]
DELETE /api/Cart/items/{id}               # Xóa khỏi giỏ hàng [Customer]
```

### 📦 Orders
```http
GET    /api/Orders                        # Lấy đơn hàng [Role-based]
POST   /api/Orders                        # Tạo đơn hàng [Customer]
PUT    /api/Orders/{id}/status            # Cập nhật trạng thái [OrderManager+]
GET    /api/Orders/{id}/tracking          # Theo dõi đơn hàng
```

---

## 🏃‍♂️ Chạy Ứng Dụng

### Development Mode
```bash
# Chạy ứng dụng
cd ShoppingOnline.API
dotnet run

# Hoặc với hot reload
dotnet watch run
```

Ứng dụng sẽ chạy tại: `https://localhost:7177` hoặc `http://localhost:5177`

### 📖 Swagger UI
Truy cập Swagger documentation tại:
```
https://localhost:7177/swagger
```

### 🧪 Testing với Postman

1. **Import Collection**: Tải file `ShoppingOnline.postman_collection.json`
2. **Thiết lập Environment**:
   ```json
   {
     "baseUrl": "https://localhost:7177",
     "adminToken": "{{generated_after_login}}",
     "customerToken": "{{generated_after_login}}"
   }
   ```

3. **Test Flow**:
   ```
   1. POST /api/Users/login (Admin) → Lấy token
   2. POST /api/Categories → Tạo danh mục
   3. POST /api/Products → Tạo sản phẩm
   4. POST /api/Users (Customer) → Đăng ký khách hàng
   5. POST /api/Cart/items → Thêm vào giỏ hàng
   6. POST /api/Orders → Đặt hàng
   ```

---

## 🚀 Deployment

### 🐳 Docker Deployment
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["ShoppingOnline.API/ShoppingOnline.API.csproj", "ShoppingOnline.API/"]
RUN dotnet restore "ShoppingOnline.API/ShoppingOnline.API.csproj"
COPY . .
WORKDIR "/src/ShoppingOnline.API"
RUN dotnet build "ShoppingOnline.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShoppingOnline.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShoppingOnline.API.dll"]
```

```bash
# Build và chạy container
docker build -t shopping-online-api .
docker run -p 8080:80 shopping-online-api
```

### ☁️ Azure App Service
```bash
# Publish to Azure
az webapp up --name shopping-online-api --resource-group rg-shopping --location "Southeast Asia"
```

### 🖥️ IIS Deployment
1. **Publish Project**:
   ```bash
   dotnet publish -c Release -o ./publish
   ```

2. **IIS Configuration**:
   - Tạo Application Pool (.NET CLR Version: No Managed Code)
   - Deploy files từ thư mục `./publish`
   - Cấu hình connection string trong `web.config`

---

## 🔒 Bảo Mật

### 🛡️ JWT Authentication
```csharp
// JWT Configuration
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });
```

### 🔐 Security Best Practices
- ✅ Password hashing với BCrypt (cost factor: 11)
- ✅ JWT token với expiration time (3 hours)
- ✅ Role-based authorization
- ✅ Input validation với FluentValidation
- ✅ SQL Injection protection với EF Core
- ✅ HTTPS enforced trong production
- ✅ CORS configuration cho specific domains

---

## ⚡ Tối Ưu Hiệu Năng

### 💾 Caching Strategy
```csharp
// In-Memory Caching
services.AddMemoryCache();

// Redis Caching (Production)
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
});
```

### 📄 Pagination
```csharp
// Example: Products with pagination
[HttpGet]
public async Task<ActionResult<PagedResult<ProductResponse>>> GetProducts(
    [FromQuery] int page = 1, 
    [FromQuery] int size = 20)
{
    var skip = (page - 1) * size;
    var products = await _context.Products
        .Skip(skip)
        .Take(size)
        .ToListAsync();
    
    var total = await _context.Products.CountAsync();
    
    return new PagedResult<ProductResponse>
    {
        Data = products,
        Page = page,
        Size = size,
        Total = total
    };
}
```

### 🗃️ Database Optimization
- ✅ Proper indexing trên columns thường query
- ✅ Connection pooling
- ✅ Lazy loading configuration
- ✅ Query optimization với EF Core

---

## 📊 Logging & Monitoring

### 📝 Serilog Configuration
```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File", "Serilog.Sinks.Console"],
    "MinimumLevel": "Information",
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/shopping-api-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      { "Name": "Console" }
    ]
  }
}
```

### 📈 Performance Monitoring
```csharp
// Custom middleware for request logging
public class RequestLoggingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();
        
        _logger.LogInformation("Request {Method} {Path} took {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
    }
}
```

### 🔍 Health Checks
```csharp
services.AddHealthChecks()
    .AddSqlServer(connectionString)
    .AddCheck("api", () => HealthCheckResult.Healthy("API is running"));
```

Endpoint: `GET /health`

---

## 🤝 Contributing

### 📋 Development Workflow
1. **Fork** repository
2. **Create** feature branch: `git checkout -b feature/new-feature`
3. **Commit** changes: `git commit -m 'Add new feature'`
4. **Push** to branch: `git push origin feature/new-feature`
5. **Submit** Pull Request

### 🔄 Git Workflow Standards

#### 🌿 Branch Naming Convention
```bash
# Feature branches
feature/user-authentication
feature/product-management
feature/order-processing

# Bug fix branches
bugfix/fix-login-issue
bugfix/resolve-payment-error

# Hotfix branches
hotfix/critical-security-patch
hotfix/database-connection-fix

# Release branches
release/v1.0.0
release/v1.1.0
```

#### 📝 Commit Message Standards
```bash
# Format: <type>(<scope>): <description>

# Examples
feat(auth): add JWT token refresh mechanism
fix(products): resolve product image upload issue
docs(readme): update API documentation
style(categories): fix code formatting
refactor(orders): improve order processing logic
test(users): add unit tests for user service
chore(deps): update Entity Framework to 9.0.8

# Breaking changes
feat(api)!: change product API response format

BREAKING CHANGE: Product API now returns ProductResponse instead of Product entity
```

#### 🔍 Code Review Checklist
```markdown
## Code Review Checklist

### ✅ Functionality
- [ ] Code works as expected and meets requirements
- [ ] Edge cases are handled properly
- [ ] Error handling is implemented correctly
- [ ] Business logic is sound

### ✅ Code Quality
- [ ] Code follows naming conventions
- [ ] Methods are not too long (< 50 lines)
- [ ] Classes have single responsibility
- [ ] No code duplication
- [ ] Comments explain "why", not "what"

### ✅ Security
- [ ] Input validation is implemented
- [ ] SQL injection is prevented
- [ ] Authentication/authorization is correct
- [ ] Sensitive data is not logged
- [ ] No hardcoded secrets

### ✅ Performance
- [ ] Database queries are optimized
- [ ] Proper use of async/await
- [ ] No unnecessary loops or iterations
- [ ] Caching is used where appropriate

### ✅ Testing
- [ ] Unit tests cover new functionality
- [ ] Integration tests pass
- [ ] Code coverage meets requirements (80%+)
- [ ] Tests are meaningful and not trivial

### ✅ Documentation
- [ ] XML documentation for public APIs
- [ ] README updated if needed
- [ ] API documentation is accurate
```

### 🤖 CI/CD Pipeline

#### GitHub Actions Workflow
Tạo file `.github/workflows/ci-cd.yml`:
```yaml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2019-latest
        env:
          SA_PASSWORD: YourStrong@Passw0rd
          ACCEPT_EULA: Y
        ports:
          - 1433:1433

    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage"
      
    - name: Code Coverage Report
      uses: codecov/codecov-action@v3
      
    - name: Security Scan
      run: dotnet list package --vulnerable --include-transitive
```

### 🧪 Testing
```bash
# Run unit tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### 📝 Code Standards
- ✅ Follow C# coding conventions
- ✅ Add XML documentation cho public APIs
- ✅ Write unit tests với minimum 80% coverage
- ✅ Use async/await pattern
- ✅ Implement proper error handling

---

## 📋 Development Guidelines

### 🎨 Code Style & Conventions

#### 🔧 EditorConfig
Tạo file `.editorconfig` trong root directory:
```ini
root = true

[*]
charset = utf-8
end_of_line = crlf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx}]
dotnet_analyzer_diagnostic.category-style.severity = warning
dotnet_analyzer_diagnostic.category-maintainability.severity = warning

# C# Style Rules
csharp_new_line_before_open_brace = all
csharp_new_line_before_else = true
csharp_new_line_before_catch = true
csharp_new_line_before_finally = true
csharp_new_line_before_members_in_object_init = true
csharp_new_line_before_members_in_anonymous_types = true

[*.{json,yml,yaml}]
indent_size = 2
```

#### 📐 Naming Conventions
```csharp
// ✅ Good Examples
public class ProductController : ControllerBase              // PascalCase cho classes
public async Task<ActionResult> GetProductsAsync()           // PascalCase cho methods
private readonly IProductService _productService;           // camelCase với _ cho private fields
public string ProductName { get; set; }                     // PascalCase cho properties
const int MAX_RETRY_COUNT = 3;                              // UPPER_CASE cho constants

// ❌ Bad Examples
public class productController                               // Wrong case
public async Task<ActionResult> get_products()              // Wrong naming
private readonly IProductService productService;           // Missing underscore
public string productname { get; set; }                    // Wrong case
```

#### 🏗️ Project Structure Standards
```
ShoppingOnline.API/
├── Controllers/           # API Controllers
├── DTOs/                 # Data Transfer Objects
│   ├── Products/
│   ├── Categories/
│   └── Users/
├── Models/               # Entity Framework Models
├── Services/             # Business Logic Services
│   ├── Interfaces/
│   └── Implementations/
├── Middleware/           # Custom Middleware
├── Extensions/           # Extension Methods
├── Constants/            # Application Constants
├── Enums/               # Enumeration Types
├── Exceptions/          # Custom Exception Classes
├── Validators/          # FluentValidation Validators
└── Utils/               # Utility Classes
```

### 🛡️ Error Handling Standards

#### 🚨 Custom Exception Classes
```csharp
// Base exception class
public abstract class ShoppingOnlineException : Exception
{
    public string ErrorCode { get; }
    public object? Details { get; }

    protected ShoppingOnlineException(string errorCode, string message, object? details = null) 
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }
}

// Specific exceptions
public class ProductNotFoundException : ShoppingOnlineException
{
    public ProductNotFoundException(int productId) 
        : base("PRODUCT_NOT_FOUND", $"Product with ID {productId} not found", new { ProductId = productId })
    {
    }
}

public class InsufficientStockException : ShoppingOnlineException
{
    public InsufficientStockException(int productId, int requestedQuantity, int availableStock)
        : base("INSUFFICIENT_STOCK", $"Not enough stock. Requested: {requestedQuantity}, Available: {availableStock}",
               new { ProductId = productId, RequestedQuantity = requestedQuantity, AvailableStock = availableStock })
    {
    }
}
```

#### 🔄 Global Exception Handler
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = exception.Message,
            ErrorCode = exception is ShoppingOnlineException shopEx ? shopEx.ErrorCode : "INTERNAL_ERROR",
            Data = exception is ShoppingOnlineException shopEx2 ? shopEx2.Details : null
        };

        var statusCode = exception switch
        {
            ProductNotFoundException => StatusCodes.Status404NotFound,
            UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
            ValidationException => StatusCodes.Status400BadRequest,
            InsufficientStockException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

### 📊 API Response Standards

#### 🎯 Consistent Response Format
```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PagedResponse<T> : ApiResponse<IEnumerable<T>>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
```

#### 🎨 Controller Response Helpers
```csharp
public abstract class BaseController : ControllerBase
{
    protected ActionResult<ApiResponse<T>> SuccessResponse<T>(T data, string message = "Success")
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        });
    }

    protected ActionResult<ApiResponse<object>> ErrorResponse(string message, string errorCode, object? details = null)
    {
        return BadRequest(new ApiResponse<object>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Data = details
        });
    }

    protected ActionResult<PagedResponse<T>> PagedResponse<T>(IEnumerable<T> data, int page, int size, int totalItems)
    {
        return Ok(new PagedResponse<T>
        {
            Success = true,
            Message = "Success",
            Data = data,
            Page = page,
            Size = size,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling((double)totalItems / size)
        });
    }
}
```

### 🧪 Testing Standards

#### 🔬 Unit Test Structure
```csharp
[TestClass]
public class ProductServiceTests
{
    private Mock<IProductRepository> _mockProductRepository;
    private Mock<ILogger<ProductService>> _mockLogger;
    private ProductService _productService;

    [TestInitialize]
    public void Setup()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _productService = new ProductService(_mockProductRepository.Object, _mockLogger.Object);
    }

    [TestMethod]
    public async Task GetProductAsync_WhenProductExists_ShouldReturnProduct()
    {
        // Arrange
        var productId = 1;
        var expectedProduct = new Product { ProductId = productId, ProductName = "Test Product" };
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
                             .ReturnsAsync(expectedProduct);

        // Act
        var result = await _productService.GetProductAsync(productId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedProduct.ProductId, result.ProductId);
        _mockProductRepository.Verify(x => x.GetByIdAsync(productId), Times.Once);
    }

    [TestMethod]
    public async Task GetProductAsync_WhenProductNotExists_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = 999;
        _mockProductRepository.Setup(x => x.GetByIdAsync(productId))
                             .ReturnsAsync((Product?)null);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ProductNotFoundException>(
            () => _productService.GetProductAsync(productId));
    }
}
```

#### 🎭 Integration Test Example
```csharp
[TestClass]
public class ProductsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProductsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [TestMethod]
    public async Task GetProducts_ShouldReturnProductsList()
    {
        // Act
        var response = await _client.GetAsync("/api/Products");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ApiResponse<IEnumerable<ProductResponse>>>(content);
        
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.Data);
    }
}
```

### 🔒 Security Guidelines

#### 🛡️ Input Validation
```csharp
public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Tên sản phẩm là bắt buộc")
            .Length(1, 255).WithMessage("Tên sản phẩm phải từ 1-255 ký tự")
            .Matches(@"^[a-zA-Z0-9\s\-_.,]+$").WithMessage("Tên sản phẩm chỉ chứa ký tự hợp lệ");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Giá phải lớn hơn 0")
            .LessThanOrEqualTo(10000000).WithMessage("Giá không được vượt quá 10 triệu");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId phải lớn hơn 0");
    }
}
```

#### 🔐 Authorization Examples
```csharp
[HttpPost]
[Authorize(Roles = "Admin,ProductManager")]
[ValidateAntiForgeryToken]
public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct(
    [FromBody] CreateProductRequest request)
{
    // Implementation
}

[HttpGet("{id}")]
[Authorize] // Any authenticated user
public async Task<ActionResult<ApiResponse<ProductResponse>>> GetProduct(int id)
{
    // Implementation
}

[HttpDelete("{id}")]
[Authorize(Policy = "AdminOnly")]
public async Task<ActionResult> DeleteProduct(int id)
{
    // Implementation
}
```

### 📈 Performance Guidelines

#### ⚡ Database Query Optimization
```csharp
// ✅ Good: Use projection to avoid loading unnecessary data
public async Task<IEnumerable<ProductSummaryResponse>> GetProductSummariesAsync()
{
    return await _context.Products
        .Where(p => p.IsActive)
        .Select(p => new ProductSummaryResponse
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Price = p.Price
        })
        .ToListAsync();
}

// ❌ Bad: Loading full entities when only summary needed
public async Task<IEnumerable<Product>> GetProductsAsync()
{
    return await _context.Products
        .Include(p => p.Category)
        .Include(p => p.ProductImages)
        .ToListAsync(); // Loads all data unnecessarily
}
```

#### 💾 Caching Implementation
```csharp
public class ProductService : IProductService
{
    private readonly IMemoryCache _cache;
    private readonly IProductRepository _repository;

    public async Task<ProductResponse> GetProductAsync(int id)
    {
        var cacheKey = $"product_{id}";
        
        if (_cache.TryGetValue(cacheKey, out ProductResponse? cachedProduct))
        {
            return cachedProduct!;
        }

        var product = await _repository.GetByIdAsync(id);
        if (product == null)
            throw new ProductNotFoundException(id);

        var response = _mapper.Map<ProductResponse>(product);
        
        _cache.Set(cacheKey, response, TimeSpan.FromMinutes(15));
        
        return response;
    }
}
```

### 📝 Documentation Standards

#### 📖 XML Documentation
```csharp
/// <summary>
/// Creates a new product in the system
/// </summary>
/// <param name="request">The product creation request containing product details</param>
/// <returns>The created product with assigned ID</returns>
/// <response code="201">Product created successfully</response>
/// <response code="400">Invalid input data</response>
/// <response code="401">Unauthorized access</response>
/// <response code="403">Insufficient permissions</response>
/// <example>
/// POST /api/Products
/// {
///   "productName": "Áo Thun Nam",
///   "description": "Áo thun cotton 100%",
///   "price": 299000,
///   "categoryId": 1,
///   "stockQuantity": 100
/// }
/// </example>
[HttpPost]
[Authorize(Roles = "Admin,ProductManager")]
[ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<ApiResponse<ProductResponse>>> CreateProduct(CreateProductRequest request)
{
    // Implementation
}
```

---

## 📞 Hỗ Trợ

### 🐛 Báo Lỗi
Tạo issue mới tại: [GitHub Issues](https://github.com/your-username/shopping-online-api/issues)

### 📧 Liên Hệ
- **Email**: support@shoppingonline.com
- **Slack**: #shopping-api-support
- **Documentation**: [Wiki](https://github.com/your-username/shopping-online-api/wiki)

---

## 📄 License

Dự án này được cấp phép theo [MIT License](LICENSE)

---

## 🏆 Acknowledgments

- **Entity Framework Core Team** - ORM framework
- **ASP.NET Core Team** - Web framework
- **JWT.NET** - JWT implementation
- **BCrypt.Net** - Password hashing
- **Serilog** - Structured logging

---

**Made with ❤️ by Shopping Online Team**

---

## 🔧 Troubleshooting & FAQ

### ❓ Frequently Asked Questions

#### Q: Database connection failed với error "Login failed for user"?
**A:** Kiểm tra connection string và đảm bảo SQL Server đang chạy:
```bash
# Check if SQL Server is running
docker ps | grep mssql

# Test connection
sqlcmd -S localhost -U sa -P "YourPassword" -Q "SELECT 1"
```

#### Q: JWT token expired quá nhanh, làm sao tăng thời gian?
**A:** Cập nhật `ExpiryHours` trong `appsettings.json`:
```json
{
  "JwtSettings": {
    "ExpiryHours": 8  // Tăng từ 3 lên 8 giờ
  }
}
```

#### Q: Entity Framework migrations không chạy được?
**A:** Thử các bước sau:
```bash
# Clear migrations
rm -rf Migrations/

# Recreate migration
dotnet ef migrations add InitialCreate
dotnet ef database update

# If still fails, drop database and recreate
dotnet ef database drop
dotnet ef database update
```

#### Q: API trả về 500 Internal Server Error không có thông tin chi tiết?
**A:** Enable chi tiết error trong Development:
```csharp
// In Program.cs
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

### 🐛 Common Issues & Solutions

#### Issue: "Cannot access file because it is being used by another process"
**Solution:**
```bash
# Kill running processes
taskkill /F /IM dotnet.exe
taskkill /F /IM ShoppingOnline.API.exe
```

#### Issue: CORS errors khi call API từ frontend
**Solution:**
```csharp
// In Program.cs
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### 📊 Performance Tips

#### Database Optimization
```sql
-- Index suggestions for high-traffic queries
CREATE NONCLUSTERED INDEX IX_Products_CategoryId 
ON Products (category_id) INCLUDE (product_name, price);

CREATE NONCLUSTERED INDEX IX_Orders_UserId_OrderDate 
ON Orders (user_id, order_date DESC);
```

#### Caching Strategy
```csharp
// Cache frequently accessed data
public async Task<IEnumerable<CategoryResponse>> GetCategoriesAsync()
{
    const string cacheKey = "categories_all";
    
    if (_cache.TryGetValue(cacheKey, out IEnumerable<CategoryResponse>? cached))
        return cached!;
    
    var categories = await _context.Categories.ToListAsync();
    _cache.Set(cacheKey, categories, TimeSpan.FromMinutes(30));
    
    return categories;
}
```

### 🔐 Security Best Practices

#### Production Security Checklist
- [ ] Remove default connection strings
- [ ] Enable HTTPS redirections
- [ ] Configure proper CORS origins
- [ ] Set secure JWT secret key (min 256-bit)
- [ ] Enable SQL Server encryption
- [ ] Configure API rate limiting
- [ ] Set up proper logging (no sensitive data)
- [ ] Enable security headers

#### Rate Limiting
```csharp
// Install: AspNetCoreRateLimit
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Limit = 100,
            Period = "1m"
        }
    };
});
```

---
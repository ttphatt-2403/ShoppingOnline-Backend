# 🚀 QUICK START: GitHub + CI/CD Setup
## Hướng dẫn nhanh từng bước cho người mới

---

## ⚡ **BƯỚC 1: CHẠY SCRIPT TỰ ĐỘNG (DỄ NHẤT)**

### Mở PowerShell và chạy:
```powershell
# Di chuyển đến thư mục project
cd "d:\SEM6-FPTu\Makenewlife\ShoppingOnline"

# Chạy script setup tự động
powershell -ExecutionPolicy Bypass -File setup-git.ps1
```

**Script sẽ tự động:**
- ✅ Kiểm tra Git đã cài chưa
- ✅ Cấu hình user name và email
- ✅ Khởi tạo Git repository
- ✅ Add và commit files
- ✅ Hướng dẫn bước tiếp theo

---

## 🌐 **BƯỚC 2: TẠO GITHUB REPOSITORY**

### 2.1 Truy cập GitHub:
1. Vào: **https://github.com**
2. Đăng nhập tài khoản

### 2.2 Tạo repository mới:
1. Click nút **"New"** (màu xanh)
2. **Repository name:** `ShoppingOnline-Backend`
3. **Description:** `ASP.NET Core E-commerce Backend API`
4. Chọn **Public** hoặc **Private**
5. **❌ KHÔNG tick** "Add a README file"
6. **❌ KHÔNG tick** "Add .gitignore"  
7. **❌ KHÔNG tick** "Choose a license"
8. Click **"Create repository"**

### 2.3 Copy repository URL:
Sau khi tạo xong, bạn sẽ thấy URL như:
```
https://github.com/YOUR_USERNAME/ShoppingOnline-Backend.git
```

---

## ⬆️ **BƯỚC 3: UPLOAD CODE LÊN GITHUB**

### Chạy lệnh trong PowerShell:
```powershell
# Thay YOUR_USERNAME bằng username GitHub của bạn
git remote add origin https://github.com/YOUR_USERNAME/ShoppingOnline-Backend.git

# Push code lên GitHub
git push -u origin main

# Nếu bị lỗi với main, thử master:
git push -u origin master
```

**🎉 Done! Code đã lên GitHub!**

---

## 🔄 **BƯỚC 4: CI/CD SẼ TỰ ĐỘNG CHẠY**

### Sau khi push code:
1. Vào repository trên GitHub
2. Click tab **"Actions"**
3. Sẽ thấy workflow **"Shopping Online Backend CI/CD"** đang chạy
4. Click vào để xem progress

### CI/CD sẽ tự động:
- ✅ **Build** project
- ✅ **Test** code (nếu có tests)
- ✅ **Security scan** 
- ✅ **Create artifacts**
- ✅ **Docker build** (nếu có Dockerfile)

---

## 🛠️ **BƯỚC 5: SETUP SECRETS (TÙY CHỌN)**

### Nếu muốn deploy tự động:
1. Vào **Settings** của repository
2. Click **"Secrets and variables"** → **"Actions"**
3. Click **"New repository secret"**

### Thêm secrets:
```
Name: CONNECTION_STRING
Value: Server=your-server;Database=ShoppingDB;User Id=sa;Password=your-password;

Name: JWT_SECRET  
Value: ThisIsMySecretKeyForShoppingOnlineAPI_2024_DoNotShareWithAnyone

Name: DOCKER_USERNAME
Value: your-docker-hub-username

Name: DOCKER_PASSWORD
Value: your-docker-hub-password
```

---

## 📝 **CÁC LỆNH GIT THƯỜNG DÙNG SAU NÀY**

### Khi có thay đổi code:
```powershell
# Kiểm tra status
git status

# Add files mới/thay đổi
git add .

# Commit với message
git commit -m "Add new feature: user authentication"

# Push lên GitHub (trigger CI/CD)
git push
```

### Tạo branch mới:
```powershell
# Tạo và chuyển sang branch mới
git checkout -b feature/payment-integration

# Push branch mới lên GitHub
git push -u origin feature/payment-integration
```

---

## 🎯 **KẾT QUẢ MONG ĐỢI**

### Sau khi hoàn thành setup:
- ✅ **Code trên GitHub** - Repository public/private
- ✅ **Version control** - Track tất cả thay đổi
- ✅ **CI/CD Pipeline** - Auto build/test khi commit
- ✅ **Professional setup** - Như developer thực thụ
- ✅ **Collaboration ready** - Team có thể contribute

### Các URL quan trọng:
```
🌐 Repository: https://github.com/YOUR_USERNAME/ShoppingOnline-Backend
🔄 Actions: https://github.com/YOUR_USERNAME/ShoppingOnline-Backend/actions
⚙️ Settings: https://github.com/YOUR_USERNAME/ShoppingOnline-Backend/settings
```

---

## 🆘 **TROUBLESHOOTING**

### ❌ Lỗi Git not found:
```powershell
# Download và cài đặt Git:
# https://git-scm.com/download/win
```

### ❌ Lỗi permission denied:
```powershell
# Cấu hình credential helper
git config --global credential.helper manager-core
```

### ❌ Lỗi remote already exists:
```powershell
# Remove và add lại remote
git remote remove origin
git remote add origin https://github.com/YOUR_USERNAME/ShoppingOnline-Backend.git
```

### ❌ Lỗi push bị reject:
```powershell
# Pull trước khi push
git pull origin main
git push
```

---

## 🎊 **CHÚC MỪNG!**

**Bạn đã thành công setup:**
- ✅ Git version control
- ✅ GitHub repository  
- ✅ CI/CD automation
- ✅ Professional workflow

**Next steps:**
- 🚀 Deploy to cloud (Azure, AWS, etc.)
- 🧪 Add more tests
- 📊 Setup monitoring
- 🔒 Enhance security

**Happy coding!** 🎉

---

*Made with ❤️ for Shopping Online Backend Project*

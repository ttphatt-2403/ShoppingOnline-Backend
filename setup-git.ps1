# 🚀 Setup Git và GitHub - Script tự động
# Chạy script này để setup Git repository

Write-Host "=========================================" -ForegroundColor Green
Write-Host "    SHOPPING ONLINE - GIT SETUP         " -ForegroundColor Green  
Write-Host "=========================================" -ForegroundColor Green

# Bước 1: Kiểm tra Git
Write-Host "🔍 Checking Git installation..." -ForegroundColor Yellow
try {
    $gitVersion = git --version
    Write-Host "✅ Git found: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Git not found. Please install Git first:" -ForegroundColor Red
    Write-Host "   Download: https://git-scm.com/download/win" -ForegroundColor White
    exit 1
}

# Bước 2: Cấu hình Git user (nếu chưa có)
Write-Host "`n⚙️ Checking Git configuration..." -ForegroundColor Yellow
$userName = git config --global user.name
$userEmail = git config --global user.email

if (-not $userName) {
    Write-Host "❓ Git user name not configured." -ForegroundColor Yellow
    $inputName = Read-Host "Enter your full name (e.g., Phat Tran)"
    git config --global user.name "$inputName"
    Write-Host "✅ User name set to: $inputName" -ForegroundColor Green
} else {
    Write-Host "✅ Git user name: $userName" -ForegroundColor Green
}

if (-not $userEmail) {
    Write-Host "❓ Git user email not configured." -ForegroundColor Yellow
    $inputEmail = Read-Host "Enter your email (e.g., your.email@gmail.com)"
    git config --global user.email "$inputEmail"
    Write-Host "✅ User email set to: $inputEmail" -ForegroundColor Green
} else {
    Write-Host "✅ Git user email: $userEmail" -ForegroundColor Green
}

# Bước 3: Khởi tạo Git repository
Write-Host "`n🏗️ Initializing Git repository..." -ForegroundColor Yellow
if (Test-Path ".git") {
    Write-Host "✅ Git repository already exists" -ForegroundColor Green
} else {
    git init
    Write-Host "✅ Git repository initialized" -ForegroundColor Green
}

# Bước 4: Check status
Write-Host "`n📊 Checking repository status..." -ForegroundColor Yellow
git status

# Bước 5: Add files
Write-Host "`n📦 Adding files to Git..." -ForegroundColor Yellow
git add .
Write-Host "✅ Files added to staging area" -ForegroundColor Green

# Bước 6: First commit
Write-Host "`n💾 Creating initial commit..." -ForegroundColor Yellow
$commitMessage = "Initial commit: ASP.NET Core Shopping Online Backend"
git commit -m "$commitMessage"
Write-Host "✅ Initial commit created" -ForegroundColor Green

# Bước 7: Hướng dẫn setup GitHub
Write-Host "`n=========================================" -ForegroundColor Cyan
Write-Host "    NEXT: CREATE GITHUB REPOSITORY      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

Write-Host "🌐 Step-by-step GitHub setup:" -ForegroundColor White
Write-Host "1. Go to: https://github.com" -ForegroundColor Yellow
Write-Host "2. Click 'New' button (green)" -ForegroundColor Yellow
Write-Host "3. Repository name: ShoppingOnline-Backend" -ForegroundColor Yellow
Write-Host "4. Description: ASP.NET Core E-commerce Backend API" -ForegroundColor Yellow
Write-Host "5. Choose Public or Private" -ForegroundColor Yellow
Write-Host "6. DON'T add README, .gitignore, or license" -ForegroundColor Red
Write-Host "7. Click 'Create repository'" -ForegroundColor Yellow

Write-Host "`n📋 After creating GitHub repository, run:" -ForegroundColor White
Write-Host "git remote add origin https://github.com/YOUR_USERNAME/ShoppingOnline-Backend.git" -ForegroundColor Cyan
Write-Host "git push -u origin main" -ForegroundColor Cyan

Write-Host "`n✅ Git setup completed!" -ForegroundColor Green
Write-Host "Ready to push to GitHub!" -ForegroundColor Green

# Bước 8: Show Git log
Write-Host "`n📈 Current Git log:" -ForegroundColor Yellow
git log --oneline -5

Write-Host "`n=========================================" -ForegroundColor Green
Write-Host "Use 'git status' to check current state" -ForegroundColor White
Write-Host "Use 'git log' to see commit history" -ForegroundColor White
Write-Host "=========================================" -ForegroundColor Green

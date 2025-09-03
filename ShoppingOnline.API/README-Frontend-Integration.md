# ShoppingOnline API - Frontend Integration Guide

## 🚀 Quick Start

### Base URL
```
Development: http://localhost:5177/api
Production: https://your-api-domain.com/api
```

### Authentication
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

## 🔐 Authentication Endpoints

### 1. User Registration
```http
POST /api/Users/register
Content-Type: application/json

{
  "username": "newuser",
  "email": "user@example.com", 
  "password": "password123",
  "phone": "1234567890"
}

Response:
{
  "success": true,
  "message": "User registered successfully",
  "data": {
    "userId": 123,
    "username": "newuser",
    "email": "user@example.com"
  }
}
```

### 2. User Login
```http
POST /api/Users/login
Content-Type: application/json

{
  "username": "testuser",
  "password": "password123"
}

Response:
{
  "userId": 123,
  "username": "testuser", 
  "email": "test@example.com",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "roleId": 6,
  "roleName": "Customer"
}
```

## 🛒 Shopping Cart Endpoints

### 1. Get Cart Items
```http
GET /api/CartItem
Authorization: Bearer <token>

Response:
{
  "cartItems": [...],
  "totalItems": 3,
  "totalAmount": 299.99
}
```

### 2. Add to Cart
```http
POST /api/CartItem
Authorization: Bearer <token>
Content-Type: application/json

{
  "productId": 1,
  "variantId": 2,
  "quantity": 1
}
```

### 3. Update Cart Item
```http
PUT /api/CartItem/{cartItemId}
Authorization: Bearer <token>

{
  "quantity": 3
}
```

### 4. Remove from Cart
```http
DELETE /api/CartItem/{cartItemId}
Authorization: Bearer <token>
```

## 📦 Product Endpoints

### 1. Get Products (Public)
```http
GET /api/ProductsSimple?page=1&pageSize=10&categoryId=1

Response:
{
  "products": [...],
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

### 2. Get Product Details
```http
GET /api/ProductsSimple/{productId}

Response:
{
  "productId": 1,
  "productName": "Sample Product",
  "price": 99.99,
  "description": "Product description",
  "stockQuantity": 50,
  "categoryName": "Electronics"
}
```

### 3. Get Product Variants
```http
GET /api/ProductVariants?productId=1

Response:
{
  "variants": [
    {
      "variantId": 1,
      "size": "M",
      "color": "Red", 
      "stockQuantity": 10
    }
  ]
}
```

## 📋 Order Endpoints

### 1. Create Order
```http
POST /api/Orders
Authorization: Bearer <token>

{
  "shippingAddress": "123 Main St, City, Country",
  "items": [
    {
      "productId": 1,
      "variantId": 2,
      "quantity": 1,
      "price": 99.99
    }
  ]
}
```

### 2. Get User Orders
```http
GET /api/Orders?page=1&pageSize=10
Authorization: Bearer <token>
```

### 3. Get Order Details
```http
GET /api/Orders/{orderId}
Authorization: Bearer <token>
```

## 💳 Payment Endpoints

### 1. Process Payment
```http
POST /api/Payments
Authorization: Bearer <token>

{
  "orderId": 123,
  "paymentMethod": "credit_card",
  "amount": 199.99
}
```

### 2. Get Payment Status
```http
GET /api/Payments/order/{orderId}
Authorization: Bearer <token>
```

## ⭐ Review Endpoints

### 1. Get Product Reviews
```http
GET /api/Reviews?productId=1&page=1&pageSize=5

Response:
{
  "reviews": [...],
  "averageRating": 4.5,
  "totalCount": 25
}
```

### 2. Add Review
```http
POST /api/Reviews
Authorization: Bearer <token>

{
  "productId": 1,
  "rating": 5,
  "comment": "Great product!"
}
```

## 💬 Chat Endpoints

### 1. Get Conversations
```http
GET /api/Chat/conversations
Authorization: Bearer <token>
```

### 2. Send Message
```http
POST /api/Chat/conversations/{conversationId}/messages
Authorization: Bearer <token>

{
  "messageText": "Hello, I need help with my order",
  "messageType": "text"
}
```

## 📊 Category Endpoints (Public)

### 1. Get All Categories
```http
GET /api/Categories

Response:
{
  "categories": [
    {
      "categoryId": 1,
      "categoryName": "Electronics",
      "description": "Electronic devices",
      "productCount": 25
    }
  ]
}
```

## 🚨 Error Handling

### Standard Error Response
```json
{
  "success": false,
  "message": "Error description",
  "errors": {
    "field": ["Validation error message"]
  },
  "statusCode": 400,
  "timestamp": "2025-08-30T10:30:00Z"
}
```

### Common HTTP Status Codes
- `200` - Success
- `201` - Created
- `400` - Bad Request (validation errors)
- `401` - Unauthorized (invalid/missing token)
- `403` - Forbidden (insufficient permissions)
- `404` - Not Found
- `500` - Internal Server Error

## 🔧 Frontend Implementation Examples

### JavaScript/Fetch Example
```javascript
// Login function
async function login(username, password) {
  try {
    const response = await fetch('http://localhost:5177/api/Users/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ username, password })
    });
    
    const data = await response.json();
    
    if (response.ok) {
      localStorage.setItem('token', data.token);
      localStorage.setItem('user', JSON.stringify(data));
      return data;
    } else {
      throw new Error(data.message);
    }
  } catch (error) {
    console.error('Login error:', error);
    throw error;
  }
}

// Authenticated API call
async function getCartItems() {
  const token = localStorage.getItem('token');
  
  const response = await fetch('http://localhost:5177/api/CartItem', {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  
  return response.json();
}
```

### React Hook Example
```javascript
import { useState, useEffect } from 'react';

function useApi(url, options = {}) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  
  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await fetch(`http://localhost:5177/api${url}`, {
          ...options,
          headers: {
            'Content-Type': 'application/json',
            ...(token && { 'Authorization': `Bearer ${token}` }),
            ...options.headers
          }
        });
        
        const result = await response.json();
        setData(result);
      } catch (err) {
        setError(err);
      } finally {
        setLoading(false);
      }
    };
    
    fetchData();
  }, [url]);
  
  return { data, loading, error };
}

// Usage
function ProductList() {
  const { data, loading, error } = useApi('/ProductsSimple?page=1&pageSize=10');
  
  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error.message}</div>;
  
  return (
    <div>
      {data?.products?.map(product => (
        <div key={product.productId}>
          <h3>{product.productName}</h3>
          <p>${product.price}</p>
        </div>
      ))}
    </div>
  );
}
```

## 🌐 CORS Configuration

The API is configured to accept requests from:
- `http://localhost:3000` (React)
- `http://localhost:5173` (Vite) 
- `http://localhost:4200` (Angular)
- `http://localhost:8080` (Vue)

For production, update the CORS policy in `Program.cs` with your domain.

## 📱 Mobile App Integration

For React Native or mobile apps, use the same endpoints with proper headers:

```javascript
// React Native example
import AsyncStorage from '@react-native-async-storage/async-storage';

const apiCall = async (endpoint, options = {}) => {
  const token = await AsyncStorage.getItem('token');
  
  const response = await fetch(`http://localhost:5177/api${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token && { 'Authorization': `Bearer ${token}` }),
      ...options.headers
    }
  });
  
  return response.json();
};
```

## 🔍 Testing with Swagger

Visit `http://localhost:5177/swagger` to test all endpoints interactively.

## 📞 Support

For integration issues or questions, refer to:
- Swagger documentation: `http://localhost:5177/swagger`
- API base URL: `http://localhost:5177/api`
- This documentation file

// ==================== REACT CONFIGURATION ====================

// 1. Create .env file in React project root
// .env
REACT_APP_API_BASE_URL=http://localhost:5177/api
REACT_APP_API_TIMEOUT=30000

// 2. API service configuration
// src/services/api.js
import axios from 'axios';

const API_BASE_URL = process.env.REACT_APP_API_BASE_URL;

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: parseInt(process.env.REACT_APP_API_TIMEOUT) || 30000,
  headers: {
    'Content-Type': 'application/json',
  }
});

// Request interceptor to add auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;

// 3. Auth service
// src/services/authService.js
import apiClient from './api';

export const authService = {
  login: async (username, password) => {
    const response = await apiClient.post('/Users/login', { username, password });
    if (response.data.token) {
      localStorage.setItem('token', response.data.token);
      localStorage.setItem('user', JSON.stringify(response.data));
    }
    return response.data;
  },

  register: async (userData) => {
    const response = await apiClient.post('/Users/register', userData);
    return response.data;
  },

  logout: () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  },

  getCurrentUser: () => {
    return JSON.parse(localStorage.getItem('user') || 'null');
  },

  isAuthenticated: () => {
    return !!localStorage.getItem('token');
  }
};

// 4. Product service
// src/services/productService.js
import apiClient from './api';

export const productService = {
  getProducts: async (page = 1, pageSize = 10, categoryId = null) => {
    const params = { page, pageSize };
    if (categoryId) params.categoryId = categoryId;
    
    const response = await apiClient.get('/ProductsSimple', { params });
    return response.data;
  },

  getProductById: async (productId) => {
    const response = await apiClient.get(`/ProductsSimple/${productId}`);
    return response.data;
  },

  getProductVariants: async (productId) => {
    const response = await apiClient.get('/ProductVariants', { 
      params: { productId } 
    });
    return response.data;
  }
};

// 5. Cart service
// src/services/cartService.js
import apiClient from './api';

export const cartService = {
  getCartItems: async () => {
    const response = await apiClient.get('/CartItem');
    return response.data;
  },

  addToCart: async (productId, variantId, quantity) => {
    const response = await apiClient.post('/CartItem', {
      productId,
      variantId,
      quantity
    });
    return response.data;
  },

  updateCartItem: async (cartItemId, quantity) => {
    const response = await apiClient.put(`/CartItem/${cartItemId}`, {
      quantity
    });
    return response.data;
  },

  removeFromCart: async (cartItemId) => {
    const response = await apiClient.delete(`/CartItem/${cartItemId}`);
    return response.data;
  },

  clearCart: async () => {
    const response = await apiClient.delete('/CartItem/clear');
    return response.data;
  }
};

// 6. React Context for authentication
// src/contexts/AuthContext.jsx
import React, { createContext, useContext, useState, useEffect } from 'react';
import { authService } from '../services/authService';

const AuthContext = createContext();

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const currentUser = authService.getCurrentUser();
    setUser(currentUser);
    setLoading(false);
  }, []);

  const login = async (username, password) => {
    try {
      const userData = await authService.login(username, password);
      setUser(userData);
      return userData;
    } catch (error) {
      throw error;
    }
  };

  const logout = () => {
    authService.logout();
    setUser(null);
  };

  const value = {
    user,
    login,
    logout,
    isAuthenticated: !!user,
    loading
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};

// ==================== VUE.JS CONFIGURATION ====================

// 1. Create .env file in Vue project root
// .env
VUE_APP_API_BASE_URL=http://localhost:5177/api

// 2. API configuration
// src/services/api.js
import axios from 'axios';

const apiClient = axios.create({
  baseURL: process.env.VUE_APP_API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  }
});

apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  }
);

export default apiClient;

// 3. Vuex store for authentication
// src/store/modules/auth.js
import apiClient from '@/services/api';

const state = {
  user: null,
  token: localStorage.getItem('token') || null,
  isAuthenticated: false
};

const mutations = {
  SET_USER(state, user) {
    state.user = user;
    state.isAuthenticated = !!user;
  },
  SET_TOKEN(state, token) {
    state.token = token;
    localStorage.setItem('token', token);
  },
  LOGOUT(state) {
    state.user = null;
    state.token = null;
    state.isAuthenticated = false;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
};

const actions = {
  async login({ commit }, { username, password }) {
    try {
      const response = await apiClient.post('/Users/login', { username, password });
      const { token, ...userData } = response.data;
      
      commit('SET_TOKEN', token);
      commit('SET_USER', userData);
      localStorage.setItem('user', JSON.stringify(userData));
      
      return response.data;
    } catch (error) {
      throw error;
    }
  },

  logout({ commit }) {
    commit('LOGOUT');
  }
};

export default {
  namespaced: true,
  state,
  mutations,
  actions
};

// ==================== ANGULAR CONFIGURATION ====================

// 1. Environment configuration
// src/environments/environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5177/api'
};

// 2. HTTP Interceptor for authentication
// src/app/interceptors/auth.interceptor.ts
import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler } from '@angular/common/http';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler) {
    const token = localStorage.getItem('token');
    
    if (token) {
      const authReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });
      return next.handle(authReq);
    }
    
    return next.handle(req);
  }
}

// 3. Auth service
// src/app/services/auth.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const user = localStorage.getItem('user');
    if (user) {
      this.currentUserSubject.next(JSON.parse(user));
    }
  }

  login(username: string, password: string): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/Users/login`, { username, password });
  }

  logout(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    this.currentUserSubject.next(null);
  }

  get currentUserValue() {
    return this.currentUserSubject.value;
  }
}

// ==================== REACT NATIVE CONFIGURATION ====================

// 1. API configuration
// src/services/api.js
import AsyncStorage from '@react-native-async-storage/async-storage';

const API_BASE_URL = 'http://localhost:5177/api'; // Use 10.0.2.2:5177 for Android emulator

class ApiService {
  async request(endpoint, options = {}) {
    const url = `${API_BASE_URL}${endpoint}`;
    const token = await AsyncStorage.getItem('token');
    
    const config = {
      headers: {
        'Content-Type': 'application/json',
        ...(token && { 'Authorization': `Bearer ${token}` }),
      },
      ...options,
    };

    try {
      const response = await fetch(url, config);
      const data = await response.json();
      
      if (!response.ok) {
        throw new Error(data.message || 'API request failed');
      }
      
      return data;
    } catch (error) {
      if (error.message.includes('401')) {
        await AsyncStorage.multiRemove(['token', 'user']);
        // Navigate to login screen
      }
      throw error;
    }
  }

  // Auth methods
  async login(username, password) {
    const data = await this.request('/Users/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    });
    
    if (data.token) {
      await AsyncStorage.setItem('token', data.token);
      await AsyncStorage.setItem('user', JSON.stringify(data));
    }
    
    return data;
  }

  // Cart methods
  async getCartItems() {
    return this.request('/CartItem');
  }

  async addToCart(productId, variantId, quantity) {
    return this.request('/CartItem', {
      method: 'POST',
      body: JSON.stringify({ productId, variantId, quantity }),
    });
  }
}

export default new ApiService();

import axios, { type AxiosInstance, type AxiosError } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';

// Create axios instance
const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Enable sending cookies and auth headers with CORS
});

// Request interceptor to add JWT token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('jwt_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor for error handling
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401) {
      // Clear token and redirect to login
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('user_email');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

// Types
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  tokenType: string;
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
}

export interface ConversionRequest {
  from: string;
  to: string;
  amount: number;
}

export interface ConversionResponse {
  fromCurrency: string;
  toCurrency: string;
  originalAmount: number;
  convertedAmount: number;
  exchangeRate: number;
  date: string;
}

export interface LatestRatesResponse {
  baseCurrency: string;
  date: string | null;
  rates: Record<string, number>;
}

export interface HistoricalRatesRequest {
  baseCurrency: string;
  startDate: string;
  endDate: string;
  pageNumber?: number;
  pageSize?: number;
}

export interface PagedHistoricalRatesData {
  base: string;
  startDate: string;
  endDate: string;
  rates: Record<string, Record<string, number>>; // date -> { currency -> rate }
}

export interface PaginationMetadata {
  currentPage: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PagedHistoricalRatesResponse {
  data: PagedHistoricalRatesData;
  pagination: PaginationMetadata;
}

// Authentication API
export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.post<LoginResponse>('/login', data);
    if (response.data.accessToken) {
      localStorage.setItem('jwt_token', response.data.accessToken);
      localStorage.setItem('user_email', data.email);
    }
    return response.data;
  },

  register: async (data: RegisterRequest): Promise<void> => {
    await apiClient.post('/register', data);
  },

  logout: () => {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_email');
  },

  isAuthenticated: (): boolean => {
    return !!localStorage.getItem('jwt_token');
  },

  getUserEmail: (): string | null => {
    return localStorage.getItem('user_email');
  },
};

// Currency API
export const currencyApi = {
  // GET /api/v1/currency/convert?from={from}&to={to}&amount={amount}
  convert: async (data: ConversionRequest): Promise<ConversionResponse> => {
    const response = await apiClient.get<ConversionResponse>('/api/v1/currency/convert', {
      params: {
        from: data.from,
        to: data.to,
        amount: data.amount,
      },
    });
    return response.data;
  },

  // GET /api/v1/currency/latest/{baseCurrency}
  getLatestRates: async (baseCurrency: string): Promise<LatestRatesResponse> => {
    const response = await apiClient.get<LatestRatesResponse>(`/api/v1/currency/latest/${baseCurrency}`);
    return response.data;
  },

  // GET /api/v1/currency/rate?from={from}&to={to}
  getExchangeRate: async (from: string, to: string): Promise<LatestRatesResponse> => {
    const response = await apiClient.get<LatestRatesResponse>('/api/v1/currency/rate', {
      params: { from, to },
    });
    return response.data;
  },

  // GET /api/v1/currency/historical?baseCurrency={baseCurrency}&startDate={startDate}&endDate={endDate}&pageNumber={pageNumber}&pageSize={pageSize}
  getHistoricalRates: async (data: HistoricalRatesRequest): Promise<PagedHistoricalRatesResponse> => {
    const response = await apiClient.get<PagedHistoricalRatesResponse>('/api/v1/currency/historical', {
      params: {
        baseCurrency: data.baseCurrency,
        startDate: data.startDate,
        endDate: data.endDate,
        pageNumber: data.pageNumber || 1,
        pageSize: data.pageSize || 10,
      },
    });
    return response.data;
  },
};

export default apiClient;

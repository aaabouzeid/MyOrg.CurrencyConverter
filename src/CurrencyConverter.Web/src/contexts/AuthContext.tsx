import React, { createContext, useContext, useState, useEffect, type ReactNode } from 'react';
import { authApi, type LoginRequest, type RegisterRequest } from '../services/api';

interface AuthContextType {
  isAuthenticated: boolean;
  userEmail: string | null;
  login: (data: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
  loading: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [userEmail, setUserEmail] = useState<string | null>(null);
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    // Check authentication status on mount
    const authenticated = authApi.isAuthenticated();
    const email = authApi.getUserEmail();
    setIsAuthenticated(authenticated);
    setUserEmail(email);
    setLoading(false);
  }, []);

  const login = async (data: LoginRequest) => {
    await authApi.login(data);
    setIsAuthenticated(true);
    setUserEmail(data.email);
  };

  const register = async (data: RegisterRequest) => {
    await authApi.register(data);
  };

  const logout = () => {
    authApi.logout();
    setIsAuthenticated(false);
    setUserEmail(null);
  };

  return (
    <AuthContext.Provider value={{ isAuthenticated, userEmail, login, register, logout, loading }}>
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

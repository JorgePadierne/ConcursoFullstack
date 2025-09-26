/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useState, useEffect } from "react";
import type { ReactNode } from "react";
import type { User } from "../types/api";
import apiClient from "../utils/apiClient";

interface AuthContextType {
  user: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (token: string, user: User) => void;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const initializeAuth = async () => {
      try {
        const token = localStorage.getItem("auth_token");
        const storedUser = localStorage.getItem("user");
        
        console.log("Initializing auth, token exists:", !!token);
        console.log("Stored user:", storedUser);

        if (token && storedUser) {
          apiClient.setAuthToken(token);
          const parsedUser = JSON.parse(storedUser);
          
          // Ensure user has a role
          if (!parsedUser.role) {
            console.warn("No role found for user, setting default role");
            parsedUser.role = 'student';
            localStorage.setItem("user", JSON.stringify(parsedUser));
          }
          
          setUser(parsedUser);
          console.log("User from storage:", parsedUser);

          // Verify token is still valid
          try {
            const response = await apiClient.getCurrentUser();
            console.log("Token is valid, user:", response.data);
          } catch (error) {
            console.error("Token validation failed:", error);
            throw error; // Will be caught in the outer catch
          }
        }
      } catch (error) {
        console.error("Auth initialization error:", error);
        // Token is invalid or error occurred, clear auth data
        localStorage.removeItem("auth_token");
        localStorage.removeItem("user");
        apiClient.clearAuthToken();
        setUser(null);
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();
  }, []);

  const login = (token: string, userData: User) => {
    console.log("Login called with:", { token, userData });
    
    // Ensure user has a default role if not provided
    const userWithRole = {
      ...userData,
      role: userData.role || 'student' // Default role if not provided
    };
    
    apiClient.setAuthToken(token);
    localStorage.setItem("auth_token", token);
    localStorage.setItem("user", JSON.stringify(userWithRole));
    setUser(userWithRole);
    console.log("User logged in successfully:", userWithRole);
  };

  const logout = () => {
    apiClient.clearAuthToken();
    localStorage.removeItem("auth_token");
    localStorage.removeItem("user");
    setUser(null);
  };

  const refreshUser = async () => {
    try {
      const response = await apiClient.getCurrentUser();
      const updatedUser = response.data;
      setUser(updatedUser);
      localStorage.setItem("user", JSON.stringify(updatedUser));
    } catch (error) {
      console.error("Failed to refresh user data:", error);
    }
  };

  const value: AuthContextType = {
    user,
    isLoading,
    isAuthenticated: !!user,
    login,
    logout,
    refreshUser,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

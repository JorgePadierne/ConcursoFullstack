import axios, { type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';
import type { 
  User, 
  Course, 
  Coursework, 
  StudentDashboard, 
  TeacherCourseSummary, 
  Notification
} from '../types/api';

// Extend the User interface to include data property
interface UserWithData extends User {
  data?: Record<string, unknown>;
}

// Update the AuthResponse interface to use UserWithData
interface ExtendedAuthResponse {
  token: string;
  user: UserWithData;
  data?: Record<string, unknown>;
  expiresIn: number;
}

// Google API types
interface GoogleAuthInstance {
  signIn: (options?: { prompt?: string }) => Promise<GoogleUser>;
  signOut: () => Promise<void>;
  isSignedIn: {
    get: () => boolean;
    listen: (callback: (isSignedIn: boolean) => void) => void;
  };
  currentUser: {
    get: () => GoogleUser;
    listen: (callback: (user: GoogleUser) => void) => void;
  };
}

interface GoogleUser {
  getAuthResponse: (includeAuthorizationData?: boolean) => {
    access_token: string;
    id_token: string;
    expires_in: number;
    token_type: string;
  };
  getBasicProfile: () => {
    getId: () => string;
    getName: () => string;
    getGivenName: () => string;
    getFamilyName: () => string;
    getImageUrl: () => string;
    getEmail: () => string;
  };
  isSignedIn: () => boolean;
  hasGrantedScopes: (scopes: string) => boolean;
}

// Extend the Window interface to include Google API types
declare global {
  interface Window {
    gapi: {
      load: (module: string, callback: () => void) => void;
      auth2: {
        init: (config: {
          client_id: string;
          cookie_policy?: string;
          scope?: string;
          fetch_basic_profile?: boolean;
        }) => Promise<GoogleAuthInstance>;
        getAuthInstance: () => GoogleAuthInstance;
      };
    };
  }
}

// Re-export types for convenience
export type { User, Course, Coursework, StudentDashboard, TeacherCourseSummary, Notification };

class ApiClient {
  private readonly client: AxiosInstance;

  constructor(baseURL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080') {
    // Usar la URL del parámetro, luego la de las variables de entorno, o localhost:8080 como último recurso
    let resolvedBaseURL = baseURL || import.meta.env.VITE_API_BASE_URL || 'http://localhost:8080';
    
    // Asegurarse de que la URL base termine con /api si es necesario
    if (resolvedBaseURL && !resolvedBaseURL.endsWith('/')) {
      resolvedBaseURL = `${resolvedBaseURL}/api`;
    } else if (resolvedBaseURL) {
      resolvedBaseURL = `${resolvedBaseURL}api`;
    }
    
    console.log('API Base URL:', resolvedBaseURL);
    
    this.client = axios.create({
      baseURL: resolvedBaseURL,
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
      timeout: 30000, // Aumentado a 30 segundos
      withCredentials: true, // Importante para cookies de autenticación
    });

    this.setupInterceptors();
  }

  private setupInterceptors(): void {
    this.client.interceptors.request.use(
      (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token');
        if (token) {
          config.headers = config.headers || {};
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('token');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // Auth methods
  async login(email: string, password: string): Promise<ExtendedAuthResponse> {
    const response = await this.client.post<ExtendedAuthResponse>('/auth/login', { email, password });
    return response.data;
  }

  logout(): void {
    localStorage.removeItem('token');
  }

  async googleLogin(): Promise<{ url: string }> {
    const response = await this.client.get<{ url: string }>('/auth/google');
    return response.data;
  }

  async googleCallback(code: string): Promise<ExtendedAuthResponse> {
    const response = await this.client.get<ExtendedAuthResponse>(`/auth/oauth2/callback?code=${code}`);
    return response.data;
  }

  // User methods
  async getCurrentUser(): Promise<User> {
    const response = await this.client.get<User>('/auth/me');
    return response.data;
  }

  // Course methods
  async getCourses(): Promise<Course[]> {
    const response = await this.client.get<Course[]>('/courses');
    return response.data;
  }

  async getCoursework(courseId: string): Promise<Coursework[]> {
    const response = await this.client.get<Coursework[]>(`/api/courses/${courseId}/coursework`);
    return response.data;
  }

  // Dashboard endpoints
  async getStudentDashboard(): Promise<StudentDashboard> {
    const response = await this.client.get<StudentDashboard>('/api/dashboard/alumno');
    return response.data;
  }
  async getTeacherDashboard(): Promise<TeacherCourseSummary[]> {
    const response = await this.client.get<TeacherCourseSummary[]>('/api/dashboard/profesor');
    return response.data;
  }

  // Notification methods
  async sendNotification(message: string, userEmail: string): Promise<{ success: boolean }> {
    const response = await this.client.post<{ success: boolean }>('/notifications', { message, userEmail });
    return response.data;
  }

  async getNotifications(userEmail: string): Promise<Notification[]> {
    const response = await this.client.get<Notification[]>(`/notifications?userEmail=${encodeURIComponent(userEmail)}`);
    return response.data;
  }

  async markNotificationAsRead(id: number): Promise<{ success: boolean }> {
    const response = await this.client.patch<{ success: boolean }>(`/notifications/${id}/read`, {});
    return response.data;
  }

  // Set authentication token
  setAuthToken(token: string): void {
    if (token) {
      this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    }
  }

  // Clear authentication token
  clearAuthToken(): void {
    delete this.client.defaults.headers.common['Authorization'];
  }
}

// Create a singleton instance
const apiClient = new ApiClient();
export default apiClient;
export { ApiClient, type ExtendedAuthResponse as AuthResponse };


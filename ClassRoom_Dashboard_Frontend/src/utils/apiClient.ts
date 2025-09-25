import axios, { type AxiosInstance, type AxiosResponse } from 'axios';
import type { User, Course, Coursework, StudentDashboard, TeacherCourseSummary } from '../types/api';

class ApiClient {
  private client: AxiosInstance;

  constructor(baseURL?: string) {
    const resolvedBaseURL = baseURL || (import.meta.env.VITE_API_BASE_URL as string) || 'http://localhost:8080';
    this.client = axios.create({
      baseURL: resolvedBaseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Request interceptor to add auth token
    this.client.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Response interceptor to handle errors
    this.client.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          localStorage.removeItem('auth_token');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  public setAuthToken(token: string) {
    localStorage.setItem('auth_token', token);
    this.client.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  public clearAuthToken() {
    localStorage.removeItem('auth_token');
    delete this.client.defaults.headers.common['Authorization'];
  }

  public get isAuthenticated(): boolean {
    return !!localStorage.getItem('auth_token');
  }

  // Auth endpoints
  public async googleLogin(): Promise<AxiosResponse<{ url: string }>> {
    return this.client.get('/auth/google');
  }

  public async googleCallback(code: string): Promise<AxiosResponse<{ token: string; user: User; expiresIn: number }>> {
    return this.client.get('/auth/oauth2/callback', { params: { code } });
  }

  // User endpoints
  public async getCurrentUser(): Promise<AxiosResponse<User>> {
    return this.client.get('/api/me');
  }

  // Course endpoints
  public async getCourses(): Promise<AxiosResponse<Course[]>> {
    return this.client.get('/api/courses');
  }

  public async getCoursework(courseId: string): Promise<AxiosResponse<Coursework[]>> {
    return this.client.get(`/api/courses/${courseId}/coursework`);
  }

  // Dashboard endpoints
  public async getStudentDashboard(): Promise<AxiosResponse<StudentDashboard>> {
    return this.client.get('/api/dashboard/alumno');
  }

  public async getTeacherDashboard(): Promise<AxiosResponse<TeacherCourseSummary[]>> {
    return this.client.get('/api/dashboard/profesor');
  }

  // Notification endpoints
  public async sendNotification(message: string, userEmail: string): Promise<AxiosResponse<void>> {
    return this.client.post('/api/notifications/send', { message, userEmail });
  }

  public async getNotifications(): Promise<AxiosResponse<Notification[]>> {
    return this.client.get('/api/notifications');
  }

  public async markNotificationAsRead(id: number): Promise<AxiosResponse<void>> {
    return this.client.patch(`/api/notifications/${id}/read`);
  }
}

// Create a singleton instance
const apiClient = new ApiClient();

export default apiClient;
export { ApiClient };

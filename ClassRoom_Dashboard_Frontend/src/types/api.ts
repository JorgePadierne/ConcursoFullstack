export interface User {
  id: number;
  email: string;
  name?: string;
  role: string;
  googleRefreshToken?: string;
  googleAccessToken?: string;
  tokenExpiry?: string;
}

export interface Course {
  id: string;
  name?: string;
  section?: string;
  courseState?: string;
  teacherEmails?: string[];
  lastSync?: string;
}

export interface Coursework {
  id: string;
  courseId?: string;
  title?: string;
  description?: string;
  dueDate?: string;
  maxPoints?: number;
  lastSync?: string;
}

export interface Submission {
  id: string;
  courseworkId?: string;
  courseId?: string;
  userEmail?: string;
  handedIn?: boolean;
  late?: boolean;
  grade?: number;
  lastSync?: string;
}

export interface Attendance {
  id: number;
  courseId?: string;
  date?: string;
  userEmail?: string;
  present?: boolean;
}

export interface Notification {
  id: number;
  message: string;
  userEmail?: string;
  createdAt: string;
  isRead: boolean;
}

export interface StudentDashboard {
  email: string;
  totalSubmissions: number;
  handedIn: number;
  pending: number;
  late: number;
  completionPercent: number;
}

export interface TeacherCourseSummary {
  courseId: string;
  tasks: number;
  submissions: number;
  handedIn: number;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiresIn: number;
}

export interface ApiError {
  message: string;
  statusCode: number;
}

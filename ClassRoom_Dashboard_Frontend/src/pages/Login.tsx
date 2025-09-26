import React, { useEffect, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";
import apiClient from "../utils/apiClient";
import { BookOpenIcon } from "@heroicons/react/24/outline";
import axios from "axios";


const Login: React.FC = () => {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isAuthenticated) {
      navigate("/dashboard");
    }
  }, [isAuthenticated, navigate]);

  useEffect(() => {
    const handleGoogleCallback = async () => {
      const code = searchParams.get("code");
      const error = searchParams.get("error");

      console.log("Google callback received:", { code, error });

      if (error) {
        const errorDescription = searchParams.get("error_description") || "Error desconocido";
        console.error("Google OAuth error:", { error, errorDescription });
        setError(`Error de autenticación: ${errorDescription}`);
        return;
      }

      if (!code) {
        console.log("No code parameter found in URL");
        return;
      }

      try {
        setIsLoading(true);
        setError(null);
        console.log("Exchanging code for token...");
        
        const response = await apiClient.googleCallback(code);
        console.log("Auth response:", response);
        
        if (!response?.token || !response.user) {
          console.error("Invalid response format:", response);
          throw new Error("Respuesta del servidor inválida");
        }
        
        const { token, user } = response;
        console.log("Login successful, user:", user);
        
        // Asegurarse de que el usuario tenga un rol y el ID sea un número
        const userWithRole = {
          ...user,
          id: Number(user.id) || 0, // Convertir a número o usar 0 como respaldo
          role: user.role || 'student' // Rol por defecto
        };
        
        console.log("User with role and converted ID:", userWithRole);
        
        // Guardar el token y usuario
        login(token, userWithRole);
        
        // Redirigir al dashboard después de un breve retraso
        setTimeout(() => {
          console.log("Redirecting to dashboard");
          navigate("/dashboard", { replace: true });
        }, 100);
        
      } catch (err: unknown) {
        console.error("Authentication error:", err);
        let errorMessage = "Error al conectar con el servidor";
        
        // Check if it's an Axios error
        if (axios.isAxiosError(err)) {
          const axiosError = err;
          console.error("Axios error:", {
            message: axiosError.message,
            code: axiosError.code,
            status: axiosError.response?.status,
            responseData: axiosError.response?.data,
            request: axiosError.request
          });
          
          if (axiosError.code === 'ECONNABORTED') {
            errorMessage = "El servidor está tardando demasiado en responder. Por favor, verifica tu conexión e inténtalo de nuevo.";
          } else if (axiosError.response) {
            // Server responded with a status code outside the 2xx range
            const { status, data } = axiosError.response;
            
            if (status === 401) {
              errorMessage = "Credenciales inválidas. Por favor, verifica e inténtalo de nuevo.";
            } else if (status === 403) {
              errorMessage = "No tienes permiso para acceder. Por favor, contacta al administrador.";
            } else if (status === 404) {
              errorMessage = "Recurso no encontrado. Por favor, verifica la URL e inténtalo de nuevo.";
            } else if (status >= 500) {
              errorMessage = "Error interno del servidor. Por favor, inténtalo de nuevo más tarde.";
            } else {
              errorMessage = (data as { message?: string })?.message || `Error del servidor (${status})`;
            }
          } else if (axiosError.request) {
            // Request was made but no response received
            console.error("No se recibió respuesta del servidor:", axiosError.request);
            errorMessage = "No se pudo conectar con el servidor. Verifica tu conexión a internet e inténtalo de nuevo.";
          }
        }
        
        setError(errorMessage);
      } finally {
        setIsLoading(false);
      }
    };

    handleGoogleCallback();
  }, [searchParams, login, navigate]);
  const handleGoogleLogin = () => {
    const clientId = (import.meta.env.VITE_GOOGLE_CLIENT_ID as string) || "";
    const redirectUri =
      (import.meta.env.VITE_GOOGLE_REDIRECT_URI as string) || "/login";
    const scope = encodeURIComponent(
      [
        "openid",
        "email",
        "profile",
        "https://www.googleapis.com/auth/classroom.courses.readonly",
        "https://www.googleapis.com/auth/classroom.rosters.readonly",
        "https://www.googleapis.com/auth/classroom.coursework.me",
        "https://www.googleapis.com/auth/classroom.coursework.students",
        "https://www.googleapis.com/auth/classroom.student-submissions.students.readonly",
      ].join(" ")
    );

    if (clientId) {
      const authUrl = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${encodeURIComponent(
        clientId
      )}&response_type=code&scope=${scope}&redirect_uri=${encodeURIComponent(
        redirectUri
      )}&access_type=offline&prompt=consent`;
      window.location.href = authUrl;
      return;
    }

    // Fallback: use backend endpoint if client id is not provided in the frontend
    const base =
      (import.meta.env.VITE_API_BASE_URL as string) ||
      "https://concursofullstack.onrender.com";
    window.location.href = `${base}/auth/google`;
  };

  // Mostrar estado de carga
  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:to-gray-800">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-t-2 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-700 dark:text-gray-300">Iniciando sesión...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:to-gray-800 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="mx-auto h-12 w-12 flex items-center justify-center rounded-full bg-blue-600">
            <BookOpenIcon className="h-6 w-6 text-white" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-white">
            Classroom Dashboard
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
            Inicia sesión con tu cuenta de Google para acceder al aula virtual
          </p>
          
          {error && (
            <div className="mt-4 p-3 bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded relative" role="alert">
              <span className="block sm:inline">{error}</span>
              <button 
                className="absolute top-0 bottom-0 right-0 px-4 py-3"
                onClick={() => setError(null)}
              >
                <span className="sr-only">Cerrar</span>
                <svg className="h-6 w-6 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          )}
        </div>
        <div className="mt-8 space-y-6">
          <button
            onClick={handleGoogleLogin}
            className="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 dark:bg-blue-700 dark:hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors duration-200"
          >
            <svg className="w-5 h-5 mr-3" viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
              />
              <path
                fill="currentColor"
                d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
              />
              <path
                fill="currentColor"
                d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
              />
              <path
                fill="currentColor"
                d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
              />
            </svg>
            {isLoading ? 'Iniciando sesión...' : 'Iniciar sesión con Google'}
          </button>
          <div className="text-center">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              By signing in, you agree to our terms of service and privacy
              policy
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;

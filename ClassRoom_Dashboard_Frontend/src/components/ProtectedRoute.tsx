import React, { useEffect } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../contexts/AuthContext";

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredRole?: string;
}

const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredRole
}) => {
  const { user, isLoading, isAuthenticated } = useAuth();
  const location = useLocation();

  // Debug logs
  useEffect(() => {
    console.log("ProtectedRoute - Auth State:", {
      isAuthenticated,
      user,
      isLoading,
      requiredRole,
      currentPath: location.pathname
    });
  }, [isAuthenticated, user, isLoading, requiredRole, location.pathname]);

  // Show loading spinner while authentication is being checked
  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  // If not authenticated, redirect to login with the return url
  if (!isAuthenticated) {
    console.log("Not authenticated, redirecting to login");
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // If user doesn't have the required role, redirect to appropriate dashboard
  if (requiredRole && user?.role !== requiredRole) {
    console.log(`User role ${user?.role} doesn't match required role ${requiredRole}`);
    const redirectPath = user?.role === "profesor" ? "/dashboard" : "/dashboard";
    return <Navigate to={redirectPath} replace />;
  }

  // If we get here, the user is authenticated and has the required role
  console.log("Access granted to protected route");
  return <>{children}</>;
};

export default ProtectedRoute;

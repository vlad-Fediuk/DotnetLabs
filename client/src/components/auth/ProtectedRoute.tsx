import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/useAuthStore';

interface ProtectedRouteProps {
  permission?: string;
  anyPermissions?: string[];
  children: ReactNode;
}

export function ProtectedRoute({
  permission,
  anyPermissions,
  children,
}: ProtectedRouteProps) {
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const hasAnyPermission = useAuthStore((s) => s.hasAnyPermission);
  const location = useLocation();

  if (!user || !accessToken) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  if (permission && !hasPermission(permission)) {
    return <Navigate to="/" replace />;
  }

  if (anyPermissions && !hasAnyPermission(anyPermissions)) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}

import type { ReactNode } from 'react';
import { useAuthStore } from '@/store/useAuthStore';

interface CanProps {
  permission?: string;
  anyPermissions?: string[];
  role?: string;
  fallback?: ReactNode;
  children: ReactNode;
}

export function Can({
  permission,
  anyPermissions,
  role,
  fallback = null,
  children,
}: CanProps) {
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const hasAnyPermission = useAuthStore((s) => s.hasAnyPermission);
  const hasRole = useAuthStore((s) => s.hasRole);

  if (permission && !hasPermission(permission)) {
    return <>{fallback}</>;
  }

  if (anyPermissions && !hasAnyPermission(anyPermissions)) {
    return <>{fallback}</>;
  }

  if (role && !hasRole(role)) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}

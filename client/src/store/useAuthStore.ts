import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { AuthResponse, UserDto } from '@/types/api';

interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  refreshToken: string | null;
  expiresAt: string | null;
  setSession: (auth: AuthResponse) => void;
  updateUser: (user: UserDto) => void;
  clearSession: () => void;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (permissions: string[]) => boolean;
  hasRole: (role: string) => boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      user: null,
      accessToken: null,
      refreshToken: null,
      expiresAt: null,

      setSession: (auth) =>
        set({
          user: auth.user,
          accessToken: auth.accessToken,
          refreshToken: auth.refreshToken,
          expiresAt: auth.expiresAt,
        }),

      updateUser: (user) => set({ user }),

      clearSession: () =>
        set({
          user: null,
          accessToken: null,
          refreshToken: null,
          expiresAt: null,
        }),

      hasPermission: (permission) => {
        const currentUser = get().user;
        if (!currentUser || !currentUser.permissions) {
          return false;
        }
        return currentUser.permissions.some(
          (p) => p.toLowerCase() === permission.toLowerCase()
        );
      },

      hasAnyPermission: (permissions) => {
        const currentUser = get().user;
        if (!currentUser || !currentUser.permissions) {
          return false;
        }
        const lowerSet = new Set(
          currentUser.permissions.map((p) => p.toLowerCase())
        );
        return permissions.some((p) => lowerSet.has(p.toLowerCase()));
      },

      hasRole: (role) => {
        const currentUser = get().user;
        if (!currentUser || !currentUser.roles) {
          return false;
        }
        return currentUser.roles.some(
          (r) => r.toLowerCase() === role.toLowerCase()
        );
      },
    }),
    {
      name: 'nexus-ecommerce-auth',
    }
  )
);

import { useEffect } from 'react';
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'sonner';
import { AppLayout } from '@/components/layout/AppLayout';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { CatalogPage } from '@/pages/CatalogPage';
import { ItemDetailsPage } from '@/pages/ItemDetailsPage';
import { LoginPage } from '@/pages/LoginPage';
import { RegisterPage } from '@/pages/RegisterPage';
import { OrdersPage } from '@/pages/OrdersPage';
import { WorkspacePage } from '@/pages/WorkspacePage';
import { useAuthStore } from '@/store/useAuthStore';
import { usersApi } from '@/api/services';
import { Permissions } from '@/types/api';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 10000,
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

const workspacePermissions = [
  Permissions.Catalog.Manage,
  Permissions.Orders.ViewAll,
  Permissions.Orders.ManageStatus,
  Permissions.Users.View,
  Permissions.Users.ManageRoles,
  Permissions.System.ManageCache,
];

function SessionInitializer() {
  const accessToken = useAuthStore((s) => s.accessToken);
  const updateUser = useAuthStore((s) => s.updateUser);

  useEffect(() => {
    if (!accessToken) return;
    usersApi
      .getMe()
      .then((profile) => updateUser(profile))
      .catch((err) => void err);
  }, [accessToken, updateUser]);

  return null;
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <SessionInitializer />
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<CatalogPage />} />
            <Route path="items/:id" element={<ItemDetailsPage />} />
            <Route path="login" element={<LoginPage />} />
            <Route path="register" element={<RegisterPage />} />
            <Route
              path="orders"
              element={
                <ProtectedRoute>
                  <OrdersPage />
                </ProtectedRoute>
              }
            />
            <Route
              path="workspace"
              element={
                <ProtectedRoute anyPermissions={workspacePermissions}>
                  <WorkspacePage />
                </ProtectedRoute>
              }
            />
            <Route path="*" element={<Navigate to="/" replace />} />
          </Route>
        </Routes>
        <Toaster richColors position="top-right" closeButton />
      </BrowserRouter>
    </QueryClientProvider>
  );
}

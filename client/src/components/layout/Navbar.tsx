import { Link, NavLink, useNavigate } from 'react-router-dom';
import {
  ShoppingBag,
  Package,
  LayoutDashboard,
  LogOut,
  User as UserIcon,
  Layers,
} from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/Button';
import { useAuthStore } from '@/store/useAuthStore';
import { useCartStore } from '@/store/useCartStore';
import { authApi } from '@/api/services';
import { Permissions } from '@/types/api';
import { cn } from '@/lib/utils';

const managementPermissions = [
  Permissions.Catalog.Manage,
  Permissions.Orders.ViewAll,
  Permissions.Orders.ManageStatus,
  Permissions.Users.View,
  Permissions.Users.ManageRoles,
  Permissions.System.ManageCache,
];

export function Navbar() {
  const user = useAuthStore((s) => s.user);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const clearSession = useAuthStore((s) => s.clearSession);
  const hasAnyPermission = useAuthStore((s) => s.hasAnyPermission);

  const totalItems = useCartStore((s) => s.getTotalItems());
  const setCartOpen = useCartStore((s) => s.setOpen);
  const navigate = useNavigate();

  const canAccessWorkspace = hasAnyPermission(managementPermissions);

  const handleLogout = async () => {
    try {
      await authApi.logout(refreshToken);
    } catch (err) {
      void err;
    } finally {
      clearSession();
      toast.success('Ви вийшли з облікового запису');
      navigate('/');
    }
  };

  return (
    <header className="sticky top-0 z-40 w-full border-b border-zinc-200/80 bg-white/90 backdrop-blur-md">
      <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-4 sm:px-6 lg:px-8">
        <div className="flex items-center gap-8">
          <Link to="/" className="flex items-center gap-2.5 group">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-indigo-600 text-white shadow-xs transition-transform group-hover:scale-105">
              <Layers className="h-5 w-5" />
            </div>
            <div className="flex flex-col">
              <span className="text-base font-bold tracking-tight text-zinc-900 leading-none">
                NEXUS
              </span>
              <span className="text-[10px] font-medium uppercase tracking-widest text-indigo-600 mt-0.5">
                Store
              </span>
            </div>
          </Link>

          <nav className="hidden md:flex items-center gap-1">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                cn(
                  'px-3.5 py-2 rounded-lg text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-zinc-100 text-zinc-900'
                    : 'text-zinc-600 hover:bg-zinc-50 hover:text-zinc-900'
                )
              }
            >
              Каталог
            </NavLink>

            {user && (
              <NavLink
                to="/orders"
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-zinc-100 text-zinc-900'
                      : 'text-zinc-600 hover:bg-zinc-50 hover:text-zinc-900'
                  )
                }
              >
                <Package className="h-4 w-4 text-zinc-500" />
                Мої замовлення
              </NavLink>
            )}

            {user && canAccessWorkspace && (
              <NavLink
                to="/workspace"
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-1.5 px-3.5 py-2 rounded-lg text-sm font-medium transition-colors',
                    isActive
                      ? 'bg-indigo-50 text-indigo-700'
                      : 'text-indigo-600 hover:bg-indigo-50/60'
                  )
                }
              >
                <LayoutDashboard className="h-4 w-4" />
                Управління магазином
              </NavLink>
            )}
          </nav>
        </div>

        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={() => setCartOpen(true)}
            className="relative inline-flex h-10 items-center gap-2 rounded-xl border border-zinc-200 bg-white px-3.5 text-sm font-medium text-zinc-800 shadow-2xs transition-colors hover:bg-zinc-50 cursor-pointer"
          >
            <ShoppingBag className="h-4 w-4 text-indigo-600" />
            <span className="hidden sm:inline">Кошик</span>
            {totalItems > 0 && (
              <span className="flex h-5 min-w-5 items-center justify-center rounded-full bg-indigo-600 px-1.5 text-[11px] font-bold text-white">
                {totalItems}
              </span>
            )}
          </button>

          {user ? (
            <div className="flex items-center gap-2.5 pl-1">
              <div className="hidden sm:flex flex-col items-end">
                <span className="text-xs font-semibold text-zinc-900">
                  {user.displayName}
                </span>
                <span className="text-[11px] text-zinc-500">{user.email}</span>
              </div>

              <Button
                variant="ghost"
                size="icon"
                onClick={handleLogout}
                title="Вийти з облікового запису"
              >
                <LogOut className="h-4 w-4 text-zinc-600" />
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" asChild>
                <Link to="/login">
                  <UserIcon className="h-4 w-4" />
                  Увійти
                </Link>
              </Button>
              <Button variant="default" size="sm" asChild>
                <Link to="/register">Реєстрація</Link>
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  );
}

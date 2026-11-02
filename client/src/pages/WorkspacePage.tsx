import { useState } from 'react';
import {
  Package,
  ClipboardList,
  Users,
  Activity,
} from 'lucide-react';
import { CatalogManagerTab } from '@/components/workspace/CatalogManagerTab';
import { OrdersOperatorTab } from '@/components/workspace/OrdersOperatorTab';
import { UsersRolesTab } from '@/components/workspace/UsersRolesTab';
import { GatewaySystemTab } from '@/components/workspace/GatewaySystemTab';
import { useAuthStore } from '@/store/useAuthStore';
import { Permissions } from '@/types/api';
import { cn } from '@/lib/utils';

type TabKey = 'catalog' | 'orders' | 'users' | 'gateway';

export function WorkspacePage() {
  const hasPermission = useAuthStore((s) => s.hasPermission);
  const hasAnyPermission = useAuthStore((s) => s.hasAnyPermission);

  const canCatalog = hasPermission(Permissions.Catalog.Manage);
  const canOrders = hasAnyPermission([
    Permissions.Orders.ViewAll,
    Permissions.Orders.ManageStatus,
  ]);
  const canUsers = hasAnyPermission([
    Permissions.Users.View,
    Permissions.Users.ManageRoles,
  ]);
  const canGateway = hasPermission(Permissions.System.ManageCache);

  const availableTabs: Array<{
    key: TabKey;
    label: string;
    icon: typeof Package;
    enabled: boolean;
  }> = [
    {
      key: 'catalog',
      label: 'Товари та категорії',
      icon: Package,
      enabled: canCatalog,
    },
    {
      key: 'orders',
      label: 'Замовлення клієнтів',
      icon: ClipboardList,
      enabled: canOrders,
    },
    {
      key: 'users',
      label: 'Користувачі та ролі',
      icon: Users,
      enabled: canUsers,
    },
    {
      key: 'gateway',
      label: 'Стан магазину',
      icon: Activity,
      enabled: canGateway,
    },
  ];

  const firstEnabled = availableTabs.find((t) => t.enabled)?.key ?? 'catalog';
  const [activeTab, setActiveTab] = useState<TabKey>(firstEnabled);

  const effectiveTab =
    availableTabs.find((t) => t.key === activeTab && t.enabled)?.key ?? firstEnabled;

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 rounded-2xl border border-zinc-200/80 bg-white p-6 sm:flex-row sm:items-center sm:justify-between shadow-2xs">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900">
            Управління магазином
          </h1>
          <p className="text-xs sm:text-sm text-zinc-500">
            Керування асортиментом, обробка замовлень клієнтів та налаштування доступу.
          </p>
        </div>

        <div className="flex flex-wrap gap-1.5 rounded-xl bg-zinc-100 p-1.5">
          {availableTabs
            .filter((t) => t.enabled)
            .map((tab) => {
              const Icon = tab.icon;
              const isActive = effectiveTab === tab.key;
              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setActiveTab(tab.key)}
                  className={cn(
                    'flex items-center gap-1.5 rounded-lg px-3.5 py-2 text-xs font-semibold transition-all cursor-pointer',
                    isActive
                      ? 'bg-white text-indigo-600 shadow-xs'
                      : 'text-zinc-600 hover:text-zinc-900'
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {tab.label}
                </button>
              );
            })}
        </div>
      </div>

      {effectiveTab === 'catalog' && canCatalog && <CatalogManagerTab />}
      {effectiveTab === 'orders' && canOrders && <OrdersOperatorTab />}
      {effectiveTab === 'users' && canUsers && <UsersRolesTab />}
      {effectiveTab === 'gateway' && canGateway && <GatewaySystemTab />}
    </div>
  );
}

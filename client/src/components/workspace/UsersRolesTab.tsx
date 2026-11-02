import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Users, Shield, Check } from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/Dialog';
import { Can } from '@/components/auth/Can';
import { usersApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { useAuthStore } from '@/store/useAuthStore';
import { AllRoleList, Permissions, type UserDto } from '@/types/api';
import { cn, formatDateTime } from '@/lib/utils';

const roleLabels: Record<string, string> = {
  User: 'Покупець',
  CatalogManager: 'Менеджер каталогу',
  OrderOperator: 'Оператор замовлень',
  Support: 'Служба підтримки',
  Manager: 'Керівник зміни',
  Admin: 'Адміністратор',
};

const permissionLabels: Record<string, string> = {
  'catalog:view': 'Перегляд каталогу',
  'catalog:manage': 'Керування товарами',
  'orders:create': 'Оформлення покупок',
  'orders:view-own': 'Власні замовлення',
  'orders:view-all': 'Усі замовлення',
  'orders:manage-status': 'Зміна статусів замовлень',
  'users:view': 'Перегляд клієнтів',
  'users:manage-roles': 'Призначення ролей',
  'system:manage-cache': 'Налаштування системи',
};

export function UsersRolesTab() {
  const queryClient = useQueryClient();
  const currentUser = useAuthStore((s) => s.user);
  const updateCurrentUser = useAuthStore((s) => s.updateUser);

  const [editingUser, setEditingUser] = useState<UserDto | null>(null);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);

  const usersQuery = useQuery({
    queryKey: ['users-paged'],
    queryFn: () => usersApi.getUsersPaged(1, 50),
  });

  const updateRolesMutation = useMutation({
    mutationFn: async () => {
      if (!editingUser) return;
      await usersApi.updateUserRoles(editingUser.id, { roles: selectedRoles });
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users-paged'] });
      if (editingUser && currentUser && editingUser.id === currentUser.id) {
        const refreshedProfile = await usersApi.getMe();
        updateCurrentUser(refreshedProfile);
      }
      toast.success(`Права доступу для ${editingUser?.displayName} оновлено`);
      setEditingUser(null);
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const openRoleModal = (user: UserDto) => {
    setEditingUser(user);
    setSelectedRoles([...user.roles]);
  };

  const toggleRole = (role: string) => {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]
    );
  };

  const users = usersQuery.data?.items ?? [];

  return (
    <Card className="p-6 space-y-5">
      <div>
        <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
          <Users className="h-4 w-4 text-indigo-600" />
          Користувачі та співробітники ({users.length})
        </h3>
        <p className="text-xs text-zinc-500">
          Керування правами доступу клієнтів, менеджерів каталогу, операторів та служби підтримки.
        </p>
      </div>

      <div className="overflow-x-auto rounded-xl border border-zinc-200/80">
        <table className="w-full text-left text-xs">
          <thead className="border-b border-zinc-200 bg-zinc-50 text-zinc-500 uppercase">
            <tr>
              <th className="px-4 py-3 font-semibold">Користувач</th>
              <th className="px-4 py-3 font-semibold">Ролі</th>
              <th className="px-4 py-3 font-semibold">Доступні можливості</th>
              <th className="px-4 py-3 font-semibold">Дата реєстрації</th>
              <th className="px-4 py-3 text-right font-semibold">Дії</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-100 bg-white">
            {users.map((u) => (
              <tr key={u.id} className="hover:bg-zinc-50/60">
                <td className="px-4 py-3">
                  <div className="font-semibold text-zinc-900">{u.displayName}</div>
                  <div className="text-zinc-500">{u.email}</div>
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap gap-1">
                    {u.roles.map((r) => (
                      <Badge
                        key={r}
                        variant={r === 'Admin' ? 'primary' : 'default'}
                      >
                        {roleLabels[r] || r}
                      </Badge>
                    ))}
                  </div>
                </td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap gap-1 max-w-md">
                    {u.permissions.map((perm) => (
                      <span
                        key={perm}
                        className="rounded-md bg-zinc-100 px-2 py-0.5 text-[11px] text-zinc-700"
                      >
                        {permissionLabels[perm] || perm}
                      </span>
                    ))}
                  </div>
                </td>
                <td className="px-4 py-3 text-zinc-500">
                  {formatDateTime(u.createdAtUtc)}
                </td>
                <td className="px-4 py-3 text-right">
                  <Can
                    permission={Permissions.Users.ManageRoles}
                    fallback={
                      <span className="text-zinc-400 italic">Лише перегляд</span>
                    }
                  >
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => openRoleModal(u)}
                    >
                      <Shield className="h-3.5 w-3.5 text-indigo-600" />
                      Змінити ролі
                    </Button>
                  </Can>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Dialog
        open={Boolean(editingUser)}
        onOpenChange={(open) => !open && setEditingUser(null)}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Права доступу користувача</DialogTitle>
            <DialogDescription>
              <strong>{editingUser?.displayName}</strong> ({editingUser?.email})
            </DialogDescription>
          </DialogHeader>

          <div className="grid grid-cols-1 gap-2 py-2 sm:grid-cols-2">
            {AllRoleList.map((role) => {
              const isSelected = selectedRoles.includes(role);
              return (
                <button
                  key={role}
                  type="button"
                  onClick={() => toggleRole(role)}
                  className={cn(
                    'flex items-center justify-between rounded-xl border p-3 text-left text-xs font-medium transition-all cursor-pointer',
                    isSelected
                      ? 'border-indigo-500 bg-indigo-50/70 text-indigo-950'
                      : 'border-zinc-200 bg-white text-zinc-700 hover:bg-zinc-50'
                  )}
                >
                  <span>{roleLabels[role] || role}</span>
                  {isSelected && <Check className="h-4 w-4 text-indigo-600" />}
                </button>
              );
            })}
          </div>

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setEditingUser(null)}>
              Скасувати
            </Button>
            <Button
              disabled={selectedRoles.length === 0 || updateRolesMutation.isPending}
              onClick={() => updateRolesMutation.mutate()}
            >
              {updateRolesMutation.isPending ? 'Збереження...' : 'Зберегти'}
            </Button>
          </div>
        </DialogContent>
      </Dialog>
    </Card>
  );
}

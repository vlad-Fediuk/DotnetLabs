import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ClipboardList, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Can } from '@/components/auth/Can';
import { ordersApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { OrderStatus, Permissions } from '@/types/api';
import {
  cn,
  formatCurrency,
  formatDateTime,
  getOrderStatusMeta,
  getValidNextStatuses,
} from '@/lib/utils';

export function OrdersOperatorTab() {
  const [statusFilter, setStatusFilter] = useState<OrderStatus | undefined>(undefined);
  const queryClient = useQueryClient();

  const allOrdersQuery = useQuery({
    queryKey: ['all-orders', statusFilter],
    queryFn: () =>
      ordersApi.getAllOrders({
        status: statusFilter,
        page: 1,
        pageSize: 50,
      }),
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: OrderStatus }) =>
      ordersApi.updateOrderStatus(id, status),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: ['all-orders'] });
      queryClient.invalidateQueries({ queryKey: ['my-orders'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items-admin'] });
      toast.success(
        `Статус замовлення № ${updated.orderNumber} змінено на "${getOrderStatusMeta(updated.status).label}"`
      );
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const orders = Array.isArray(allOrdersQuery.data) ? allOrdersQuery.data : [];

  return (
    <Card className="p-6 space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
            <ClipboardList className="h-4 w-4 text-indigo-600" />
            Замовлення клієнтів ({orders.length})
          </h3>
          <p className="text-xs text-zinc-500">
            Обробка нових замовлень, підтвердження оплати та зміна етапів доставки.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <button
            type="button"
            onClick={() => setStatusFilter(undefined)}
            className={cn(
              'rounded-lg px-2.5 py-1 text-xs font-medium transition-colors cursor-pointer',
              statusFilter === undefined
                ? 'bg-zinc-900 text-white'
                : 'bg-zinc-100 text-zinc-600 hover:bg-zinc-200'
            )}
          >
            Усі
          </button>
          {[
            OrderStatus.Pending,
            OrderStatus.Paid,
            OrderStatus.Shipped,
            OrderStatus.Delivered,
            OrderStatus.Cancelled,
          ].map((st) => (
            <button
              key={st}
              type="button"
              onClick={() => setStatusFilter(st)}
              className={cn(
                'rounded-lg px-2.5 py-1 text-xs font-medium transition-colors cursor-pointer',
                statusFilter === st
                  ? 'bg-indigo-600 text-white'
                  : 'bg-zinc-100 text-zinc-600 hover:bg-zinc-200'
              )}
            >
              {getOrderStatusMeta(st).label}
            </button>
          ))}
        </div>
      </div>

      <div className="overflow-x-auto rounded-xl border border-zinc-200/80">
        <table className="w-full text-left text-xs">
          <thead className="border-b border-zinc-200 bg-zinc-50 text-zinc-500 uppercase">
            <tr>
              <th className="px-4 py-3 font-semibold">Замовлення</th>
              <th className="px-4 py-3 font-semibold">Клієнт</th>
              <th className="px-4 py-3 font-semibold">Товари</th>
              <th className="px-4 py-3 font-semibold">Сума</th>
              <th className="px-4 py-3 font-semibold">Статус</th>
              <th className="px-4 py-3 text-right font-semibold">
                Зміна етапу
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-100 bg-white">
            {orders.map((order) => {
              const meta = getOrderStatusMeta(order.status);
              const nextStatuses = getValidNextStatuses(order.status);

              return (
                <tr key={order.id} className="hover:bg-zinc-50/60">
                  <td className="px-4 py-3">
                    <div className="font-bold text-zinc-900">
                      № {order.orderNumber}
                    </div>
                    <div className="text-zinc-400">
                      {formatDateTime(order.createdAtUtc)}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-zinc-600">
                    Клієнт #{order.userId.slice(0, 8).toUpperCase()}
                  </td>
                  <td className="px-4 py-3">
                    <div className="space-y-0.5">
                      {order.items.map((item) => (
                        <div key={item.productId} className="text-zinc-700">
                          {item.productName}{' '}
                          <span className="text-zinc-400">× {item.quantity}</span>
                        </div>
                      ))}
                    </div>
                  </td>
                  <td className="px-4 py-3 font-bold text-zinc-900">
                    {formatCurrency(order.totalAmount)}
                  </td>
                  <td className="px-4 py-3">
                    <Badge variant={meta.badgeVariant}>{meta.label}</Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Can
                      permission={Permissions.Orders.ManageStatus}
                      fallback={
                        <span className="text-zinc-400 italic">
                          Лише перегляд
                        </span>
                      }
                    >
                      {nextStatuses.length === 0 ? (
                        <span className="text-zinc-400">Завершено</span>
                      ) : (
                        <div className="inline-flex flex-wrap justify-end gap-1.5">
                          {nextStatuses.map((nextSt) => {
                            const nextMeta = getOrderStatusMeta(nextSt);
                            const isCancel = nextSt === OrderStatus.Cancelled;
                            return (
                              <Button
                                key={nextSt}
                                size="sm"
                                variant={isCancel ? 'danger' : 'outline'}
                                disabled={statusMutation.isPending}
                                onClick={() =>
                                  statusMutation.mutate({
                                    id: order.id,
                                    status: nextSt,
                                  })
                                }
                              >
                                <ArrowRight className="h-3 w-3" />
                                {nextMeta.label}
                              </Button>
                            );
                          })}
                        </div>
                      )}
                    </Can>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </Card>
  );
}

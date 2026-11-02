import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Package,
  Clock,
  CreditCard,
  Truck,
  CheckCircle2,
  XCircle,
  Ban,
  ArrowUpRight,
  ShoppingBag,
} from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { ordersApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { OrderStatus } from '@/types/api';
import {
  canUserCancelOrder,
  cn,
  formatCurrency,
  formatDateTime,
  getOrderStatusMeta,
} from '@/lib/utils';

const lifecycleSteps = [
  { status: OrderStatus.Pending, label: 'Очікує оплати', icon: Clock },
  { status: OrderStatus.Paid, label: 'Оплачено', icon: CreditCard },
  { status: OrderStatus.Shipped, label: 'Відправлено', icon: Truck },
  { status: OrderStatus.Delivered, label: 'Доставлено', icon: CheckCircle2 },
];

export function OrdersPage() {
  const [statusFilter, setStatusFilter] = useState<OrderStatus | undefined>(undefined);
  const queryClient = useQueryClient();

  const ordersQuery = useQuery({
    queryKey: ['my-orders', statusFilter],
    queryFn: () =>
      ordersApi.getMyOrders({
        status: statusFilter,
        page: 1,
        pageSize: 50,
      }),
  });

  const cancelMutation = useMutation({
    mutationFn: (orderId: string) => ordersApi.cancelOrder(orderId),
    onSuccess: (updated) => {
      queryClient.invalidateQueries({ queryKey: ['my-orders'] });
      queryClient.invalidateQueries({ queryKey: ['all-orders'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['item-screen'] });
      toast.success(`Замовлення ${updated.orderNumber} успішно скасовано.`);
    },
    onError: (err) => {
      toast.error(extractApiErrorMessage(err));
    },
  });

  const orders = ordersQuery.data ?? [];

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 rounded-2xl border border-zinc-200/80 bg-white p-6 sm:flex-row sm:items-center sm:justify-between shadow-2xs">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900">
            Мої замовлення
          </h1>
          <p className="text-xs sm:text-sm text-zinc-500">
            Відстежуйте поточний статус доставки та переглядайте історію ваших покупок.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-1.5">
          <button
            type="button"
            onClick={() => setStatusFilter(undefined)}
            className={cn(
              'rounded-lg px-3 py-1.5 text-xs font-medium transition-colors cursor-pointer',
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
                'rounded-lg px-3 py-1.5 text-xs font-medium transition-colors cursor-pointer',
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

      {ordersQuery.isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, idx) => (
            <Card key={idx} className="p-6 space-y-4">
              <Skeleton className="h-6 w-1/3" />
              <Skeleton className="h-12 w-full" />
              <Skeleton className="h-20 w-full" />
            </Card>
          ))}
        </div>
      ) : orders.length === 0 ? (
        <Card className="flex flex-col items-center justify-center p-12 text-center">
          <div className="mb-3 flex h-14 w-14 items-center justify-center rounded-2xl bg-zinc-100 text-zinc-400">
            <Package className="h-7 w-7" />
          </div>
          <h3 className="text-base font-semibold text-zinc-900">
            Список замовлень порожній
          </h3>
          <p className="mt-1 max-w-sm text-xs text-zinc-500">
            Оберіть бажані товари в каталозі та оформіть замовлення.
          </p>
          <Button variant="default" size="sm" className="mt-4" asChild>
            <Link to="/">
              <ShoppingBag className="h-4 w-4" />
              Перейти до каталогу
            </Link>
          </Button>
        </Card>
      ) : (
        <div className="space-y-5">
          {orders.map((order) => {
            const meta = getOrderStatusMeta(order.status);
            const isCancelled = Number(order.status) === OrderStatus.Cancelled;
            const canCancel = canUserCancelOrder(order.status);

            return (
              <Card
                key={order.id}
                className="overflow-hidden border-zinc-200/90 transition-shadow hover:shadow-sm"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-zinc-100 bg-zinc-50/70 px-6 py-4">
                  <div className="flex flex-wrap items-center gap-3">
                    <span className="text-sm font-bold text-zinc-900">
                      Замовлення № {order.orderNumber}
                    </span>
                    <Badge variant={meta.badgeVariant}>{meta.label}</Badge>
                    <span className="text-xs text-zinc-500">
                      від {formatDateTime(order.createdAtUtc)}
                    </span>
                  </div>

                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <span className="text-xs text-zinc-500 mr-1.5">Разом:</span>
                      <span className="text-base font-extrabold text-zinc-900">
                        {formatCurrency(order.totalAmount)}
                      </span>
                    </div>

                    {canCancel && (
                      <Button
                        variant="outline"
                        size="sm"
                        disabled={cancelMutation.isPending}
                        onClick={() => cancelMutation.mutate(order.id)}
                        className="text-rose-600 border-rose-200 hover:bg-rose-50 hover:text-rose-700"
                      >
                        <Ban className="h-3.5 w-3.5" />
                        Скасувати замовлення
                      </Button>
                    )}
                  </div>
                </div>

                <div className="px-6 py-5 space-y-5">
                  {isCancelled ? (
                    <div className="flex items-center gap-2.5 rounded-xl border border-rose-200 bg-rose-50/60 px-4 py-3 text-xs text-rose-800">
                      <XCircle className="h-4 w-4 text-rose-600 shrink-0" />
                      <span>Це замовлення було скасовано.</span>
                    </div>
                  ) : (
                    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                      {lifecycleSteps.map((step, index) => {
                        const Icon = step.icon;
                        const isCompleted = index <= meta.stepIndex;
                        const isCurrent = index === meta.stepIndex;

                        return (
                          <div
                            key={step.status}
                            className={cn(
                              'flex items-center gap-2.5 rounded-xl border p-3 text-xs transition-colors',
                              isCurrent
                                ? 'border-indigo-300 bg-indigo-50/80 text-indigo-950 font-semibold'
                                : isCompleted
                                ? 'border-emerald-200 bg-emerald-50/40 text-emerald-900'
                                : 'border-zinc-100 bg-zinc-50/50 text-zinc-400'
                            )}
                          >
                            <Icon
                              className={cn(
                                'h-4 w-4 shrink-0',
                                isCurrent
                                  ? 'text-indigo-600'
                                  : isCompleted
                                  ? 'text-emerald-600'
                                  : 'text-zinc-400'
                              )}
                            />
                            <span>{step.label}</span>
                          </div>
                        );
                      })}
                    </div>
                  )}

                  <div className="divide-y divide-zinc-100 rounded-xl border border-zinc-200/70 bg-white">
                    {order.items.map((item) => (
                      <div
                        key={item.productId}
                        className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 px-4 py-3 text-xs"
                      >
                        <div className="flex items-center gap-2">
                          <Link
                            to={`/items/${item.productId}`}
                            className="font-semibold text-zinc-900 hover:text-indigo-600 inline-flex items-center gap-1"
                          >
                            {item.productName}
                            <ArrowUpRight className="h-3.5 w-3.5 text-indigo-500" />
                          </Link>
                          <span className="text-zinc-400 text-[11px]">
                            × {item.quantity} шт.
                          </span>
                        </div>

                        <div className="flex items-center gap-4 text-right">
                          <span className="text-zinc-500">
                            {formatCurrency(item.unitPrice)} / шт.
                          </span>
                          <span className="font-bold text-zinc-900">
                            {formatCurrency(item.totalPrice)}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}

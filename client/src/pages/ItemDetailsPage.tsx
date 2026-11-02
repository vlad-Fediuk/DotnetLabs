import { useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  ShoppingBag,
  CheckCircle2,
  Package,
  Plus,
  Minus,
  ShieldCheck,
  Truck,
  RotateCcw,
} from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { catalogApi, screensApi } from '@/api/services';
import { useAuthStore } from '@/store/useAuthStore';
import { useCartStore } from '@/store/useCartStore';
import { formatCurrency, formatDateTime, getOrderStatusMeta } from '@/lib/utils';

export function ItemDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const accessToken = useAuthStore((s) => s.accessToken);
  const addItem = useCartStore((s) => s.addItem);
  const setCartOpen = useCartStore((s) => s.setOpen);

  const [quantity, setQuantity] = useState(1);

  const isAuthenticated = Boolean(user && accessToken);

  const screenQuery = useQuery({
    queryKey: ['item-screen', id, isAuthenticated],
    enabled: Boolean(id),
    queryFn: async () => {
      if (!id) throw new Error('Invalid ID');
      if (isAuthenticated) {
        return screensApi.getItemDetailsScreen(id);
      }
      const catalogItem = await catalogApi.getItemById(id);
      return {
        item: {
          id: catalogItem.id,
          title: catalogItem.title,
          sku: catalogItem.sku,
          description: catalogItem.description,
          price: catalogItem.price,
          stockQuantity: catalogItem.stockQuantity,
          categoryName: catalogItem.categoryName,
          isActive: catalogItem.isActive,
        },
        myActiveOperation: null,
        activeOperationStatus: 'GuestMode',
        activeOperationMessage: null,
      };
    },
  });

  if (screenQuery.isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
          <Skeleton className="h-80 lg:col-span-5 rounded-2xl" />
          <div className="lg:col-span-7 space-y-4">
            <Skeleton className="h-6 w-32" />
            <Skeleton className="h-10 w-2/3" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-14 w-1/2" />
          </div>
        </div>
      </div>
    );
  }

  if (screenQuery.isError || !screenQuery.data) {
    return (
      <Card className="p-12 text-center space-y-4">
        <h2 className="text-lg font-bold text-zinc-900">
          Товар не знайдено
        </h2>
        <p className="text-xs text-zinc-500 max-w-md mx-auto">
          Можливо, позиція вже не актуальна або посилання застаріло.
        </p>
        <Button variant="outline" onClick={() => navigate('/')}>
          <ArrowLeft className="h-4 w-4" />
          Повернутися до каталогу
        </Button>
      </Card>
    );
  }

  const { item, myActiveOperation } = screenQuery.data;

  const handleAddToCart = () => {
    if (item.stockQuantity <= 0) return;
    addItem(
      {
        productId: item.id,
        title: item.title,
        sku: item.sku,
        price: item.price,
        stockQuantity: item.stockQuantity,
        categoryName: item.categoryName,
      },
      quantity
    );
    toast.success(`Додано ${quantity} шт. "${item.title}" до кошика`, {
      action: {
        label: 'Оформити',
        onClick: () => setCartOpen(true),
      },
    });
  };

  return (
    <div className="space-y-6">
      <div>
        <Button variant="ghost" size="sm" asChild>
          <Link to="/">
            <ArrowLeft className="h-4 w-4" />
            Назад до каталогу
          </Link>
        </Button>
      </div>

      {myActiveOperation && (
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-xl border border-indigo-200 bg-indigo-50/70 p-4">
          <div className="flex items-start gap-3">
            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-indigo-600 text-white">
              <Package className="h-5 w-5" />
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-xs font-bold text-indigo-950">
                  Ви вже замовляли цей товар (№ {myActiveOperation.orderNumber})
                </span>
                <Badge variant={getOrderStatusMeta(myActiveOperation.status).badgeVariant}>
                  {getOrderStatusMeta(myActiveOperation.status).label}
                </Badge>
              </div>
              <p className="text-xs text-indigo-800 mt-0.5">
                Кількість: <strong>{myActiveOperation.quantity} шт.</strong> · Сума:{' '}
                <strong>{formatCurrency(myActiveOperation.totalPrice)}</strong> · Дата:{' '}
                {formatDateTime(myActiveOperation.createdAtUtc)}
              </p>
            </div>
          </div>

          <Button variant="outline" size="sm" asChild>
            <Link to="/orders">Мої замовлення</Link>
          </Button>
        </div>
      )}

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        <Card className="lg:col-span-5 flex flex-col justify-between bg-gradient-to-br from-zinc-100 via-white to-indigo-50/60 p-8">
          <div className="flex items-center justify-between">
            <Badge variant="primary">{item.categoryName}</Badge>
            <span className="text-xs text-zinc-500">Артикул: {item.sku}</span>
          </div>

          <div className="my-16 flex flex-col items-center justify-center text-center">
            <div className="flex h-28 w-28 items-center justify-center rounded-3xl bg-white shadow-md border border-zinc-200/80 text-3xl font-bold text-indigo-600">
              {item.title.slice(0, 2).toUpperCase()}
            </div>
          </div>

          <div className="text-center text-xs text-zinc-400">
            Офіційна продукція з гарантією виробника
          </div>
        </Card>

        <Card className="lg:col-span-7 flex flex-col justify-between p-6 sm:p-8">
          <div className="space-y-6">
            <div>
              <h1 className="text-2xl sm:text-3xl font-bold text-zinc-900 tracking-tight">
                {item.title}
              </h1>
              <div className="mt-3 flex flex-wrap items-center gap-3">
                <span className="text-3xl font-extrabold text-indigo-600">
                  {formatCurrency(item.price)}
                </span>

                {item.stockQuantity > 0 ? (
                  <Badge variant="success" className="px-3 py-1 text-xs">
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    В наявності ({item.stockQuantity} шт.)
                  </Badge>
                ) : (
                  <Badge variant="danger" className="px-3 py-1 text-xs">
                    Немає в наявності
                  </Badge>
                )}
              </div>
            </div>

            <div className="border-t border-zinc-100 pt-4">
              <h3 className="text-xs font-semibold uppercase tracking-wider text-zinc-400">
                Про товар
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-zinc-700">
                {item.description || 'Детальний опис для цієї моделі наразі не додано.'}
              </p>
            </div>

            <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 border-t border-zinc-100 pt-4">
              <div className="flex items-center gap-2.5 rounded-xl bg-zinc-50 p-3 text-xs text-zinc-700">
                <Truck className="h-4 w-4 text-indigo-600 shrink-0" />
                <span>Швидка доставка по Україні</span>
              </div>
              <div className="flex items-center gap-2.5 rounded-xl bg-zinc-50 p-3 text-xs text-zinc-700">
                <ShieldCheck className="h-4 w-4 text-emerald-600 shrink-0" />
                <span>Гарантія 12 місяців</span>
              </div>
              <div className="flex items-center gap-2.5 rounded-xl bg-zinc-50 p-3 text-xs text-zinc-700">
                <RotateCcw className="h-4 w-4 text-indigo-600 shrink-0" />
                <span>Обмін і повернення 14 днів</span>
              </div>
            </div>
          </div>

          <div className="mt-8 border-t border-zinc-100 pt-6 flex flex-col sm:flex-row items-stretch sm:items-center gap-4">
            <div className="flex items-center justify-between rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 sm:w-36">
              <button
                type="button"
                onClick={() => setQuantity((q) => Math.max(1, q - 1))}
                className="flex h-7 w-7 items-center justify-center rounded-lg bg-white border border-zinc-200 text-zinc-700 hover:bg-zinc-100 cursor-pointer"
              >
                <Minus className="h-3.5 w-3.5" />
              </button>
              <span className="text-sm font-bold text-zinc-900">{quantity}</span>
              <button
                type="button"
                disabled={quantity >= item.stockQuantity}
                onClick={() =>
                  setQuantity((q) => Math.min(item.stockQuantity, q + 1))
                }
                className="flex h-7 w-7 items-center justify-center rounded-lg bg-white border border-zinc-200 text-zinc-700 hover:bg-zinc-100 disabled:opacity-40 cursor-pointer"
              >
                <Plus className="h-3.5 w-3.5" />
              </button>
            </div>

            <Button
              size="lg"
              className="flex-1"
              disabled={item.stockQuantity <= 0}
              onClick={handleAddToCart}
            >
              <ShoppingBag className="h-5 w-5" />
              {item.stockQuantity > 0
                ? `Додати до кошика · ${formatCurrency(item.price * quantity)}`
                : 'Немає в наявності'}
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}

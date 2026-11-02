import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ShoppingBag,
  Trash2,
  Plus,
  Minus,
  ArrowRight,
  Truck,
} from 'lucide-react';
import { toast } from 'sonner';
import { Sheet, SheetContent } from '@/components/ui/Sheet';
import { Button } from '@/components/ui/Button';
import { useCartStore } from '@/store/useCartStore';
import { useAuthStore } from '@/store/useAuthStore';
import { ordersApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { formatCurrency, generateUuidV4 } from '@/lib/utils';
import * as DialogPrimitive from '@radix-ui/react-dialog';

export function CartDrawer() {
  const isOpen = useCartStore((s) => s.isOpen);
  const setOpen = useCartStore((s) => s.setOpen);
  const items = useCartStore((s) => s.items);
  const updateQuantity = useCartStore((s) => s.updateQuantity);
  const removeItem = useCartStore((s) => s.removeItem);
  const clearCart = useCartStore((s) => s.clearCart);
  const getTotalPrice = useCartStore((s) => s.getTotalPrice);

  const user = useAuthStore((s) => s.user);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [idempotencyKey, setIdempotencyKey] = useState<string>(() => generateUuidV4());

  useEffect(() => {
    if (isOpen && items.length > 0) {
      setIdempotencyKey(generateUuidV4());
    }
  }, [isOpen, items.length]);

  const checkoutMutation = useMutation({
    mutationFn: async () => {
      return ordersApi.createOrder(
        {
          items: items.map((i) => ({
            productId: i.productId,
            quantity: i.quantity,
          })),
        },
        idempotencyKey
      );
    },
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['my-orders'] });
      queryClient.invalidateQueries({ queryKey: ['all-orders'] });
      queryClient.invalidateQueries({ queryKey: ['item-screen'] });

      toast.success(
        `Замовлення ${result.order.orderNumber} успішно оформлено!`
      );

      clearCart();
      setOpen(false);
      setIdempotencyKey(generateUuidV4());
      navigate('/orders');
    },
    onError: (err) => {
      toast.error(extractApiErrorMessage(err));
    },
  });

  const handleCheckout = () => {
    if (!user) {
      setOpen(false);
      toast.info('Увійдіть у свій акаунт, щоб оформити замовлення');
      navigate('/login');
      return;
    }
    checkoutMutation.mutate();
  };

  const totalPrice = getTotalPrice();

  return (
    <Sheet open={isOpen} onOpenChange={setOpen}>
      <SheetContent className="p-0">
        <div className="flex items-center justify-between border-b border-zinc-200 px-6 py-5">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-indigo-50 text-indigo-600">
              <ShoppingBag className="h-5 w-5" />
            </div>
            <div>
              <DialogPrimitive.Title className="text-base font-semibold text-zinc-900">
                Кошик
              </DialogPrimitive.Title>
              <DialogPrimitive.Description className="text-xs text-zinc-500">
                {items.length === 0
                  ? 'Немає вибраних товарів'
                  : `Обрано товарів: ${items.length}`}
              </DialogPrimitive.Description>
            </div>
          </div>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4">
          {items.length === 0 ? (
            <div className="flex h-full flex-col items-center justify-center text-center py-12">
              <div className="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl bg-zinc-100 text-zinc-400">
                <ShoppingBag className="h-8 w-8" />
              </div>
              <p className="text-sm font-medium text-zinc-900">Ваш кошик порожній</p>
              <p className="mt-1 max-w-xs text-xs text-zinc-500">
                Перегляньте каталог та додайте бажані товари до кошика.
              </p>
              <Button
                variant="outline"
                size="sm"
                className="mt-5"
                onClick={() => setOpen(false)}
              >
                Перейти до покупок
              </Button>
            </div>
          ) : (
            <div className="space-y-4">
              {items.map((item) => (
                <div
                  key={item.productId}
                  className="flex flex-col gap-3 rounded-xl border border-zinc-200/80 bg-zinc-50/60 p-4 transition-colors hover:bg-zinc-50"
                >
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <span className="text-[11px] font-medium uppercase tracking-wider text-indigo-600">
                        {item.categoryName || 'Каталог'}
                      </span>
                      <h4 className="text-sm font-semibold text-zinc-900 leading-snug">
                        {item.title}
                      </h4>
                      <p className="text-xs text-zinc-400 mt-0.5">
                        Артикул: {item.sku}
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => removeItem(item.productId)}
                      className="rounded-lg p-1.5 text-zinc-400 hover:bg-rose-50 hover:text-rose-600 transition-colors cursor-pointer"
                      title="Видалити"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>

                  <div className="flex items-center justify-between pt-1">
                    <div className="flex items-center gap-1.5 rounded-lg border border-zinc-200 bg-white p-1">
                      <button
                        type="button"
                        onClick={() => updateQuantity(item.productId, item.quantity - 1)}
                        className="flex h-6 w-6 items-center justify-center rounded-md text-zinc-600 hover:bg-zinc-100 cursor-pointer"
                      >
                        <Minus className="h-3.5 w-3.5" />
                      </button>
                      <span className="w-7 text-center text-xs font-semibold text-zinc-900">
                        {item.quantity}
                      </span>
                      <button
                        type="button"
                        disabled={item.quantity >= item.stockQuantity}
                        onClick={() => updateQuantity(item.productId, item.quantity + 1)}
                        className="flex h-6 w-6 items-center justify-center rounded-md text-zinc-600 hover:bg-zinc-100 disabled:opacity-40 cursor-pointer"
                      >
                        <Plus className="h-3.5 w-3.5" />
                      </button>
                    </div>

                    <div className="text-right">
                      <div className="text-sm font-bold text-zinc-900">
                        {formatCurrency(item.price * item.quantity)}
                      </div>
                      <div className="text-[11px] text-zinc-500">
                        {formatCurrency(item.price)} / шт.
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {items.length > 0 && (
          <div className="border-t border-zinc-200 bg-zinc-50/80 px-6 py-5 space-y-4">
            <div className="space-y-1.5">
              <div className="flex items-center justify-between text-xs text-zinc-500">
                <span>Доставка</span>
                <span className="flex items-center gap-1 text-emerald-700 font-medium">
                  <Truck className="h-3.5 w-3.5" />
                  Безкоштовно
                </span>
              </div>
              <div className="flex items-center justify-between text-base font-bold text-zinc-900 pt-1">
                <span>До сплати</span>
                <span className="text-lg text-indigo-600">{formatCurrency(totalPrice)}</span>
              </div>
            </div>

            <div className="flex gap-2">
              <Button
                variant="outline"
                size="md"
                onClick={clearCart}
                disabled={checkoutMutation.isPending}
                className="px-3"
              >
                Очистити
              </Button>
              <Button
                variant="default"
                size="md"
                className="flex-1"
                disabled={checkoutMutation.isPending}
                onClick={handleCheckout}
              >
                {checkoutMutation.isPending ? (
                  'Оформлення...'
                ) : (
                  <>
                    Оформити замовлення
                    <ArrowRight className="h-4 w-4" />
                  </>
                )}
              </Button>
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}

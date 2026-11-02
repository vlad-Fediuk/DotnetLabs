import { create } from 'zustand';
import { persist } from 'zustand/middleware';

export interface CartItem {
  productId: string;
  title: string;
  sku: string;
  price: number;
  stockQuantity: number;
  categoryName: string;
  quantity: number;
}

interface CartState {
  items: CartItem[];
  isOpen: boolean;
  setOpen: (open: boolean) => void;
  addItem: (item: Omit<CartItem, 'quantity'>, quantity?: number) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  removeItem: (productId: string) => void;
  clearCart: () => void;
  getTotalItems: () => number;
  getTotalPrice: () => number;
}

export const useCartStore = create<CartState>()(
  persist(
    (set, get) => ({
      items: [],
      isOpen: false,

      setOpen: (open) => set({ isOpen: open }),

      addItem: (newItem, quantity = 1) =>
        set((state) => {
          const existingIndex = state.items.findIndex(
            (i) => i.productId === newItem.productId
          );

          if (existingIndex > -1) {
            const updated = [...state.items];
            const current = updated[existingIndex];
            const nextQty = Math.min(
              current.quantity + quantity,
              newItem.stockQuantity
            );
            updated[existingIndex] = {
              ...current,
              ...newItem,
              quantity: Math.max(1, nextQty),
            };
            return { items: updated };
          }

          return {
            items: [
              ...state.items,
              {
                ...newItem,
                quantity: Math.min(Math.max(1, quantity), newItem.stockQuantity),
              },
            ],
          };
        }),

      updateQuantity: (productId, quantity) =>
        set((state) => {
          if (quantity <= 0) {
            return {
              items: state.items.filter((i) => i.productId !== productId),
            };
          }
          return {
            items: state.items.map((i) =>
              i.productId === productId
                ? { ...i, quantity: Math.min(quantity, i.stockQuantity) }
                : i
            ),
          };
        }),

      removeItem: (productId) =>
        set((state) => ({
          items: state.items.filter((i) => i.productId !== productId),
        })),

      clearCart: () => set({ items: [] }),

      getTotalItems: () =>
        get().items.reduce((sum, item) => sum + item.quantity, 0),

      getTotalPrice: () =>
        get().items.reduce((sum, item) => sum + item.price * item.quantity, 0),
    }),
    {
      name: 'nexus-ecommerce-cart',
      partialize: (state) => ({ items: state.items }),
    }
  )
);

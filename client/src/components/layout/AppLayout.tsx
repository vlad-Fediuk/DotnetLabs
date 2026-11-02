import { Outlet } from 'react-router-dom';
import { Navbar } from '@/components/layout/Navbar';
import { CartDrawer } from '@/components/layout/CartDrawer';

export function AppLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-zinc-50 text-zinc-900">
      <Navbar />
      <main className="flex-1 mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <Outlet />
      </main>
      <footer className="border-t border-zinc-200/80 bg-white py-6 text-xs text-zinc-500">
        <div className="mx-auto flex max-w-7xl flex-col sm:flex-row items-center justify-between gap-4 px-4 sm:px-6 lg:px-8">
          <div>
            <span className="font-semibold text-zinc-800">NEXUS Store</span> · Офіційний інтернет-магазин електроніки та гаджетів
          </div>
          <div className="flex items-center gap-6">
            <span>Доставка та оплата</span>
            <span>Офіційна гарантія</span>
            <span>Підтримка клієнтів</span>
          </div>
        </div>
      </footer>
      <CartDrawer />
    </div>
  );
}

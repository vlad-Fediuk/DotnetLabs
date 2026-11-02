import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  Search,
  SlidersHorizontal,
  ShoppingBag,
  LayoutGrid,
  List,
  RotateCcw,
  ChevronLeft,
  ChevronRight,
  ArrowUpRight,
  PackageCheck,
  AlertTriangle,
  Plus,
  Layers,
} from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Skeleton } from '@/components/ui/Skeleton';
import { Can } from '@/components/auth/Can';
import { catalogApi } from '@/api/services';
import { useCartStore } from '@/store/useCartStore';
import { Permissions, type ItemDto } from '@/types/api';
import { cn, formatCurrency } from '@/lib/utils';

export function CatalogPage() {
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [minPriceInput, setMinPriceInput] = useState<string>('');
  const [maxPriceInput, setMaxPriceInput] = useState<string>('');
  const [sortBy, setSortBy] = useState<'title' | 'price' | 'created'>('title');
  const [desc, setDesc] = useState<boolean>(false);
  const [page, setPage] = useState<number>(1);
  const [viewMode, setViewMode] = useState<'grid' | 'list'>('grid');

  const addItem = useCartStore((s) => s.addItem);
  const setCartOpen = useCartStore((s) => s.setOpen);

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearch(searchInput);
      setPage(1);
    }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const categoriesQuery = useQuery({
    queryKey: ['catalog-categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const minPriceVal = minPriceInput.trim() !== '' ? Number(minPriceInput) : undefined;
  const maxPriceVal = maxPriceInput.trim() !== '' ? Number(maxPriceInput) : undefined;

  const itemsQuery = useQuery({
    queryKey: [
      'catalog-items',
      {
        page,
        search: debouncedSearch,
        categoryId: selectedCategory,
        minPrice: minPriceVal,
        maxPrice: maxPriceVal,
        sortBy,
        desc,
      },
    ],
    queryFn: () =>
      catalogApi.getItems({
        page,
        pageSize: 9,
        search: debouncedSearch || undefined,
        categoryId: selectedCategory || undefined,
        minPrice: minPriceVal,
        maxPrice: maxPriceVal,
        sortBy,
        desc,
      }),
  });

  const handleResetFilters = () => {
    setSearchInput('');
    setDebouncedSearch('');
    setSelectedCategory('');
    setMinPriceInput('');
    setMaxPriceInput('');
    setSortBy('title');
    setDesc(false);
    setPage(1);
  };

  const handleAddToCart = (item: ItemDto) => {
    if (item.stockQuantity <= 0) {
      toast.error('Цього товару немає в наявності');
      return;
    }
    addItem(
      {
        productId: item.id,
        title: item.title,
        sku: item.sku,
        price: item.price,
        stockQuantity: item.stockQuantity,
        categoryName: item.categoryName,
      },
      1
    );
    toast.success(`"${item.title}" додано до кошика`, {
      action: {
        label: 'Перейти до кошика',
        onClick: () => setCartOpen(true),
      },
    });
  };

  const categories = categoriesQuery.data ?? [];
  const pagedData = itemsQuery.data;
  const items = pagedData?.items ?? [];
  const hasActiveFilters =
    Boolean(debouncedSearch) ||
    Boolean(selectedCategory) ||
    minPriceInput.trim() !== '' ||
    maxPriceInput.trim() !== '';

  return (
    <div className="space-y-8">
      <div className="flex flex-col gap-4 rounded-2xl border border-zinc-200/80 bg-white p-6 sm:flex-row sm:items-center sm:justify-between shadow-2xs">
        <div className="space-y-1">
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900 sm:text-3xl">
            Каталог електроніки та гаджетів
          </h1>
          <p className="text-sm text-zinc-500 max-w-2xl">
            Обирайте сучасну техніку та літературу з офіційною гарантією та швидкою доставкою.
          </p>
        </div>

        <Can permission={Permissions.Catalog.Manage}>
          <Button variant="default" size="md" asChild>
            <Link to="/workspace">
              <Plus className="h-4 w-4" />
              Керувати товарами
            </Link>
          </Button>
        </Can>
      </div>

      <div className="grid grid-cols-1 gap-8 lg:grid-cols-12">
        <aside className="lg:col-span-3 space-y-6">
          <Card className="p-5 space-y-6 sticky top-24">
            <div className="flex items-center justify-between border-b border-zinc-100 pb-3">
              <div className="flex items-center gap-2 text-sm font-semibold text-zinc-900">
                <SlidersHorizontal className="h-4 w-4 text-indigo-600" />
                Фільтри
              </div>
              {hasActiveFilters && (
                <button
                  type="button"
                  onClick={handleResetFilters}
                  className="inline-flex items-center gap-1 text-xs font-medium text-indigo-600 hover:text-indigo-700 cursor-pointer"
                >
                  <RotateCcw className="h-3 w-3" />
                  Скинути
                </button>
              )}
            </div>

            <div className="space-y-2">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-500">
                Категорії
              </label>
              <div className="space-y-1">
                <button
                  type="button"
                  onClick={() => {
                    setSelectedCategory('');
                    setPage(1);
                  }}
                  className={cn(
                    'flex w-full items-center justify-between rounded-lg px-3 py-2 text-xs font-medium transition-colors cursor-pointer',
                    selectedCategory === ''
                      ? 'bg-indigo-50 text-indigo-700 font-semibold'
                      : 'text-zinc-600 hover:bg-zinc-100 hover:text-zinc-900'
                  )}
                >
                  <span>Усі категорії</span>
                  <Layers className="h-3.5 w-3.5 opacity-60" />
                </button>

                {categories.map((cat) => (
                  <button
                    key={cat.id}
                    type="button"
                    onClick={() => {
                      setSelectedCategory(cat.id);
                      setPage(1);
                    }}
                    className={cn(
                      'flex w-full items-center justify-between rounded-lg px-3 py-2 text-xs font-medium transition-colors cursor-pointer text-left',
                      selectedCategory === cat.id
                        ? 'bg-indigo-50 text-indigo-700 font-semibold'
                        : 'text-zinc-600 hover:bg-zinc-100 hover:text-zinc-900'
                    )}
                  >
                    <span className="truncate">{cat.name}</span>
                  </button>
                ))}
              </div>
            </div>

            <div className="space-y-2 border-t border-zinc-100 pt-4">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-500">
                Ціна ($)
              </label>
              <div className="grid grid-cols-2 gap-2">
                <div>
                  <span className="text-[11px] text-zinc-400">Від</span>
                  <Input
                    type="number"
                    min="0"
                    placeholder="0"
                    value={minPriceInput}
                    onChange={(e) => {
                      setMinPriceInput(e.target.value);
                      setPage(1);
                    }}
                    className="h-9 text-xs mt-0.5"
                  />
                </div>
                <div>
                  <span className="text-[11px] text-zinc-400">До</span>
                  <Input
                    type="number"
                    min="0"
                    placeholder="5000"
                    value={maxPriceInput}
                    onChange={(e) => {
                      setMaxPriceInput(e.target.value);
                      setPage(1);
                    }}
                    className="h-9 text-xs mt-0.5"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-2 border-t border-zinc-100 pt-4">
              <label className="text-xs font-semibold uppercase tracking-wider text-zinc-500">
                Сортування
              </label>
              <select
                value={`${sortBy}:${desc ? 'desc' : 'asc'}`}
                onChange={(e) => {
                  const [field, direction] = e.target.value.split(':');
                  setSortBy(field as 'title' | 'price' | 'created');
                  setDesc(direction === 'desc');
                  setPage(1);
                }}
                className="flex h-9 w-full rounded-lg border border-zinc-200 bg-white px-3 text-xs font-medium text-zinc-800 focus:border-indigo-500 focus:outline-none"
              >
                <option value="title:asc">За назвою (А → Я)</option>
                <option value="title:desc">За назвою (Я → А)</option>
                <option value="price:asc">Від дешевших до дорожчих</option>
                <option value="price:desc">Від дорожчих до дешевших</option>
                <option value="created:desc">Новинки</option>
              </select>
            </div>
          </Card>
        </aside>

        <section className="lg:col-span-9 space-y-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="relative flex-1">
              <Search className="pointer-events-none absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-400" />
              <Input
                type="search"
                placeholder="Пошук товарів за назвою або артикулом..."
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                className="pl-10 h-10 bg-white"
              />
            </div>

            <div className="flex items-center justify-between sm:justify-end gap-3">
              <span className="text-xs text-zinc-500">
                Знайдено:{' '}
                <strong className="font-semibold text-zinc-900">
                  {pagedData?.totalCount ?? 0}
                </strong>
              </span>

              <div className="inline-flex rounded-lg border border-zinc-200 bg-white p-1">
                <button
                  type="button"
                  onClick={() => setViewMode('grid')}
                  className={cn(
                    'rounded-md p-1.5 transition-colors cursor-pointer',
                    viewMode === 'grid'
                      ? 'bg-zinc-900 text-white'
                      : 'text-zinc-500 hover:text-zinc-900'
                  )}
                  title="Плитка"
                >
                  <LayoutGrid className="h-4 w-4" />
                </button>
                <button
                  type="button"
                  onClick={() => setViewMode('list')}
                  className={cn(
                    'rounded-md p-1.5 transition-colors cursor-pointer',
                    viewMode === 'list'
                      ? 'bg-zinc-900 text-white'
                      : 'text-zinc-500 hover:text-zinc-900'
                  )}
                  title="Список"
                >
                  <List className="h-4 w-4" />
                </button>
              </div>
            </div>
          </div>

          {itemsQuery.isLoading ? (
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
              {Array.from({ length: 6 }).map((_, idx) => (
                <Card key={idx} className="p-5 space-y-4">
                  <Skeleton className="h-32 w-full rounded-xl" />
                  <Skeleton className="h-4 w-1/3" />
                  <Skeleton className="h-5 w-3/4" />
                  <Skeleton className="h-4 w-full" />
                  <div className="flex justify-between pt-2">
                    <Skeleton className="h-6 w-20" />
                    <Skeleton className="h-9 w-28" />
                  </div>
                </Card>
              ))}
            </div>
          ) : itemsQuery.isError ? (
            <Card className="flex flex-col items-center justify-center p-12 text-center space-y-3">
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-amber-50 text-amber-600">
                <AlertTriangle className="h-6 w-6" />
              </div>
              <h3 className="text-base font-semibold text-zinc-900">
                Не вдалося завантажити каталог
              </h3>
              <p className="max-w-sm text-xs text-zinc-500">
                Сервіс тимчасово недоступний або очікує підключення. Спробуйте оновити дані.
              </p>
              <Button
                variant="outline"
                size="sm"
                onClick={() => itemsQuery.refetch()}
              >
                <RotateCcw className="h-3.5 w-3.5" />
                Спробувати знову
              </Button>
            </Card>
          ) : items.length === 0 ? (
            <Card className="flex flex-col items-center justify-center p-12 text-center">
              <div className="mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-zinc-100 text-zinc-400">
                <Search className="h-6 w-6" />
              </div>
              <h3 className="text-base font-semibold text-zinc-900">
                Нічого не знайдено
              </h3>
              <p className="mt-1 max-w-sm text-xs text-zinc-500">
                Спробуйте змінити пошуковий запит або скинути вибрані фільтри.
              </p>
              {hasActiveFilters && (
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-4"
                  onClick={handleResetFilters}
                >
                  Скинути фільтри
                </Button>
              )}
            </Card>
          ) : viewMode === 'grid' ? (
            <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
              {items.map((item) => (
                <Card
                  key={item.id}
                  className="group flex flex-col justify-between overflow-hidden border-zinc-200/80 transition-all duration-200 hover:-translate-y-0.5 hover:border-indigo-300 hover:shadow-md"
                >
                  <div>
                    <Link
                      to={`/items/${item.id}`}
                      className="block bg-gradient-to-br from-zinc-100 via-zinc-50 to-indigo-50/50 p-5 border-b border-zinc-100"
                    >
                      <div className="flex items-center justify-between">
                        <Badge variant="primary" className="text-[11px]">
                          {item.categoryName || 'Каталог'}
                        </Badge>
                        <span className="text-[11px] text-zinc-400">
                          {item.sku}
                        </span>
                      </div>

                      <div className="my-6 flex h-16 items-center justify-center">
                        <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-white shadow-xs border border-zinc-200/60 text-indigo-600 font-bold text-lg group-hover:scale-105 transition-transform">
                          {item.title.slice(0, 2).toUpperCase()}
                        </div>
                      </div>

                      <div className="flex items-center justify-between text-xs">
                        {item.stockQuantity > 5 ? (
                          <span className="inline-flex items-center gap-1 text-emerald-700 font-medium">
                            <PackageCheck className="h-3.5 w-3.5" />
                            В наявності
                          </span>
                        ) : item.stockQuantity > 0 ? (
                          <span className="inline-flex items-center gap-1 text-amber-600 font-medium">
                            <AlertTriangle className="h-3.5 w-3.5" />
                            Залишилось {item.stockQuantity} шт.
                          </span>
                        ) : (
                          <span className="text-rose-600 font-medium">
                            Немає в наявності
                          </span>
                        )}
                        <span className="inline-flex items-center gap-0.5 text-indigo-600 font-medium group-hover:underline">
                          Детальніше
                          <ArrowUpRight className="h-3.5 w-3.5" />
                        </span>
                      </div>
                    </Link>

                    <div className="p-5 pb-3">
                      <Link to={`/items/${item.id}`}>
                        <h3 className="text-base font-semibold text-zinc-900 group-hover:text-indigo-600 transition-colors line-clamp-1">
                          {item.title}
                        </h3>
                      </Link>
                      <p className="mt-1.5 text-xs text-zinc-500 line-clamp-2 min-h-[2rem]">
                        {item.description || 'Опис товару уточнюється.'}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center justify-between border-t border-zinc-100 px-5 py-4 bg-zinc-50/40">
                    <div>
                      <span className="text-[10px] uppercase tracking-wider text-zinc-400 block">
                        Ціна
                      </span>
                      <span className="text-lg font-bold text-zinc-900">
                        {formatCurrency(item.price)}
                      </span>
                    </div>

                    <Button
                      variant={item.stockQuantity > 0 ? 'default' : 'outline'}
                      size="sm"
                      disabled={item.stockQuantity <= 0}
                      onClick={() => handleAddToCart(item)}
                    >
                      <ShoppingBag className="h-3.5 w-3.5" />
                      {item.stockQuantity > 0 ? 'Купити' : 'Немає'}
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          ) : (
            <div className="space-y-3">
              {items.map((item) => (
                <Card
                  key={item.id}
                  className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-5 hover:border-indigo-300 transition-colors"
                >
                  <div className="space-y-1.5 flex-1">
                    <div className="flex items-center gap-2">
                      <Badge variant="primary">{item.categoryName}</Badge>
                      <span className="text-xs text-zinc-400">
                        Артикул: {item.sku}
                      </span>
                      <Badge
                        variant={
                          item.stockQuantity > 5
                            ? 'success'
                            : item.stockQuantity > 0
                            ? 'warning'
                            : 'danger'
                        }
                      >
                        {item.stockQuantity > 0
                          ? `В наявності (${item.stockQuantity} шт.)`
                          : 'Немає в наявності'}
                      </Badge>
                    </div>
                    <Link
                      to={`/items/${item.id}`}
                      className="text-base font-semibold text-zinc-900 hover:text-indigo-600 transition-colors inline-flex items-center gap-1"
                    >
                      {item.title}
                      <ArrowUpRight className="h-4 w-4 text-indigo-500" />
                    </Link>
                    <p className="text-xs text-zinc-500">{item.description}</p>
                  </div>

                  <div className="flex items-center justify-between sm:justify-end gap-4 border-t sm:border-t-0 pt-3 sm:pt-0 border-zinc-100">
                    <div className="text-right">
                      <div className="text-lg font-bold text-zinc-900">
                        {formatCurrency(item.price)}
                      </div>
                    </div>
                    <Button
                      variant="default"
                      size="sm"
                      disabled={item.stockQuantity <= 0}
                      onClick={() => handleAddToCart(item)}
                    >
                      <ShoppingBag className="h-4 w-4" />
                      Купити
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          )}

          {pagedData && pagedData.totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-zinc-200 pt-4">
              <span className="text-xs text-zinc-500">
                Сторінка <strong>{pagedData.page}</strong> з{' '}
                <strong>{pagedData.totalPages}</strong>
              </span>
              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!pagedData.hasPreviousPage}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Назад
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={!pagedData.hasNextPage}
                  onClick={() => setPage((p) => p + 1)}
                >
                  Далі
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

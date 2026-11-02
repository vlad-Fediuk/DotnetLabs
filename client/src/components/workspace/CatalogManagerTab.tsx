import { useState, type FormEvent } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Plus,
  Pencil,
  Trash2,
  FolderTree,
  Package,
} from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Badge } from '@/components/ui/Badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/Dialog';
import { catalogApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import type { CategoryDto, ItemDto } from '@/types/api';
import { formatCurrency } from '@/lib/utils';

export function CatalogManagerTab() {
  const queryClient = useQueryClient();

  const [itemDialogOpen, setItemDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<ItemDto | null>(null);
  const [title, setTitle] = useState('');
  const [sku, setSku] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState('');
  const [stockQuantity, setStockQuantity] = useState('');
  const [categoryId, setCategoryId] = useState('');

  const [catDialogOpen, setCatDialogOpen] = useState(false);
  const [editingCat, setEditingCat] = useState<CategoryDto | null>(null);
  const [catName, setCatName] = useState('');
  const [catSlug, setCatSlug] = useState('');
  const [catDesc, setCatDesc] = useState('');

  const categoriesQuery = useQuery({
    queryKey: ['catalog-categories'],
    queryFn: () => catalogApi.getCategories(),
  });

  const itemsQuery = useQuery({
    queryKey: ['catalog-items-admin'],
    queryFn: () => catalogApi.getItems({ page: 1, pageSize: 50, sortBy: 'created', desc: true }),
  });

  const categories = Array.isArray(categoriesQuery.data) ? categoriesQuery.data : [];
  const items = Array.isArray(itemsQuery.data?.items) ? itemsQuery.data.items : [];

  const openCreateItem = () => {
    setEditingItem(null);
    setTitle('');
    setSku(`SKU-${Math.random().toString(36).substring(2, 8).toUpperCase()}`);
    setDescription('');
    setPrice('199.99');
    setStockQuantity('20');
    setCategoryId(categories[0]?.id ?? '');
    setItemDialogOpen(true);
  };

  const openEditItem = (item: ItemDto) => {
    setEditingItem(item);
    setTitle(item.title);
    setSku(item.sku);
    setDescription(item.description ?? '');
    setPrice(String(item.price));
    setStockQuantity(String(item.stockQuantity));
    setCategoryId(item.categoryId);
    setItemDialogOpen(true);
  };

  const saveItemMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        title: title.trim(),
        sku: sku.trim(),
        description: description.trim() || null,
        price: Number(price),
        stockQuantity: Number(stockQuantity),
        categoryId,
      };
      if (editingItem) {
        await catalogApi.updateItem(editingItem.id, payload);
      } else {
        await catalogApi.createItem(payload);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items-admin'] });
      queryClient.invalidateQueries({ queryKey: ['item-screen'] });
      queryClient.invalidateQueries({ queryKey: ['cache-metrics'] });
      toast.success(
        editingItem
          ? 'Інформацію про товар успішно оновлено'
          : 'Новий товар успішно додано до каталогу'
      );
      setItemDialogOpen(false);
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const deleteItemMutation = useMutation({
    mutationFn: (id: string) => catalogApi.deleteItem(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items-admin'] });
      toast.success('Товар видалено з каталогу');
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const openCreateCat = () => {
    setEditingCat(null);
    setCatName('');
    setCatSlug('');
    setCatDesc('');
    setCatDialogOpen(true);
  };

  const openEditCat = (cat: CategoryDto) => {
    setEditingCat(cat);
    setCatName(cat.name);
    setCatSlug(cat.slug);
    setCatDesc(cat.description ?? '');
    setCatDialogOpen(true);
  };

  const saveCatMutation = useMutation({
    mutationFn: async () => {
      const payload = {
        name: catName.trim(),
        slug: catSlug.trim().toLowerCase(),
        description: catDesc.trim() || null,
      };
      if (editingCat) {
        await catalogApi.updateCategory(editingCat.id, payload);
      } else {
        await catalogApi.createCategory(payload);
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-categories'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items'] });
      queryClient.invalidateQueries({ queryKey: ['catalog-items-admin'] });
      toast.success(
        editingCat ? 'Категорію успішно оновлено' : 'Нову категорію створено'
      );
      setCatDialogOpen(false);
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const deleteCatMutation = useMutation({
    mutationFn: (id: string) => catalogApi.deleteCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['catalog-categories'] });
      toast.success('Категорію успішно видалено');
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const handleItemSubmit = (e: FormEvent) => {
    e.preventDefault();
    saveItemMutation.mutate();
  };

  const handleCatSubmit = (e: FormEvent) => {
    e.preventDefault();
    saveCatMutation.mutate();
  };

  return (
    <div className="space-y-8">
      <Card className="p-6 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
              <FolderTree className="h-4 w-4 text-indigo-600" />
              Розділи каталогу ({categories.length})
            </h3>
            <p className="text-xs text-zinc-500">
              Керуйте категоріями для зручної навігації покупців.
            </p>
          </div>
          <Button size="sm" onClick={openCreateCat}>
            <Plus className="h-4 w-4" />
            Додати категорію
          </Button>
        </div>

        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {categories.map((cat) => (
            <div
              key={cat.id}
              className="flex items-start justify-between rounded-xl border border-zinc-200/80 bg-zinc-50/50 p-4"
            >
              <div className="space-y-1">
                <span className="text-sm font-semibold text-zinc-900 block">
                  {cat.name}
                </span>
                <p className="text-xs text-zinc-500 line-clamp-2">
                  {cat.description || 'Без опису'}
                </p>
              </div>

              <div className="flex items-center gap-1">
                <button
                  type="button"
                  onClick={() => openEditCat(cat)}
                  className="rounded-lg p-1.5 text-zinc-500 hover:bg-zinc-200/70 hover:text-zinc-900 cursor-pointer"
                  title="Редагувати категорію"
                >
                  <Pencil className="h-3.5 w-3.5" />
                </button>
                <button
                  type="button"
                  onClick={() => deleteCatMutation.mutate(cat.id)}
                  className="rounded-lg p-1.5 text-zinc-400 hover:bg-rose-50 hover:text-rose-600 cursor-pointer"
                  title="Видалити категорію"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            </div>
          ))}
        </div>
      </Card>

      <Card className="p-6 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
              <Package className="h-4 w-4 text-indigo-600" />
              Товари в каталозі ({items.length})
            </h3>
            <p className="text-xs text-zinc-500">
              Додавання нових позицій, оновлення цін та контроль наявності на складі.
            </p>
          </div>
          <Button size="sm" onClick={openCreateItem}>
            <Plus className="h-4 w-4" />
            Додати товар
          </Button>
        </div>

        <div className="overflow-x-auto rounded-xl border border-zinc-200/80">
          <table className="w-full text-left text-xs">
            <thead className="border-b border-zinc-200 bg-zinc-50 text-zinc-500 uppercase">
              <tr>
                <th className="px-4 py-3 font-semibold">Товар</th>
                <th className="px-4 py-3 font-semibold">Артикул</th>
                <th className="px-4 py-3 font-semibold">Категорія</th>
                <th className="px-4 py-3 font-semibold">Ціна</th>
                <th className="px-4 py-3 font-semibold">Залишок</th>
                <th className="px-4 py-3 text-right font-semibold">Дії</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-zinc-100 bg-white">
              {items.map((item) => (
                <tr key={item.id} className="hover:bg-zinc-50/60">
                  <td className="px-4 py-3">
                    <div className="font-semibold text-zinc-900">{item.title}</div>
                    <div className="text-zinc-500 line-clamp-1 max-w-xs">
                      {item.description}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-zinc-600">{item.sku}</td>
                  <td className="px-4 py-3">
                    <Badge variant="primary">{item.categoryName}</Badge>
                  </td>
                  <td className="px-4 py-3 font-bold text-zinc-900">
                    {formatCurrency(item.price)}
                  </td>
                  <td className="px-4 py-3">
                    <Badge
                      variant={
                        item.stockQuantity > 5
                          ? 'success'
                          : item.stockQuantity > 0
                          ? 'warning'
                          : 'danger'
                      }
                    >
                      {item.stockQuantity} шт.
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="inline-flex items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => openEditItem(item)}
                      >
                        <Pencil className="h-3.5 w-3.5" />
                        Змінити
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-rose-600 hover:bg-rose-50 hover:text-rose-700"
                        onClick={() => deleteItemMutation.mutate(item.id)}
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      <Dialog open={itemDialogOpen} onOpenChange={setItemDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingItem ? 'Редагування товару' : 'Новий товар'}
            </DialogTitle>
            <DialogDescription>
              Заповніть інформацію про товар та вкажіть актуальну ціну і кількість на складі.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleItemSubmit} className="space-y-4 pt-2">
            <div className="space-y-1">
              <label className="text-xs font-medium text-zinc-700">Назва товару</label>
              <Input
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="MacBook Pro 16 M4"
                required
              />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-medium text-zinc-700">Артикул</label>
                <Input
                  value={sku}
                  onChange={(e) => setSku(e.target.value)}
                  placeholder="ELEC-MBP-001"
                  required
                />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-medium text-zinc-700">Категорія</label>
                <select
                  value={categoryId}
                  onChange={(e) => setCategoryId(e.target.value)}
                  required
                  className="flex h-10 w-full rounded-lg border border-zinc-200 bg-white px-3 text-sm text-zinc-900 focus:border-indigo-500 focus:outline-none"
                >
                  <option value="" disabled>
                    Оберіть категорію
                  </option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div className="space-y-1">
                <label className="text-xs font-medium text-zinc-700">Ціна ($)</label>
                <Input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={price}
                  onChange={(e) => setPrice(e.target.value)}
                  required
                />
              </div>

              <div className="space-y-1">
                <label className="text-xs font-medium text-zinc-700">
                  Кількість на складі (шт.)
                </label>
                <Input
                  type="number"
                  min="0"
                  value={stockQuantity}
                  onChange={(e) => setStockQuantity(e.target.value)}
                  required
                />
              </div>
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-zinc-700">Опис</label>
              <Input
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Короткий опис та характеристики"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setItemDialogOpen(false)}
              >
                Скасувати
              </Button>
              <Button type="submit" disabled={saveItemMutation.isPending}>
                {saveItemMutation.isPending ? 'Збереження...' : 'Зберегти'}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={catDialogOpen} onOpenChange={setCatDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingCat ? 'Редагування категорії' : 'Нова категорія'}
            </DialogTitle>
            <DialogDescription>
              Вкажіть назву розділу та короткий опис.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleCatSubmit} className="space-y-4 pt-2">
            <div className="space-y-1">
              <label className="text-xs font-medium text-zinc-700">Назва категорії</label>
              <Input
                value={catName}
                onChange={(e) => {
                  const val = e.target.value;
                  setCatName(val);
                  if (!editingCat) {
                    setCatSlug(
                      val
                        .toLowerCase()
                        .replace(/[^a-z0-9\s-]/g, '')
                        .trim()
                        .replace(/\s+/g, '-')
                    );
                  }
                }}
                placeholder="Smart Gadgets"
                required
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-zinc-700">Коротке посилання (латиницею)</label>
              <Input
                value={catSlug}
                onChange={(e) => setCatSlug(e.target.value)}
                placeholder="smart-gadgets"
                required
              />
            </div>

            <div className="space-y-1">
              <label className="text-xs font-medium text-zinc-700">Опис категорії</label>
              <Input
                value={catDesc}
                onChange={(e) => setCatDesc(e.target.value)}
                placeholder="Розумні пристрої та аксесуари"
              />
            </div>

            <div className="flex justify-end gap-2 pt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => setCatDialogOpen(false)}
              >
                Скасувати
              </Button>
              <Button type="submit" disabled={saveCatMutation.isPending}>
                {saveCatMutation.isPending ? 'Збереження...' : 'Зберегти'}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}

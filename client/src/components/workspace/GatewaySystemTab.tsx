import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  Activity,
  Database,
  RefreshCw,
  Trash2,
  CheckCircle2,
  AlertTriangle,
} from 'lucide-react';
import { toast } from 'sonner';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { gatewayApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';

export function GatewaySystemTab() {
  const queryClient = useQueryClient();

  const metricsQuery = useQuery({
    queryKey: ['cache-metrics'],
    queryFn: () => gatewayApi.getCacheMetrics(),
    refetchInterval: 5000,
  });

  const healthQuery = useQuery({
    queryKey: ['gateway-health'],
    queryFn: () => gatewayApi.getHealth(),
    refetchInterval: 10000,
  });

  const invalidateMutation = useMutation({
    mutationFn: () => gatewayApi.invalidateCache(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['cache-metrics'] });
      toast.success('Кеш каталогу успішно очищено');
    },
    onError: (err) => toast.error(extractApiErrorMessage(err)),
  });

  const metrics = metricsQuery.data;
  const health = healthQuery.data;
  const hitRatioPercent = metrics ? Math.round(metrics.hitRatio * 100) : 0;

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
      <Card className="p-6 space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
              <Database className="h-4 w-4 text-indigo-600" />
              Швидкодія та кешування каталогу
            </h3>
            <p className="text-xs text-zinc-500">
              Статистика прискорення завантаження карток товарів.
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => metricsQuery.refetch()}
              title="Оновити показники"
            >
              <RefreshCw className="h-3.5 w-3.5" />
            </Button>
            <Button
              variant="danger"
              size="sm"
              disabled={invalidateMutation.isPending}
              onClick={() => invalidateMutation.mutate()}
            >
              <Trash2 className="h-3.5 w-3.5" />
              Очистити кеш
            </Button>
          </div>
        </div>

        {metrics && (
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
              <div className="rounded-xl border border-zinc-200/80 bg-zinc-50 p-3.5">
                <span className="text-[11px] text-zinc-500">Миттєві відповіді</span>
                <div className="text-xl font-extrabold text-emerald-600 mt-0.5">
                  {metrics.hits}
                </div>
              </div>
              <div className="rounded-xl border border-zinc-200/80 bg-zinc-50 p-3.5">
                <span className="text-[11px] text-zinc-500">Звернення до бази</span>
                <div className="text-xl font-extrabold text-amber-600 mt-0.5">
                  {metrics.misses}
                </div>
              </div>
              <div className="rounded-xl border border-zinc-200/80 bg-zinc-50 p-3.5">
                <span className="text-[11px] text-zinc-500">Усього переглядів</span>
                <div className="text-xl font-extrabold text-zinc-900 mt-0.5">
                  {metrics.totalRequests}
                </div>
              </div>
              <div className="rounded-xl border border-zinc-200/80 bg-zinc-50 p-3.5">
                <span className="text-[11px] text-zinc-500">Оновлень даних</span>
                <div className="text-xl font-extrabold text-indigo-600 mt-0.5">
                  {metrics.evictions}
                </div>
              </div>
            </div>

            <div className="space-y-1.5">
              <div className="flex justify-between text-xs font-medium">
                <span className="text-zinc-600">Частка миттєвих відповідей з кешу</span>
                <span className="font-bold text-indigo-600">{hitRatioPercent}%</span>
              </div>
              <div className="h-2.5 w-full overflow-hidden rounded-full bg-zinc-200">
                <div
                  className="h-full bg-indigo-600 transition-all duration-300"
                  style={{ width: `${Math.min(100, hitRatioPercent)}%` }}
                />
              </div>
            </div>
          </div>
        )}
      </Card>

      <Card className="p-6 space-y-5">
        <div className="flex items-center justify-between">
          <div>
            <h3 className="text-base font-bold text-zinc-900 flex items-center gap-2">
              <Activity className="h-4 w-4 text-emerald-600" />
              Доступність модулів магазину
            </h3>
            <p className="text-xs text-zinc-500">
              Моніторинг працездатності основних підсистем у реальному часі.
            </p>
          </div>
          <Badge variant={health?.status === 'Healthy' ? 'success' : 'warning'}>
            {health?.status === 'Healthy' ? 'Працює стабільно' : 'Часткові обмеження'}
          </Badge>
        </div>

        {health && (
          <div className="space-y-2.5">
            {[
              {
                name: 'Авторизація та профілі клієнтів',
                desc: 'Вхід, реєстрація та особистий кабінет',
                state: health.dependencies.authService,
              },
              {
                name: 'Каталог товарів та склад',
                desc: 'Пошук товарів, категорії та перевірка наявності',
                state: health.dependencies.catalogService,
              },
              {
                name: 'Оформлення та доставка замовлень',
                desc: 'Прийом нових замовлень та відстеження статусів',
                state: health.dependencies.orderingService,
              },
            ].map((svc) => {
              const up = svc.state === 'Healthy';
              return (
                <div
                  key={svc.name}
                  className="flex items-center justify-between rounded-xl border border-zinc-200/80 bg-zinc-50/70 px-4 py-3"
                >
                  <div>
                    <div className="text-xs font-bold text-zinc-900">{svc.name}</div>
                    <div className="text-[11px] text-zinc-500">{svc.desc}</div>
                  </div>
                  <Badge variant={up ? 'success' : 'danger'}>
                    {up ? (
                      <CheckCircle2 className="h-3.5 w-3.5" />
                    ) : (
                      <AlertTriangle className="h-3.5 w-3.5" />
                    )}
                    {up ? 'Активно' : 'Недоступно'}
                  </Badge>
                </div>
              );
            })}
          </div>
        )}
      </Card>
    </div>
  );
}

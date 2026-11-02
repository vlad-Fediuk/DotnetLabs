import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { KeyRound, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { authApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { useAuthStore } from '@/store/useAuthStore';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const setSession = useAuthStore((s) => s.setSession);
  const navigate = useNavigate();
  const location = useLocation();
  const fromPath = (location.state as { from?: string } | null)?.from || '/';

  const loginMutation = useMutation({
    mutationFn: () => authApi.login({ email: email.trim(), password }),
    onSuccess: (auth) => {
      setSession(auth);
      toast.success(`Вітаємо, ${auth.user.displayName}!`);
      navigate(fromPath, { replace: true });
    },
    onError: (err) => {
      toast.error(extractApiErrorMessage(err));
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!email.trim() || !password) {
      toast.error('Введіть електронну пошту та пароль');
      return;
    }
    loginMutation.mutate();
  };

  return (
    <div className="mx-auto flex max-w-md flex-col justify-center py-8">
      <Card className="border-zinc-200/90 shadow-md">
        <CardHeader className="space-y-2 pb-4">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
            <KeyRound className="h-5 w-5" />
          </div>
          <CardTitle className="text-xl pt-1">Вхід до особистого кабінету</CardTitle>
          <CardDescription>
            Увійдіть, щоб оформлювати замовлення та відстежувати статус доставки.
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-5">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-zinc-700">
                Електронна пошта
              </label>
              <Input
                type="email"
                placeholder="name@example.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-medium text-zinc-700">Пароль</label>
              <Input
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>

            <Button
              type="submit"
              className="w-full"
              size="lg"
              disabled={loginMutation.isPending}
            >
              {loginMutation.isPending ? (
                'Вхід...'
              ) : (
                <>
                  Увійти
                  <ArrowRight className="h-4 w-4" />
                </>
              )}
            </Button>
          </form>

          <div className="border-t border-zinc-100 pt-4 text-center text-xs text-zinc-500">
            Ще не маєте облікового запису?{' '}
            <Link
              to="/register"
              className="font-semibold text-indigo-600 hover:text-indigo-700"
            >
              Зареєструватися
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

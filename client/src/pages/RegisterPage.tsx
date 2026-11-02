import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { UserPlus, ArrowRight } from 'lucide-react';
import { toast } from 'sonner';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { authApi } from '@/api/services';
import { extractApiErrorMessage } from '@/api/httpClient';
import { useAuthStore } from '@/store/useAuthStore';

export function RegisterPage() {
  const [displayName, setDisplayName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  const setSession = useAuthStore((s) => s.setSession);
  const navigate = useNavigate();

  const registerMutation = useMutation({
    mutationFn: () =>
      authApi.register({
        displayName: displayName.trim(),
        email: email.trim(),
        password,
      }),
    onSuccess: (auth) => {
      setSession(auth);
      toast.success(`Ласкаво просимо, ${auth.user.displayName}!`);
      navigate('/', { replace: true });
    },
    onError: (err) => {
      toast.error(extractApiErrorMessage(err));
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (displayName.trim().length < 2) {
      toast.error("Ім'я має містити щонайменше 2 символи");
      return;
    }
    if (password.length < 6) {
      toast.error('Пароль має містити щонайменше 6 символів');
      return;
    }
    registerMutation.mutate();
  };

  return (
    <div className="mx-auto flex max-w-md flex-col justify-center py-8">
      <Card className="border-zinc-200/90 shadow-md">
        <CardHeader className="space-y-2 pb-4">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
            <UserPlus className="h-5 w-5" />
          </div>
          <CardTitle className="text-xl pt-1">Створення облікового запису</CardTitle>
          <CardDescription>
            Зареєструйтеся для швидкого оформлення покупок та перегляду історії замовлень.
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-5">
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label className="text-xs font-medium text-zinc-700">
                Ваше ім&apos;я та прізвище
              </label>
              <Input
                type="text"
                placeholder="Олександр Коваленко"
                value={displayName}
                onChange={(e) => setDisplayName(e.target.value)}
                required
              />
            </div>

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
              <label className="text-xs font-medium text-zinc-700">
                Пароль (щонайменше 6 символів)
              </label>
              <Input
                type="password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="new-password"
              />
            </div>

            <Button
              type="submit"
              className="w-full"
              size="lg"
              disabled={registerMutation.isPending}
            >
              {registerMutation.isPending ? (
                'Реєстрація...'
              ) : (
                <>
                  Зареєструватися
                  <ArrowRight className="h-4 w-4" />
                </>
              )}
            </Button>
          </form>

          <div className="border-t border-zinc-100 pt-4 text-center text-xs text-zinc-500">
            Вже маєте обліковий запис?{' '}
            <Link
              to="/login"
              className="font-semibold text-indigo-600 hover:text-indigo-700"
            >
              Увійти
            </Link>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

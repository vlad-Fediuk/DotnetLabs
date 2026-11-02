import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertCircle, RotateCcw } from 'lucide-react';
import { Button } from './Button';
import { Card } from './Card';

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  public override state: State = {
    hasError: false,
    error: null,
  };

  public static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  public override componentDidCatch(error: Error, errorInfo: ErrorInfo) {
    console.error('Unhandled React Error:', error, errorInfo);
  }

  private handleReset = () => {
    try {
      localStorage.removeItem('nexus-ecommerce-auth');
      localStorage.removeItem('nexus-ecommerce-cart');
    } catch {
      localStorage.clear();
    }
    window.location.href = '/';
  };

  public override render() {
    if (this.state.hasError) {
      return (
        <div className="min-h-screen flex items-center justify-center bg-zinc-50 p-4">
          <Card className="max-w-md w-full p-8 text-center space-y-4">
            <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-rose-50 text-rose-600">
              <AlertCircle className="h-6 w-6" />
            </div>
            <h2 className="text-lg font-bold text-zinc-900">
              Сталася неочікувана помилка
            </h2>
            <p className="text-xs text-zinc-600">
              {this.state.error?.message || 'Помилка відображення сторінки.'}
            </p>
            <div className="pt-2 flex justify-center gap-3">
              <Button variant="default" size="sm" onClick={this.handleReset}>
                <RotateCcw className="h-4 w-4" />
                Очистити кеш і перезавантажити
              </Button>
            </div>
          </Card>
        </div>
      );
    }

    return this.props.children;
  }
}

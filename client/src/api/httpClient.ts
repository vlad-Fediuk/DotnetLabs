import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/store/useAuthStore';
import { generateUuidV4 } from '@/lib/utils';
import type { AuthResponse, ProblemDetails } from '@/types/api';

const rawBaseURL = import.meta.env.VITE_API_BASE_URL || '';
const baseURL = rawBaseURL.replace(/\/+$/, '');

export const httpClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 15000,
});

httpClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = useAuthStore.getState().accessToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  if (!config.headers['X-Correlation-Id']) {
    config.headers['X-Correlation-Id'] = generateUuidV4();
  }
  return config;
});

let isRefreshing = false;
let pendingQueue: Array<{
  resolve: (token: string) => void;
  reject: (err: unknown) => void;
}> = [];

function flushQueue(error: unknown, token: string | null = null) {
  pendingQueue.forEach((p) => {
    if (error) {
      p.reject(error);
    } else if (token) {
      p.resolve(token);
    }
  });
  pendingQueue = [];
}

httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ProblemDetails>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    if (!originalRequest) {
      return Promise.reject(error);
    }

    const isAuthRoute =
      originalRequest.url?.includes('/api/v1/auth/login') ||
      originalRequest.url?.includes('/api/v1/auth/register') ||
      originalRequest.url?.includes('/api/v1/auth/refresh');

    if (
      error.response?.status === 401 &&
      !originalRequest._retry &&
      !isAuthRoute
    ) {
      const refreshToken = useAuthStore.getState().refreshToken;
      if (!refreshToken) {
        useAuthStore.getState().clearSession();
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise<string>((resolve, reject) => {
          pendingQueue.push({ resolve, reject });
        }).then((newToken) => {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return httpClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const refreshResponse = await axios.post<AuthResponse>(
          `${baseURL}/api/v1/auth/refresh`,
          { refreshToken },
          { headers: { 'Content-Type': 'application/json' } }
        );

        const newAuth = refreshResponse.data;
        useAuthStore.getState().setSession(newAuth);
        flushQueue(null, newAuth.accessToken);

        originalRequest.headers.Authorization = `Bearer ${newAuth.accessToken}`;
        return httpClient(originalRequest);
      } catch (refreshError) {
        flushQueue(refreshError, null);
        useAuthStore.getState().clearSession();
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export function extractApiErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const status = error.response?.status;
    const retryAfter = error.response?.headers?.['retry-after'];
    const data = error.response?.data;

    if (status === 429) {
      return retryAfter
        ? `Забагато запитів. Будь ласка, зачекайте ${retryAfter} сек.`
        : 'Забагато запитів. Спробуйте ще раз за хвилину.';
    }

    if (data?.errors) {
      const messages = Object.values(data.errors).flat();
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (data?.detail) {
      return data.detail;
    }

    if (data?.title) {
      return data.title;
    }

    if (status === 403) {
      return 'У вас немає доступу для виконання цієї дії.';
    }

    if (status === 401) {
      return 'Будь ласка, увійдіть у свій обліковий запис.';
    }

    if (status === 503) {
      return 'Сервіс тимчасово недоступний. Спробуйте трохи пізніше.';
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Сталася помилка. Спробуйте оновити сторінку.';
}

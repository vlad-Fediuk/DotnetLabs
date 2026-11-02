import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';
import { OrderStatus } from '@/types/api';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 2,
  }).format(value);
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return new Intl.DateTimeFormat('uk-UA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date);
}

export interface OrderStatusMeta {
  label: string;
  badgeVariant: 'warning' | 'info' | 'primary' | 'success' | 'danger';
  stepIndex: number;
}

export function getOrderStatusMeta(status: OrderStatus | number): OrderStatusMeta {
  switch (Number(status)) {
    case OrderStatus.Pending:
      return { label: 'Очікує оплати', badgeVariant: 'warning', stepIndex: 0 };
    case OrderStatus.Paid:
      return { label: 'Оплачено', badgeVariant: 'info', stepIndex: 1 };
    case OrderStatus.Shipped:
      return { label: 'Відправлено', badgeVariant: 'primary', stepIndex: 2 };
    case OrderStatus.Delivered:
      return { label: 'Доставлено', badgeVariant: 'success', stepIndex: 3 };
    case OrderStatus.Cancelled:
      return { label: 'Скасовано', badgeVariant: 'danger', stepIndex: -1 };
    default:
      return { label: `Статус #${status}`, badgeVariant: 'info', stepIndex: 0 };
  }
}

export function getValidNextStatuses(current: OrderStatus | number): OrderStatus[] {
  switch (Number(current)) {
    case OrderStatus.Pending:
      return [OrderStatus.Paid, OrderStatus.Shipped, OrderStatus.Cancelled];
    case OrderStatus.Paid:
      return [OrderStatus.Shipped, OrderStatus.Cancelled];
    case OrderStatus.Shipped:
      return [OrderStatus.Delivered];
    default:
      return [];
  }
}

export function canUserCancelOrder(status: OrderStatus | number): boolean {
  const s = Number(status);
  return s === OrderStatus.Pending || s === OrderStatus.Paid;
}

export function generateUuidV4(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = (Math.random() * 16) | 0;
    const v = c === 'x' ? r : (r & 0x3) | 0x8;
    return v.toString(16);
  });
}

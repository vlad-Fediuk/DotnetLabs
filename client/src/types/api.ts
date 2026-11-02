export const Permissions = {
  Catalog: {
    View: 'catalog:view',
    Manage: 'catalog:manage',
  },
  Orders: {
    Create: 'orders:create',
    ViewOwn: 'orders:view-own',
    ViewAll: 'orders:view-all',
    ManageStatus: 'orders:manage-status',
  },
  Users: {
    View: 'users:view',
    ManageRoles: 'users:manage-roles',
  },
  System: {
    ManageCache: 'system:manage-cache',
  },
} as const;

export const RoleNames = {
  User: 'User',
  CatalogManager: 'CatalogManager',
  OrderOperator: 'OrderOperator',
  Support: 'Support',
  Manager: 'Manager',
  Admin: 'Admin',
} as const;

export const AllRoleList: string[] = [
  RoleNames.User,
  RoleNames.CatalogManager,
  RoleNames.OrderOperator,
  RoleNames.Support,
  RoleNames.Manager,
  RoleNames.Admin,
];

export enum OrderStatus {
  Pending = 1,
  Paid = 2,
  Shipped = 3,
  Delivered = 4,
  Cancelled = 5,
}

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  permissions: string[];
  createdAtUtc: string;
  isActive: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserDto;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface UpdateRolesRequest {
  roles: string[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description?: string | null;
}

export interface CreateCategoryRequest {
  name: string;
  slug: string;
  description?: string | null;
}

export interface UpdateCategoryRequest {
  name: string;
  slug: string;
  description?: string | null;
}

export interface ItemDto {
  id: string;
  title: string;
  sku: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  categoryId: string;
  categoryName: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface CreateItemRequest {
  title: string;
  sku: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  categoryId: string;
}

export interface UpdateItemRequest {
  title: string;
  sku: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  categoryId: string;
}

export interface CatalogFilterParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: 'price' | 'title' | 'created';
  desc?: boolean;
}

export interface CatalogItemSummaryDto {
  id: string;
  title: string;
  sku: string;
  description?: string | null;
  price: number;
  stockQuantity: number;
  categoryName: string;
  isActive: boolean;
}

export interface ActiveOperationDto {
  orderId: string;
  orderNumber: string;
  status: OrderStatus;
  quantity: number;
  totalPrice: number;
  createdAtUtc: string;
}

export interface ItemDetailsScreenDto {
  item: CatalogItemSummaryDto;
  myActiveOperation?: ActiveOperationDto | null;
  activeOperationStatus: 'Available' | 'Unavailable' | string;
  activeOperationMessage?: string | null;
}

export interface OrderItemRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  items: OrderItemRequest[];
}

export interface OrderItemResponse {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface OrderResponse {
  id: string;
  orderNumber: string;
  userId: string;
  status: OrderStatus;
  totalAmount: number;
  createdAtUtc: string;
  items: OrderItemResponse[];
}

export interface CreateOrderHttpResult {
  order: OrderResponse;
  replayed: boolean;
}

export interface CacheMetricsDto {
  hits: number;
  misses: number;
  totalRequests: number;
  hitRatio: number;
  evictions: number;
}

export interface HealthResponse {
  service: string;
  status: 'Healthy' | 'Degraded' | string;
  port: number;
  dependencies: {
    authService: string;
    catalogService: string;
    orderingService: string;
  };
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

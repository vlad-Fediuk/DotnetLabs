import { httpClient } from '@/api/httpClient';
import type {
  AuthResponse,
  CacheMetricsDto,
  CatalogFilterParams,
  CategoryDto,
  CreateCategoryRequest,
  CreateItemRequest,
  CreateOrderHttpResult,
  CreateOrderRequest,
  HealthResponse,
  ItemDetailsScreenDto,
  ItemDto,
  LoginRequest,
  OrderResponse,
  OrderStatus,
  PagedResult,
  RegisterRequest,
  UpdateCategoryRequest,
  UpdateItemRequest,
  UpdateRolesRequest,
  UserDto,
} from '@/types/api';

export const authApi = {
  async login(payload: LoginRequest): Promise<AuthResponse> {
    const { data } = await httpClient.post<AuthResponse>('/api/v1/auth/login', payload);
    return data;
  },

  async register(payload: RegisterRequest): Promise<AuthResponse> {
    const { data } = await httpClient.post<AuthResponse>('/api/v1/auth/register', payload);
    return data;
  },

  async logout(refreshToken?: string | null): Promise<void> {
    await httpClient.post('/api/v1/auth/logout', refreshToken ? { refreshToken } : {});
  },
};

export const usersApi = {
  async getMe(): Promise<UserDto> {
    const { data } = await httpClient.get<UserDto>('/api/v1/users/me');
    return data;
  },

  async getUsersPaged(page = 1, pageSize = 20): Promise<PagedResult<UserDto>> {
    const { data } = await httpClient.get<PagedResult<UserDto>>('/api/v1/users', {
      params: { page, pageSize },
    });
    return data;
  },

  async updateUserRoles(userId: string, payload: UpdateRolesRequest): Promise<void> {
    await httpClient.put(`/api/v1/users/${userId}/roles`, payload);
  },
};

export const catalogApi = {
  async getItems(params: CatalogFilterParams): Promise<PagedResult<ItemDto>> {
    const cleanParams: Record<string, unknown> = {};
    if (params.page) cleanParams.page = params.page;
    if (params.pageSize) cleanParams.pageSize = params.pageSize;
    if (params.search && params.search.trim()) cleanParams.search = params.search.trim();
    if (params.categoryId) cleanParams.categoryId = params.categoryId;
    if (params.minPrice !== undefined && !Number.isNaN(params.minPrice)) {
      cleanParams.minPrice = params.minPrice;
    }
    if (params.maxPrice !== undefined && !Number.isNaN(params.maxPrice)) {
      cleanParams.maxPrice = params.maxPrice;
    }
    if (params.sortBy) cleanParams.sortBy = params.sortBy;
    if (params.desc !== undefined) cleanParams.desc = params.desc;

    const { data } = await httpClient.get<PagedResult<ItemDto>>('/api/v1/catalog/items', {
      params: cleanParams,
    });
    return data;
  },

  async getItemById(id: string): Promise<ItemDto> {
    const { data } = await httpClient.get<ItemDto>(`/api/v1/catalog/items/${id}`);
    return data;
  },

  async createItem(payload: CreateItemRequest): Promise<ItemDto> {
    const { data } = await httpClient.post<ItemDto>('/api/v1/catalog/items', payload);
    return data;
  },

  async updateItem(id: string, payload: UpdateItemRequest): Promise<void> {
    await httpClient.put(`/api/v1/catalog/items/${id}`, payload);
  },

  async deleteItem(id: string): Promise<void> {
    await httpClient.delete(`/api/v1/catalog/items/${id}`);
  },

  async getCategories(): Promise<CategoryDto[]> {
    const { data } = await httpClient.get<CategoryDto[]>('/api/v1/catalog/categories');
    return data;
  },

  async createCategory(payload: CreateCategoryRequest): Promise<CategoryDto> {
    const { data } = await httpClient.post<CategoryDto>('/api/v1/catalog/categories', payload);
    return data;
  },

  async updateCategory(id: string, payload: UpdateCategoryRequest): Promise<void> {
    await httpClient.put(`/api/v1/catalog/categories/${id}`, payload);
  },

  async deleteCategory(id: string): Promise<void> {
    await httpClient.delete(`/api/v1/catalog/categories/${id}`);
  },
};

export const screensApi = {
  async getItemDetailsScreen(id: string): Promise<ItemDetailsScreenDto> {
    const { data } = await httpClient.get<ItemDetailsScreenDto>(
      `/api/v1/screens/item-details/${id}`
    );
    return data;
  },
};

export const ordersApi = {
  async createOrder(
    payload: CreateOrderRequest,
    idempotencyKey: string
  ): Promise<CreateOrderHttpResult> {
    const response = await httpClient.post<OrderResponse>('/api/orders', payload, {
      headers: {
        'Idempotency-Key': idempotencyKey,
      },
    });
    const replayedHeader = response.headers['idempotency-replayed'];
    return {
      order: response.data,
      replayed: replayedHeader === 'true',
    };
  },

  async getMyOrders(params?: {
    status?: OrderStatus;
    page?: number;
    pageSize?: number;
  }): Promise<OrderResponse[]> {
    const { data } = await httpClient.get<OrderResponse[]>('/api/orders', {
      params,
    });
    return data;
  },

  async getAllOrders(params?: {
    status?: OrderStatus;
    userId?: string;
    page?: number;
    pageSize?: number;
  }): Promise<OrderResponse[]> {
    const { data } = await httpClient.get<OrderResponse[]>('/api/orders/all', {
      params,
    });
    return data;
  },

  async getOrderById(id: string): Promise<OrderResponse> {
    const { data } = await httpClient.get<OrderResponse>(`/api/orders/${id}`);
    return data;
  },

  async cancelOrder(id: string): Promise<OrderResponse> {
    const { data } = await httpClient.post<OrderResponse>(`/api/orders/${id}/cancel`);
    return data;
  },

  async updateOrderStatus(id: string, status: OrderStatus): Promise<OrderResponse> {
    const { data } = await httpClient.patch<OrderResponse>(`/api/orders/${id}/status`, {
      status,
    });
    return data;
  },
};

export const gatewayApi = {
  async getCacheMetrics(): Promise<CacheMetricsDto> {
    const { data } = await httpClient.get<CacheMetricsDto>('/api/v1/cache/metrics');
    return data;
  },

  async invalidateCache(): Promise<void> {
    await httpClient.post('/api/v1/cache/invalidate');
  },

  async getHealth(): Promise<HealthResponse> {
    const { data } = await httpClient.get<HealthResponse>('/health', {
      validateStatus: (status) => status === 200 || status === 503,
    });
    return data;
  },
};

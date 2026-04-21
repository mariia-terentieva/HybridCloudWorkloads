import { api } from './api';
import {
  CloudProvider,
  CloudRegion,
  InstanceType,
  InstancePricing,
  CloudService,
  Discount,
  ProviderPriceComparison,
  BestPriceOffer,
  SyncResult,
  SyncStatus,
  CacheStatistics,
  ComparePricesRequest,
  BestOffersRequest,
  InstanceTypesFilter,
  RecommendationsRequest,
  InstanceRecommendation,
  ProviderDetailResponse,
  RegionsResponse,
  RegionDetailResponse,
  InstanceTypesResponseExtended,
  InstanceTypeFullDetail,
  CompareInstanceTypesRequest,
  InstanceTypesComparison,
} from '../types/providers';

export const providerService = {
  // ========== ПРОВАЙДЕРЫ ==========

  /**
   * Получить список всех провайдеров
   */
  getProviders: async (includeInactive = false): Promise<CloudProvider[]> => {
    const response = await api.get<CloudProvider[]>('/providers', {
      params: { includeInactive },
    });
    return response.data;
  },

  /**
   * Получить провайдера по коду
   */
  getProvider: async (providerCode: string): Promise<CloudProvider> => {
    const response = await api.get<CloudProvider>(`/providers/${providerCode}`);
    return response.data;
  },

  /**
   * Получить список поддерживаемых провайдеров
   */
  getSupportedProviders: async (): Promise<string[]> => {
    const response = await api.get<string[]>('/providers/supported');
    return response.data;
  },

  /**
   * Проверить доступность API провайдера
   */
  checkAvailability: async (providerCode: string): Promise<{ isAvailable: boolean }> => {
    const response = await api.get(`/providers/${providerCode}/availability`);
    return response.data;
  },

  // ========== РЕГИОНЫ ==========

  /**
   * Получить регионы провайдера
   */
  getRegions: async (providerCode: string, forceRefresh = false): Promise<CloudRegion[]> => {
    const response = await api.get<CloudRegion[]>(`/providers/${providerCode}/regions`, {
      params: { forceRefresh },
    });
    return response.data;
  },

  /**
   * Получить детальную информацию о регионе
   */
  getRegion: async (providerCode: string, regionCode: string): Promise<CloudRegion> => {
    const response = await api.get<CloudRegion>(`/providers/${providerCode}/regions/${regionCode}`);
    return response.data;
  },

  // ========== ТИПЫ ИНСТАНСОВ ==========

  /**
   * Получить типы инстансов с фильтрацией
   */
  getInstanceTypes: async (filter: InstanceTypesFilter): Promise<InstanceType[]> => {
    const params: Record<string, any> = { ...filter };
    const response = await api.get<InstanceType[]>(`/providers/${filter.providerCode}/instance-types`, { params });
    return response.data;
  },

  /**
   * Получить детальную информацию о типе инстанса
   */
  getInstanceType: async (
    providerCode: string,
    typeCode: string,
    regionCode?: string
  ): Promise<InstanceType> => {
    const params = regionCode ? { regionCode } : {};
    const response = await api.get<InstanceType>(
      `/providers/${providerCode}/instance-types/${typeCode}`,
      { params }
    );
    return response.data;
  },

  // ========== ЦЕНООБРАЗОВАНИЕ ==========

  /**
   * Получить цены для типа инстанса
   */
  getPricing: async (instanceTypeId: string, forceRefresh = false): Promise<InstancePricing> => {
    const response = await api.get<InstancePricing>(`/providers/instance-types/${instanceTypeId}/pricing`, {
      params: { forceRefresh },
    });
    return response.data;
  },

  /**
   * Получить цены для нескольких типов инстансов
   */
  getBatchPricing: async (instanceTypeIds: string[], forceRefresh = false): Promise<Record<string, InstancePricing>> => {
    const response = await api.post<Record<string, InstancePricing>>(
      '/providers/batch-pricing',
      instanceTypeIds,
      { params: { forceRefresh } }
    );
    return response.data;
  },

  // ========== СРАВНЕНИЕ ЦЕН ==========

  /**
   * Сравнить цены между провайдерами
   */
  comparePrices: async (request: ComparePricesRequest): Promise<ProviderPriceComparison> => {
    const params: Record<string, any> = {
      cpu: request.cpu,
      memory: request.memory,
    };
    if (request.providers?.length) {
      params.providers = request.providers.join(',');
    }
    const response = await api.get<ProviderPriceComparison>('/providers/compare', { params });
    return response.data;
  },

  /**
   * Получить лучшие ценовые предложения
   */
  getBestOffers: async (request: BestOffersRequest): Promise<BestPriceOffer[]> => {
    const params: Record<string, any> = {
      cpu: request.cpu,
      memory: request.memory,
      includeSpot: request.includeSpot,
      includeReserved: request.includeReserved,
    };
    if (request.region) {
      params.region = request.region;
    }
    const response = await api.get<BestPriceOffer[]>('/providers/best-offers', { params });
    return response.data;
  },

  /**
   * Получить рекомендации по инстансам
   */
  getRecommendations: async (request: RecommendationsRequest): Promise<InstanceRecommendation[]> => {
    const response = await api.post<InstanceRecommendation[]>('/providers/recommendations', request);
    return response.data;
  },

  // ========== СЕРВИСЫ ==========

  /**
   * Получить сервисы провайдера
   */
  getServices: async (providerCode: string): Promise<CloudService[]> => {
    const response = await api.get<CloudService[]>(`/providers/${providerCode}/services`);
    return response.data;
  },

  // ========== СКИДКИ ==========

  /**
   * Получить скидки провайдера
   */
  getDiscounts: async (providerId: string, forceRefresh = false): Promise<Discount[]> => {
    const response = await api.get<Discount[]>(`/providers/${providerId}/discounts`, {
      params: { forceRefresh },
    });
    return response.data;
  },

  // ========== СИНХРОНИЗАЦИЯ ==========

  /**
   * Запустить синхронизацию с провайдером
   */
  syncProvider: async (providerCode: string, force = false): Promise<SyncResult> => {
    const response = await api.post<SyncResult>(`/providers/${providerCode}/sync`, null, {
      params: { force },
    });
    return response.data;
  },

  /**
   * Получить статус синхронизации
   */
  getSyncStatus: async (providerCode: string): Promise<SyncStatus> => {
    const response = await api.get<SyncStatus>(`/providers/${providerCode}/sync/status`);
    return response.data;
  },

  /**
   * Запустить синхронизацию всех провайдеров
   */
  syncAllProviders: async (force = false): Promise<Record<string, SyncResult>> => {
    const response = await api.post<Record<string, SyncResult>>('/providers/sync-all', null, {
      params: { force },
    });
    return response.data;
  },

  // ========== КЭШ ==========

  /**
   * Получить статистику кэша
   */
  getCacheStats: async (): Promise<CacheStatistics> => {
    const response = await api.get<CacheStatistics>('/providers/cache/stats');
    return response.data;
  },

  /**
   * Инвалидировать кэш провайдера
   */
  invalidateCache: async (providerCode: string): Promise<void> => {
    await api.post(`/providers/${providerCode}/cache/invalidate`);
  },

  /**
   * Инвалидировать весь кэш
   */
  invalidateAllCache: async (): Promise<void> => {
    await api.post('/providers/cache/invalidate-all');
  },

  // Получить детальную информацию о провайдере
getProviderDetail: async (providerCode: string): Promise<ProviderDetailResponse> => {
  const response = await api.get<ProviderDetailResponse>(`/providers/${providerCode}`);
  return response.data;
},

// Получить регионы провайдера с расширенной информацией
getRegionsExtended: async (
  providerCode: string, 
  forceRefresh = false,
  continent?: string
): Promise<RegionsResponse> => {
  const params: Record<string, any> = { forceRefresh };
  if (continent) params.continent = continent;
  const response = await api.get<RegionsResponse>(`/providers/${providerCode}/regions`, { params });
  return response.data;
},

// Получить детальную информацию о регионе
getRegionDetail: async (
  providerCode: string, 
  regionCode: string,
  includeInstanceTypes = false
): Promise<RegionDetailResponse> => {
  const response = await api.get<RegionDetailResponse>(
    `/providers/${providerCode}/regions/${regionCode}`,
    { params: { includeInstanceTypes } }
  );
  return response.data;
},

// Получить типы инстансов с расширенной фильтрацией
getInstanceTypesExtended: async (
  providerCode: string,
  filters: {
    regionCode?: string;
    minCpu?: number;
    maxCpu?: number;
    minMemory?: number;
    maxMemory?: number;
    category?: string;
    family?: string;
    cpuArchitecture?: string;
    hasGpu?: boolean;
    page?: number;
    pageSize?: number;
    forceRefresh?: boolean;
  }
): Promise<InstanceTypesResponseExtended> => {
  const params: Record<string, any> = { ...filters };
  const response = await api.get<InstanceTypesResponseExtended>(
    `/providers/${providerCode}/instance-types`,
    { params }
  );
  return response.data;
},

// Получить полную информацию о типе инстанса
getInstanceTypeFull: async (
  providerCode: string,
  typeCode: string,
  regionCode?: string
): Promise<InstanceTypeFullDetail> => {
  const params = regionCode ? { regionCode } : {};
  const response = await api.get<InstanceTypeFullDetail>(
    `/providers/${providerCode}/instance-types/${typeCode}`,
    { params }
  );
  return response.data;
},

// Сравнить типы инстансов
compareInstanceTypes: async (request: CompareInstanceTypesRequest): Promise<InstanceTypesComparison> => {
  const response = await api.post<InstanceTypesComparison>('/providers/instance-types/compare', request);
  return response.data;
},
};
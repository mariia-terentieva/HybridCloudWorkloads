// ========== ПРОВАЙДЕРЫ ==========

export interface CloudProvider {
  id: string;
  code: string;
  displayName: string;
  description?: string;
  logoUrl?: string;
  status: ProviderStatus;
  syncEnabled: boolean;
  lastSyncAt?: string;
  apiEndpoint?: string;
  authType: string;
  syncIntervalMinutes: number;
  createdAt: string;
  updatedAt: string;
}

export type ProviderStatus = 'Active' | 'Inactive' | 'Maintenance' | 'Error';

export type ProviderCode = 'aws' | 'azure' | 'gcp' | 'yandex' | 'vk' | 'on-premise';

// ========== ЦВЕТА ПРОВАЙДЕРОВ ==========

export const PROVIDER_COLORS: Record<string, string> = {
  aws: '#FF9900',
  azure: '#0078D4',
  gcp: '#4285F4',
  yandex: '#5282FF',
  vk: '#0077FF',
  'on-premise': '#6B7280',
};

// ========== ИНИЦИАЛЫ ПРОВАЙДЕРОВ ==========

export const getProviderInitials = (code: string): string => {
  const initials: Record<string, string> = {
    aws: 'AWS',
    azure: 'AZ',
    gcp: 'GCP',
    yandex: 'YC',
    vk: 'VK',
    'on-premise': 'OP',
  };
  return initials[code] || code.substring(0, 2).toUpperCase();
};

export const getProviderDisplayName = (code: string): string => {
  const names: Record<string, string> = {
    aws: 'Amazon Web Services',
    azure: 'Microsoft Azure',
    gcp: 'Google Cloud Platform',
    yandex: 'Yandex Cloud',
    vk: 'VK Cloud',
    'on-premise': 'Собственная инфраструктура',
  };
  return names[code] || code;
};

// ========== РЕГИОНЫ ==========

export interface CloudRegion {
  id: string;
  providerId: string;
  code: string;
  name: string;
  displayName: string;
  continent: string;
  country?: string;
  city?: string;
  coordinates?: string;
  status: RegionStatus;
  availabilityZones: number;
  compliance?: string[];
  availableServices?: string[];
  createdAt: string;
  updatedAt: string;
}

export type RegionStatus = 'Available' | 'Limited' | 'Unavailable' | 'ComingSoon';

// ========== ТИПЫ ИНСТАНСОВ ==========

export interface InstanceType {
  id: string;
  providerId: string;
  regionId: string;
  region?: CloudRegion;
  typeCode: string;
  displayName: string;
  description?: string;
  category: InstanceCategory;
  family: string;
  generation: number;
  vcpuCount: number;
  cpuModel?: string;
  cpuArchitecture: string;
  cpuClockSpeedGhz?: number;
  cpuType: string;
  memoryGb: number;
  networkBandwidthGbps: number;
  networkPerformance?: string;
  storageType: string;
  localStorageGb?: number;
  localStorageDisks?: number;
  ebsOptimized: boolean;
  maxEbsBandwidthMbps?: number;
  maxIops?: number;
  hasGpu: boolean;
  gpuModel?: string;
  gpuCount?: number;
  gpuMemoryGb?: number;
  hasFpga: boolean;
  virtualizationType: string;
  enhancedNetworking: boolean;
  placementGroupSupported: boolean;
  dedicatedHostSupported: boolean;
  physicalProcessor?: string;
  availability: InstanceAvailability;
  createdAt: string;
  updatedAt: string;
}

export type InstanceCategory = 
  | 'General Purpose'
  | 'Compute Optimized'
  | 'Memory Optimized'
  | 'Storage Optimized'
  | 'Accelerated Computing'
  | 'High Performance Computing'
  | 'Burstable';

export type InstanceAvailability = 'Available' | 'Limited' | 'Deprecated' | 'Unavailable';

// ========== ЦЕНООБРАЗОВАНИЕ ==========

export interface InstancePricing {
  id: string;
  instanceTypeId: string;
  currency: string;
  onDemandHourly: number;
  onDemandMonthly: number;
  spotCurrentPrice?: number;
  spotAveragePrice?: number;
  spotMinPrice?: number;
  spotMaxPrice?: number;
  spotSavingsPercent?: number;
  spotInterruptionRate?: number;
  reserved1YearNoUpfront?: number;
  reserved1YearPartialUpfront?: number;
  reserved1YearAllUpfront?: number;
  reserved1YearSavingsPercent?: number;
  reserved3YearNoUpfront?: number;
  reserved3YearPartialUpfront?: number;
  reserved3YearAllUpfront?: number;
  reserved3YearSavingsPercent?: number;
  storageGbMonthly?: number;
  dataTransferOutGb?: number;
  dataTransferInGb?: number;
  dataTransferInterRegionGb?: number;
  staticIpMonthly?: number;
  loadBalancerHourly?: number;
  effectiveDate: string;
  expirationDate?: string;
}

// ========== СЕРВИСЫ ==========

export interface CloudService {
  id: string;
  providerId: string;
  code: string;
  name: string;
  serviceType: ServiceType;
  description?: string;
  documentationUrl?: string;
  pricingModel?: string;
  freeTier?: string;
  slaInfo?: string;
  createdAt: string;
  updatedAt: string;
}

export type ServiceType = 
  | 'Compute'
  | 'Storage'
  | 'Database'
  | 'Network'
  | 'Container'
  | 'Serverless'
  | 'Analytics'
  | 'AI/ML'
  | 'Security'
  | 'Management';

// ========== СКИДКИ ==========

export interface Discount {
  id: string;
  providerId: string;
  name: string;
  description?: string;
  discountType: DiscountType;
  conditions?: string;
  discountPercent: number;
  appliesTo?: string;
  minimumSpend?: number;
  maximumDiscount?: number;
  promoCode?: string;
  validFrom: string;
  validUntil?: string;
  status: DiscountStatus;
  priority: number;
}

export type DiscountType = 
  | 'VolumeDiscount'
  | 'CommitmentDiscount'
  | 'Promotional'
  | 'EnterpriseAgreement'
  | 'StartupProgram'
  | 'Educational'
  | 'NonProfit';

export type DiscountStatus = 'Active' | 'Expired' | 'Suspended' | 'Pending';

// ========== СРАВНЕНИЕ ЦЕН ==========

export interface ProviderPriceComparison {
  cpu: number;
  memoryGb: number;
  comparedAt: string;
  options: ProviderPriceOption[];
  bestOption?: ProviderPriceOption;
  cheapestOption?: ProviderPriceOption;
  bestPerformanceOption?: ProviderPriceOption;
}

export interface ProviderPriceOption {
  providerCode: string;
  providerName: string;
  regionCode: string;
  regionName: string;
  instanceType: string;
  instanceCategory: string;
  vcpu: number;
  memoryGb: number;
  onDemandHourly: number;
  onDemandMonthly: number;
  spotHourly?: number;
  spotSavingsPercent?: number;
  reserved1YearHourly?: number;
  reserved3YearHourly?: number;
  networkBandwidthGbps: number;
  currency: string;
  score: number;
}

// ========== ЛУЧШИЕ ПРЕДЛОЖЕНИЯ ==========

export interface BestPriceOffer {
  providerCode: string;
  providerName: string;
  regionCode: string;
  regionName: string;
  instanceType: string;
  vcpu: number;
  memoryGb: number;
  pricingModel: string;
  hourlyPrice: number;
  monthlyPrice: number;
  savingsPercent: number;
  currency: string;
  features: string[];
  metadata: Record<string, any>;
}

// ========== СИНХРОНИЗАЦИЯ ==========

export interface SyncResult {
  providerId: string;
  providerCode: string;
  success: boolean;
  startedAt: string;
  completedAt?: string;
  duration: string;
  errorMessage?: string;
  statistics: SyncStatistics;
}

export interface SyncStatistics {
  regionsAdded: number;
  regionsUpdated: number;
  servicesAdded: number;
  servicesUpdated: number;
  instanceTypesAdded: number;
  instanceTypesUpdated: number;
  pricingsUpdated: number;
  discountsAdded: number;
  discountsUpdated: number;
  totalChanges: number;
}

export interface SyncStatus {
  providerId: string;
  providerCode: string;
  lastSyncAt?: string;
  lastSyncSuccess: boolean;
  lastSyncError?: string;
  isRunning: boolean;
  nextSyncAt?: string;
  lastStatistics?: SyncStatistics;
}

// ========== КЭШ ==========

export interface CacheStatistics {
  cachedProviders: number;
  cachedRegions: number;
  cachedInstanceTypes: number;
  cachedPricings: number;
  totalCacheSizeBytes: number;
  lastCleanup: string;
  hitsPerKey: Record<string, number>;
  missesPerKey: Record<string, number>;
  hitRatio: number;
}

// ========== ЗАПРОСЫ ==========

export interface ComparePricesRequest {
  cpu: number;
  memory: number;
  providers?: string[];
}

export interface BestOffersRequest {
  cpu: number;
  memory: number;
  region?: string;
  includeSpot: boolean;
  includeReserved: boolean;
}

export interface InstanceTypesFilter {
  providerCode: string;
  regionCode?: string;
  minCpu?: number;
  maxCpu?: number;
  minMemory?: number;
  maxMemory?: number;
  category?: string;
  forceRefresh?: boolean;
}

export interface RecommendationsRequest {
  cpu: number;
  memory: number;
  providers?: string[];
  preferredRegion?: string;
  preferredCategory?: string;
  maxBudget?: number;
  includeSpot: boolean;
  includeReserved: boolean;
}

export interface InstanceRecommendation {
  providerCode: string;
  providerName: string;
  regionCode: string;
  regionName: string;
  instanceType: string;
  vcpu: number;
  memoryGb: number;
  category: string;
  onDemandHourly: number;
  onDemandMonthly: number;
  spotHourly?: number;
  currency: string;
  matchScore: number;
  features: string[];
}

// ========== КОНСТАНТЫ ДЛЯ РЕГИОНОВ ==========

export const REGION_CONTINENTS: Record<string, string> = {
  'North America': '🌎',
  'South America': '🌎',
  'Europe': '🌍',
  'Asia': '🌏',
  'Oceania': '🌏',
  'Africa': '🌍',
  'Middle East': '🌍',
};

export const INSTANCE_CATEGORY_COLORS: Record<InstanceCategory, string> = {
  'General Purpose': '#10B981',
  'Compute Optimized': '#3B82F6',
  'Memory Optimized': '#8B5CF6',
  'Storage Optimized': '#F59E0B',
  'Accelerated Computing': '#EF4444',
  'High Performance Computing': '#EC4899',
  'Burstable': '#6B7280',
};
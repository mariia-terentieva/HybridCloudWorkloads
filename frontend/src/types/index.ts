export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
}

// НОВЫЕ ТИПЫ для классификации
export type UsagePattern = 'Constant' | 'Periodic' | 'Burst' | 'Unpredictable';
export type CriticalityClass = 'MissionCritical' | 'BusinessEssential' | 'NonCritical';
export type BudgetTier = 'High' | 'Medium' | 'Low';

export interface SlaRequirement {
  maxResponseTimeMs: number;
  allowedDowntimePerMonth: number;
  availabilityTarget: number;
  requiresRedundancy: boolean;
  minReplicas: number;
  maxRecoveryTimeMinutes: number;
}

export interface BusinessHours {
  timezone: string;
  peakHours: TimeRange[];
  weekendLoadPercent: number;
  workingDays: number[]; // 1-7, где 1=Monday
}

export interface TimeRange {
  start: string;
  end: string;
}

export interface BaselinePerformance {
  avgCpuPercent: number;
  avgMemoryPercent: number;
  peakCpuPercent: number;
  peakMemoryPercent: number;
  avgResponseTimeMs: number;
  p95ResponseTimeMs: number;
  requestsPerSecond: number;
  measuredAt: string;
  sampleCount: number;
}

export interface Workload {
  id: string;
  name: string;
  description?: string;
  type: string;
  requiredCpu: number;
  requiredMemory: number;
  requiredStorage: number;
  createdAt: string;
  updatedAt: string;
  
  // Существующие поля для деплоя
  containerImage?: string;
  exposedPort?: number;
  environmentVariables?: string;
  deploymentStatus?: string;
  containerId?: string;
  accessUrl?: string;
  deployedAt?: string;
  
  // НОВЫЕ ПОЛЯ
  usagePattern: UsagePattern;
  criticality: CriticalityClass;
  budgetTier: BudgetTier;
  slaRequirements?: SlaRequirement;
  businessHours?: BusinessHours;
  tags?: string[];
  lastProfiledAt?: string;
  baselinePerformance?: BaselinePerformance;
}

export interface CreateWorkloadRequest {
  name: string;
  description?: string;
  type: string;
  requiredCpu: number;
  requiredMemory: number;
  requiredStorage: number;
  
  // Существующие поля для деплоя
  containerImage?: string;
  exposedPort?: number;
  environmentVariables?: string;
  
  // НОВЫЕ ПОЛЯ
  usagePattern: UsagePattern;
  criticality: CriticalityClass;
  budgetTier: BudgetTier;
  slaRequirements?: SlaRequirement;
  businessHours?: BusinessHours;
  tags?: string[];
}

export interface UpdateWorkloadRequest extends CreateWorkloadRequest {}

// Профиль для модуля оптимизации
export interface WorkloadProfileForOptimization {
  id: string;
  name: string;
  requiredCpu: number;
  requiredMemory: number;
  requiredStorage: number;
  usagePattern: UsagePattern;
  criticality: CriticalityClass;
  budgetTier: BudgetTier;
  slaRequirements?: SlaRequirement;
  businessHours?: BusinessHours;
  tags?: string[];
  lastProfiledAt?: string;
  baselinePerformance?: BaselinePerformance;
}

// Остальные существующие интерфейсы
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface AuthResponse {
  token: string;
  expiration: string;
  user: User;
}

export interface EnvironmentVariable {
  key: string;
  value: string;
}

export interface DeploymentResponse {
  success: boolean;
  message: string;
  accessUrl?: string;
  containerId?: string;
  deployedAt?: string;
}

export interface ContainerStatus {
  id: string;
  name: string;
  state: string;
  status: string;
  created: string;
  startedAt?: string;
  finishedAt?: string;
  exitCode?: number;
  error?: string;
  ports: string[];
}

// Метрики производительности
export interface PerformanceMetric {
  id: string;
  workloadId: string;
  timestamp: string;
  cpuUsagePercent: number;
  memoryUsagePercent: number;
  memoryUsageMB: number;
  networkInBytesPerSec: number;
  networkOutBytesPerSec: number;
  diskReadOpsPerSec: number;
  diskWriteOpsPerSec: number;
  responseTimeMs: number;
  requestsPerSecond: number;
  errorCount: number;
  containerStatus?: string;
}

export interface AggregatedMetrics {
  workloadId: string;
  workloadName: string;
  periodStart: string;
  periodEnd: string;
  periodType: string;
  
  avgCpu: number;
  avgMemory: number;
  peakCpu: number;
  peakMemory: number;
  p95Cpu: number;
  p95Memory: number;
  
  avgResponseTime: number;
  p95ResponseTime: number;
  p99ResponseTime: number;
  
  avgRps: number;
  peakRps: number;
  
  totalNetworkIn: number;
  totalNetworkOut: number;
  
  availability: number;
  errorCount: number;
  sampleCount: number;
  
  timeSeriesData: ChartDataPoint[];
}

export interface ChartDataPoint {
  timestamp: string;
  cpu: number;
  memory: number;
  responseTime: number;
  requests: number;
}

export interface PerformanceProfile {
  calculatedAt: string;
  daysOfData: number;
  totalSamples: number;
  baselineCpu: number;
  baselineMemory: number;
  baselineResponseTime: number;
  hasDailyPattern: boolean;
  hasWeeklyPattern: boolean;
  peakPeriods: PeakPeriod[];
  recommendations: string[];
}

export interface PeakPeriod {
  pattern: string;
  timeRange: string;
  avgLoad: number;
  peakLoad: number;
}

export interface BatchMetricsRequest {
  workloadIds: string[];
  from?: string;
  to?: string;
}
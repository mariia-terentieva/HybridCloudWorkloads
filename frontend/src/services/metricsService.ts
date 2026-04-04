import { api } from './api';
import { 
  AggregatedMetrics, 
  PerformanceMetric, 
  ChartDataPoint,
  PerformanceProfile,
  BatchMetricsRequest 
} from '../types';

export const metricsService = {
  // Получить последние метрики
  getLatestMetrics: async (workloadId: string, count = 100): Promise<PerformanceMetric[]> => {
    const response = await api.get(`/metrics/workload/${workloadId}/latest`, {
      params: { count }
    });
    return response.data;
  },

  // Получить агрегированные метрики за период
  getAggregatedMetrics: async (
    workloadId: string,
    from?: Date,
    to?: Date,
    periodType = 'Day'
  ): Promise<AggregatedMetrics> => {
    const params: any = { periodType };
    if (from) params.from = from.toISOString();
    if (to) params.to = to.toISOString();
    
    const response = await api.get(`/metrics/workload/${workloadId}/aggregated`, { params });
    return response.data;
  },

  // Получить метрики для нескольких workloads
  getBatchMetrics: async (request: BatchMetricsRequest): Promise<Record<string, AggregatedMetrics>> => {
    const response = await api.post('/metrics/workloads/batch', request);
    return response.data;
  },

  // Получить временной ряд для графика
  getTimeSeries: async (
    workloadId: string,
    from: Date,
    to: Date,
    interval = '1h'
  ): Promise<ChartDataPoint[]> => {
    const response = await api.get(`/metrics/workload/${workloadId}/timeseries`, {
      params: {
        from: from.toISOString(),
        to: to.toISOString(),
        interval
      }
    });
    return response.data;
  },

  // Получить профиль производительности
  getPerformanceProfile: async (workloadId: string): Promise<PerformanceProfile> => {
    const response = await api.get(`/metrics/workload/${workloadId}/profile`);
    return response.data;
  },

  // Добавить метрику (для тестирования)
  addMetric: async (workloadId: string, metric: Partial<PerformanceMetric>): Promise<void> => {
    await api.post(`/metrics/workload/${workloadId}`, metric);
  }
};
import { api } from './api';
import { OptimizationReadyProfile } from '../types';

export const profileService = {
  // Экспорт профиля в JSON
  exportToJson: async (workloadId: string): Promise<string> => {
    try {
      const response = await api.get(`/profile/${workloadId}/json`);
      return typeof response.data === 'string' ? response.data : JSON.stringify(response.data);
    } catch (error: any) {
      console.error('Export to JSON error:', error);
      throw new Error(error?.response?.data?.error || 'Ошибка экспорта в JSON');
    }
  },

  // Экспорт профиля в CSV
  exportToCsv: async (workloadId: string): Promise<Blob> => {
    try {
      const response = await api.get(`/profile/${workloadId}/csv`, {
        responseType: 'blob'
      });
      return response.data;
    } catch (error: any) {
      console.error('Export to CSV error:', error);
      throw new Error(error?.response?.data?.error || 'Ошибка экспорта в CSV');
    }
  },

  // Экспорт нескольких профилей в CSV
  exportBatchToCsv: async (workloadIds: string[]): Promise<Blob> => {
    try {
      const response = await api.post('/profile/batch/csv', { workloadIds }, {
        responseType: 'blob'
      });
      return response.data;
    } catch (error: any) {
      console.error('Batch export to CSV error:', error);
      throw new Error(error?.response?.data?.error || 'Ошибка массового экспорта');
    }
  },

  // Получить компактный профиль для оптимизации
  getOptimizationProfile: async (workloadId: string): Promise<OptimizationReadyProfile> => {
    const response = await api.get(`/profile/${workloadId}/optimization`);
    return response.data;
  },

  // Получить компактные профили для нескольких workloads
  getBatchOptimizationProfiles: async (workloadIds: string[]): Promise<OptimizationReadyProfile[]> => {
    const response = await api.post('/profile/batch/optimization', { workloadIds });
    return response.data;
  },

  // Получить все профили пользователя
  getAllOptimizationProfiles: async (): Promise<OptimizationReadyProfile[]> => {
    const response = await api.get('/profile/all/optimization');
    return response.data;
  },

  // Экспорт в формате Prometheus
  exportToPrometheus: async (workloadId: string): Promise<string> => {
    const response = await api.get(`/profile/${workloadId}/prometheus`);
    return response.data;
  },

  // Скачать CSV файл
  downloadCsv: (blob: Blob, filename: string) => {
    // Создаем URL для blob
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename.endsWith('.csv') ? filename : `${filename}.csv`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    window.URL.revokeObjectURL(url);
  },

  //экспорт всех нагрузок в JSON
exportBatchToJson: async (workloadIds: string[]): Promise<string> => {
  try {
    const response = await api.post('/profile/batch/json', { workloadIds });
    return typeof response.data === 'string' ? response.data : JSON.stringify(response.data);
  } catch (error: any) {
    console.error('Batch export to JSON error:', error);
    throw new Error(error?.response?.data?.error || 'Ошибка массового экспорта в JSON');
  }
}
};
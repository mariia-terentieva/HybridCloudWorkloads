import { api } from './api';
import { Workload, CreateWorkloadRequest, UpdateWorkloadRequest, DeploymentResponse, ContainerStatus } from '../types';

export const workloadService = {
  getAll: async (search?: string): Promise<Workload[]> => {
    const params = search ? { search } : {};
    const response = await api.get<Workload[]>('/workloads', { params });
    return response.data;
  },

  getById: async (id: string): Promise<Workload> => {
    const response = await api.get<Workload>(`/workloads/${id}`);
    return response.data;
  },

  create: async (workload: CreateWorkloadRequest): Promise<Workload> => {
    const response = await api.post<Workload>('/workloads', workload);
    return response.data;
  },

  update: async (id: string, workload: UpdateWorkloadRequest): Promise<void> => {
    await api.put(`/workloads/${id}`, workload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/workloads/${id}`);
  },

  // Новые методы для деплоя
  deploy: async (workloadId: string): Promise<DeploymentResponse> => {
    const response = await api.post<DeploymentResponse>(`/deployment/deploy/${workloadId}`);
    return response.data;
  },

  getDeploymentStatus: async (workloadId: string): Promise<ContainerStatus> => {
    const response = await api.get<ContainerStatus>(`/deployment/status/${workloadId}`);
    return response.data;
  },

  stopDeployment: async (workloadId: string): Promise<void> => {
    await api.post(`/deployment/stop/${workloadId}`);
  },

  removeDeployment: async (workloadId: string): Promise<void> => {
    await api.delete(`/deployment/remove/${workloadId}`);
  },

  getMyDeployments: async (): Promise<Workload[]> => {
    const response = await api.get<Workload[]>('/deployment/my-deployments');
    return response.data;
  },
};
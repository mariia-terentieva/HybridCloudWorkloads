export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
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
  
  // Новые поля для деплоя
  containerImage?: string;
  exposedPort?: number;
  environmentVariables?: string;
  deploymentStatus?: string;
  containerId?: string;
  accessUrl?: string;
  deployedAt?: string;
}

export interface CreateWorkloadRequest {
  name: string;
  description?: string;
  type: string;
  requiredCpu: number;
  requiredMemory: number;
  requiredStorage: number;
  containerImage?: string;
  exposedPort?: number;
  environmentVariables?: string;
}

export interface UpdateWorkloadRequest extends CreateWorkloadRequest {}

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

// Новые типы для деплоя
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
import { api } from './api';
import { LoginRequest, RegisterRequest, AuthResponse } from '../types';

export const authService = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', credentials);
    return response.data;
  },

  register: async (userData: RegisterRequest): Promise<void> => {
    await api.post('/auth/register', userData);
  },

    updateProfile: async (profileData: UpdateProfileRequest): Promise<User> => {
    const response = await api.put<{ user: User }>('/auth/profile', profileData);
    return response.data.user;
  },

  changePassword: async (passwordData: ChangePasswordRequest): Promise<void> => {
    await api.put('/auth/change-password', passwordData);
  },
};
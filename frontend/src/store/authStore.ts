import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { User, AuthResponse } from '../types';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (response: AuthResponse) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,
      login: (response: AuthResponse) => 
        set({ 
          user: response.user, 
          token: response.token,
          isAuthenticated: true 
        }),
        updateUser: (user: User) => 
          set(state => ({ 
            ...state, 
            user: { ...state.user, ...user } 
          })),
      logout: () => 
        set({ 
          user: null, 
          token: null,
          isAuthenticated: false 
        }),
    }),
    {
      name: 'auth-storage',
    }
  )
);
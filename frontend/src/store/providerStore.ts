import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { CloudProvider, CloudRegion, InstanceType } from '../types/providers';
import { providerService } from '../services/providerService';

interface ProviderState {
  // Данные
  providers: CloudProvider[];
  selectedProvider: CloudProvider | null;
  regions: Record<string, CloudRegion[]>;
  instanceTypes: Record<string, InstanceType[]>;
  
  // Состояние загрузки
  isLoading: boolean;
  error: string | null;
  
  // Действия
  fetchProviders: (includeInactive?: boolean) => Promise<void>;
  fetchRegions: (providerCode: string, forceRefresh?: boolean) => Promise<void>;
  fetchInstanceTypes: (providerCode: string, regionCode?: string) => Promise<void>;
  setSelectedProvider: (provider: CloudProvider | null) => void;
  clearError: () => void;
  
  // Селекторы
  getProviderByCode: (code: string) => CloudProvider | undefined;
  getRegionsForProvider: (providerCode: string) => CloudRegion[];
  getInstanceTypesForRegion: (providerCode: string, regionCode: string) => InstanceType[];
}

export const useProviderStore = create<ProviderState>()(
  persist(
    (set, get) => ({
      // Начальное состояние
      providers: [],
      selectedProvider: null,
      regions: {},
      instanceTypes: {},
      isLoading: false,
      error: null,

      // Действия
      fetchProviders: async (includeInactive = false) => {
        set({ isLoading: true, error: null });
        try {
          const providers = await providerService.getProviders(includeInactive);
          set({ providers, isLoading: false });
        } catch (error: any) {
          set({
            error: error.response?.data?.message || 'Ошибка загрузки провайдеров',
            isLoading: false,
          });
        }
      },

      fetchRegions: async (providerCode: string, forceRefresh = false) => {
        set({ isLoading: true, error: null });
        try {
          const regions = await providerService.getRegions(providerCode, forceRefresh);
          set((state) => ({
            regions: { ...state.regions, [providerCode]: regions },
            isLoading: false,
          }));
        } catch (error: any) {
          set({
            error: error.response?.data?.message || 'Ошибка загрузки регионов',
            isLoading: false,
          });
        }
      },

      fetchInstanceTypes: async (providerCode: string, regionCode?: string) => {
        set({ isLoading: true, error: null });
        try {
          const instanceTypes = await providerService.getInstanceTypes({
            providerCode,
            regionCode,
          });
          const key = regionCode ? `${providerCode}:${regionCode}` : providerCode;
          set((state) => ({
            instanceTypes: { ...state.instanceTypes, [key]: instanceTypes },
            isLoading: false,
          }));
        } catch (error: any) {
          set({
            error: error.response?.data?.message || 'Ошибка загрузки типов инстансов',
            isLoading: false,
          });
        }
      },

      setSelectedProvider: (provider) => set({ selectedProvider: provider }),
      
      clearError: () => set({ error: null }),

      // Селекторы
      getProviderByCode: (code) => {
        return get().providers.find((p) => p.code === code);
      },

      getRegionsForProvider: (providerCode) => {
        return get().regions[providerCode] || [];
      },

      getInstanceTypesForRegion: (providerCode, regionCode) => {
        const key = `${providerCode}:${regionCode}`;
        return get().instanceTypes[key] || [];
      },
    }),
    {
      name: 'provider-storage',
      partialize: (state) => ({
        providers: state.providers,
        regions: state.regions,
      }),
    }
  )
);
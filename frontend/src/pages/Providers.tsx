import React, { useEffect, useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Grid,
  Paper,
  Tabs,
  Tab,
  Button,
  Alert,
  Snackbar,
  CircularProgress,
  Divider,
  Tooltip,
} from '@mui/material';
import {
  Cloud,
  Refresh,
  CompareArrows,
  Storage as StorageIcon,
  Home,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useProviderStore } from '../store/providerStore';
import { providerService } from '../services/providerService';
import { ProviderCard } from '../components/ProviderCard';
import { PriceComparison } from '../components/PriceComparison';
import { CloudProvider, SyncStatus } from '../types/providers';
import { ProviderRegions } from '../components/ProviderRegions';
import { InstanceTypesList } from '../components/InstanceTypesList';

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index} style={{ marginTop: 24 }}>
    {value === index && children}
  </div>
);

export const Providers: React.FC = () => {
  const navigate = useNavigate();
  const [tabValue, setTabValue] = useState(0);
  const [syncStatuses, setSyncStatuses] = useState<Record<string, SyncStatus>>({});
  const [syncingProvider, setSyncingProvider] = useState<string | null>(null);
  const [snackbar, setSnackbar] = useState<{
    open: boolean;
    message: string;
    severity: 'success' | 'error' | 'info';
  }>({ open: false, message: '', severity: 'info' });

  const {
    providers,
    selectedProvider,
    isLoading,
    error,
    fetchProviders,
    setSelectedProvider,
  } = useProviderStore();

  useEffect(() => {
    fetchProviders();
  }, []);

  useEffect(() => {
    const loadSyncStatuses = async () => {
      for (const provider of providers) {
        if (provider.syncEnabled) {
          try {
            const status = await providerService.getSyncStatus(provider.code);
            setSyncStatuses((prev) => ({ ...prev, [provider.code]: status }));
          } catch (error) {
            console.error(`Failed to get sync status for ${provider.code}`, error);
          }
        }
      }
    };
    if (providers.length > 0) {
      loadSyncStatuses();
    }
  }, [providers]);

  const handleSync = async (providerCode: string) => {
    setSyncingProvider(providerCode);
    try {
      const result = await providerService.syncProvider(providerCode);
      setSnackbar({
        open: true,
        message: `Синхронизация ${providerCode} завершена. Изменений: ${result.statistics.totalChanges}`,
        severity: result.success ? 'success' : 'error',
      });
      
      const status = await providerService.getSyncStatus(providerCode);
      setSyncStatuses((prev) => ({ ...prev, [providerCode]: status }));
      
      await fetchProviders();
    } catch (error: any) {
      setSnackbar({
        open: true,
        message: error.message || 'Ошибка синхронизации',
        severity: 'error',
      });
    } finally {
      setSyncingProvider(null);
    }
  };

  const handleSyncAll = async () => {
    setSyncingProvider('all');
    try {
      const results = await providerService.syncAllProviders();
      const successCount = Object.values(results).filter((r) => r.success).length;
      setSnackbar({
        open: true,
        message: `Синхронизация завершена. Успешно: ${successCount}/${Object.keys(results).length}`,
        severity: 'success',
      });
      await fetchProviders();
    } catch (error: any) {
      setSnackbar({
        open: true,
        message: error.message || 'Ошибка синхронизации',
        severity: 'error',
      });
    } finally {
      setSyncingProvider(null);
    }
  };

  const handleSelectProvider = (provider: CloudProvider) => {
    setSelectedProvider(provider);
    setTabValue(1);
  };

  const handleBackToProviders = () => {
    setSelectedProvider(null);
    setTabValue(0);
  };

  if (isLoading && providers.length === 0) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  return (
    <Container maxWidth="xl" sx={{ mt: 4, mb: 4 }}>
      {/* Заголовок с навигацией */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={2}>
          <Box display="flex" alignItems="center" gap={2}>
            <Cloud sx={{ fontSize: 40 }} color="primary" />
            <Box>
              <Typography variant="h4" component="h1">
                Облачные провайдеры
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Управление провайдерами и сравнение цен
              </Typography>
            </Box>
          </Box>
          
          {/* === НАВИГАЦИОННЫЕ КНОПКИ === */}
          <Box display="flex" gap={1}>
            <Tooltip title="На главную">
              <Button
                variant="outlined"
                startIcon={<Home />}
                onClick={() => navigate('/dashboard')}
              >
                Дашборд
              </Button>
            </Tooltip>
            
            <Tooltip title="Управление нагрузками">
              <Button
                variant="outlined"
                startIcon={<StorageIcon />}
                onClick={() => navigate('/workloads')}
              >
                Нагрузки
              </Button>
            </Tooltip>
            
            <Tooltip title="Активные развертывания">
              <Button
                variant="outlined"
                startIcon={<Cloud />}
                onClick={() => navigate('/deployments')}
              >
                Развертывания
              </Button>
            </Tooltip>
            
            <Button
              variant="outlined"
              startIcon={<CompareArrows />}
              onClick={() => setTabValue(2)}
            >
              Сравнение цен
            </Button>
            
            <Button
              variant="contained"
              startIcon={<Refresh />}
              onClick={handleSyncAll}
              disabled={syncingProvider === 'all'}
            >
              {syncingProvider === 'all' ? 'Синхронизация...' : 'Синхронизировать всё'}
            </Button>
          </Box>
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}


      {/* Табы */}
      <Paper sx={{ mb: 3 }}>
        <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
          <Tab label="Провайдеры" />
          <Tab label="Регионы и инстансы" disabled={!selectedProvider} />
          <Tab label="Типы инстансов" disabled={!selectedProvider} />
          <Tab label="Сравнение цен" />
        </Tabs>
      </Paper>

      {/* Панель провайдеров */}
      <TabPanel value={tabValue} index={0}>
        <Grid container spacing={3}>
          {providers.map((provider) => (
            <Grid item xs={12} sm={6} md={4} key={provider.id}>
              <ProviderCard
                provider={provider}
                syncStatus={syncStatuses[provider.code]}
                onSync={handleSync}
                onSelect={handleSelectProvider}
                isLoading={syncingProvider === provider.code}
              />
            </Grid>
          ))}
        </Grid>
      </TabPanel>

      {/* Панель регионов и инстансов */}
      <TabPanel value={tabValue} index={1}>
        {selectedProvider && (
          <>
            <Box mb={3}>
              <Typography variant="h5" gutterBottom>
                {selectedProvider.displayName}
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                {selectedProvider.description}
              </Typography>
              <Divider />
            </Box>
            <ProviderRegions 
              providerCode={selectedProvider.code}
              providerName={selectedProvider.displayName}
            />
          </>
        )}
      </TabPanel>
      {/* Панель типов инстансов */}
      <TabPanel value={tabValue} index={2}>
        {selectedProvider && (
          <InstanceTypesList
            providerCode={selectedProvider.code}
            providerName={selectedProvider.displayName}
          />
        )}
      </TabPanel>

      {/* Панель сравнения цен */}
      <TabPanel value={tabValue} index={3}>
        <PriceComparison />
      </TabPanel>

      {/* Уведомления */}
      <Snackbar
        open={snackbar.open}
        autoHideDuration={6000}
        onClose={() => setSnackbar((prev) => ({ ...prev, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity={snackbar.severity}
          onClose={() => setSnackbar((prev) => ({ ...prev, open: false }))}
          variant="filled"
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </Container>
  );
};
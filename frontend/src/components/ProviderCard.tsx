import React from 'react';
import {
  Card,
  CardContent,
  CardActions,
  Typography,
  Box,
  Chip,
  IconButton,
  Tooltip,
  LinearProgress,
  Avatar,
} from '@mui/material';
import {
  Sync,
  CheckCircle,
  Error as ErrorIcon,
  Storage,
  Public,
} from '@mui/icons-material';
import { CloudProvider, PROVIDER_COLORS, getProviderInitials } from '../types/providers';

interface ProviderCardProps {
  provider: CloudProvider;
  syncStatus?: {
    isRunning: boolean;
    lastSyncSuccess: boolean;
    lastSyncAt?: string;
  };
  onSync: (providerCode: string) => void;
  onSelect: (provider: CloudProvider) => void;
  isLoading?: boolean;
}

export const ProviderCard: React.FC<ProviderCardProps> = ({
  provider,
  syncStatus,
  onSync,
  onSelect,
  isLoading,
}) => {
  const getStatusIcon = () => {
    if (syncStatus?.isRunning) {
      return (
        <Sync 
          sx={{ 
            fontSize: 16, 
            color: 'info.main', 
            animation: 'spin 2s linear infinite',
            '@keyframes spin': {
              '0%': { transform: 'rotate(0deg)' },
              '100%': { transform: 'rotate(360deg)' },
            },
          }} 
        />
      );
    }
    if (syncStatus?.lastSyncSuccess) {
      return <CheckCircle sx={{ fontSize: 16, color: 'success.main' }} />;
    }
    return <ErrorIcon sx={{ fontSize: 16, color: 'error.main' }} />;
  };

  const formatLastSync = (date?: string) => {
    if (!date) return 'Никогда';
    const diff = Date.now() - new Date(date).getTime();
    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours < 1) return 'Только что';
    if (hours < 24) return `${hours} ч назад`;
    return `${Math.floor(hours / 24)} дн назад`;
  };

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        cursor: 'pointer',
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: 6,
        },
      }}
      onClick={() => onSelect(provider)}
    >
      <CardContent sx={{ flexGrow: 1 }}>
        {/* Заголовок */}
        <Box display="flex" alignItems="center" gap={2} mb={2}>
          <Avatar
            sx={{
              width: 48,
              height: 48,
              bgcolor: PROVIDER_COLORS[provider.code] || 'grey.300',
              color: '#fff',
              fontWeight: 'bold',
              fontSize: provider.code === 'yandex' ? 12 : 16,
            }}
          >
            {getProviderInitials(provider.code)}
          </Avatar>
          <Box flex={1}>
            <Typography variant="h6" component="div">
              {provider.displayName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {provider.code.toUpperCase()}
            </Typography>
          </Box>
        </Box>

        {/* Статус */}
        <Box display="flex" alignItems="center" gap={1} mb={2} flexWrap="wrap">
          <Chip
            size="small"
            label={provider.status === 'Active' ? 'Активен' : provider.status}
            color={provider.status === 'Active' ? 'success' : 'default'}
            variant="outlined"
          />
          {provider.syncEnabled && (
            <Chip
              size="small"
              icon={getStatusIcon()}
              label={`Синк: ${formatLastSync(provider.lastSyncAt)}`}
              variant="outlined"
            />
          )}
        </Box>

        {/* Описание */}
        {provider.description && (
          <Typography variant="body2" color="text.secondary" paragraph>
            {provider.description}
          </Typography>
        )}

        {/* Дополнительная информация */}
        <Box display="flex" flexWrap="wrap" gap={1} mt={1}>
          <Tooltip title="Тип аутентификации">
            <Chip
              size="small"
              icon={<Storage sx={{ fontSize: 14 }} />}
              label={provider.authType || 'не указан'}
              variant="outlined"
            />
          </Tooltip>
          {provider.apiEndpoint && (
            <Tooltip title="API Endpoint">
              <Chip
                size="small"
                icon={<Public sx={{ fontSize: 14 }} />}
                label="API"
                variant="outlined"
              />
            </Tooltip>
          )}
        </Box>
      </CardContent>

      <CardActions sx={{ justifyContent: 'space-between', px: 2, pb: 2 }}>
        <Box display="flex" alignItems="center" gap={1}>
          {provider.syncEnabled && (
            <Tooltip title="Синхронизировать">
              <IconButton
                size="small"
                onClick={(e) => {
                  e.stopPropagation();
                  onSync(provider.code);
                }}
                disabled={isLoading || syncStatus?.isRunning}
              >
                <Sync />
              </IconButton>
            </Tooltip>
          )}
        </Box>
        <Typography variant="caption" color="text.secondary">
          Интервал: {provider.syncIntervalMinutes} мин
        </Typography>
      </CardActions>

      {isLoading && <LinearProgress sx={{ mt: 'auto' }} />}
    </Card>
  );
};
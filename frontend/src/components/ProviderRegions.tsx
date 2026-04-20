import React from 'react';
import { Typography, Paper } from '@mui/material';
import { CloudProvider } from '../types/providers';

interface ProviderRegionsProps {
  provider: CloudProvider;
}

export const ProviderRegions: React.FC<ProviderRegionsProps> = ({ provider }) => {
  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Регионы {provider.displayName}
      </Typography>
      <Typography color="text.secondary">
        Компонент в разработке
      </Typography>
    </Paper>
  );
};
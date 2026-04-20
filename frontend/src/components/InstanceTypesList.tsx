import React from 'react';
import { Typography, Paper } from '@mui/material';

interface InstanceTypesListProps {
  providerCode?: string;
  regionCode?: string;
}

export const InstanceTypesList: React.FC<InstanceTypesListProps> = () => {
  return (
    <Paper sx={{ p: 3 }}>
      <Typography variant="h6" gutterBottom>
        Типы инстансов
      </Typography>
      <Typography color="text.secondary">
        Компонент в разработке
      </Typography>
    </Paper>
  );
};
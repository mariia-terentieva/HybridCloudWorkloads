import React, { useState } from 'react';
import {
  Button,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
  CircularProgress,
  Snackbar,
  Alert,
  IconButton,
  Tooltip,
} from '@mui/material';
import {
  Download,
  FileDownload,
  TableChart,
  DataObject,
} from '@mui/icons-material';
import { profileService } from '../services/profileService';

interface ProfileExportButtonProps {
  workloadId: string;
  workloadName: string;
  variant?: 'button' | 'icon';
  onExportComplete?: () => void;
}

export const ProfileExportButton: React.FC<ProfileExportButtonProps> = ({
  workloadId,
  workloadName,
  variant = 'button',
  onExportComplete,
}) => {
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [loading, setLoading] = useState(false);
  const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
    open: false,
    message: '',
    severity: 'success',
  });

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    event.stopPropagation();
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const showMessage = (message: string, severity: 'success' | 'error') => {
    setSnackbar({ open: true, message, severity });
  };

  const handleExportCsv = async () => {
    if (!workloadId) {
      showMessage('ID нагрузки не указан', 'error');
      return;
    }
    
    setLoading(true);
    handleMenuClose();
    try {
      const blob = await profileService.exportToCsv(workloadId);
      
      // Проверяем, что blob не пустой
      if (!blob || blob.size === 0) {
        throw new Error('Получен пустой файл');
      }
      
      const filename = `workload_${workloadName.replace(/[^a-zа-я0-9]/gi, '_')}_profile.csv`;
      profileService.downloadCsv(blob, filename);
      showMessage('Профиль успешно экспортирован в CSV', 'success');
      onExportComplete?.();
    } catch (error: any) {
      console.error('Export CSV error:', error);
      showMessage(error?.message || 'Ошибка при экспорте профиля', 'error');
    } finally {
      setLoading(false);
    }
  };

  const handleExportJson = async () => {
    if (!workloadId) {
      showMessage('ID нагрузки не указан', 'error');
      return;
    }
    
    setLoading(true);
    handleMenuClose();
    try {
      const json = await profileService.exportToJson(workloadId);
      const blob = new Blob([json], { type: 'application/json' });
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `workload_${workloadName.replace(/[^a-zа-я0-9]/gi, '_')}_profile.json`;
      a.click();
      window.URL.revokeObjectURL(url);
      showMessage('Профиль успешно экспортирован в JSON', 'success');
      onExportComplete?.();
    } catch (error: any) {
      console.error('Export JSON error:', error);
      showMessage(error?.message || 'Ошибка при экспорте профиля', 'error');
    } finally {
      setLoading(false);
    }
  };

  if (!workloadId) {
    return null;
  }

  if (variant === 'icon') {
    return (
      <>
        <Tooltip title="Экспорт профиля">
          <IconButton size="small" onClick={handleMenuOpen} disabled={loading}>
            {loading ? <CircularProgress size={20} /> : <Download />}
          </IconButton>
        </Tooltip>
        <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
          <MenuItem onClick={handleExportJson}>
            <ListItemIcon><DataObject fontSize="small" /></ListItemIcon>
            <ListItemText>JSON</ListItemText>
          </MenuItem>
          <MenuItem onClick={handleExportCsv}>
            <ListItemIcon><TableChart fontSize="small" /></ListItemIcon>
            <ListItemText>CSV</ListItemText>
          </MenuItem>
        </Menu>
        <Snackbar 
          open={snackbar.open} 
          autoHideDuration={3000} 
          onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
          anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        >
          <Alert 
            severity={snackbar.severity} 
            onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
          >
            {snackbar.message}
          </Alert>
        </Snackbar>
      </>
    );
  }

  return (
    <>
      <Button
        variant="outlined"
        startIcon={<FileDownload />}
        onClick={handleMenuOpen}
        disabled={loading}
        size="small"
      >
        {loading ? <CircularProgress size={20} /> : 'Экспорт'}
      </Button>
      <Menu anchorEl={anchorEl} open={Boolean(anchorEl)} onClose={handleMenuClose}>
        <MenuItem onClick={handleExportJson}>
          <ListItemIcon><DataObject fontSize="small" /></ListItemIcon>
          <ListItemText>Экспорт в JSON</ListItemText>
        </MenuItem>
        <MenuItem onClick={handleExportCsv}>
          <ListItemIcon><TableChart fontSize="small" /></ListItemIcon>
          <ListItemText>Экспорт в CSV</ListItemText>
        </MenuItem>
      </Menu>
      <Snackbar 
        open={snackbar.open} 
        autoHideDuration={3000} 
        onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert 
          severity={snackbar.severity} 
          onClose={() => setSnackbar(prev => ({ ...prev, open: false }))}
        >
          {snackbar.message}
        </Alert>
      </Snackbar>
    </>
  );
};
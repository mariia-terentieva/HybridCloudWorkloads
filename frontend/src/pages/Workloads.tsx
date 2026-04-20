import React from 'react';
import {
  Container,
  Paper,
  Typography,
  Button,
  Box,
  TextField,
  Chip,
  Alert,
  Tooltip,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Menu,
  MenuItem,
  ListItemIcon,
  ListItemText,
} from '@mui/material';
import { 
  Add, 
  Edit, 
  Delete, 
  Search,
  PlayArrow,
  Stop,
  Publish,
  Link,
  ArrowBack,
  Replay,
  ShowChart,
  Download,
  DataObject,
  TableChart,
  Timeline,
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { MetricsDashboard } from '../components/MetricsDashboard';
import { WorkloadForm } from '../components/WorkloadForm';
import { workloadService } from '../services/workloadService';
import { Workload, CreateWorkloadRequest, UpdateWorkloadRequest } from '../types';
import { getTypeLabel, getStatusLabel } from '../utils/translations';
import { isWebService } from '../utils/dockerImages';
//import { ProfileExportButton } from '../components/ProfileExportButton';
import { profileService } from '../services/profileService';
import { useNavigate } from 'react-router-dom';
import { Storage } from '@mui/icons-material';   

export const Workloads: React.FC = () => {
  const queryClient = useQueryClient();
  const navigate = useNavigate(); 
  const [search, setSearch] = React.useState('');
  const [formOpen, setFormOpen] = React.useState(false);
  const [editingWorkload, setEditingWorkload] = React.useState<Workload | null>(null);
  const [deployMessage, setDeployMessage] = React.useState<{type: 'success' | 'error', text: string} | null>(null);
  const [metricsDialogOpen, setMetricsDialogOpen] = React.useState(false);
  const [selectedWorkloadForMetrics, setSelectedWorkloadForMetrics] = React.useState<Workload | null>(null);
  const [exportMenuAnchor, setExportMenuAnchor] = React.useState<null | HTMLElement>(null);
  const [exportWorkload, setExportWorkload] = React.useState<Workload | null>(null);
  const [batchExportAnchor, setBatchExportAnchor] = React.useState<null | HTMLElement>(null);

  const { data: workloads, isLoading } = useQuery({
    queryKey: ['workloads', search],
    queryFn: () => workloadService.getAll(search),
  });

  const createMutation = useMutation({
    mutationFn: workloadService.create,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateWorkloadRequest }) =>
      workloadService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: workloadService.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const deployMutation = useMutation({
    mutationFn: workloadService.deploy,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      setDeployMessage({ 
        type: 'success', 
        text: `Успешно развернуто! URL доступа: ${data.accessUrl}` 
      });
      setTimeout(() => setDeployMessage(null), 5000);
    },
    onError: (error: any) => {
      setDeployMessage({ 
        type: 'error', 
        text: `Ошибка развертывания: ${error.response?.data?.message || error.message}` 
      });
    },
  });

  const stopMutation = useMutation({
    mutationFn: workloadService.stopDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
      setDeployMessage({ type: 'success', text: 'Нагрузка успешно остановлена' });
      setTimeout(() => setDeployMessage(null), 3000);
    },
  });

  const removeDeploymentMutation = useMutation({
    mutationFn: workloadService.removeDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      setDeployMessage({ type: 'success', text: 'Развертывание успешно удалено' });
      setTimeout(() => setDeployMessage(null), 3000);
    },
  });

  const handleCreate = (data: CreateWorkloadRequest) => {
    createMutation.mutate(data);
  };

  const handleUpdate = (data: UpdateWorkloadRequest) => {
    if (editingWorkload) {
      updateMutation.mutate({ id: editingWorkload.id, data });
    }
  };

  const handleShowMetrics = (workload: Workload) => {
    setSelectedWorkloadForMetrics(workload);
    setMetricsDialogOpen(true);
  };

  const handleRedeploy = (id: string) => {
    if (window.confirm('Перезапустить это развертывание?')) {
      deployMutation.mutate(id);
    }
  };

  const handleDelete = (id: string) => {
    const workload = workloads?.find(w => w.id === id);
    
    let message = 'Вы уверены, что хотите удалить эту нагрузку?';
  
    if (workload?.deploymentStatus === 'Running') {
      message = 'Нагрузка запущена. При удалении контейнер будет остановлен и удален. Продолжить?';
    }
  
    if (window.confirm(message)) {
      deleteMutation.mutate(id);
    }
  };

  const handleDeploy = (id: string) => {
    if (window.confirm('Развернуть эту нагрузку как Docker контейнер?')) {
      deployMutation.mutate(id);
    }
  };

  const handleStop = (id: string) => {
    if (window.confirm('Остановить это развертывание?')) {
      stopMutation.mutate(id);
    }
  };

  const handleRemoveDeployment = (id: string) => {
    if (window.confirm('Удалить это развертывание?')) {
      removeDeploymentMutation.mutate(id);
    }
  };

  const handleOpenExportMenu = (event: React.MouseEvent<HTMLElement>, workload: Workload) => {
  event.stopPropagation();
  setExportMenuAnchor(event.currentTarget);
  setExportWorkload(workload);
};

const handleCloseExportMenu = () => {
  setExportMenuAnchor(null);
  setExportWorkload(null);
};

const handleExport = async (format: 'csv' | 'json' | 'prometheus') => {
  if (!exportWorkload) return;
  
  try {
    let blob: Blob;
    let filename: string;
    const baseName = `workload_${exportWorkload.name.replace(/[^a-zа-я0-9]/gi, '_')}`;
    
    switch (format) {
      case 'csv':
        blob = await profileService.exportToCsv(exportWorkload.id);
        filename = `${baseName}_profile.csv`;
        profileService.downloadCsv(blob, filename);
        break;
        
      case 'json':
        const json = await profileService.exportToJson(exportWorkload.id);
        blob = new Blob([json], { type: 'application/json' });
        filename = `${baseName}_profile.json`;
        break;
        
      case 'prometheus':
        const prometheus = await profileService.exportToPrometheus(exportWorkload.id);
        blob = new Blob([prometheus], { type: 'text/plain' });
        filename = `${baseName}_metrics.prom`;
        break;
        
      default:
        throw new Error('Unknown format');
    }
    
    // Скачивание для JSON и Prometheus
    if (format !== 'csv') {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      window.URL.revokeObjectURL(url);
    }
    
    setDeployMessage({ 
      type: 'success', 
      text: `Профиль экспортирован в формате ${format.toUpperCase()}` 
    });
    setTimeout(() => setDeployMessage(null), 3000);
  } catch (error: any) {
    console.error('Export failed:', error);
    setDeployMessage({ 
      type: 'error', 
      text: `Ошибка экспорта: ${error.message}` 
    });
    setTimeout(() => setDeployMessage(null), 5000);
  } finally {
    handleCloseExportMenu();
  }
};

const handleOpenBatchExport = (event: React.MouseEvent<HTMLElement>) => {
  setBatchExportAnchor(event.currentTarget);
};

const handleCloseBatchExport = () => {
  setBatchExportAnchor(null);
};

const handleBatchExport = async (format: 'csv' | 'json') => {
  if (!workloads || workloads.length === 0) {
    setDeployMessage({ type: 'error', text: 'Нет нагрузок для экспорта' });
    setTimeout(() => setDeployMessage(null), 3000);
    return;
  }
  
  try {
    const workloadIds = workloads.map(w => w.id);
    const timestamp = new Date().toISOString().split('T')[0];
    let blob: Blob;
    let filename: string;
    
    if (format === 'csv') {
      blob = await profileService.exportBatchToCsv(workloadIds);
      filename = `all_workloads_${timestamp}.csv`;
      profileService.downloadCsv(blob, filename);
    } else {
      const json = await profileService.exportBatchToJson(workloadIds);
      blob = new Blob([json], { type: 'application/json' });
      filename = `all_workloads_${timestamp}.json`;
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      window.URL.revokeObjectURL(url);
    }
    
    setDeployMessage({ 
      type: 'success', 
      text: `Экспортировано ${workloads.length} нагрузок в ${format.toUpperCase()}` 
    });
    setTimeout(() => setDeployMessage(null), 3000);
  } catch (error: any) {
    console.error('Batch export failed:', error);
    setDeployMessage({ 
      type: 'error', 
      text: `Ошибка экспорта: ${error.message}` 
    });
    setTimeout(() => setDeployMessage(null), 5000);
  } finally {
    handleCloseBatchExport();
  }
};

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'Running': return 'success';
      case 'Deploying': return 'info';
      case 'Stopped': return 'warning';
      case 'Error': return 'error';
      default: return 'default';
    }
  };

  const columns: GridColDef[] = [
    { 
      field: 'name', 
      headerName: 'Название', 
      flex: 1,
      renderCell: (params) => (
        <Box>
          <Typography variant="body2">{params.value}</Typography>
          {params.row.accessUrl && isWebService(params.row.containerImage) && (
            <Typography variant="caption" color="primary">
              <Link fontSize="small" sx={{ fontSize: '0.75rem', mr: 0.5 }} />
              <a 
                href={params.row.accessUrl} 
                target="_blank" 
                rel="noopener noreferrer"
                style={{ textDecoration: 'none' }}
                onClick={(e) => e.stopPropagation()}
              >
                {params.row.accessUrl}
              </a>
            </Typography>
          )}
        </Box>
      )
    },
    { 
      field: 'type', 
      headerName: 'Тип', 
      width: 150,
      //valueGetter: (_, row) => getTypeLabel(row.type) 
      renderCell: (params) => (
       <Typography variant="body2">
         {getTypeLabel(params.row.type)}
        </Typography>
      )
    },
    { 
      field: 'deploymentStatus', 
      headerName: 'Статус', 
      width: 130,
      renderCell: (params) => {
        if (!params.value) return null;
        return (
          <Chip 
            label={getStatusLabel(params.value)} 
            size="small" 
            color={getStatusColor(params.value) as any}
            variant="outlined"
          />
        );
      }
    },
    { field: 'requiredCpu', headerName: 'CPU', width: 80 },
    { field: 'requiredMemory', headerName: 'ОЗУ (ГБ)', width: 100 },
    { field: 'requiredStorage', headerName: 'Хранилище (ГБ)', width: 120 },
    { 
      field: 'exposedPort', 
      headerName: 'Порт', 
      width: 80,
      renderCell: (params) => (
        <Typography variant="body2">
          {params.row.exposedPort || 80}
        </Typography>
      )
    },
    {
      field: 'actions',
      type: 'actions',
      headerName: 'Действия',
      width: 230,
      getActions: (params) => {
        const actions = [
          <GridActionsCellItem
            icon={<Edit />}
            label="Редактировать"
            onClick={() => {
              setEditingWorkload(params.row);
              setFormOpen(true);
            }}
          />,
          <GridActionsCellItem
            icon={<ShowChart />}
            label="Метрики"
            onClick={() => handleShowMetrics(params.row)}
            showInMenu
          />,
          <GridActionsCellItem
            icon={<Delete />}
            label="Удалить"
            onClick={() => handleDelete(params.row.id)}
          />,
      <GridActionsCellItem
        icon={<Download />}
        label="Экспорт"
        onClick={(event) => handleOpenExportMenu(event, params.row)}
        showInMenu
      />,
        ];

        // ОБНОВЛЕННАЯ ЛОГИКА ДЛЯ КНОПОК ДЕПЛОЯ
        if (params.row.containerImage) {
          if (!params.row.deploymentStatus || params.row.deploymentStatus === 'NotDeployed') {
            // Для неразвернутых
            actions.unshift(
              <GridActionsCellItem
                icon={<PlayArrow />}
                label="Развернуть"
                onClick={() => handleDeploy(params.row.id)}
                disabled={deployMutation.isPending}
              />
            );
          } else if (params.row.deploymentStatus === 'Running') {
            // Для работающих
            actions.unshift(
              <GridActionsCellItem
                icon={<Stop />}
                label="Остановить"
                onClick={() => handleStop(params.row.id)}
                disabled={stopMutation.isPending}
              />
            );
          } else if (params.row.deploymentStatus === 'Stopped') {
            // НОВОЕ: Для остановленных - кнопка перезапуска
            actions.unshift(
              <GridActionsCellItem
                icon={<Replay />}
                label="Перезапустить"
                onClick={() => handleRedeploy(params.row.id)}
                disabled={deployMutation.isPending}
                sx={{ color: 'primary.main' }}
              />
            );
            actions.unshift(
              <GridActionsCellItem
                icon={<Publish />}
                label="Удалить развертывание"
                onClick={() => handleRemoveDeployment(params.row.id)}
                disabled={removeDeploymentMutation.isPending}
              />
            );
          } else if (params.row.deploymentStatus === 'Error') {
            // НОВОЕ: Для ошибок - кнопка перезапуска
            actions.unshift(
              <GridActionsCellItem
                icon={<Replay />}
                label="Перезапустить"
                onClick={() => handleRedeploy(params.row.id)}
                disabled={deployMutation.isPending}
                sx={{ color: 'primary.main' }}
              />
            );
            actions.unshift(
              <GridActionsCellItem
                icon={<Publish />}
                label="Удалить развертывание"
                onClick={() => handleRemoveDeployment(params.row.id)}
                disabled={removeDeploymentMutation.isPending}
              />
            );
          }
        }

        return actions;
      },
    },
  ];

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper sx={{ p: 2 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Typography variant="h4" component="h1">
            Мои нагрузки
          </Typography>
          <Box display="flex" gap={1}>
            <Button
              variant="outlined"
              startIcon={<ArrowBack />}
              onClick={() => window.location.href = '/dashboard'}
            >
              На главную
            </Button>
  <Button
    variant="outlined"
    startIcon={<Storage />}
    onClick={() => navigate('/providers')}
  >
    Провайдеры
  </Button>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => setFormOpen(true)}
            >
              Новая нагрузка
            </Button>
          </Box>
        </Box>

        {deployMessage && (
          <Alert 
            severity={deployMessage.type} 
            sx={{ mb: 2 }}
            onClose={() => setDeployMessage(null)}
          >
            {deployMessage.text}
          </Alert>
        )}

        <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
          <TextField
            placeholder="Поиск нагрузок..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            InputProps={{
              startAdornment: <Search sx={{ color: 'text.secondary', mr: 1 }} />,
            }}
            sx={{ width: 300 }}
          />
          
<Box display="flex" gap={1}>
  <Button
    variant="outlined"
    startIcon={<Download />}
    onClick={handleOpenBatchExport}
    disabled={!workloads || workloads.length === 0}
  >
    Экспорт
  </Button>
  <Menu
    anchorEl={batchExportAnchor}
    open={Boolean(batchExportAnchor)}
    onClose={handleCloseBatchExport}
  >
    <MenuItem onClick={() => handleBatchExport('csv')}>
      <ListItemIcon><TableChart fontSize="small" /></ListItemIcon>
      <ListItemText>Все в CSV</ListItemText>
    </MenuItem>
    <MenuItem onClick={() => handleBatchExport('json')}>
      <ListItemIcon><DataObject fontSize="small" /></ListItemIcon>
      <ListItemText>Все в JSON</ListItemText>
    </MenuItem>
  </Menu>    
    <Tooltip title="Посмотреть развернутые нагрузки">
      <Button
        variant="outlined"
        onClick={() => window.location.href = '/deployments'}
      >
        Развертывания
      </Button>
    </Tooltip>
    <Tooltip title="Нагрузки с контейнерными образами могут быть развернуты как Docker контейнеры">
      <Chip 
        label={`${workloads?.filter(w => w.containerImage).length || 0} развертываемых`} 
        color="info" 
        variant="outlined"
      />
    </Tooltip>
  </Box>
        </Box>

        <DataGrid
          rows={workloads || []}
          columns={columns}
          loading={isLoading}
          autoHeight
          pageSizeOptions={[5, 10, 25]}
          initialState={{
            pagination: {
              paginationModel: { page: 0, pageSize: 10 },
            },
            sorting: {
              sortModel: [{ field: 'updatedAt', sort: 'desc' }],
            },
          }}
          sx={{
            '& .MuiDataGrid-cell': {
              borderBottom: '1px solid',
              borderColor: 'divider',
            },
          }}
        />
      </Paper>

      <WorkloadForm
        open={formOpen}
        workload={editingWorkload}
        onClose={() => {
          setFormOpen(false);
          setEditingWorkload(null);
        }}
        onSubmit={editingWorkload ? handleUpdate : handleCreate}
      />

<Dialog
  open={metricsDialogOpen}
  onClose={() => setMetricsDialogOpen(false)}
  maxWidth="lg"
  fullWidth
>
  <DialogTitle>
    Метрики производительности
  </DialogTitle>
  <DialogContent dividers>
    {selectedWorkloadForMetrics && (
      <MetricsDashboard
        workloadId={selectedWorkloadForMetrics.id}
        workloadName={selectedWorkloadForMetrics.name}
      />
    )}
  </DialogContent>
  <DialogActions>
    <Button onClick={() => setMetricsDialogOpen(false)}>
      Закрыть
    </Button>
  </DialogActions>
</Dialog>
{/* Меню выбора формата экспорта */}
<Menu
  anchorEl={exportMenuAnchor}
  open={Boolean(exportMenuAnchor)}
  onClose={handleCloseExportMenu}
>
  <MenuItem onClick={() => handleExport('csv')}>
    <ListItemIcon>
      <TableChart fontSize="small" />
    </ListItemIcon>
    <ListItemText>Экспорт в CSV</ListItemText>
  </MenuItem>
  <MenuItem onClick={() => handleExport('json')}>
    <ListItemIcon>
      <DataObject fontSize="small" />
    </ListItemIcon>
    <ListItemText>Экспорт в JSON</ListItemText>
  </MenuItem>
  <MenuItem onClick={() => handleExport('prometheus')}>
    <ListItemIcon>
      <Timeline fontSize="small" />
    </ListItemIcon>
    <ListItemText>Экспорт в Prometheus</ListItemText>
  </MenuItem>
</Menu>
    </Container>
  );
};
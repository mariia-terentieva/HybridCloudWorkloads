import React from 'react';
import {
  Container,
  Paper,
  Typography,
  Button,
  Box,
  TextField,
  IconButton,
  Chip,
  Alert,
  Tooltip,
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
} from '@mui/icons-material';
import { DataGrid, GridColDef, GridActionsCellItem } from '@mui/x-data-grid';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { WorkloadForm } from '../components/WorkloadForm';
import { workloadService } from '../services/workloadService';
import { Workload, CreateWorkloadRequest, UpdateWorkloadRequest } from '../types';
import { getTypeLabel, getStatusLabel } from '../utils/translations';
import { isWebService, getImageLabel } from '../utils/dockerImages';

export const Workloads: React.FC = () => {
  const queryClient = useQueryClient();
  const [search, setSearch] = React.useState('');
  const [formOpen, setFormOpen] = React.useState(false);
  const [editingWorkload, setEditingWorkload] = React.useState<Workload | null>(null);
  const [deployMessage, setDeployMessage] = React.useState<{type: 'success' | 'error', text: string} | null>(null);

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
    { field: 'requiredStorage', headerName: 'Хранилище (ГБ)', width: 100 },
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
      width: 250,
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
            icon={<Delete />}
            label="Удалить"
            onClick={() => handleDelete(params.row.id)}
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
          
          <Box>
            <Tooltip title="Посмотреть развернутые нагрузки">
              <Button
                variant="outlined"
                onClick={() => window.location.href = '/deployments'}
                sx={{ mr: 1 }}
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
    </Container>
  );
};
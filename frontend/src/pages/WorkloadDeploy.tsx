import React from 'react';
import {
  Container,
  Paper,
  Typography,
  Button,
  Box,
  Grid,
  Card,
  CardContent,
  CardActions,
  Chip,
  LinearProgress,
  Alert,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  MenuItem,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Delete,
  OpenInNew,
  Add,
  CloudUpload,
  ArrowBack,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { workloadService } from '../services/workloadService';
import { Workload, UpdateWorkloadRequest } from '../types';
import { isWebService } from '../utils/dockerImages';

interface DeploymentFormData {
  containerImage: string;
  exposedPort: number;
  environmentVariables: string;
}

const predefinedImages = [
  { value: 'nginx:latest', label: 'NGINX Веб-сервер' },
  { value: 'postgres:15', label: 'PostgreSQL База данных' },
  { value: 'redis:alpine', label: 'Redis Кэш' },
  { value: 'mysql:8', label: 'MySQL База данных' },
  { value: 'node:18-alpine', label: 'Node.js Приложение' },
  { value: 'python:3.11-slim', label: 'Python Приложение' },
  { value: 'httpd:alpine', label: 'Apache HTTP Сервер' },
  { value: 'mongo:6', label: 'MongoDB База данных' },
];

export const WorkloadDeploy: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [selectedWorkload, setSelectedWorkload] = React.useState<Workload | null>(null);
  const [deployDialogOpen, setDeployDialogOpen] = React.useState(false);
  const [deployForm, setDeployForm] = React.useState<DeploymentFormData>({
    containerImage: 'nginx:latest',
    exposedPort: 80,
    environmentVariables: '',
  });

  const { data: workloads, isLoading } = useQuery({
    queryKey: ['workloads'],
    queryFn: () => workloadService.getAll(),
  });

  const deployMutation = useMutation({
    mutationFn: async ({ workloadId, formData }: { workloadId: string; formData: DeploymentFormData }) => {
      // Находим workload для обновления
      const workload = workloads?.find(w => w.id === workloadId);
      
      if (workload) {
        // === ИСПРАВЛЕННЫЙ ВЫЗОВ UPDATE ===
        const updateData: UpdateWorkloadRequest = {
          name: workload.name,
          type: workload.type,
          requiredCpu: workload.requiredCpu,
          requiredMemory: workload.requiredMemory,
          requiredStorage: workload.requiredStorage,
          containerImage: formData.containerImage,
          exposedPort: formData.exposedPort,
          environmentVariables: formData.environmentVariables,
          usagePattern: workload.usagePattern,
          criticality: workload.criticality,
          budgetTier: workload.budgetTier,
        };
        
        // Добавляем опциональные поля, если они есть
        if (workload.description) {
          updateData.description = workload.description;
        }
        if (workload.slaRequirements) {
          updateData.slaRequirements = workload.slaRequirements;
        }
        if (workload.businessHours) {
          updateData.businessHours = workload.businessHours;
        }
        if (workload.tags) {
          updateData.tags = workload.tags;
        }
        
        await workloadService.update(workloadId, updateData);
      }

      // Вызываем API деплоя
      const response = await fetch(`/api/deployment/deploy/${workloadId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Ошибка развертывания');
      }
      
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
      setDeployDialogOpen(false);
    },
  });

  const stopMutation = useMutation({
    mutationFn: async (workloadId: string) => {
      const response = await fetch(`/api/deployment/stop/${workloadId}`, {
        method: 'POST',
      });
      if (!response.ok) {
        throw new Error('Ошибка остановки');
      }
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (workloadId: string) => {
      const response = await fetch(`/api/deployment/remove/${workloadId}`, {
        method: 'DELETE',
      });
      if (!response.ok) {
        throw new Error('Ошибка удаления');
      }
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
    },
  });

  const handleDeploy = (workload: Workload) => {
    setSelectedWorkload(workload);
    
    // Предзаполняем форму существующими данными
    setDeployForm({
      containerImage: workload.containerImage || 'nginx:latest',
      exposedPort: workload.exposedPort || 80,
      environmentVariables: workload.environmentVariables || '',
    });
    
    setDeployDialogOpen(true);
  };

  const handleDeploySubmit = () => {
    if (selectedWorkload) {
      deployMutation.mutate({
        workloadId: selectedWorkload.id,
        formData: deployForm,
      });
    }
  };

  const handleStop = (workloadId: string) => {
    if (window.confirm('Остановить это развертывание?')) {
      stopMutation.mutate(workloadId);
    }
  };

  const handleRemove = (workloadId: string) => {
    if (window.confirm('Удалить это развертывание?')) {
      removeMutation.mutate(workloadId);
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

  const getStatusLabel = (status?: string): string => {
    switch (status) {
      case 'Running': return 'Работает';
      case 'Deploying': return 'Развертывается';
      case 'Stopped': return 'Остановлено';
      case 'Error': return 'Ошибка';
      case 'NotDeployed': return 'Не развернуто';
      default: return status || 'Неизвестно';
    }
  };

  const getTypeLabel = (type: string): string => {
    switch (type) {
      case 'VirtualMachine': return 'Виртуальная машина';
      case 'Database': return 'База данных';
      case 'Container': return 'Контейнер';
      case 'WebService': return 'Веб-сервис';
      case 'BatchJob': return 'Пакетное задание';
      default: return type;
    }
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper sx={{ p: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box display="flex" alignItems="center" gap={2}>
            <Typography variant="h4">Развертывание нагрузок</Typography>
          </Box>
          <Box display="flex" gap={1}>
            <Button
              variant="outlined"
              startIcon={<ArrowBack />}
              onClick={() => navigate('/dashboard')}
            >
              На панель
            </Button>
            <Button
              variant="contained"
              startIcon={<CloudUpload />}
              onClick={() => navigate('/workloads')}
            >
              Создать нагрузку
            </Button>
          </Box>
        </Box>

        {isLoading ? (
          <LinearProgress />
        ) : workloads && workloads.length > 0 ? (
          <Grid container spacing={3}>
            {workloads.map((workload) => (
              <Grid item xs={12} md={6} lg={4} key={workload.id}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      {workload.name}
                    </Typography>
                    
                    <Box display="flex" alignItems="center" gap={1} mb={2} flexWrap="wrap">
                      <Chip
                        label={getTypeLabel(workload.type)}
                        size="small"
                        variant="outlined"
                      />
                      {workload.deploymentStatus && (
                        <Chip
                          label={getStatusLabel(workload.deploymentStatus)}
                          size="small"
                          color={getStatusColor(workload.deploymentStatus) as any}
                        />
                      )}
                    </Box>

                    <Typography variant="body2" color="text.secondary" paragraph>
                      CPU: {workload.requiredCpu} ядер | ОЗУ: {workload.requiredMemory} ГБ | Хранилище: {workload.requiredStorage} ГБ
                    </Typography>
                    
                    {workload.containerImage && (
                      <Typography variant="body2" color="text.secondary">
                        Образ: {workload.containerImage} | Порт: {workload.exposedPort || 80}
                      </Typography>
                    )}

                    {workload.accessUrl && isWebService(workload.containerImage) && (
                      <Alert severity="info" sx={{ mb: 2, mt: 2 }}>
                        <Box display="flex" alignItems="center" justifyContent="space-between">
                          <Typography variant="body2">
                            Доступ: {workload.accessUrl}
                          </Typography>
                          <IconButton
                            size="small"
                            onClick={() => window.open(workload.accessUrl, '_blank')}
                          >
                            <OpenInNew fontSize="small" />
                          </IconButton>
                        </Box>
                      </Alert>
                    )}
                  </CardContent>

                  <CardActions>
                    {(!workload.deploymentStatus || workload.deploymentStatus === 'NotDeployed') && (
                      <Button
                        size="small"
                        startIcon={<PlayArrow />}
                        onClick={() => handleDeploy(workload)}
                        disabled={deployMutation.isPending}
                      >
                        Развернуть
                      </Button>
                    )}
                    
                    {workload.deploymentStatus === 'Running' && (
                      <Button
                        size="small"
                        color="warning"
                        startIcon={<Stop />}
                        onClick={() => handleStop(workload.id)}
                        disabled={stopMutation.isPending}
                      >
                        Остановить
                      </Button>
                    )}

                    {(workload.deploymentStatus === 'Stopped' || workload.deploymentStatus === 'Error') && (
                      <>
                        <Button
                          size="small"
                          startIcon={<PlayArrow />}
                          onClick={() => handleDeploy(workload)}
                          disabled={deployMutation.isPending}
                        >
                          Перезапустить
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          startIcon={<Delete />}
                          onClick={() => handleRemove(workload.id)}
                          disabled={removeMutation.isPending}
                        >
                          Удалить
                        </Button>
                      </>
                    )}
                  </CardActions>
                </Card>
              </Grid>
            ))}
          </Grid>
        ) : (
          <Box textAlign="center" py={4}>
            <Typography variant="h6" gutterBottom>
              Нагрузки не найдены
            </Typography>
            <Typography variant="body2" color="text.secondary" paragraph>
              Сначала создайте нагрузку, затем разверните ее для работы на сервере.
            </Typography>
            <Button
              variant="contained"
              startIcon={<Add />}
              onClick={() => navigate('/workloads')}
            >
              Создать нагрузку
            </Button>
          </Box>
        )}
      </Paper>

      {/* Диалог настройки развертывания */}
      <Dialog open={deployDialogOpen} onClose={() => setDeployDialogOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Развернуть нагрузку</DialogTitle>
        <DialogContent>
          {selectedWorkload && (
            <Box pt={1}>
              <Typography variant="subtitle2" gutterBottom>
                Нагрузка: {selectedWorkload.name}
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Требования: {selectedWorkload.requiredCpu} CPU, {selectedWorkload.requiredMemory} ГБ RAM
              </Typography>
              
              <TextField
                select
                fullWidth
                label="Контейнерный образ"
                value={deployForm.containerImage}
                onChange={(e) => setDeployForm({ ...deployForm, containerImage: e.target.value })}
                sx={{ mt: 2 }}
              >
                {predefinedImages.map((image) => (
                  <MenuItem key={image.value} value={image.value}>
                    {image.label}
                  </MenuItem>
                ))}
              </TextField>
              
              <TextField
                fullWidth
                label="Открытый порт"
                type="number"
                value={deployForm.exposedPort}
                onChange={(e) => setDeployForm({ ...deployForm, exposedPort: parseInt(e.target.value) || 80 })}
                sx={{ mt: 2 }}
                inputProps={{ min: 1, max: 65535 }}
              />
              
              <TextField
                fullWidth
                label="Переменные окружения (JSON)"
                multiline
                rows={3}
                value={deployForm.environmentVariables}
                onChange={(e) => setDeployForm({ ...deployForm, environmentVariables: e.target.value })}
                sx={{ mt: 2 }}
                placeholder='{"KEY": "value", "DB_HOST": "localhost"}'
                helperText="Оставьте пустым, если не требуется"
              />
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeployDialogOpen(false)}>Отмена</Button>
          <Button
            variant="contained"
            onClick={handleDeploySubmit}
            disabled={deployMutation.isPending}
          >
            {deployMutation.isPending ? 'Развертывание...' : 'Развернуть'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

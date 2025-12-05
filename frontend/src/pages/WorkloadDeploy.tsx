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
  Refresh,
  OpenInNew,
  Add,
  CloudUpload,
  ArrowBack,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workloadService } from '../services/workloadService';
import { Workload } from '../types';

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
      // Обновляем workload с данными деплоя
      await workloadService.update(workloadId, {
        ...workloads?.find(w => w.id === workloadId),
        containerImage: formData.containerImage,
        exposedPort: formData.exposedPort,
        environmentVariables: formData.environmentVariables,
      });

      // Вызываем API деплоя
      const response = await fetch(`/api/deployment/deploy/${workloadId}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      });
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
      setDeployDialogOpen(false);
    },
  });

  const stopMutation = useMutation({
    mutationFn: async (workloadId: string) => {
      const response = await fetch(`/api/deployment/stop/${workloadId}`, {
        method: 'POST',
      });
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (workloadId: string) => {
      const response = await fetch(`/api/deployment/remove/${workloadId}`, {
        method: 'DELETE',
      });
      return response.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const handleDeploy = (workload: Workload) => {
    setSelectedWorkload(workload);
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

  const getStatusColor = (status?: string) => {
    switch (status) {
      case 'Running': return 'success';
      case 'Deploying': return 'info';
      case 'Stopped': return 'warning';
      case 'Error': return 'error';
      default: return 'default';
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
              onClick={() => window.location.href = '/dashboard'}
            >
              На панель
            </Button>
            <Button
              variant="contained"
              startIcon={<CloudUpload />}
              onClick={() => window.location.href = '/workloads'}
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
                    
                    <Box display="flex" alignItems="center" gap={1} mb={2}>
                      <Chip
                        label={workload.type === 'VirtualMachine' ? 'Виртуальная машина' :
                               workload.type === 'Database' ? 'База данных' :
                               workload.type === 'Container' ? 'Контейнер' :
                               workload.type === 'WebService' ? 'Веб-сервис' :
                               workload.type === 'BatchJob' ? 'Пакетное задание' : workload.type}
                        size="small"
                        variant="outlined"
                      />
                      {workload.deploymentStatus && (
                        <Chip
                          label={workload.deploymentStatus === 'Running' ? 'Работает' :
                                 workload.deploymentStatus === 'Deploying' ? 'Развертывается' :
                                 workload.deploymentStatus === 'Stopped' ? 'Остановлено' :
                                 workload.deploymentStatus === 'Error' ? 'Ошибка' : workload.deploymentStatus}
                          size="small"
                          color={getStatusColor(workload.deploymentStatus) as any}
                        />
                      )}
                    </Box>

                    <Typography variant="body2" color="text.secondary" paragraph>
                      CPU: {workload.requiredCpu} ядер | ОЗУ: {workload.requiredMemory} ГБ | Хранилище: {workload.requiredStorage} ГБ
                    </Typography>

                    {workload.accessUrl && (
                      <Alert severity="info" sx={{ mb: 2 }}>
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
                    {!workload.deploymentStatus || workload.deploymentStatus === 'NotDeployed' ? (
                      <Button
                        size="small"
                        startIcon={<PlayArrow />}
                        onClick={() => handleDeploy(workload)}
                        disabled={deployMutation.isPending}
                      >
                        Развернуть
                      </Button>
                    ) : workload.deploymentStatus === 'Running' ? (
                      <Button
                        size="small"
                        color="warning"
                        startIcon={<Stop />}
                        onClick={() => stopMutation.mutate(workload.id)}
                        disabled={stopMutation.isPending}
                      >
                        Остановить
                      </Button>
                    ) : null}

                    {(workload.deploymentStatus === 'Stopped' || workload.deploymentStatus === 'Error') && (
                      <Button
                        size="small"
                        color="error"
                        startIcon={<Delete />}
                        onClick={() => removeMutation.mutate(workload.id)}
                        disabled={removeMutation.isPending}
                      >
                        Удалить
                      </Button>
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
              onClick={() => window.location.href = '/workloads'}
            >
              Создать нагрузку
            </Button>
          </Box>
        )}
      </Paper>

      {/* Диалог настройки развертывания */}
      <Dialog open={deployDialogOpen} onClose={() => setDeployDialogOpen(false)}>
        <DialogTitle>Развернуть нагрузку</DialogTitle>
        <DialogContent>
          {selectedWorkload && (
            <Box pt={1}>
              <Typography variant="subtitle2" gutterBottom>
                Нагрузка: {selectedWorkload.name}
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
                onChange={(e) => setDeployForm({ ...deployForm, exposedPort: parseInt(e.target.value) })}
                sx={{ mt: 2 }}
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

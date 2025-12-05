import React from 'react';
import {
  Container,
  Paper,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  Chip,
  LinearProgress,
  Alert,
  IconButton,
} from '@mui/material';
import {
  PlayArrow,
  Stop,
  Delete,
  OpenInNew,
  Refresh,
  Dashboard,
  ArrowBack,
  Replay, 
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { workloadService } from '../services/workloadService';
import { getTypeLabel, getStatusLabel } from '../utils/translations';
import { useNavigate } from 'react-router-dom';
import { isWebService, getImageLabel, getImageInfo } from '../utils/dockerImages';

export const Deployments: React.FC = () => {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const { data: deployments, isLoading, refetch } = useQuery({
    queryKey: ['deployments'],
    queryFn: () => workloadService.getMyDeployments(),
  });

  const deployMutation = useMutation({
    mutationFn: workloadService.deploy,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const stopMutation = useMutation({
    mutationFn: workloadService.stopDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const removeMutation = useMutation({
    mutationFn: workloadService.removeDeployment,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deployments'] });
      queryClient.invalidateQueries({ queryKey: ['workloads'] });
    },
  });

  const handleDeploy = (workloadId: string) => {
    if (window.confirm('Развернуть эту рабочую нагрузку?')) {
      deployMutation.mutate(workloadId);
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

  const handleRedeploy = (workloadId: string) => {
    if (window.confirm('Перезапустить это развертывание?')) {
      deployMutation.mutate(workloadId);
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

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'Не развернуто';
    return new Date(dateString).toLocaleString('ru-RU');
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      <Paper sx={{ p: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
          <Box display="flex" alignItems="center" gap={2}>
            <Dashboard color="primary" />
            <Typography variant="h4">Активные развертывания</Typography>
          </Box>
          <Box display="flex" gap={1}>
            <Button
              variant="outlined"
              startIcon={<ArrowBack />}
              onClick={() => navigate('/dashboard')}
            >
              На главную
            </Button>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => refetch()}
              disabled={isLoading}
            >
              Обновить
            </Button>
            <Button
              variant="contained"
              onClick={() => navigate('/workloads')}
            >
              Управление нагрузками
            </Button>
          </Box>
        </Box>

        {isLoading ? (
          <LinearProgress />
        ) : deployments && deployments.length > 0 ? (
          <Grid container spacing={3}>
            {deployments.map((deployment) => (
              <Grid item xs={12} md={6} key={deployment.id}>
                <Card>
                  <CardContent>
                    <Box display="flex" justifyContent="space-between" alignItems="flex-start" mb={2}>
                      <Typography variant="h6">{deployment.name}</Typography>
                      {deployment.deploymentStatus && (
                        <Chip
                          label={getStatusLabel(deployment.deploymentStatus)}
                          color={getStatusColor(deployment.deploymentStatus) as any}
                          size="small"
                        />
                      )}
                    </Box>

                    <Typography variant="body2" color="text.secondary" gutterBottom>
                      Тип: {getTypeLabel(deployment.type)} • Образ: {deployment.containerImage}
                    </Typography>

                    <Box display="flex" gap={1} flexWrap="wrap" my={1}>
                      <Chip label={`CPU: ${deployment.requiredCpu} ядер`} size="small" variant="outlined" />
                      <Chip label={`ОЗУ: ${deployment.requiredMemory} ГБ`} size="small" variant="outlined" />
                      <Chip label={`Хранилище: ${deployment.requiredStorage} ГБ`} size="small" variant="outlined" />
                    </Box>

                    {deployment.accessUrl && isWebService(deployment.containerImage) && (
                      <Alert 
                        severity="info" 
                        sx={{ mt: 2 }}
                        action={
                          <IconButton
                            size="small"
                            onClick={() => window.open(deployment.accessUrl, '_blank')}
                          >
                            <OpenInNew fontSize="small" />
                          </IconButton>
                        }
                      >
                        <Typography variant="body2">
                          URL доступа: {deployment.accessUrl}
                        </Typography>
                      </Alert>
                    )}

                    {!isWebService(deployment.containerImage) && (
                      <Alert severity="info" sx={{ mt: 2 }}>
                        <Typography variant="body2">
                          <strong>Сервис без веб-интерфейса</strong><br />
                          Подключение через порт {getImageInfo(deployment.containerImage)?.port}<br />
                          <small>Используйте специализированные клиенты для подключения</small>
                        </Typography>
                      </Alert>
                    )}

                    {deployment.deployedAt && (
                      <Typography variant="caption" color="text.secondary" display="block" mt={1}>
                        Развернуто: {formatDate(deployment.deployedAt)}
                      </Typography>
                    )}
                  </CardContent>

                  <CardActions>
                    {(!deployment.deploymentStatus || deployment.deploymentStatus === 'NotDeployed') && (
                      <Button
                        size="small"
                        startIcon={<PlayArrow />}
                        onClick={() => handleDeploy(deployment.id)}
                        disabled={deployMutation.isPending}
                      >
                        Развернуть
                      </Button>
                    )}

                    {deployment.deploymentStatus === 'Running' && (
                      <Button
                        size="small"
                        color="warning"
                        startIcon={<Stop />}
                        onClick={() => handleStop(deployment.id)}
                        disabled={stopMutation.isPending}
                      >
                        Остановить
                      </Button>
                    )}

                    {(deployment.deploymentStatus === 'Stopped' || deployment.deploymentStatus === 'Error') && (
                      <Box display="flex" gap={1}>
                        <Button
                          size="small"
                          color="primary"
                          startIcon={<Replay />}
                          onClick={() => handleRedeploy(deployment.id)}
                          disabled={deployMutation.isPending}
                        >
                          Перезапустить
                        </Button>
                        <Button
                          size="small"
                          color="error"
                          startIcon={<Delete />}
                          onClick={() => handleRemove(deployment.id)}
                          disabled={removeMutation.isPending}
                        >
                          Удалить
                        </Button>
                      </Box>
                    )}
                  </CardActions>
                </Card>
              </Grid>
            ))}
          </Grid>
        ) : (
          <Box textAlign="center" py={4}>
            <Typography variant="h6" gutterBottom>
              Нет активных развертываний
            </Typography>
            <Typography variant="body2" color="text.secondary" paragraph>
              Создайте рабочую нагрузку с контейнерным образом и разверните ее, чтобы увидеть здесь.
            </Typography>
            <Button
              variant="contained"
              onClick={() => navigate('/workloads')}
            >
              Перейти к нагрузкам
            </Button>
          </Box>
        )}
      </Paper>
    </Container>
  );
};

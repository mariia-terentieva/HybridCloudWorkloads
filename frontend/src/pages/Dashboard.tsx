import React from 'react';
import {
  Container,
  Grid,
  Paper,
  Typography,
  Box,
  Card,
  CardContent,
  CardActions,
  Button,
  Avatar,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Divider,
  Chip,
  LinearProgress,
} from '@mui/material';
import {
  Dashboard as DashboardIcon,
  Computer,
  Storage,
  Memory,
  Add,
  TrendingUp,
  Cloud,
  Security,
  Settings,
  Person,
  ArrowForward,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuthStore } from '../store/authStore';
import { workloadService } from '../services/workloadService';
import { Workload } from '../types';
import { getTypeLabel, getStatusLabel } from '../utils/translations';
import { isWebService } from '../utils/dockerImages';

export const Dashboard: React.FC = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
  const logout = useAuthStore((state) => state.logout);

  const { data: workloads, isLoading } = useQuery({
    queryKey: ['workloads'],
    queryFn: () => workloadService.getAll(),
  });

  // Получаем только развернутые workloads
  const deployedWorkloads = React.useMemo(() => {
    return workloads?.filter(w => w.deploymentStatus && w.deploymentStatus !== 'NotDeployed') || [];
  }, [workloads]);

  // Получаем работающие деплои
  const runningDeployments = React.useMemo(() => {
    return deployedWorkloads.filter(d => d.deploymentStatus === 'Running') || [];
  }, [deployedWorkloads]);

  const stats = React.useMemo(() => {
    if (!workloads) return null;

    const totalWorkloads = workloads.length;
    const totalCpu = workloads.reduce((sum, w) => sum + w.requiredCpu, 0);
    const totalMemory = workloads.reduce((sum, w) => sum + w.requiredMemory, 0);
    const totalStorage = workloads.reduce((sum, w) => sum + w.requiredStorage, 0);

    // Count by type
    const typeCounts = workloads.reduce((acc, w) => {
      acc[w.type] = (acc[w.type] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    return {
      totalWorkloads,
      totalCpu,
      totalMemory,
      totalStorage,
      typeCounts,
    };
  }, [workloads]);

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const getWorkloadTypeIcon = (type: string) => {
    switch (type) {
      case 'VirtualMachine':
        return <Computer />;
      case 'Database':
        return <Storage />;
      case 'Container':
        return <Cloud />;
      default:
        return <Computer />;
    }
  };

  const getWorkloadTypeColor = (type: string) => {
    switch (type) {
      case 'VirtualMachine':
        return 'primary';
      case 'Database':
        return 'secondary';
      case 'WebService':
        return 'success';
      case 'Container':
        return 'info';
      case 'BatchJob':
        return 'warning';
      default:
        return 'default';
    }
  };

  return (
    <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
      {/* Header */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Box display="flex" alignItems="center" gap={2}>
            <DashboardIcon sx={{ fontSize: 40 }} color="primary" />
            <Box>
              <Typography variant="h4" component="h1">
                Панель управления
              </Typography>
              <Typography variant="body1" color="text.secondary">
                С возвращением, {user?.firstName} {user?.lastName}
              </Typography>
            </Box>
          </Box>
          <Box display="flex" alignItems="center" gap={2}>
            <Avatar sx={{ bgcolor: 'primary.main' }}>
              {user?.firstName?.[0]}{user?.lastName?.[0]}
            </Avatar>
            <Button
              variant="outlined"
              onClick={handleLogout}
            >
              Выйти
            </Button>
          </Box>
        </Box>
      </Paper>

      <Grid container spacing={3}>
{/* Quick Stats */}
<Grid item xs={12}>
  <Box
    display="flex"
    flexWrap="wrap"
    gap={3}
    sx={{
      '& > .MuiPaper-root': {
        flex: '1 1 200px', // Карточки будут минимум 200px, переносятся при нехватке места
        minWidth: '200px',
        maxWidth: '100%',
      },
    }}
  >
    {/* Карточка 1: Всего нагрузок */}
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="text.secondary" gutterBottom>
              Всего нагрузок
            </Typography>
            <Typography variant="h4">
              {stats?.totalWorkloads || 0}
            </Typography>
          </Box>
          <Computer sx={{ fontSize: 40, color: 'primary.main' }} />
        </Box>
        {isLoading && <LinearProgress sx={{ mt: 1 }} />}
      </CardContent>
    </Card>

    {/* Карточка 2: Всего ядер CPU */}
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="text.secondary" gutterBottom>
              Всего ядер CPU
            </Typography>
            <Typography variant="h4">
              {stats?.totalCpu || 0}
            </Typography>
          </Box>
          <TrendingUp sx={{ fontSize: 40, color: 'success.main' }} />
        </Box>
        {isLoading && <LinearProgress sx={{ mt: 1 }} />}
      </CardContent>
    </Card>

    {/* Карточка 3: Всего ОЗУ (ГБ) */}
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="text.secondary" gutterBottom>
              Всего ОЗУ (ГБ)
            </Typography>
            <Typography variant="h4">
              {stats?.totalMemory?.toFixed(1) || 0}
            </Typography>
          </Box>
          <Memory sx={{ fontSize: 40, color: 'info.main' }} />
        </Box>
        {isLoading && <LinearProgress sx={{ mt: 1 }} />}
      </CardContent>
    </Card>

    {/* Карточка 4: Всего хранилища (ГБ) */}
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="text.secondary" gutterBottom>
              Всего хранилища (ГБ)
            </Typography>
            <Typography variant="h4">
              {stats?.totalStorage?.toFixed(1) || 0}
            </Typography>
          </Box>
          <Storage sx={{ fontSize: 40, color: 'warning.main' }} />
        </Box>
        {isLoading && <LinearProgress sx={{ mt: 1 }} />}
      </CardContent>
    </Card>

    {/* Карточка 5: Активные развертывания */}
    <Card>
      <CardContent>
        <Box display="flex" alignItems="center" justifyContent="space-between">
          <Box>
            <Typography color="text.secondary" gutterBottom>
              Активные развертывания
            </Typography>
            <Typography variant="h4">
              {runningDeployments.length || 0}
            </Typography>
          </Box>
          <Cloud sx={{ fontSize: 40, color: 'info.main' }} />
        </Box>
        {isLoading && <LinearProgress sx={{ mt: 1 }} />}
      </CardContent>
      <CardActions>
        <Button
          size="small"
          onClick={() => navigate('/deployments')}
          startIcon={<ArrowForward />}
        >
          Посмотреть развертывания
        </Button>
      </CardActions>
    </Card>
  </Box>
</Grid>

        {/* Main Content */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 2 }}>
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
              <Typography variant="h5">Последние нагрузки</Typography>
              <Button
                variant="contained"
                startIcon={<Add />}
                onClick={() => navigate('/workloads')}
              >
                Управление нагрузками
              </Button>
            </Box>

            {isLoading ? (
              <Box p={3}>
                <LinearProgress />
                <Typography align="center" sx={{ mt: 2 }}>
                  Загрузка нагрузок...
                </Typography>
              </Box>
            ) : workloads && workloads.length > 0 ? (
              <List>
                {workloads.slice(0, 5).map((workload, index) => (
                  <React.Fragment key={workload.id}>
                    <ListItem
                      button
                      onClick={() => navigate('/workloads')}
                    >
                      <ListItemIcon>
                        {getWorkloadTypeIcon(workload.type)}
                      </ListItemIcon>
                      <ListItemText
                        primary={workload.name}
                        secondary={
                          <Box display="flex" gap={1} mt={1}>
                            <Chip
                              size="small"
                              label={`${getTypeLabel(workload.type)}`}
                              color={getWorkloadTypeColor(workload.type) as any}
                              variant="outlined"
                            />
                            <Chip
                              size="small"
                              label={`CPU: ${workload.requiredCpu} ядер`}
                              variant="outlined"
                            />
                            <Chip
                              size="small"
                              label={`ОЗУ: ${workload.requiredMemory} ГБ`}
                              variant="outlined"
                            />
                            <Chip
                              size="small"
                              label={`Хранилище: ${workload.requiredStorage} ГБ`}
                              variant="outlined"
                            />
                           {/* {workload.accessUrl && isWebService(workload.containerImage) && (
                              <Chip
                                size="small"
                                label={`Доступ: ${workload.accessUrl}`}
                                color="info"
                                variant="outlined"
                                onClick={() => window.open(workload.accessUrl, '_blank')}
                                clickable
                              />
                            )} */}
                            {workload.deploymentStatus && workload.deploymentStatus !== 'NotDeployed' && (
                              <Chip
                                size="small"
                                label={getStatusLabel(workload.deploymentStatus)}
                                color={
                                  workload.deploymentStatus === 'Running' ? 'success' :
                                  workload.deploymentStatus === 'Error' ? 'error' : 'warning'
                                }
                                variant="outlined"
                              />
                            )}
                          </Box>
                        }
                      />
                      <Typography variant="caption" color="text.secondary">
                        {new Date(workload.createdAt).toLocaleDateString('ru-RU')}
                      </Typography>
                    </ListItem>
                    {index < workloads.length - 1 && <Divider />}
                  </React.Fragment>
                ))}
              </List>
            ) : (
              <Box p={3} textAlign="center">
                <Computer sx={{ fontSize: 60, color: 'text.secondary', mb: 2 }} />
                <Typography variant="h6" gutterBottom>
                  Пока нет нагрузок
                </Typography>
                <Typography variant="body2" color="text.secondary" paragraph>
                  Начните с создания первой нагрузки для оптимизации ее размещения в гибридном облаке.
                </Typography>
                <Button
                  variant="contained"
                  startIcon={<Add />}
                  onClick={() => navigate('/workloads')}
                >
                  Создать первую нагрузку
                </Button>
              </Box>
            )}
          </Paper>
        </Grid>

        {/* Sidebar */}
        <Grid item xs={12} md={4}>
          <Grid container spacing={3}>
            {/* Быстрые действия */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Быстрые действия
                  </Typography>
                  <List>
                    <ListItem button onClick={() => navigate('/workloads')}>
                      <ListItemIcon>
                        <Add color="primary" />
                      </ListItemIcon>
                      <ListItemText primary="Создать новую нагрузку" />
                    </ListItem>
                    <ListItem button onClick={() => navigate('/deployments')}>
                      <ListItemIcon>
                        <Cloud color="action" />
                      </ListItemIcon>
                      <ListItemText primary="Посмотреть развертывания" />
                    </ListItem>
                    <ListItem button onClick={() => navigate('/profile')}>
                      <ListItemIcon>
                        <Settings color="action" />
                      </ListItemIcon>
                      <ListItemText primary="Настройки профиля" />
                    </ListItem>
                   {/* <ListItem button>
                      <ListItemIcon>
                        <Security color="action" />
                      </ListItemIcon>
                      <ListItemText primary="Настройки безопасности" />
                    </ListItem> */}
                  </List>
                </CardContent>
              </Card>
            </Grid>

            {/* Распределение по типам нагрузок */}
            {stats && stats.typeCounts && Object.keys(stats.typeCounts).length > 0 && (
              <Grid item xs={12}>
                <Card>
                  <CardContent>
                    <Typography variant="h6" gutterBottom>
                      Типы нагрузок
                    </Typography>
                    <Box display="flex" flexDirection="column" gap={1}>
                      {Object.entries(stats.typeCounts).map(([type, count]) => (
                        <Box key={type} display="flex" justifyContent="space-between" alignItems="center">
                          <Box display="flex" alignItems="center" gap={1}>
                            {getWorkloadTypeIcon(type)}
                            <Typography variant="body2">
                               {getTypeLabel(type)}
                            </Typography>
                          </Box>
                          <Chip
                            label={count}
                            size="small"
                            color={getWorkloadTypeColor(type) as any}
                          />
                        </Box>
                      ))}
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            )}

            {/* Статус системы */}
            <Grid item xs={12}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    Статус системы
                  </Typography>
                  <Box display="flex" flexDirection="column" gap={2}>
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Подключение к API
                      </Typography>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Box
                          sx={{
                            width: 8,
                            height: 8,
                            borderRadius: '50%',
                            bgcolor: 'success.main',
                          }}
                        />
                        <Typography variant="body2">Подключено</Typography>
                      </Box>
                    </Box>
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Статус базы данных
                      </Typography>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Box
                          sx={{
                            width: 8,
                            height: 8,
                            borderRadius: '50%',
                            bgcolor: 'success.main',
                          }}
                        />
                        <Typography variant="body2">Онлайн</Typography>
                      </Box>
                    </Box>
                    <Box>
                      <Typography variant="body2" color="text.secondary">
                        Статус Docker
                      </Typography>
                      <Box display="flex" alignItems="center" gap={1}>
                        <Box
                          sx={{
                            width: 8,
                            height: 8,
                            borderRadius: '50%',
                            bgcolor: runningDeployments.length > 0 ? 'success.main' : 'warning.main',
                          }}
                        />
                        <Typography variant="body2">
                          {runningDeployments.length > 0 ? 'Работает' : 'Готов'}
                        </Typography>
                      </Box>
                    </Box>
                  </Box>
                </CardContent>
                {/* <CardActions>
                  <Button size="small" startIcon={<Settings />}>
                    Настройки системы
                  </Button>
                </CardActions> */}
              </Card>
            </Grid>
          </Grid>
        </Grid>
      </Grid>

      {/* Footer */}
      <Box mt={4}>
        <Typography variant="body2" color="text.secondary" align="center">
          Платформа оптимизации рабочих нагрузок в гибридном облаке v1.0 • 
          Всего ресурсов: {stats?.totalCpu || 0} ядер CPU, 
          {stats?.totalMemory?.toFixed(1) || 0} ГБ ОЗУ, 
          {stats?.totalStorage?.toFixed(1) || 0} ГБ хранилища •
          Активных развертываний: {runningDeployments.length}
        </Typography>
      </Box>
    </Container>
  );
};
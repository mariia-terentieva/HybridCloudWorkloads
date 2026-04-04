import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Grid,
  Box,
  CircularProgress,
  Alert,
  Chip,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Button,
  Paper,
  Divider,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tab,
  Tabs,
  Tooltip,
} from '@mui/material';
import {
  Memory,
  Speed,
  NetworkCheck,
  Storage,
  Warning,
  CheckCircle,
  TrendingUp,
  TrendingDown,
  Timeline,
  Insights,
  Recommend,
} from '@mui/icons-material';
import {
  LineChart,
  Line,
  AreaChart,
  Area,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  Legend,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import { useQuery } from '@tanstack/react-query';
import { metricsService } from '../services/metricsService';
import { AggregatedMetrics, PerformanceProfile } from '../types';
import { format, subDays, subWeeks, subMonths } from 'date-fns';
import { ru } from 'date-fns/locale';

interface MetricsDashboardProps {
  workloadId: string;
  workloadName: string;
}

type TabValue = 'overview' | 'cpu' | 'memory' | 'network' | 'profile';

const COLORS = ['#1976d2', '#dc004e', '#2e7d32', '#ed6c02', '#9c27b0'];

export const MetricsDashboard: React.FC<MetricsDashboardProps> = ({
  workloadId,
  workloadName,
}) => {
  const [period, setPeriod] = useState<'1h' | '24h' | '7d' | '30d'>('24h');
  const [tab, setTab] = useState<TabValue>('overview');

  // Вычисляем даты в зависимости от выбранного периода
  const getDateRange = () => {
    const now = new Date();
    switch (period) {
      case '1h':
        return { from: subDays(now, 1/24), to: now };
      case '24h':
        return { from: subDays(now, 1), to: now };
      case '7d':
        return { from: subDays(now, 7), to: now };
      case '30d':
        return { from: subDays(now, 30), to: now };
      default:
        return { from: subDays(now, 1), to: now };
    }
  };

  const { from, to } = getDateRange();

  // Запрос агрегированных метрик
  const { 
    data: metrics, 
    isLoading: metricsLoading,
    error: metricsError,
    refetch: refetchMetrics
  } = useQuery({
    queryKey: ['metrics', workloadId, period],
    queryFn: () => metricsService.getAggregatedMetrics(
      workloadId,
      from,
      to,
      period === '1h' ? 'Hour' : period === '24h' ? 'Hour' : 'Day'
    ),
  });

  // Запрос временного ряда
  const {
    data: timeSeries,
    isLoading: timeSeriesLoading,
  } = useQuery({
    queryKey: ['timeseries', workloadId, from, to],
    queryFn: () => metricsService.getTimeSeries(
      workloadId,
      from,
      to,
      period === '1h' ? '5m' : period === '24h' ? '30m' : '1h'
    ),
  });

  // Запрос профиля производительности
  const {
    data: profile,
    isLoading: profileLoading,
    refetch: refetchProfile,
  } = useQuery({
    queryKey: ['profile', workloadId],
    queryFn: () => metricsService.getPerformanceProfile(workloadId),
    enabled: tab === 'profile',
  });

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  const formatNumber = (num: number) => {
    return new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 }).format(num);
  };

  const getHealthColor = (value: number, warning: number, critical: number) => {
    if (value >= critical) return '#d32f2f';
    if (value >= warning) return '#ed6c02';
    return '#2e7d32';
  };

  if (metricsLoading && !metrics) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight={400}>
        <CircularProgress />
      </Box>
    );
  }

  if (metricsError) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        Ошибка загрузки метрик
      </Alert>
    );
  }

  return (
    <Box>
      {/* Заголовок и управление периодом */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h6">
          Метрики: {workloadName}
        </Typography>
        <Box display="flex" gap={1}>
          <Button
            size="small"
            variant={period === '1h' ? 'contained' : 'outlined'}
            onClick={() => setPeriod('1h')}
          >
            1 час
          </Button>
          <Button
            size="small"
            variant={period === '24h' ? 'contained' : 'outlined'}
            onClick={() => setPeriod('24h')}
          >
            24 часа
          </Button>
          <Button
            size="small"
            variant={period === '7d' ? 'contained' : 'outlined'}
            onClick={() => setPeriod('7d')}
          >
            7 дней
          </Button>
          <Button
            size="small"
            variant={period === '30d' ? 'contained' : 'outlined'}
            onClick={() => setPeriod('30d')}
          >
            30 дней
          </Button>
        </Box>
      </Box>

      {/* Tabs */}
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab value="overview" label="Обзор" />
        <Tab value="cpu" label="CPU" />
        <Tab value="memory" label="Память" />
        <Tab value="network" label="Сеть" />
        <Tab value="profile" label="Профиль" />
      </Tabs>

      {/* Overview Tab */}
      {tab === 'overview' && metrics && (
        <Grid container spacing={3}>
          {/* Ключевые метрики */}
          <Grid item xs={12}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Box>
                        <Typography color="text.secondary" gutterBottom>
                          CPU
                        </Typography>
                        <Typography variant="h5">
                          {formatNumber(metrics.avgCpu)}%
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Пик: {formatNumber(metrics.peakCpu)}%
                        </Typography>
                      </Box>
                      <Memory sx={{ fontSize: 40, color: getHealthColor(metrics.avgCpu, 70, 90) }} />
                    </Box>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Box>
                        <Typography color="text.secondary" gutterBottom>
                          Память
                        </Typography>
                        <Typography variant="h5">
                          {formatNumber(metrics.avgMemory)}%
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Пик: {formatNumber(metrics.peakMemory)}%
                        </Typography>
                      </Box>
                      <Storage sx={{ fontSize: 40, color: getHealthColor(metrics.avgMemory, 80, 95) }} />
                    </Box>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Box>
                        <Typography color="text.secondary" gutterBottom>
                          Время отклика
                        </Typography>
                        <Typography variant="h5">
                          {formatNumber(metrics.avgResponseTime)} мс
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          P95: {formatNumber(metrics.p95ResponseTime)} мс
                        </Typography>
                      </Box>
                      <Speed sx={{ fontSize: 40, color: getHealthColor(metrics.avgResponseTime, 200, 500) }} />
                    </Box>
                  </CardContent>
                </Card>
              </Grid>

              <Grid item xs={12} sm={6} md={3}>
                <Card>
                  <CardContent>
                    <Box display="flex" alignItems="center" justifyContent="space-between">
                      <Box>
                        <Typography color="text.secondary" gutterBottom>
                          Доступность
                        </Typography>
                        <Typography variant="h5">
                          {formatNumber(metrics.availability)}%
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Ошибок: {metrics.errorCount}
                        </Typography>
                      </Box>
                      {metrics.availability > 99.9 ? (
                        <CheckCircle sx={{ fontSize: 40, color: '#2e7d32' }} />
                      ) : (
                        <Warning sx={{ fontSize: 40, color: '#ed6c02' }} />
                      )}
                    </Box>
                  </CardContent>
                </Card>
              </Grid>
            </Grid>
          </Grid>

          {/* График CPU и памяти */}
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  CPU и Память
                </Typography>
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={timeSeries || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={(ts) => format(new Date(ts), 'HH:mm')}
                    />
                    <YAxis yAxisId="left" />
                    <YAxis yAxisId="right" orientation="right" />
                    <RechartsTooltip 
                      labelFormatter={(ts) => format(new Date(ts), 'dd.MM.yyyy HH:mm')}
                    />
                    <Legend />
                    <Line 
                      yAxisId="left"
                      type="monotone" 
                      dataKey="cpu" 
                      stroke="#1976d2" 
                      name="CPU %"
                      dot={false}
                    />
                    <Line 
                      yAxisId="left"
                      type="monotone" 
                      dataKey="memory" 
                      stroke="#dc004e" 
                      name="Память %"
                      dot={false}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>

          {/* График времени отклика */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Время отклика
                </Typography>
                <ResponsiveContainer width="100%" height={250}>
                  <AreaChart data={timeSeries || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={(ts) => format(new Date(ts), 'HH:mm')}
                    />
                    <YAxis />
                    <RechartsTooltip />
                    <Area 
                      type="monotone" 
                      dataKey="responseTime" 
                      stroke="#2e7d32" 
                      fill="#2e7d32" 
                      fillOpacity={0.3}
                      name="мс"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>

          {/* График запросов */}
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Запросы в секунду
                </Typography>
                <ResponsiveContainer width="100%" height={250}>
                  <BarChart data={timeSeries || []}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={(ts) => format(new Date(ts), 'HH:mm')}
                    />
                    <YAxis />
                    <RechartsTooltip />
                    <Bar dataKey="requests" fill="#9c27b0" name="RPS" />
                  </BarChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
        </Grid>
      )}

      {/* Profile Tab */}
      {tab === 'profile' && profile && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                <Insights sx={{ mr: 1, verticalAlign: 'middle' }} />
                Профиль производительности
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Рассчитан на основе {profile.totalSamples} измерений за {profile.daysOfData} дней
              </Typography>

              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={12} md={4}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom>
                        Базовый CPU
                      </Typography>
                      <Typography variant="h5">
                        {formatNumber(profile.baselineCpu)}%
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom>
                        Базовая память
                      </Typography>
                      <Typography variant="h5">
                        {formatNumber(profile.baselineMemory)}%
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
                <Grid item xs={12} md={4}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom>
                        Базовое время отклика
                      </Typography>
                      <Typography variant="h5">
                        {formatNumber(profile.baselineResponseTime)} мс
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>

              {profile.peakPeriods.length > 0 && (
                <>
                  <Typography variant="subtitle1" sx={{ mt: 3, mb: 2 }}>
                    Пиковые периоды
                  </Typography>
                  <List>
                    {profile.peakPeriods.map((period, idx) => (
                      <ListItem key={idx}>
                        <ListItemIcon>
                          <Timeline />
                        </ListItemIcon>
                        <ListItemText
                          primary={`${period.pattern === 'daily' ? 'Ежедневно' : 'Еженедельно'}: ${period.timeRange}`}
                          secondary={`Средняя нагрузка: ${formatNumber(period.avgLoad)}%, пик: ${formatNumber(period.peakLoad)}%`}
                        />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}

              {profile.recommendations.length > 0 && (
                <>
                  <Typography variant="subtitle1" sx={{ mt: 3, mb: 2 }}>
                    <Recommend sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Рекомендации
                  </Typography>
                  <List>
                    {profile.recommendations.map((rec, idx) => (
                      <ListItem key={idx}>
                        <ListItemIcon>
                          {rec.includes('уменьшить') ? <TrendingDown color="success" /> : <TrendingUp color="warning" />}
                        </ListItemIcon>
                        <ListItemText primary={rec} />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}
            </Paper>
          </Grid>
        </Grid>
      )}
    </Box>
  );
};
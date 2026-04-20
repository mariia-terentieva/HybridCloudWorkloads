import React, { useState, useMemo } from 'react';
import {
  Card,
  CardContent,
  Typography,
  Grid,
  Box,
  CircularProgress,
  Alert,
  Button,
  Paper,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tab,
  Tabs,
} from '@mui/material';
import {
  Memory,
  Speed,
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
} from 'recharts';
import { useQuery } from '@tanstack/react-query';
import { metricsService } from '../services/metricsService';
import { format, subDays } from 'date-fns';

interface MetricsDashboardProps {
  workloadId: string;
  workloadName: string;
}

type TabValue = 'overview' | 'cpu' | 'memory' | 'network' | 'profile';

//const COLORS = ['#1976d2', '#dc004e', '#2e7d32', '#ed6c02', '#9c27b0'];

export const MetricsDashboard: React.FC<MetricsDashboardProps> = ({
  workloadId,
  workloadName,
}) => {
  const [period, setPeriod] = useState<'1h' | '24h' | '7d' | '30d'>('24h');
  const [tab, setTab] = useState<TabValue>('overview');

  // ========== ФУНКЦИИ ДЛЯ ЗАЩИТЫ ОТ НЕКОРРЕКТНЫХ ЗНАЧЕНИЙ ==========
  
  /**
   * Безопасное форматирование числа - защита от NaN, Infinity, null, undefined
   */
  const safeNumber = (value: number | undefined | null, defaultValue: number = 0): number => {
    if (value === undefined || value === null || isNaN(value) || !isFinite(value)) {
      return defaultValue;
    }
    return value;
  };

  /**
   * Форматирование процентов с защитой
   */
  const formatPercent = (value: number | undefined | null): string => {
    const safe = safeNumber(value);
    return `${safe.toFixed(1)}%`;
  };

  /**
   * Общее форматирование чисел с защитой
   */
  const formatNumber = (value: number | undefined | null): string => {
    const safe = safeNumber(value);
    return new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 }).format(safe);
  };

  /**
   * Форматирование байт с защитой
   */
  const formatBytes = (bytes: number | undefined | null): string => {
    const safe = safeNumber(bytes);
    if (safe === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(safe) / Math.log(k));
    return `${(safe / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
  };

  /**
   * Безопасное получение цвета здоровья
   */
  const getHealthColor = (value: number | undefined | null, warning: number, critical: number): string => {
    const safe = safeNumber(value);
    if (safe >= critical) return '#d32f2f';
    if (safe >= warning) return '#ed6c02';
    return '#2e7d32';
  };

  // ========== ВЫЧИСЛЕНИЕ ДАТ ==========
  
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

  // ========== ЗАПРОСЫ ДАННЫХ ==========
  
  const { 
    data: metrics, 
    isLoading: metricsLoading,
    error: metricsError,
    //refetch: refetchMetrics
  } = useQuery({
    queryKey: ['metrics', workloadId, period],
    queryFn: () => metricsService.getAggregatedMetrics(
      workloadId,
      from,
      to,
      period === '1h' ? 'Hour' : period === '24h' ? 'Hour' : 'Day'
    ),
  });

  const {
    data: timeSeriesRaw,
    //isLoading: timeSeriesLoading,
  } = useQuery({
    queryKey: ['timeseries', workloadId, from, to],
    queryFn: () => metricsService.getTimeSeries(
      workloadId,
      from,
      to,
      period === '1h' ? '5m' : period === '24h' ? '30m' : '1h'
    ),
  });

  const {
    data: profile,
    //isLoading: profileLoading,
    //refetch: refetchProfile,
  } = useQuery({
    queryKey: ['profile', workloadId],
    queryFn: () => metricsService.getPerformanceProfile(workloadId),
    enabled: tab === 'profile',
  });

  // ========== ОЧИСТКА ДАННЫХ ДЛЯ ГРАФИКОВ ==========
  
  const safeTimeSeries = useMemo(() => {
    if (!timeSeriesRaw || !Array.isArray(timeSeriesRaw)) return [];
    return timeSeriesRaw.map(point => ({
      timestamp: point.timestamp,
      cpu: safeNumber(point.cpu),
      memory: safeNumber(point.memory),
      responseTime: safeNumber(point.responseTime),
      requests: safeNumber(point.requests)
    }));
  }, [timeSeriesRaw]);

  // Безопасные метрики
  const safeMetrics = useMemo(() => {
    if (!metrics) return null;
    return {
      avgCpu: safeNumber(metrics.avgCpu),
      avgMemory: safeNumber(metrics.avgMemory),
      peakCpu: safeNumber(metrics.peakCpu),
      peakMemory: safeNumber(metrics.peakMemory),
      avgResponseTime: safeNumber(metrics.avgResponseTime),
      p95ResponseTime: safeNumber(metrics.p95ResponseTime),
      p99ResponseTime: safeNumber(metrics.p99ResponseTime),
      availability: safeNumber(metrics.availability, 100),
      errorCount: safeNumber(metrics.errorCount),
      avgRps: safeNumber(metrics.avgRps),
      peakRps: safeNumber(metrics.peakRps),
      totalNetworkIn: safeNumber(metrics.totalNetworkIn),
      totalNetworkOut: safeNumber(metrics.totalNetworkOut),
      sampleCount: safeNumber(metrics.sampleCount),
      p95Cpu: (metrics as any).p95Cpu || 0,
      p95Memory: (metrics as any).p95Memory || 0
    };
  }, [metrics]);

  // Безопасный профиль
  const safeProfile = useMemo(() => {
    if (!profile) return null;
    return {
      ...profile,
      baselineCpu: safeNumber(profile.baselineCpu),
      baselineMemory: safeNumber(profile.baselineMemory),
      baselineResponseTime: safeNumber(profile.baselineResponseTime),
      totalSamples: safeNumber(profile.totalSamples),
      daysOfData: safeNumber(profile.daysOfData),
      peakPeriods: profile.peakPeriods?.map(p => ({
        ...p,
        avgLoad: safeNumber(p.avgLoad),
        peakLoad: safeNumber(p.peakLoad)
      })) || []
    };
  }, [profile]);

  // ========== РЕНДЕРИНГ ==========
  
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
        Ошибка загрузки метрик. Возможно, для этой нагрузки еще нет данных.
      </Alert>
    );
  }

  return (
    <Box>
      {/* Заголовок и управление периодом */}
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3} flexWrap="wrap" gap={1}>
        <Typography variant="h6">
          Метрики: {workloadName}
        </Typography>
        <Box display="flex" gap={1} flexWrap="wrap">
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
      {tab === 'overview' && safeMetrics && (
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
                          {formatPercent(safeMetrics.avgCpu)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Пик: {formatPercent(safeMetrics.peakCpu)}
                        </Typography>
                      </Box>
                      <Memory sx={{ fontSize: 40, color: getHealthColor(safeMetrics.avgCpu, 70, 90) }} />
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
                          {formatPercent(safeMetrics.avgMemory)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Пик: {formatPercent(safeMetrics.peakMemory)}
                        </Typography>
                      </Box>
                      <Storage sx={{ fontSize: 40, color: getHealthColor(safeMetrics.avgMemory, 80, 95) }} />
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
                          {formatNumber(safeMetrics.avgResponseTime)} мс
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          P95: {formatNumber(safeMetrics.p95ResponseTime)} мс
                        </Typography>
                      </Box>
                      <Speed sx={{ fontSize: 40, color: getHealthColor(safeMetrics.avgResponseTime, 200, 500) }} />
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
                          {formatPercent(safeMetrics.availability)}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          Ошибок: {safeMetrics.errorCount}
                        </Typography>
                      </Box>
                      {safeMetrics.availability > 99.9 ? (
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
                {safeTimeSeries.length > 0 ? (
                  <ResponsiveContainer width="100%" height={300}>
                    <LineChart data={safeTimeSeries}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="timestamp" 
                        tickFormatter={(ts) => ts ? format(new Date(ts), 'HH:mm') : ''}
                      />
                      <YAxis yAxisId="left" domain={[0, 100]} />
                      <YAxis yAxisId="right" orientation="right" domain={[0, 100]} />
                      <RechartsTooltip 
                        labelFormatter={(ts) => ts ? format(new Date(ts), 'dd.MM.yyyy HH:mm') : ''}
                        formatter={(value: any) => {
                          const num = safeNumber(value);
                          return [`${num.toFixed(1)}%`, ''];
                        }}
                      />
                      <Legend />
                      <Line 
                        yAxisId="left"
                        type="monotone" 
                        dataKey="cpu" 
                        stroke="#1976d2" 
                        name="CPU %"
                        dot={false}
                        connectNulls={true}
                      />
                      <Line 
                        yAxisId="right"
                        type="monotone" 
                        dataKey="memory" 
                        stroke="#dc004e" 
                        name="Память %"
                        dot={false}
                        connectNulls={true}
                      />
                    </LineChart>
                  </ResponsiveContainer>
                ) : (
                  <Box textAlign="center" py={4}>
                    <Typography color="text.secondary">Нет данных для отображения</Typography>
                  </Box>
                )}
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
                {safeTimeSeries.length > 0 ? (
                  <ResponsiveContainer width="100%" height={250}>
                    <AreaChart data={safeTimeSeries}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="timestamp" 
                        tickFormatter={(ts) => ts ? format(new Date(ts), 'HH:mm') : ''}
                      />
                      <YAxis />
                      <RechartsTooltip 
                        formatter={(value: any) => {
                          const num = safeNumber(value);
                          return [`${num.toFixed(0)} мс`, ''];
                        }}
                      />
                      <Area 
                        type="monotone" 
                        dataKey="responseTime" 
                        stroke="#2e7d32" 
                        fill="#2e7d32" 
                        fillOpacity={0.3}
                        name="мс"
                        connectNulls={true}
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                ) : (
                  <Box textAlign="center" py={4}>
                    <Typography color="text.secondary">Нет данных для отображения</Typography>
                  </Box>
                )}
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
                {safeTimeSeries.length > 0 ? (
                  <ResponsiveContainer width="100%" height={250}>
                    <BarChart data={safeTimeSeries}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis 
                        dataKey="timestamp" 
                        tickFormatter={(ts) => ts ? format(new Date(ts), 'HH:mm') : ''}
                      />
                      <YAxis />
                      <RechartsTooltip 
                        formatter={(value: any) => {
                          const num = safeNumber(value);
                          return [`${num.toFixed(1)} RPS`, ''];
                        }}
                      />
                      <Bar dataKey="requests" fill="#9c27b0" name="RPS" />
                    </BarChart>
                  </ResponsiveContainer>
                ) : (
                  <Box textAlign="center" py={4}>
                    <Typography color="text.secondary">Нет данных для отображения</Typography>
                  </Box>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Статистика по выборке */}
          {safeMetrics.sampleCount > 0 && (
            <Grid item xs={12}>
              <Alert severity="info" sx={{ mt: 1 }}>
                Показаны данные за период: {format(new Date(from), 'dd.MM.yyyy HH:mm')} - {format(new Date(to), 'dd.MM.yyyy HH:mm')}
                {' • '}Количество точек данных: {safeMetrics.sampleCount}
              </Alert>
            </Grid>
          )}
        </Grid>
      )}

      {/* CPU Tab */}
      {tab === 'cpu' && safeMetrics && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Детальная метрика CPU
                </Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={safeTimeSeries}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={(ts) => ts ? format(new Date(ts), 'dd.MM HH:mm') : ''}
                    />
                    <YAxis domain={[0, 100]} />
                    <RechartsTooltip 
                      labelFormatter={(ts) => ts ? format(new Date(ts), 'dd.MM.yyyy HH:mm') : ''}
                      formatter={(value: any) => [`${safeNumber(value).toFixed(1)}%`, 'CPU']}
                    />
                    <Legend />
                    <Line 
                      type="monotone" 
                      dataKey="cpu" 
                      stroke="#1976d2" 
                      name="CPU %"
                      strokeWidth={2}
                      dot={false}
                      connectNulls={true}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12}>
            <Alert severity="info">
              Средняя загрузка CPU: {formatPercent(safeMetrics.avgCpu)} | Пиковая: {formatPercent(safeMetrics.peakCpu)} | P95: {formatPercent(safeMetrics.p95Cpu)}
            </Alert>
          </Grid>
        </Grid>
      )}

      {/* Memory Tab */}
      {tab === 'memory' && safeMetrics && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Детальная метрика памяти
                </Typography>
                <ResponsiveContainer width="100%" height={400}>
                  <LineChart data={safeTimeSeries}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis 
                      dataKey="timestamp" 
                      tickFormatter={(ts) => ts ? format(new Date(ts), 'dd.MM HH:mm') : ''}
                    />
                    <YAxis domain={[0, 100]} />
                    <RechartsTooltip 
                      labelFormatter={(ts) => ts ? format(new Date(ts), 'dd.MM.yyyy HH:mm') : ''}
                      formatter={(value: any) => [`${safeNumber(value).toFixed(1)}%`, 'Память']}
                    />
                    <Legend />
                    <Line 
                      type="monotone" 
                      dataKey="memory" 
                      stroke="#dc004e" 
                      name="Память %"
                      strokeWidth={2}
                      dot={false}
                      connectNulls={true}
                    />
                  </LineChart>
                </ResponsiveContainer>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12}>
            <Alert severity="info">
              Среднее использование памяти: {formatPercent(safeMetrics.avgMemory)} | Пиковое: {formatPercent(safeMetrics.peakMemory)} | P95: {formatPercent(safeMetrics.p95Memory)}
            </Alert>
          </Grid>
        </Grid>
      )}

      {/* Network Tab */}
      {tab === 'network' && safeMetrics && (
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Входящий трафик
                </Typography>
                <Typography variant="h4" color="primary">
                  {formatBytes(safeMetrics.totalNetworkIn)}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  За выбранный период
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12} md={6}>
            <Card>
              <CardContent>
                <Typography variant="h6" gutterBottom>
                  Исходящий трафик
                </Typography>
                <Typography variant="h4" color="secondary">
                  {formatBytes(safeMetrics.totalNetworkOut)}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  За выбранный период
                </Typography>
              </CardContent>
            </Card>
          </Grid>
          <Grid item xs={12}>
            <Alert severity="info">
              Всего трафика: {formatBytes(safeMetrics.totalNetworkIn + safeMetrics.totalNetworkOut)}
            </Alert>
          </Grid>
        </Grid>
      )}

      {/* Profile Tab */}
      {tab === 'profile' && safeProfile && (
        <Grid container spacing={3}>
          <Grid item xs={12}>
            <Paper sx={{ p: 3 }}>
              <Typography variant="h6" gutterBottom>
                <Insights sx={{ mr: 1, verticalAlign: 'middle' }} />
                Профиль производительности
              </Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Рассчитан на основе {safeProfile.totalSamples} измерений за {safeProfile.daysOfData} дней
              </Typography>

              <Grid container spacing={2} sx={{ mt: 1 }}>
                <Grid item xs={12} md={4}>
                  <Card variant="outlined">
                    <CardContent>
                      <Typography color="text.secondary" gutterBottom>
                        Базовый CPU
                      </Typography>
                      <Typography variant="h5">
                        {formatPercent(safeProfile.baselineCpu)}
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
                        {formatPercent(safeProfile.baselineMemory)}
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
                        {formatNumber(safeProfile.baselineResponseTime)} мс
                      </Typography>
                    </CardContent>
                  </Card>
                </Grid>
              </Grid>

              {safeProfile.peakPeriods && safeProfile.peakPeriods.length > 0 && (
                <>
                  <Typography variant="subtitle1" sx={{ mt: 3, mb: 2 }}>
                    Пиковые периоды
                  </Typography>
                  <List>
                    {safeProfile.peakPeriods.map((period, idx) => (
                      <ListItem key={idx}>
                        <ListItemIcon>
                          <Timeline />
                        </ListItemIcon>
                        <ListItemText
                          primary={`${period.pattern === 'daily' ? 'Ежедневно' : 'Еженедельно'}: ${period.timeRange}`}
                          secondary={`Средняя нагрузка: ${formatPercent(period.avgLoad)}, пик: ${formatPercent(period.peakLoad)}`}
                        />
                      </ListItem>
                    ))}
                  </List>
                </>
              )}

              {safeProfile.recommendations && safeProfile.recommendations.length > 0 && (
                <>
                  <Typography variant="subtitle1" sx={{ mt: 3, mb: 2 }}>
                    <Recommend sx={{ mr: 1, verticalAlign: 'middle' }} />
                    Рекомендации
                  </Typography>
                  <List>
                    {safeProfile.recommendations.map((rec, idx) => (
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
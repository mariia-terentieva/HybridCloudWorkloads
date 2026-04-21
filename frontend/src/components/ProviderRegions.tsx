import React, { useEffect, useState } from 'react';
import {
  Paper,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  IconButton,
  Tooltip,
  Collapse,
  LinearProgress,
  Tabs,
  Tab,
  Divider,
} from '@mui/material';
import {
  Public,
  Storage,
  CheckCircle,
  Warning,
  Error as ErrorIcon,
  ExpandMore,
  ExpandLess,
  Info,
  LocationOn,
  Security,
  Dns,
} from '@mui/icons-material';
import { providerService } from '../services/providerService';
import { RegionDetail, InstanceTypeSummary } from '../types/providers';

interface ProviderRegionsProps {
  providerCode: string;
  providerName: string;
}

interface TabPanelProps {
  children?: React.ReactNode;
  index: number;
  value: number;
}

const TabPanel: React.FC<TabPanelProps> = ({ children, value, index }) => (
  <div hidden={value !== index} style={{ marginTop: 16 }}>
    {value === index && children}
  </div>
);

export const ProviderRegions: React.FC<ProviderRegionsProps> = ({
  providerCode,
  providerName,
}) => {
  const [tabValue, setTabValue] = useState(0);
  const [regions, setRegions] = useState<RegionDetail[]>([]);
  const [continents, setContinents] = useState<string[]>([]);
  const [selectedContinent, setSelectedContinent] = useState<string | null>(null);
  const [expandedRegion, setExpandedRegion] = useState<string | null>(null);
  const [regionInstanceTypes, setRegionInstanceTypes] = useState<Record<string, InstanceTypeSummary[]>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingTypes, setIsLoadingTypes] = useState<Record<string, boolean>>({});
  const [error, setError] = useState<string | null>(null);
  const [stats, setStats] = useState({
    totalRegions: 0,
    totalInstanceTypes: 0,
    activeRegions: 0,
  });

  useEffect(() => {
    loadRegions();
  }, [providerCode]);

  const loadRegions = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await providerService.getRegionsExtended(providerCode);
      setRegions(response.regions);
      setContinents(response.continents);
      
      // Подсчет статистики
      const active = response.regions.filter(r => r.status === 'Available').length;
      const totalTypes = response.regions.reduce((sum, r) => sum + r.instanceTypesCount, 0);
      
      setStats({
        totalRegions: response.totalRegions,
        totalInstanceTypes: totalTypes,
        activeRegions: active,
      });
    } catch (err: any) {
      setError(err.message || 'Ошибка загрузки регионов');
    } finally {
      setIsLoading(false);
    }
  };

  const loadInstanceTypes = async (regionCode: string) => {
    if (regionInstanceTypes[regionCode]) return;
    
    setIsLoadingTypes(prev => ({ ...prev, [regionCode]: true }));
    try {
      const response = await providerService.getRegionDetail(providerCode, regionCode, true);
      setRegionInstanceTypes(prev => ({ ...prev, [regionCode]: response.instanceTypes }));
    } catch (err) {
      console.error(`Failed to load instance types for ${regionCode}`, err);
    } finally {
      setIsLoadingTypes(prev => ({ ...prev, [regionCode]: false }));
    }
  };

  const handleExpandRegion = async (regionCode: string) => {
    if (expandedRegion === regionCode) {
      setExpandedRegion(null);
    } else {
      setExpandedRegion(regionCode);
      await loadInstanceTypes(regionCode);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Available':
        return <CheckCircle sx={{ fontSize: 16, color: 'success.main' }} />;
      case 'Limited':
        return <Warning sx={{ fontSize: 16, color: 'warning.main' }} />;
      case 'Unavailable':
        return <ErrorIcon sx={{ fontSize: 16, color: 'error.main' }} />;
      default:
        return <Info sx={{ fontSize: 16, color: 'info.main' }} />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Available': return 'success';
      case 'Limited': return 'warning';
      case 'Unavailable': return 'error';
      default: return 'default';
    }
  };

  const filteredRegions = selectedContinent
    ? regions.filter(r => r.continent === selectedContinent)
    : regions;

  const continentStats = continents.map(continent => ({
    name: continent,
    count: regions.filter(r => r.continent === continent).length,
    active: regions.filter(r => r.continent === continent && r.status === 'Available').length,
  }));

  if (isLoading) {
    return (
      <Paper sx={{ p: 3, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Paper>
    );
  }

  if (error) {
    return (
      <Alert severity="error" sx={{ m: 2 }}>
        {error}
      </Alert>
    );
  }

  return (
    <Box>
      {/* Статистика */}
      <Grid container spacing={2} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center' }}>
              <Public sx={{ fontSize: 40, color: 'primary.main', mb: 1 }} />
              <Typography variant="h5">{stats.totalRegions}</Typography>
              <Typography variant="body2" color="text.secondary">
                Всего регионов
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center' }}>
              <CheckCircle sx={{ fontSize: 40, color: 'success.main', mb: 1 }} />
              <Typography variant="h5">{stats.activeRegions}</Typography>
              <Typography variant="body2" color="text.secondary">
                Активных регионов
              </Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} sm={4}>
          <Card variant="outlined">
            <CardContent sx={{ textAlign: 'center' }}>
              <Storage sx={{ fontSize: 40, color: 'info.main', mb: 1 }} />
              <Typography variant="h5">{stats.totalInstanceTypes}</Typography>
              <Typography variant="body2" color="text.secondary">
                Типов инстансов
              </Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Континенты */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Typography variant="subtitle2" gutterBottom>
          Фильтр по континентам
        </Typography>
        <Box display="flex" flexWrap="wrap" gap={1}>
          <Chip
            label={`Все (${regions.length})`}
            onClick={() => setSelectedContinent(null)}
            color={!selectedContinent ? 'primary' : 'default'}
            variant={!selectedContinent ? 'filled' : 'outlined'}
          />
          {continentStats.map(stat => (
            <Chip
              key={stat.name}
              label={`${stat.name} (${stat.count})`}
              onClick={() => setSelectedContinent(stat.name)}
              color={selectedContinent === stat.name ? 'primary' : 'default'}
              variant={selectedContinent === stat.name ? 'filled' : 'outlined'}
            />
          ))}
        </Box>
      </Paper>

      {/* Табы */}
      <Paper sx={{ mb: 2 }}>
        <Tabs value={tabValue} onChange={(_, v) => setTabValue(v)}>
          <Tab label="Список регионов" />
          <Tab label="Карта" disabled />
          <Tab label="Сравнение" disabled />
        </Tabs>
      </Paper>

      {/* Список регионов */}
      <TabPanel value={tabValue} index={0}>
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Регион</TableCell>
                <TableCell>Континент</TableCell>
                <TableCell align="center">Зоны доступности</TableCell>
                <TableCell align="center">Типы инстансов</TableCell>
                <TableCell align="center">Статус</TableCell>
                <TableCell align="center">Действия</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {filteredRegions.map((region) => (
                <React.Fragment key={region.code}>
                  <TableRow hover>
                    <TableCell>
                      <Box display="flex" alignItems="center" gap={1}>
                        <LocationOn sx={{ color: 'text.secondary' }} />
                        <Box>
                          <Typography variant="body2" fontWeight="medium">
                            {region.displayName}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {region.code}
                          </Typography>
                        </Box>
                      </Box>
                    </TableCell>
                    <TableCell>{region.continent}</TableCell>
                    <TableCell align="center">
                      <Chip
                        size="small"
                        label={region.availabilityZones}
                        icon={<Dns sx={{ fontSize: 14 }} />}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Chip
                        size="small"
                        label={region.instanceTypesCount}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Chip
                        size="small"
                        icon={getStatusIcon(region.status)}
                        label={region.status === 'Available' ? 'Доступен' : region.status}
                        color={getStatusColor(region.status) as any}
                        variant="outlined"
                      />
                    </TableCell>
                    <TableCell align="center">
                      <Tooltip title={expandedRegion === region.code ? 'Скрыть' : 'Подробнее'}>
                        <IconButton
                          size="small"
                          onClick={() => handleExpandRegion(region.code)}
                        >
                          {expandedRegion === region.code ? <ExpandLess /> : <ExpandMore />}
                        </IconButton>
                      </Tooltip>
                    </TableCell>
                  </TableRow>
                  
                  {/* Расширенная информация */}
                  <TableRow>
                    <TableCell colSpan={6} sx={{ p: 0 }}>
                      <Collapse in={expandedRegion === region.code} timeout="auto" unmountOnExit>
                        <Box sx={{ p: 3, bgcolor: 'grey.50' }}>
                          {/* Информация о регионе */}
                          <Grid container spacing={2} sx={{ mb: 2 }}>
                            <Grid item xs={12} md={6}>
                              <Typography variant="subtitle2" gutterBottom>
                                Информация о регионе
                              </Typography>
                              <Box display="flex" flexWrap="wrap" gap={1}>
                                {region.country && (
                                  <Chip size="small" label={`🇺🇳 ${region.country}`} variant="outlined" />
                                )}
                                {region.city && (
                                  <Chip size="small" label={`🏙️ ${region.city}`} variant="outlined" />
                                )}
                              </Box>
                            </Grid>
                            <Grid item xs={12} md={6}>
                              <Typography variant="subtitle2" gutterBottom>
                                <Security sx={{ fontSize: 14, mr: 0.5, verticalAlign: 'middle' }} />
                                Комплаенс
                              </Typography>
                              <Box display="flex" flexWrap="wrap" gap={0.5}>
                                {region.compliance?.map(c => (
                                  <Chip key={c} size="small" label={c} variant="outlined" />
                                )) || <Typography variant="caption" color="text.secondary">Нет данных</Typography>}
                              </Box>
                            </Grid>
                          </Grid>
                          
                          <Divider sx={{ my: 2 }} />
                          
                          {/* Типы инстансов */}
                          <Typography variant="subtitle2" gutterBottom>
                            Доступные типы инстансов
                          </Typography>
                          
                          {isLoadingTypes[region.code] ? (
                            <LinearProgress sx={{ my: 2 }} />
                          ) : regionInstanceTypes[region.code]?.length > 0 ? (
                            <TableContainer>
                              <Table size="small">
                                <TableHead>
                                  <TableRow>
                                    <TableCell>Тип</TableCell>
                                    <TableCell>Категория</TableCell>
                                    <TableCell align="right">vCPU</TableCell>
                                    <TableCell align="right">RAM (ГБ)</TableCell>
                                    <TableCell>Статус</TableCell>
                                  </TableRow>
                                </TableHead>
                                <TableBody>
                                  {regionInstanceTypes[region.code].slice(0, 10).map((type) => (
                                    <TableRow key={type.id} hover>
                                      <TableCell>
                                        <Typography variant="body2" fontWeight="medium">
                                          {type.typeCode}
                                        </Typography>
                                        <Typography variant="caption" color="text.secondary">
                                          {type.displayName}
                                        </Typography>
                                      </TableCell>
                                      <TableCell>
                                        <Chip size="small" label={type.category} variant="outlined" />
                                      </TableCell>
                                      <TableCell align="right">{type.vcpuCount}</TableCell>
                                      <TableCell align="right">{type.memoryGb}</TableCell>
                                      <TableCell>
                                        <Chip
                                          size="small"
                                          label={type.availability === 'Available' ? 'Доступен' : type.availability}
                                          color={type.availability === 'Available' ? 'success' : 'default'}
                                          variant="outlined"
                                        />
                                      </TableCell>
                                    </TableRow>
                                  ))}
                                </TableBody>
                              </Table>
                            </TableContainer>
                          ) : (
                            <Typography variant="body2" color="text.secondary">
                              Нет данных о типах инстансов
                            </Typography>
                          )}
                          
                          {regionInstanceTypes[region.code]?.length > 10 && (
                            <Typography variant="caption" color="text.secondary" sx={{ mt: 1, display: 'block' }}>
                              Показано 10 из {regionInstanceTypes[region.code].length} типов
                            </Typography>
                          )}
                        </Box>
                      </Collapse>
                    </TableCell>
                  </TableRow>
                </React.Fragment>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
        
        {filteredRegions.length === 0 && (
          <Paper sx={{ p: 3, textAlign: 'center' }}>
            <Typography color="text.secondary">
              Нет регионов для отображения
            </Typography>
          </Paper>
        )}
      </TabPanel>
    </Box>
  );
};
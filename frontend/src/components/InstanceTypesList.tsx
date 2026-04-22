import React, { useEffect, useState } from 'react';
import {
  Paper,
  Typography,
  Box,
  Grid,
  Chip,
  CircularProgress,
  Alert,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  MenuItem,
  Slider,
  FormControlLabel,
  Switch,
  Pagination,
  Button,
  Tooltip,
} from '@mui/material';
import {
  FilterList,
  Refresh,
  Memory,
  Speed,
} from '@mui/icons-material';
import { providerService } from '../services/providerService';
import { InstanceTypeDetail, FilterOptions, InstanceTypesResponseExtended } from '../types/providers';

interface InstanceTypesListProps {
  providerCode: string;
  providerName: string;
  regionCode?: string;
}

export const InstanceTypesList: React.FC<InstanceTypesListProps> = ({
  providerCode,
  providerName,
  regionCode,
}) => {
  const [instanceTypes, setInstanceTypes] = useState<InstanceTypeDetail[]>([]);
  const [filterOptions, setFilterOptions] = useState<FilterOptions | null>({
    categories: [],
    families: [],
    regions: [],
    cpuArchitectures: [],
    cpuRange: { min: 0, max: 128 },
    memoryRange: { min: 0, max: 512 },
    hasGpuAvailable: false,
  });
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [absoluteCpuRange, setAbsoluteCpuRange] = useState<{ min: number; max: number }>({ min: 0, max: 128 });
  const [absoluteMemoryRange, setAbsoluteMemoryRange] = useState<{ min: number; max: number }>({ min: 0, max: 512 });
  const [shouldReset, setShouldReset] = useState(false);
  
  // Фильтры
  const [showFilters, setShowFilters] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState<string>('');
  const [selectedFamily, setSelectedFamily] = useState<string>('');
  const [cpuRange, setCpuRange] = useState<[number, number]>([0, 128]);
  const [memoryRange, setMemoryRange] = useState<[number, number]>([0, 512]);
  const [hasGpu, setHasGpu] = useState<boolean | null>(null);
  const [selectedCpuArch, setSelectedCpuArch] = useState<string>('');
  
  // Пагинация
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);

useEffect(() => {
  loadInstanceTypes(true); // Первая загрузка
}, [providerCode, regionCode]);

useEffect(() => {
  if (page > 1) {
    loadInstanceTypes(false); // Пагинация - не первая загрузка
  }
}, [page]);

useEffect(() => {
  if (shouldReset) {
    loadInstanceTypes(false);
    setShouldReset(false);
  }
}, [shouldReset, selectedCategory, selectedFamily, cpuRange, memoryRange, hasGpu, selectedCpuArch, page]);

const loadInstanceTypes = async (isInitialLoad: boolean = false) => {
  setIsLoading(true);
  setError(null);
  try {
    const response = await providerService.getInstanceTypesExtended(providerCode, {
      regionCode,
      category: selectedCategory || undefined,
      family: selectedFamily || undefined,
      minCpu: cpuRange[0],
      maxCpu: cpuRange[1],
      minMemory: memoryRange[0],
      maxMemory: memoryRange[1],
      hasGpu: hasGpu ?? undefined,
      cpuArchitecture: selectedCpuArch || undefined,
      page,
      pageSize,
    });
    
    setInstanceTypes(response.items);
    setTotalCount(response.totalCount);
    setTotalPages(response.totalPages);
    
    // При первой загрузке устанавливаем абсолютные границы
    if (isInitialLoad && response.availableFilters) {
      setAbsoluteCpuRange({
        min: response.availableFilters.cpuRange.min,
        max: response.availableFilters.cpuRange.max,
      });
      setAbsoluteMemoryRange({
        min: response.availableFilters.memoryRange.min,
        max: response.availableFilters.memoryRange.max,
      });
      
      // Устанавливаем начальные значения слайдеров
      setCpuRange([
        response.availableFilters.cpuRange.min,
        response.availableFilters.cpuRange.max,
      ]);
      setMemoryRange([
        response.availableFilters.memoryRange.min,
        response.availableFilters.memoryRange.max,
      ]);
    }
    
    // Обновляем только опции фильтров (категории, семейства и т.д.)
    // НЕ трогаем границы слайдеров
    if (response.availableFilters) {
      setFilterOptions({
        categories: response.availableFilters.categories,
        families: response.availableFilters.families,
        regions: response.availableFilters.regions,
        cpuArchitectures: response.availableFilters.cpuArchitectures,
        cpuRange: response.availableFilters.cpuRange, // Сохраняем текущие границы с сервера
        memoryRange: response.availableFilters.memoryRange,
        hasGpuAvailable: response.availableFilters.hasGpuAvailable,
      });
    }
  } catch (err: any) {
    setError(err.message || 'Ошибка загрузки типов инстансов');
  } finally {
    setIsLoading(false);
  }
};

const handleApplyFilters = () => {
  setPage(1);
  loadInstanceTypes(false);
};

const handleResetFilters = () => {
  setSelectedCategory('');
  setSelectedFamily('');
  setHasGpu(null);
  setSelectedCpuArch('');
  setCpuRange([absoluteCpuRange.min, absoluteCpuRange.max]);
  setMemoryRange([absoluteMemoryRange.min, absoluteMemoryRange.max]);
  setPage(1);
  setShouldReset(true); // Устанавливаем флаг после обновления всех состояний
};

  const formatPrice = (price: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency,
      minimumFractionDigits: 3,
      maximumFractionDigits: 3,
    }).format(price);
  };

  if (isLoading && instanceTypes.length === 0) {
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
      {/* Заголовок с фильтрами */}
      <Paper sx={{ p: 2, mb: 3 }}>
        <Box display="flex" justifyContent="space-between" alignItems="center">
          <Typography variant="h6">
            Типы инстансов ({totalCount})
          </Typography>
          <Box display="flex" gap={1}>
            <Button
              variant="outlined"
              startIcon={<FilterList />}
              onClick={() => setShowFilters(!showFilters)}
            >
              Фильтры
            </Button>
            <Button
              variant="outlined"
              startIcon={<Refresh />}
              onClick={() => loadInstanceTypes(false)}
            >
              Обновить
            </Button>
          </Box>
        </Box>
        
{/* Панель фильтров */}
{showFilters && (
  <Grid container spacing={3} sx={{ mt: 2 }}>
    <Grid item xs={12} md={4}>
      <TextField
        select
        fullWidth
        label="Категория"
        value={selectedCategory}
        onChange={(e) => setSelectedCategory(e.target.value)}
        size="small"
      >
        <MenuItem value="">Все</MenuItem>
        {filterOptions?.categories?.map(cat => (
          <MenuItem key={cat} value={cat}>{cat}</MenuItem>
        ))}
      </TextField>
    </Grid>
    
    <Grid item xs={12} md={4}>
      <TextField
        select
        fullWidth
        label="Семейство"
        value={selectedFamily}
        onChange={(e) => setSelectedFamily(e.target.value)}
        size="small"
      >
        <MenuItem value="">Все</MenuItem>
        {filterOptions?.families?.map(fam => (
          <MenuItem key={fam} value={fam}>{fam}</MenuItem>
        ))}
      </TextField>
    </Grid>
    
    <Grid item xs={12} md={4}>
      <TextField
        select
        fullWidth
        label="Архитектура CPU"
        value={selectedCpuArch}
        onChange={(e) => setSelectedCpuArch(e.target.value)}
        size="small"
      >
        <MenuItem value="">Все</MenuItem>
        {filterOptions?.cpuArchitectures?.map(arch => (
          <MenuItem key={arch} value={arch}>{arch}</MenuItem>
        ))}
      </TextField>
    </Grid>
    
<Grid item xs={12} md={6}>
  <Typography variant="caption" gutterBottom>
    CPU (ядер): {cpuRange[0]} - {cpuRange[1]}
  </Typography>
  <Slider
    value={cpuRange}
    onChange={(_, val) => setCpuRange(val as [number, number])}
    valueLabelDisplay="auto"
    min={absoluteCpuRange.min}  // Используем абсолютные границы
    max={absoluteCpuRange.max}  // Используем абсолютные границы
    step={1}
  />
</Grid>

<Grid item xs={12} md={6}>
  <Typography variant="caption" gutterBottom>
    Память (ГБ): {memoryRange[0]} - {memoryRange[1]}
  </Typography>
  <Slider
    value={memoryRange}
    onChange={(_, val) => setMemoryRange(val as [number, number])}
    valueLabelDisplay="auto"
    min={absoluteMemoryRange.min}  // Используем абсолютные границы
    max={absoluteMemoryRange.max}  // Используем абсолютные границы
    step={0.5}
  />
</Grid>
    
    <Grid item xs={12}>
      <FormControlLabel
        control={
          <Switch
            checked={hasGpu === true}
            onChange={(e) => setHasGpu(e.target.checked ? true : null)}
            disabled={!filterOptions?.hasGpuAvailable}
          />
        }
        label="Только с GPU"
      />
    </Grid>
    
    <Grid item xs={12}>
      <Box display="flex" gap={2}>
        <Button variant="contained" onClick={handleApplyFilters}>
          Применить
        </Button>
        <Button variant="outlined" onClick={handleResetFilters}>
          Сбросить
        </Button>
      </Box>
    </Grid>
  </Grid>
)}
      </Paper>

      {/* Таблица инстансов */}
      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Тип</TableCell>
              <TableCell align="right">vCPU</TableCell>
              <TableCell align="right">RAM (ГБ)</TableCell>
              <TableCell>Сеть</TableCell>
              <TableCell align="right">Цена/час</TableCell>
              <TableCell align="right">Цена/мес</TableCell>
              <TableCell align="center">Spot</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {instanceTypes.map((type) => (
              <TableRow key={type.id} hover>
                <TableCell>
                  <Box display="flex" alignItems="center" gap={1}>
                    <Box>
                      <Typography variant="body2" fontWeight="medium">
                        {type.typeCode}
                      </Typography>
                      <Box display="flex" gap={0.5} flexWrap="wrap">
                        <Chip size="small" label={type.category} variant="outlined" />
                        {type.hasGpu && (
                          <Chip size="small" label="GPU" color="secondary" variant="outlined" />
                        )}
                      </Box>
                    </Box>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Box display="flex" alignItems="center" justifyContent="flex-end" gap={0.5}>
                    <Speed sx={{ fontSize: 14, color: 'text.secondary' }} />
                    {type.vcpuCount}
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Box display="flex" alignItems="center" justifyContent="flex-end" gap={0.5}>
                    <Memory sx={{ fontSize: 14, color: 'text.secondary' }} />
                    {type.memoryGb}
                  </Box>
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={`${type.networkBandwidthGbps} Gbps`}
                    variant="outlined"
                  />
                </TableCell>
                <TableCell align="right">
                  {type.pricing ? (
                    <Typography variant="body2" color="primary">
                      {formatPrice(type.pricing.onDemandHourly, type.pricing.currency)}
                    </Typography>
                  ) : (
                    <Typography variant="caption" color="text.secondary">—</Typography>
                  )}
                </TableCell>
                <TableCell align="right">
                  {type.pricing ? (
                    <Typography variant="body2">
                      {formatPrice(type.pricing.onDemandMonthly, type.pricing.currency)}
                    </Typography>
                  ) : (
                    <Typography variant="caption" color="text.secondary">—</Typography>
                  )}
                </TableCell>
                <TableCell align="center">
                  {type.pricing?.spotCurrentPrice ? (
                    <Tooltip title={`Экономия ${type.pricing.spotSavingsPercent}%`}>
                      <Chip
                        size="small"
                        label={formatPrice(type.pricing.spotCurrentPrice)}
                        color="success"
                        variant="outlined"
                      />
                    </Tooltip>
                  ) : (
                    <Typography variant="caption" color="text.secondary">—</Typography>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      {/* Пагинация */}
      {totalPages > 1 && (
        <Box display="flex" justifyContent="center" mt={3}>
          <Pagination
            count={totalPages}
            page={page}
            onChange={(_, val) => setPage(val)}
            color="primary"
          />
        </Box>
      )}

      {instanceTypes.length === 0 && !isLoading && (
        <Paper sx={{ p: 3, textAlign: 'center' }}>
          <Typography color="text.secondary">
            Нет типов инстансов, соответствующих фильтрам
          </Typography>
        </Paper>
      )}
    </Box>
  );
};
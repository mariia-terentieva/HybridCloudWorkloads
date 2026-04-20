import React, { useState } from 'react';
import {
  Paper,
  Typography,
  Box,
  Grid,
  TextField,
  Button,
  Card,
  CardContent,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Chip,
  FormControlLabel,
  Switch,
  InputAdornment,
  CircularProgress,
  Alert,
  Autocomplete,
  Avatar, 
} from '@mui/material';
import {
  CompareArrows,
  AttachMoney,
  Memory,
  Speed,
  Star,
} from '@mui/icons-material';
import { providerService } from '../services/providerService';
import {
  ProviderPriceComparison,
  BestPriceOffer,
  PROVIDER_COLORS,
  getProviderInitials,
} from '../types/providers';

export const PriceComparison: React.FC = () => {
  const [cpu, setCpu] = useState<number>(2);
  const [memory, setMemory] = useState<number>(4);
  const [selectedProviders, setSelectedProviders] = useState<string[]>(['aws', 'azure', 'gcp']);
  const [includeSpot, setIncludeSpot] = useState(true);
  const [includeReserved, setIncludeReserved] = useState(false);
  const [preferredRegion, setPreferredRegion] = useState<string>('');
  
  const [comparison, setComparison] = useState<ProviderPriceComparison | null>(null);
  const [bestOffers, setBestOffers] = useState<BestPriceOffer[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleCompare = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await providerService.comparePrices({
        cpu,
        memory,
        providers: selectedProviders,
      });
      setComparison(result);
      
      const offers = await providerService.getBestOffers({
        cpu,
        memory,
        region: preferredRegion || undefined,
        includeSpot,
        includeReserved,
      });
      setBestOffers(offers);
    } catch (err: any) {
      setError(err.message || 'Ошибка сравнения цен');
    } finally {
      setIsLoading(false);
    }
  };

  const formatPrice = (price: number, currency: string = 'USD') => {
    return new Intl.NumberFormat('ru-RU', {
      style: 'currency',
      currency: currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(price);
  };

  const providerOptions = [
    { code: 'aws', label: 'AWS' },
    { code: 'azure', label: 'Azure' },
    { code: 'gcp', label: 'GCP' },
    { code: 'yandex', label: 'Yandex Cloud' },
    { code: 'vk', label: 'VK Cloud' },
  ];

  return (
    <Box>
      {/* Форма параметров */}
      <Paper sx={{ p: 3, mb: 3 }}>
        <Typography variant="h6" gutterBottom>
          Параметры нагрузки
        </Typography>
        <Grid container spacing={3} alignItems="center">
          <Grid item xs={12} md={3}>
            <TextField
              fullWidth
              label="CPU (ядер)"
              type="number"
              value={cpu}
              onChange={(e) => setCpu(Number(e.target.value))}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Speed />
                  </InputAdornment>
                ),
                inputProps: { min: 1, max: 128 },
              }}
            />
          </Grid>
          <Grid item xs={12} md={3}>
            <TextField
              fullWidth
              label="Память (ГБ)"
              type="number"
              value={memory}
              onChange={(e) => setMemory(Number(e.target.value))}
              InputProps={{
                startAdornment: (
                  <InputAdornment position="start">
                    <Memory />
                  </InputAdornment>
                ),
                inputProps: { min: 1, max: 512, step: 0.5 },
              }}
            />
          </Grid>
          <Grid item xs={12} md={4}>
            <Autocomplete
              multiple
              options={providerOptions}
              getOptionLabel={(option) => option.label}
              value={providerOptions.filter((p) => selectedProviders.includes(p.code))}
              onChange={(_, newValue) => {
                setSelectedProviders(newValue.map((v) => v.code));
              }}
              renderInput={(params) => (
                <TextField {...params} label="Провайдеры" placeholder="Выберите провайдеров" />
              )}
            />
          </Grid>
          <Grid item xs={12} md={2}>
            <Button
              fullWidth
              variant="contained"
              startIcon={<CompareArrows />}
              onClick={handleCompare}
              disabled={isLoading}
            >
              {isLoading ? <CircularProgress size={24} /> : 'Сравнить'}
            </Button>
          </Grid>
        </Grid>

        <Box display="flex" gap={3} mt={2}>
          <FormControlLabel
            control={
              <Switch
                checked={includeSpot}
                onChange={(e) => setIncludeSpot(e.target.checked)}
              />
            }
            label="Включать Spot инстансы"
          />
          <FormControlLabel
            control={
              <Switch
                checked={includeReserved}
                onChange={(e) => setIncludeReserved(e.target.checked)}
              />
            }
            label="Включать Reserved инстансы"
          />
          <TextField
            size="small"
            label="Предпочтительный регион"
            value={preferredRegion}
            onChange={(e) => setPreferredRegion(e.target.value)}
            placeholder="например, eu-west-1"
            sx={{ width: 200 }}
          />
        </Box>
      </Paper>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Результаты сравнения */}
      {comparison && (
        <>
          {/* Лучшие предложения */}
          {bestOffers.length > 0 && (
            <Paper sx={{ p: 3, mb: 3 }}>
              <Typography variant="h6" gutterBottom>
                <Star sx={{ mr: 1, verticalAlign: 'middle', color: 'warning.main' }} />
                Лучшие предложения
              </Typography>
              <Grid container spacing={2}>
                {bestOffers.slice(0, 3).map((offer, index) => (
                  <Grid item xs={12} md={4} key={index}>
                    <Card variant="outlined">
                      <CardContent>
                        <Box display="flex" alignItems="center" gap={1} mb={1}>
          <Avatar
            sx={{
              width: 24,
              height: 24,
              bgcolor: PROVIDER_COLORS[offer.providerCode] || 'grey.300',
              color: '#fff',
              fontSize: 10,
              fontWeight: 'bold',
            }}
          >
            {getProviderInitials(offer.providerCode)}
          </Avatar>
                          <Typography variant="subtitle1">
                            {offer.providerName}
                          </Typography>
                          <Chip
                            size="small"
                            label={offer.pricingModel}
                            color="primary"
                            variant="outlined"
                          />
                        </Box>
                        <Typography variant="body2" color="text.secondary">
                          {offer.instanceType} • {offer.regionName}
                        </Typography>
                        <Typography variant="h6" color="primary" sx={{ mt: 1 }}>
                          {formatPrice(offer.hourlyPrice)}/час
                        </Typography>
                        <Typography variant="body2">
                          {offer.vcpu} vCPU • {offer.memoryGb} ГБ
                        </Typography>
                        {offer.savingsPercent > 0 && (
                          <Chip
                            size="small"
                            label={`Экономия ${offer.savingsPercent}%`}
                            color="success"
                            sx={{ mt: 1 }}
                          />
                        )}
                      </CardContent>
                    </Card>
                  </Grid>
                ))}
              </Grid>
            </Paper>
          )}

          {/* Таблица сравнения */}
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Все варианты ({comparison.options.length})
            </Typography>
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Провайдер</TableCell>
                    <TableCell>Регион</TableCell>
                    <TableCell>Тип инстанса</TableCell>
                    <TableCell align="right">vCPU</TableCell>
                    <TableCell align="right">RAM (ГБ)</TableCell>
                    <TableCell align="right">Цена/час</TableCell>
                    <TableCell align="right">Цена/мес</TableCell>
                    <TableCell align="right">Spot</TableCell>
                    <TableCell align="right">Оценка</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {comparison.options.map((option, index) => (
                    <TableRow
                      key={index}
                      sx={{
                        backgroundColor: option === comparison.bestOption ? 'action.hover' : 'inherit',
                      }}
                    >
                      <TableCell>
                        <Box display="flex" alignItems="center" gap={1}>
          <Avatar
            sx={{
              width: 24,
              height: 24,
              bgcolor: PROVIDER_COLORS[option.providerCode] || 'grey.300',
              color: '#fff',
              fontSize: 10,
              fontWeight: 'bold',
            }}
          >
            {getProviderInitials(option.providerCode)}
          </Avatar>
                          {option.providerName}
                        </Box>
                      </TableCell>
                      <TableCell>{option.regionName}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          label={option.instanceType}
                          sx={{ bgcolor: PROVIDER_COLORS[option.providerCode] + '20' }}
                        />
                      </TableCell>
                      <TableCell align="right">{option.vcpu}</TableCell>
                      <TableCell align="right">{option.memoryGb}</TableCell>
                      <TableCell align="right">
                        <AttachMoney sx={{ fontSize: 14, verticalAlign: 'middle' }} />
                        {option.onDemandHourly.toFixed(3)}
                      </TableCell>
                      <TableCell align="right">
                        {formatPrice(option.onDemandMonthly)}
                      </TableCell>
                      <TableCell align="right">
                        {option.spotHourly ? (
                          <Chip
                            size="small"
                            label={`${option.spotHourly.toFixed(3)} (${option.spotSavingsPercent}%)`}
                            color="success"
                            variant="outlined"
                          />
                        ) : (
                          '-'
                        )}
                      </TableCell>
                      <TableCell align="right">
                        <Chip
                          size="small"
                          label={option.score.toFixed(0)}
                          color={option.score > 80 ? 'success' : option.score > 60 ? 'warning' : 'default'}
                        />
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </Paper>
        </>
      )}
    </Box>
  );
};
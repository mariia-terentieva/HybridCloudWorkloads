import React from 'react';
import {
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  TextField,
  Button,
  MenuItem,
  Box,
  Grid,
  Alert,
  IconButton,
  Typography,
  InputAdornment,
  FormControl,
  InputLabel,
  Select,
  Chip,
  Divider,
} from '@mui/material';
import {
  Add,
  Remove,
  ArrowBack,
  Speed,
  BusinessCenter,
  AccessTime,
} from '@mui/icons-material';
import { useForm, Controller, useFieldArray } from 'react-hook-form';
import { 
  Workload, 
  CreateWorkloadRequest, 
  UpdateWorkloadRequest,
  UsagePattern,
  CriticalityClass,
  BudgetTier,
  TimeRange,
} from '../types';
import { predefinedImages, getImageInfo } from '../utils/dockerImages';

// Интерфейс для переменных окружения
interface EnvironmentVariable {
  key: string;
  value: string;
}

// Интерфейс данных формы с новыми полями классификации
interface WorkloadFormData {
  name: string;
  description?: string;
  type: string;
  requiredCpu: number;
  requiredMemory: number;
  requiredStorage: number;
  containerImage?: string;
  exposedPort?: number;
  environmentVars: EnvironmentVariable[];
  
  // НОВЫЕ ПОЛЯ КЛАССИФИКАЦИИ
  usagePattern: UsagePattern;
  criticality: CriticalityClass;
  budgetTier: BudgetTier;
  
  // SLA требования
  slaRequirements: {
    maxResponseTimeMs: number;
    allowedDowntimePerMonth: number;
    availabilityTarget: number;
    requiresRedundancy: boolean;
    minReplicas: number;
    maxRecoveryTimeMinutes: number;
  };
  
  // Бизнес-часы
  businessHours: {
    timezone: string;
    peakHours: TimeRange[];
    weekendLoadPercent: number;
    workingDays: number[];
  };
  
  tags: string;
}

interface WorkloadFormProps {
  open: boolean;
  workload?: Workload | null;
  onClose: () => void;
  onSubmit: (data: CreateWorkloadRequest | UpdateWorkloadRequest) => void;
}

// Опции для выпадающих списков
const workloadTypes = [
  { value: 'VirtualMachine', label: 'Виртуальная машина' },
  { value: 'Database', label: 'База данных' },
  { value: 'WebService', label: 'Веб-сервис' },
  { value: 'Container', label: 'Контейнер' },
  { value: 'BatchJob', label: 'Пакетное задание' },
];

const usagePatternOptions = [
  { value: 'Constant', label: 'Постоянная', description: 'Нагрузка 24/7 без значительных колебаний', icon: '🔄' },
  { value: 'Periodic', label: 'Периодическая', description: 'Нагрузка изменяется по предсказуемому расписанию', icon: '📅' },
  { value: 'Burst', label: 'Пиковая', description: 'Кратковременные пиковые нагрузки', icon: '⚡' },
  { value: 'Unpredictable', label: 'Непредсказуемая', description: 'Случайные всплески нагрузки', icon: '🎲' },
];

const criticalityOptions = [
  { value: 'MissionCritical', label: 'Критическая', description: 'Критически важные для бизнеса системы, простой недопустим', icon: '🔴', color: 'error' },
  { value: 'BusinessEssential', label: 'Важная', description: 'Важные системы, допустимы кратковременные перерывы', icon: '🟠', color: 'warning' },
  { value: 'NonCritical', label: 'Некритичная', description: 'Некритичные нагрузки, тестовые среды', icon: '🟢', color: 'success' },
];

const budgetTierOptions = [
  { value: 'High', label: 'Высокий', description: 'Приоритет производительности, стоимость вторична', icon: '💰' },
  { value: 'Medium', label: 'Средний', description: 'Баланс стоимости и производительности', icon: '⚖️' },
  { value: 'Low', label: 'Низкий', description: 'Приоритет экономии, допустимо снижение производительности', icon: '📉' },
];

const timezoneOptions = [
  'UTC',
  'Europe/Moscow',
  'Europe/London',
  'America/New_York',
  'Asia/Singapore',
  'Asia/Tokyo',
];

const weekDays = [
  { value: 1, label: 'Пн' },
  { value: 2, label: 'Вт' },
  { value: 3, label: 'Ср' },
  { value: 4, label: 'Чт' },
  { value: 5, label: 'Пт' },
  { value: 6, label: 'Сб' },
  { value: 7, label: 'Вс' },
];

export const WorkloadForm: React.FC<WorkloadFormProps> = ({
  open,
  workload,
  onClose,
  onSubmit,
}) => {
  const {
    control,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<WorkloadFormData>({
    defaultValues: {
      name: '',
      description: '',
      type: 'VirtualMachine',
      requiredCpu: 1,
      requiredMemory: 1,
      requiredStorage: 10,
      containerImage: 'nginx:latest',
      exposedPort: 80,
      environmentVars: [{ key: '', value: '' }],
      
      // НОВЫЕ ПОЛЯ - значения по умолчанию
      usagePattern: 'Constant',
      criticality: 'NonCritical',
      budgetTier: 'Medium',
      
      slaRequirements: {
        maxResponseTimeMs: 1000,
        allowedDowntimePerMonth: 60,
        availabilityTarget: 99.9,
        requiresRedundancy: false,
        minReplicas: 1,
        maxRecoveryTimeMinutes: 60,
      },
      
      businessHours: {
        timezone: 'UTC',
        peakHours: [{ start: '09:00', end: '18:00' }],
        weekendLoadPercent: 30,
        workingDays: [1, 2, 3, 4, 5],
      },
      
      tags: '',
    },
  });

  const [inputMode, setInputMode] = React.useState<'select' | 'custom'>('select');
  const [showSlaDetails, setShowSlaDetails] = React.useState(false);
  const [showBusinessHours, setShowBusinessHours] = React.useState(false);

  const { fields: envFields, append: appendEnv, remove: removeEnv } = useFieldArray({
    control,
    name: 'environmentVars',
  });

  const { fields: peakHoursFields, append: appendPeakHour, remove: removePeakHour } = useFieldArray({
    control,
    name: 'businessHours.peakHours',
  });

  //const containerImage = watch('containerImage');
  //const criticality = watch('criticality');
  //const budgetTier = watch('budgetTier');

  // Определяем является ли образ custom
  /*const isCustomImage = React.useMemo(() => {
    if (!containerImage) return false;
    if (inputMode === 'custom') return true;
    return !predefinedImages.some(img => img.value === containerImage);
  }, [containerImage, inputMode]);*/

  // Функция для парсинга JSON строки в массив объектов
  const parseEnvironmentVariables = (envJson?: string): EnvironmentVariable[] => {
    if (!envJson) return [];
    try {
      const parsed = JSON.parse(envJson);
      return Object.entries(parsed).map(([key, value]) => ({
        key,
        value: typeof value === 'string' ? value : String(value)
      }));
    } catch {
      return [];
    }
  };

  // Функция для парсинга тегов
  /*const parseTags = (tagsString?: string): string[] => {
    if (!tagsString) return [];
    return tagsString.split(',').map(t => t.trim()).filter(t => t);
  };*/

  // Загрузка данных при редактировании
  React.useEffect(() => {
    if (workload) {
      const environmentVars = parseEnvironmentVariables(workload.environmentVariables);
      
      // Парсим SLA требования
      let slaRequirements = {
        maxResponseTimeMs: 1000,
        allowedDowntimePerMonth: 60,
        availabilityTarget: 99.9,
        requiresRedundancy: false,
        minReplicas: 1,
        maxRecoveryTimeMinutes: 60,
      };
      
      if (workload.slaRequirements) {
        slaRequirements = { ...slaRequirements, ...workload.slaRequirements };
      }
      
      // Парсим бизнес-часы
      let businessHours = {
        timezone: 'UTC',
        peakHours: [{ start: '09:00', end: '18:00' }],
        weekendLoadPercent: 30,
        workingDays: [1, 2, 3, 4, 5],
      };
      
      if (workload.businessHours) {
        businessHours = { ...businessHours, ...workload.businessHours };
      }
      
      reset({
        name: workload.name,
        description: workload.description,
        type: workload.type,
        requiredCpu: workload.requiredCpu,
        requiredMemory: workload.requiredMemory,
        requiredStorage: workload.requiredStorage,
        containerImage: workload.containerImage || 'nginx:latest',
        exposedPort: workload.exposedPort || 80,
        environmentVars: environmentVars.length > 0 ? environmentVars : [{ key: '', value: '' }],
        
        // НОВЫЕ ПОЛЯ
        usagePattern: workload.usagePattern || 'Constant',
        criticality: workload.criticality || 'NonCritical',
        budgetTier: workload.budgetTier || 'Medium',
        slaRequirements,
        businessHours,
        tags: workload.tags?.join(', ') || '',
      });
    }
  }, [workload, reset]);

  // Обработчик изменения образа
  const handleImageChange = (newImage: string) => {
    setValue('containerImage', newImage);
    const selectedImage = getImageInfo(newImage);
    if (selectedImage?.port) {
      setValue('exposedPort', selectedImage.port);
    }
  };

  // Валидация чисел
  /*const allowOnlyDigits = (e: React.KeyboardEvent) => {
    if (!/[\d]/.test(e.key) && 
        e.key !== 'Backspace' && 
        e.key !== 'Tab' && 
        e.key !== 'Delete' && 
        e.key !== 'ArrowLeft' && 
        e.key !== 'ArrowRight') {
      e.preventDefault();
    }
  };*/

  const onSubmitForm = (data: WorkloadFormData) => {
    // Преобразуем environmentVars в JSON строку
    const envVars = data.environmentVars.reduce((acc, curr) => {
      if (curr.key && curr.value) {
        acc[curr.key] = curr.value;
      }
      return acc;
    }, {} as Record<string, string>);

    // Преобразуем теги
    const tags = data.tags ? data.tags.split(',').map(t => t.trim()).filter(t => t) : undefined;

    const submitData: CreateWorkloadRequest = {
      name: data.name,
      description: data.description,
      type: data.type,
      requiredCpu: data.requiredCpu,
      requiredMemory: data.requiredMemory,
      requiredStorage: data.requiredStorage,
      containerImage: data.containerImage || undefined,
      exposedPort: data.exposedPort || 80,
      environmentVariables: Object.keys(envVars).length > 0 ? JSON.stringify(envVars) : undefined,
      
      // НОВЫЕ ПОЛЯ
      usagePattern: data.usagePattern,
      criticality: data.criticality,
      budgetTier: data.budgetTier,
      slaRequirements: data.slaRequirements,
      businessHours: data.businessHours,
      tags,
    };

    onSubmit(submitData);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth scroll="body">
      <DialogTitle>
        {workload ? 'Редактировать нагрузку' : 'Создать новую нагрузку'}
      </DialogTitle>
      <form onSubmit={handleSubmit(onSubmitForm)}>
        <DialogContent dividers>
          <Grid container spacing={3}>
            
            {/* ========== ОСНОВНАЯ ИНФОРМАЦИЯ ========== */}
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom>
                Основная информация
              </Typography>
            </Grid>
            
            <Grid item xs={12} md={6}>
              <Controller
                name="name"
                control={control}
                rules={{ required: 'Имя обязательно' }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Название"
                    error={!!errors.name}
                    helperText={errors.name?.message}
                    fullWidth
                    required
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="type"
                control={control}
                rules={{ required: 'Тип обязателен' }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label="Тип"
                    error={!!errors.type}
                    helperText={errors.type?.message}
                    fullWidth
                    required
                  >
                    {workloadTypes.map((option) => (
                      <MenuItem key={option.value} value={option.value}>
                        {option.label}
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>

            <Grid item xs={12}>
              <Controller
                name="description"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Описание"
                    multiline
                    rows={2}
                    fullWidth
                  />
                )}
              />
            </Grid>

            {/* ========== ТЕГИ ========== */}
            <Grid item xs={12}>
              <Controller
                name="tags"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    label="Теги"
                    placeholder="backend, production, critical (через запятую)"
                    fullWidth
                    helperText="Теги для категоризации и поиска"
                  />
                )}
              />
            </Grid>

            {/* ========== РЕСУРСЫ ========== */}
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                Требования к ресурсам
              </Typography>
            </Grid>

            <Grid item xs={12} md={4}>
              <Controller
                name="requiredCpu"
                control={control}
                rules={{ 
                  required: 'CPU обязательно',
                  min: { value: 1, message: 'Минимум 1 ядро CPU' },
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется CPU (ядер)"
                    error={!!errors.requiredCpu}
                    helperText={errors.requiredCpu?.message}
                    fullWidth
                    inputProps={{ min: 1 }}
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={4}>
              <Controller
                name="requiredMemory"
                control={control}
                rules={{ 
                  required: 'Память обязательна',
                  min: { value: 0.5, message: 'Минимум 0.5 ГБ' },
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется памяти (ГБ)"
                    error={!!errors.requiredMemory}
                    helperText={errors.requiredMemory?.message}
                    fullWidth
                    inputProps={{ min: 0.5, step: 0.1 }}
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={4}>
              <Controller
                name="requiredStorage"
                control={control}
                rules={{ 
                  required: 'Хранилище обязательно',
                  min: { value: 1, message: 'Минимум 1 ГБ' },
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется хранилища (ГБ)"
                    error={!!errors.requiredStorage}
                    helperText={errors.requiredStorage?.message}
                    fullWidth
                    inputProps={{ min: 1 }}
                  />
                )}
              />
            </Grid>

            {/* ========== НОВЫЙ РАЗДЕЛ: КЛАССИФИКАЦИЯ ========== */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>
                <BusinessCenter sx={{ mr: 1, verticalAlign: 'middle' }} />
                Классификация нагрузки
              </Typography>
              <Alert severity="info" sx={{ mb: 2 }}>
                Эти параметры помогают алгоритму оптимизации выбрать лучшее размещение
              </Alert>
            </Grid>

            {/* Паттерн использования */}
            <Grid item xs={12} md={4}>
              <Controller
                name="usagePattern"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label="Паттерн использования"
                    fullWidth
                    helperText="Как изменяется нагрузка"
                  >
                    {usagePatternOptions.map((option) => (
                      <MenuItem key={option.value} value={option.value}>
                        <Box display="flex" alignItems="center" gap={1}>
                          <span>{option.icon}</span>
                          <Box>
                            <Typography variant="body2">{option.label}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {option.description}
                            </Typography>
                          </Box>
                        </Box>
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>

            {/* Класс критичности */}
            <Grid item xs={12} md={4}>
              <Controller
                name="criticality"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label="Класс критичности"
                    fullWidth
                    helperText="Важность для бизнеса"
                  >
                    {criticalityOptions.map((option) => (
                      <MenuItem key={option.value} value={option.value}>
                        <Box display="flex" alignItems="center" gap={1}>
                          <span>{option.icon}</span>
                          <Box>
                            <Typography variant="body2">{option.label}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {option.description}
                            </Typography>
                          </Box>
                        </Box>
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>

            {/* Уровень бюджета */}
            <Grid item xs={12} md={4}>
              <Controller
                name="budgetTier"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label="Уровень бюджета"
                    fullWidth
                    helperText="Приоритет стоимости vs производительности"
                  >
                    {budgetTierOptions.map((option) => (
                      <MenuItem key={option.value} value={option.value}>
                        <Box display="flex" alignItems="center" gap={1}>
                          <span>{option.icon}</span>
                          <Box>
                            <Typography variant="body2">{option.label}</Typography>
                            <Typography variant="caption" color="text.secondary">
                              {option.description}
                            </Typography>
                          </Box>
                        </Box>
                      </MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>

            {/* ========== НОВЫЙ РАЗДЕЛ: ТРЕБОВАНИЯ SLA ========== */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="h6" gutterBottom>
                  <Speed sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Требования SLA
                </Typography>
                <Button 
                  size="small" 
                  onClick={() => setShowSlaDetails(!showSlaDetails)}
                >
                  {showSlaDetails ? 'Скрыть детали' : 'Настроить детали'}
                </Button>
              </Box>
            </Grid>

            {/* Базовые SLA параметры (всегда видны) */}
            <Grid item xs={12} md={4}>
              <Controller
                name="slaRequirements.availabilityTarget"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Целевая доступность (%)"
                    fullWidth
                    inputProps={{ min: 90, max: 99.999, step: 0.1 }}
                    helperText="Например: 99.9, 99.99"
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={4}>
              <Controller
                name="slaRequirements.maxResponseTimeMs"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Макс. время отклика (мс)"
                    fullWidth
                    inputProps={{ min: 1 }}
                  />
                )}
              />
            </Grid>

            <Grid item xs={12} md={4}>
              <Controller
                name="slaRequirements.allowedDowntimePerMonth"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Допустимый простой (мин/мес)"
                    fullWidth
                    inputProps={{ min: 0 }}
                  />
                )}
              />
            </Grid>

            {/* Детальные SLA параметры (по кнопке) */}
            {showSlaDetails && (
              <>
                <Grid item xs={12} md={4}>
                  <Controller
                    name="slaRequirements.minReplicas"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        type="number"
                        label="Минимум реплик"
                        fullWidth
                        inputProps={{ min: 1 }}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12} md={4}>
                  <Controller
                    name="slaRequirements.maxRecoveryTimeMinutes"
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        type="number"
                        label="Макс. время восстановления (мин)"
                        fullWidth
                        inputProps={{ min: 1 }}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12} md={4}>
                  <Controller
                    name="slaRequirements.requiresRedundancy"
                    control={control}
                    render={({ field }) => (
                      <FormControl fullWidth>
                        <InputLabel>Резервирование</InputLabel>
                        <Select
                          value={field.value ? 'yes' : 'no'}
                          onChange={(e) => field.onChange(e.target.value === 'yes')}
                          label="Резервирование"
                        >
                          <MenuItem value="yes">Требуется</MenuItem>
                          <MenuItem value="no">Не требуется</MenuItem>
                        </Select>
                      </FormControl>
                    )}
                  />
                </Grid>
              </>
            )}

            {/* ========== НОВЫЙ РАЗДЕЛ: БИЗНЕС-ЧАСЫ ========== */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Box display="flex" alignItems="center" justifyContent="space-between">
                <Typography variant="h6" gutterBottom>
                  <AccessTime sx={{ mr: 1, verticalAlign: 'middle' }} />
                  Бизнес-часы
                </Typography>
                <Button 
                  size="small" 
                  onClick={() => setShowBusinessHours(!showBusinessHours)}
                >
                  {showBusinessHours ? 'Скрыть' : 'Настроить'}
                </Button>
              </Box>
            </Grid>

            {/* Часовой пояс (всегда виден) */}
            <Grid item xs={12} md={6}>
              <Controller
                name="businessHours.timezone"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    select
                    label="Часовой пояс"
                    fullWidth
                  >
                    {timezoneOptions.map((tz) => (
                      <MenuItem key={tz} value={tz}>{tz}</MenuItem>
                    ))}
                  </TextField>
                )}
              />
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="businessHours.weekendLoadPercent"
                control={control}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Нагрузка в выходные (% от пиковой)"
                    fullWidth
                    inputProps={{ min: 0, max: 100 }}
                  />
                )}
              />
            </Grid>

            {/* Детальные настройки бизнес-часов */}
            {showBusinessHours && (
              <>
                <Grid item xs={12}>
                  <Controller
                    name="businessHours.workingDays"
                    control={control}
                    render={({ field }) => (
                      <FormControl fullWidth>
                        <InputLabel>Рабочие дни</InputLabel>
                        <Select
                          multiple
                          value={field.value || []}
                          onChange={(e) => field.onChange(e.target.value)}
                          renderValue={(selected) => (
                            <Box display="flex" flexWrap="wrap" gap={0.5}>
                              {(selected as number[]).map((value) => (
                                <Chip 
                                  key={value} 
                                  label={weekDays.find(d => d.value === value)?.label} 
                                  size="small" 
                                />
                              ))}
                            </Box>
                          )}
                          label="Рабочие дни"
                        >
                          {weekDays.map((day) => (
                            <MenuItem key={day.value} value={day.value}>
                              {day.label}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>
                    )}
                  />
                </Grid>

                <Grid item xs={12}>
                  <Typography variant="subtitle2" gutterBottom>
                    Пиковые часы
                  </Typography>
                  {peakHoursFields.map((field, index) => (
                    <Box key={field.id} display="flex" gap={2} mb={2}>
                      <Controller
                        name={`businessHours.peakHours.${index}.start`}
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            label="Начало"
                            placeholder="09:00"
                            fullWidth
                          />
                        )}
                      />
                      <Controller
                        name={`businessHours.peakHours.${index}.end`}
                        control={control}
                        render={({ field }) => (
                          <TextField
                            {...field}
                            label="Конец"
                            placeholder="18:00"
                            fullWidth
                          />
                        )}
                      />
                      <IconButton 
                        onClick={() => removePeakHour(index)}
                        disabled={peakHoursFields.length <= 1}
                      >
                        <Remove />
                      </IconButton>
                    </Box>
                  ))}
                  <Button
                    startIcon={<Add />}
                    onClick={() => appendPeakHour({ start: '09:00', end: '18:00' })}
                    size="small"
                  >
                    Добавить пиковый период
                  </Button>
                </Grid>
              </>
            )}

            {/* ========== СУЩЕСТВУЮЩИЙ РАЗДЕЛ: КОНФИГУРАЦИЯ ДЕПЛОЯ ========== */}
            <Grid item xs={12}>
              <Divider sx={{ my: 2 }} />
              <Typography variant="h6" gutterBottom>
                Конфигурация развертывания
              </Typography>
            </Grid>

            {/* Детали деплоя (без изменений) */}
            <Grid item xs={12} md={6}>
              {inputMode === 'select' ? (
                <Controller
                  name="containerImage"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      select
                      label="Container Image"
                      fullWidth
                      onChange={(e) => {
                        const newValue = e.target.value;
                        if (newValue === '') {
                          setInputMode('custom');
                          field.onChange('');
                        } else {
                          field.onChange(newValue);
                          handleImageChange(newValue);
                        }
                      }}
                    >
                      {predefinedImages.map((option) => (
                        <MenuItem key={option.value || 'custom'} value={option.value}>
                          {option.label}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />
              ) : (
                <Controller
                  name="containerImage"
                  control={control}
                  rules={{ required: 'Имя образа обязательно' }}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      label="Custom Docker Image"
                      placeholder="nginx:alpine, postgres:15"
                      fullWidth
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                      InputProps={{
                        endAdornment: (
                          <InputAdornment position="end">
                            <IconButton 
                              onClick={() => {
                                setInputMode('select');
                                field.onChange('nginx:latest');
                                handleImageChange('nginx:latest');
                              }}
                            >
                              <ArrowBack />
                            </IconButton>
                          </InputAdornment>
                        ),
                      }}
                    />
                  )}
                />
              )}
            </Grid>

            <Grid item xs={12} md={6}>
              <Controller
                name="exposedPort"
                control={control}
                rules={{ 
                  min: { value: 1, message: 'Port > 0' },
                  max: { value: 65535, message: 'Port < 65536' }
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Container Port"
                    error={!!errors.exposedPort}
                    helperText="Порт внутри контейнера"
                    fullWidth
                  />
                )}
              />
            </Grid>

            {/* Переменные окружения */}
            <Grid item xs={12}>
              <Typography variant="subtitle1" gutterBottom>
                Environment Variables
              </Typography>
              
              {envFields.map((field, index) => (
                <Box key={field.id} display="flex" gap={2} mb={2}>
                  <Controller
                    name={`environmentVars.${index}.key`}
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Key"
                        placeholder="DB_PASSWORD"
                        fullWidth
                      />
                    )}
                  />
                  <Controller
                    name={`environmentVars.${index}.value`}
                    control={control}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        label="Value"
                        type={field.value?.includes('PASSWORD') ? 'password' : 'text'}
                        fullWidth
                      />
                    )}
                  />
                  <IconButton 
                    onClick={() => removeEnv(index)}
                    disabled={envFields.length <= 1}
                  >
                    <Remove />
                  </IconButton>
                </Box>
              ))}
              
              <Button
                startIcon={<Add />}
                onClick={() => appendEnv({ key: '', value: '' })}
                size="small"
              >
                Добавить переменную
              </Button>
            </Grid>

          </Grid>
        </DialogContent>
        
        <DialogActions>
          <Button onClick={onClose}>Отмена</Button>
          <Button type="submit" variant="contained">
            {workload ? 'Обновить' : 'Создать'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  );
};
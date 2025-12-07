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
  Tooltip,
  InputAdornment,
} from '@mui/material';
import { Info, Add, Remove, Terminal, ArrowBack } from '@mui/icons-material';
import { useForm, Controller, useFieldArray, SubmitHandler } from 'react-hook-form';
import { Workload, CreateWorkloadRequest, UpdateWorkloadRequest } from '../types';
import { predefinedImages, getImageInfo } from '../utils/dockerImages';

// Интерфейс для переменных окружения
interface EnvironmentVariable {
  key: string;
  value: string;
}

// Интерфейс данных формы
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
}

interface WorkloadFormProps {
  open: boolean;
  workload?: Workload | null;
  onClose: () => void;
  onSubmit: (data: CreateWorkloadRequest | UpdateWorkloadRequest) => void;
}

const workloadTypes = [
  { value: 'VirtualMachine', label: 'Виртуальная машина' },
  { value: 'Database', label: 'База данных' },
  { value: 'WebService', label: 'Веб-сервис' },
  { value: 'Container', label: 'Контейнер' },
  { value: 'BatchJob', label: 'Пакетное задание' },
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
    watch,
    setValue,
    formState: { errors },
  } = useForm<WorkloadFormData>({
    defaultValues: {
      name: workload?.name || '',
      description: workload?.description || '',
      type: workload?.type || 'VirtualMachine',
      requiredCpu: workload?.requiredCpu || 1,
      requiredMemory: workload?.requiredMemory || 1,
      requiredStorage: workload?.requiredStorage || 10,
      containerImage: workload?.containerImage || 'nginx:latest',
      exposedPort: workload?.exposedPort || 80,
      environmentVars: [{ key: '', value: '' }],
    },
  });

  const [inputMode, setInputMode] = React.useState<'select' | 'custom'>('select');

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'environmentVars',
  });

// Функция для проверки, является ли переменная обязательной или зарезервированной
const isReservedEnvVar = (key: string, imageName: string): { isRequired: boolean; isReserved: boolean } => {
  const image = getImageInfo(imageName);
  
  // Определяем обязательные переменные для каждого типа БД
  const requiredVars: Record<string, string[]> = {
    'postgres': ['POSTGRES_PASSWORD', 'POSTGRES_DB', 'POSTGRES_USER'],
    'mysql': ['MYSQL_ROOT_PASSWORD', 'MYSQL_DATABASE'],
    'mongo': ['MONGO_INITDB_ROOT_USERNAME', 'MONGO_INITDB_ROOT_PASSWORD'],
  };
  
  // Для предопределенных образов
  if (image) {
    // Находим тип базы данных
    const dbType = Object.keys(requiredVars).find(type => 
      imageName.toLowerCase().includes(type)
    );
    
    const isRequired = dbType ? requiredVars[dbType].includes(key) : false;
    
    // Для предопределенных образов, START_COMMAND всегда зарезервирована
    const isReserved = key === 'START_COMMAND';
    
    return { isRequired, isReserved };
  }
  
  // Для custom image: START_COMMAND не является обязательной или зарезервированной
  // Пользователь может изменить ключ или удалить ее
  return { isRequired: false, isReserved: false };
};

// Обработчик изменения образа
const handleImageChange = (newImage: string) => {
  const selectedImage = getImageInfo(newImage);
  
  // Устанавливаем новое значение образа и порт
  setValue('containerImage', newImage);
  setValue('exposedPort', selectedImage?.port || 80);
  
  // ОСОБАЯ ЛОГИКА ДЛЯ CUSTOM IMAGE
  if (newImage === '') {
    // Для Custom Image: добавляем START_COMMAND как зарезервированную
    setValue('environmentVars', [{ 
      key: 'START_COMMAND', 
      value: ''
    }]);
  } 
  // ДЛЯ ПРЕДОПРЕДЕЛЕННЫХ ОБРАЗОВ - ЗАМЕНЯЕМ ПЕРЕМЕННЫЕ
  else if (selectedImage?.envVars && selectedImage.envVars.length > 0) {
    setValue('environmentVars', selectedImage.envVars);
  } else {
    setValue('environmentVars', [{ key: '', value: '' }]);
  }
};

  const containerImage = watch('containerImage');

    // Определяем является ли образ custom
  const isCustomImage = React.useMemo(() => {
    if (!containerImage) return false;
    // Если режим custom, то это точно custom image
    if (inputMode === 'custom') return true;
    // Иначе проверяем, есть ли в predefinedImages
    return !predefinedImages.some(img => img.value === containerImage);
  }, [containerImage, inputMode]);

React.useEffect(() => {
  if (containerImage && !workload) {
    const selectedImage = getImageInfo(containerImage);
    
    if (selectedImage) {
      // Автоматически устанавливаем порт
      setValue('exposedPort', selectedImage.port);
      
      // Автоматически заполняем переменные окружения
      if (selectedImage.envVars && selectedImage.envVars.length > 0) {
        const currentVars = watch('environmentVars') || [];
        const existingKeys = currentVars.map(v => v.key);
        
        const newVars = [...currentVars];
        
        selectedImage.envVars.forEach(defaultVar => {
          const existingIndex = existingKeys.indexOf(defaultVar.key);
          
          if (existingIndex === -1) {
            // Добавляем новую переменную
            newVars.push(defaultVar);
          } else if (defaultVar.key === 'START_COMMAND') {
            // ОБНОВЛЯЕМ ЗНАЧЕНИЕ START_COMMAND, НО НЕ ПЕРЕЗАПИСЫВАЕМ ДРУГИЕ
            newVars[existingIndex] = {
              ...newVars[existingIndex],
              value: defaultVar.value
            };
          }
          // Для других переменных сохраняем существующие значения
        });
        
        setValue('environmentVars', newVars);
      }
    }
  }
}, [containerImage, setValue, workload, watch]);


  const showDeploymentFields = containerImage !== undefined && containerImage !== null;

  // Функция для парсинга JSON строки в массив объектов
const parseEnvironmentVariables = (envJson?: string): EnvironmentVariable[] => {
  if (!envJson) return [];
  
  try {
    const parsed = JSON.parse(envJson);
    const entries = Object.entries(parsed);
    
    return entries.map(([key, value]) => ({
      key,
      value: typeof value === 'string' ? value : String(value)
    }));
  } catch {
    return [];
  }
};

  // Функции для ограничения ввода
  const allowOnlyDigits = (e: React.KeyboardEvent) => {
    if (!/[\d]/.test(e.key) && 
        e.key !== 'Backspace' && 
        e.key !== 'Tab' && 
        e.key !== 'Delete' && 
        e.key !== 'ArrowLeft' && 
        e.key !== 'ArrowRight') {
      e.preventDefault();
    }
  };

  const allowDigitsAndDot = (e: React.KeyboardEvent) => {
    if (!/[\d.]/.test(e.key) && 
        e.key !== 'Backspace' && 
        e.key !== 'Tab' && 
        e.key !== 'Delete' && 
        e.key !== 'ArrowLeft' && 
        e.key !== 'ArrowRight') {
      e.preventDefault();
    }
    // Запрет на ввод более одной точки
    if (e.key === '.' && (e.target as HTMLInputElement).value.includes('.')) {
      e.preventDefault();
    }
  };

// ТОЛЬКО ДЛЯ НАЧАЛЬНОЙ ЗАГРУЗКИ, НЕ ДЛЯ ИЗМЕНЕНИЙ
React.useEffect(() => {
  if (workload) {
    // При редактировании существующей нагрузки
    const environmentVars = parseEnvironmentVariables(workload.environmentVariables);
    const image = getImageInfo(workload.containerImage);
    
    // Проверяем является ли образ custom
    const isCustom = workload.containerImage && 
      !predefinedImages.some(img => img.value === workload.containerImage);
    
    reset({
      name: workload.name,
      description: workload.description,
      type: workload.type,
      requiredCpu: workload.requiredCpu,
      requiredMemory: workload.requiredMemory,
      requiredStorage: workload.requiredStorage,
      containerImage: workload.containerImage || 'nginx:latest',
      exposedPort: workload.exposedPort || image?.port || 80,
      environmentVars: environmentVars.length > 0 ? environmentVars : 
        (isCustom ? [{ key: 'START_COMMAND', value: '' }] : [{ key: '', value: '' }]),
    });
  } else {
    // При создании новой нагрузки
    reset({
      name: '',
      description: '',
      type: 'VirtualMachine',
      requiredCpu: 1,
      requiredMemory: 1,
      requiredStorage: 10,
      containerImage: 'nginx:latest', // Стандартный образ по умолчанию
      exposedPort: 80,
      environmentVars: [{ key: '', value: '' }],
    });
  }
}, [workload, reset]);

  const onFormSubmit: SubmitHandler<WorkloadFormData> = (data) => {
    const selectedImage = getImageInfo(data.containerImage);
    const hasStartCommand = selectedImage?.envVars?.some(v => v.key === 'START_COMMAND');
    const userHasStartCommand = data.environmentVars.some(v => v.key === 'START_COMMAND');

    if (data.containerImage === '') {
      alert('Пожалуйста, введите имя кастомного Docker образа');
      return;
    }
  
    if (data.containerImage && !data.containerImage.includes(':')) {
      const confirmed = window.confirm(
        `Образ "${data.containerImage}" не содержит тега (например, :latest).\n` +
        `Рекомендуется указывать тег для стабильности.\n` +
        `Продолжить без тега?`
      );
    
      if (!confirmed) {
        return;
      }
    }
  
   if (hasStartCommand && !userHasStartCommand) {
      // ПОКАЗЫВАЕМ ОШИБКУ, ЕСЛИ START_COMMAND ОБЯЗАТЕЛЕН, НО ОТСУТСТВУЕТ
      alert('Для выбранного образа обязательна переменная START_COMMAND. Пожалуйста, добавьте ее.');
      return;
    }
    // Преобразуем environmentVars в JSON строку
    const envVars = data.environmentVars.reduce((acc, curr) => {
      if (curr.key && curr.value) {
        acc[curr.key] = curr.value;
      }
      return acc;
    }, {} as Record<string, string>);

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
    };

    onSubmit(submitData);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>
        {workload ? 'Редактировать нагрузку' : 'Создать новую нагрузку'}
      </DialogTitle>
      <form onSubmit={handleSubmit(onFormSubmit)}>
        <DialogContent>
          <Grid container spacing={2}>
            {/* Основная информация */}
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

            {/* Ресурсы */}
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
                  validate: (value) => Number.isInteger(value) || 'Должно быть целое число'
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется CPU (ядер)"
                    error={!!errors.requiredCpu}
                    helperText={errors.requiredCpu?.message}
                    fullWidth
                    inputProps={{ 
                      min: 1,
                      step: 1,
                      onKeyDown: allowOnlyDigits
                    }}
                    onChange={(e) => {
                      const value = parseInt(e.target.value);
                      if (!isNaN(value) && value >= 1) {
                        field.onChange(value);
                      }
                    }}
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
                  validate: (value) => {
                    const num = Number(value);
                    const decimalPart = num.toString().split('.')[1];
                    return decimalPart ? decimalPart.length <= 1 : true || 'Максимум 1 знак после запятой';
                  }
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется памяти (ГБ)"
                    error={!!errors.requiredMemory}
                    helperText={errors.requiredMemory?.message}
                    fullWidth
                    inputProps={{ 
                      min: 0.5,
                      step: 0.1,
                      onKeyDown: allowDigitsAndDot
                    }}
                    onChange={(e) => {
                      const value = parseFloat(e.target.value);
                      if (!isNaN(value) && value >= 0.5) {
                        // Округляем до одного знака после запятой
                        field.onChange(Math.round(value * 10) / 10);
                      }
                    }}
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
                  validate: (value) => Number.isInteger(value) || 'Должно быть целое число'
                }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    type="number"
                    label="Требуется хранилища (ГБ)"
                    error={!!errors.requiredStorage}
                    helperText={errors.requiredStorage?.message}
                    fullWidth
                    inputProps={{ 
                      min: 1,
                      step: 1,
                      onKeyDown: allowOnlyDigits
                    }}
                    onChange={(e) => {
                      const value = parseInt(e.target.value);
                      if (!isNaN(value) && value >= 1) {
                        field.onChange(value);
                      }
                    }}
                  />
                )}
              />
            </Grid>

            {/* Поля для деплоя */}
            <Grid item xs={12}>
              <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                Конфигурация развертывания
                <IconButton size="small" sx={{ ml: 1 }}>
                  <Info fontSize="small" />
                </IconButton>
              </Typography>
              <Alert severity="info" sx={{ mb: 2 }}>
                Настройте эти параметры, если хотите развернуть эту нагрузку как Docker-контейнер
              </Alert>
              {getImageInfo(containerImage)?.isWebService === false && (
                <Alert severity="warning" sx={{ mb: 2 }}>
                  <Typography variant="body2">
                    <strong>Внимание: Это сервис без веб-интерфейса!</strong><br />
                    • Не открывается в браузере<br />
                    • Используется для подключения других приложений<br />
                    • Порт {getImageInfo(containerImage)?.port} используется специальными клиентами
                  </Typography>
                </Alert>
              )}
            </Grid>
{/* ВАРИАНТ С ДВУМЯ РЕЖИМАМИ - БОЛЕЕ НАДЕЖНЫЙ */}
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
          helperText="Select a predefined image or choose 'Custom Image'"
          onChange={(e) => {
            const newValue = e.target.value;
            if (newValue === '') {
              // Переключаемся в режим custom
              setInputMode('custom');
              // Устанавливаем пустое значение
              field.onChange('');
              // Очищаем переменные окружения
              handleImageChange('');
            } else {
              // Обычный выбор из списка
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
    // РЕЖИМ ВВОДА CUSTOM IMAGE
    <Controller
      name="containerImage"
      control={control}
      rules={{
        required: 'Имя образа обязательно',
        validate: (value) => {
          if (!value || value.trim() === '') {
            return 'Введите имя Docker образа';
          }
          return true;
        }
      }}
      render={({ field, fieldState }) => (
<TextField
  {...field}
  label="Custom Docker Image"
  placeholder="nginx:alpine, postgres:15, myapp:latest"
  fullWidth
  error={!!fieldState.error}
  helperText={fieldState.error?.message || "Введите имя Docker образа"}
  InputProps={{
    endAdornment: (
      <InputAdornment position="end">
        <IconButton 
          onClick={() => {
            setInputMode('select');
            // Возвращаем стандартный образ при возврате к списку
            field.onChange('nginx:latest');
            handleImageChange('nginx:latest');
          }}
          title="Выбрать из списка"
        >
          <ArrowBack />
        </IconButton>
      </InputAdornment>
    ),
  }}
  // ОБРАБАТЫВАЕМ РУЧНОЙ ВВОД
  onChange={(e) => {
    const newValue = e.target.value;
    field.onChange(newValue);
    
    // Если пользователь ввел custom image
    if (newValue.trim() !== '') {
      // Проверяем является ли это custom image
      const isCustom = !predefinedImages.some(img => img.value === newValue);
      if (isCustom) {
        // Для custom image предлагаем добавить START_COMMAND, но не навязываем
        const currentVars = watch('environmentVars');
        const hasStartCommand = currentVars?.some(v => v.key === 'START_COMMAND');
        
        if (!hasStartCommand && (!currentVars || currentVars.length === 0 || 
            (currentVars.length === 1 && currentVars[0].key === '' && currentVars[0].value === ''))) {
          // Предлагаем добавить START_COMMAND только если нет других переменных
          setValue('environmentVars', [{ 
            key: 'START_COMMAND', 
            value: ''
          }]);
        }
      }
    }
  }}
/>
      )}
    />
  )}
</Grid>

{/* КНОПКА ДЛЯ ВОЗВРАТА К ВЫБОРУ ИЗ СПИСКА (если в режиме custom) */}
{inputMode === 'custom' && (
  <Grid item xs={12}>
    <Button
      startIcon={<ArrowBack />}
      onClick={() => {
        setInputMode('select');
        setValue('containerImage', 'nginx:latest');
        handleImageChange('nginx:latest');
      }}
      variant="outlined"
      size="small"
      sx={{ mb: 2 }}
    >
      Выбрать из списка
    </Button>
  </Grid>
)}
{/* ПОКАЗЫВАЕМ ПОЛЯ ДЕПЛОЯ ДЛЯ ЛЮБОГО ОБРАЗА, ВКЛЮЧАЯ CUSTOM */}
{(showDeploymentFields || containerImage !== undefined) && (
  <>
    <Grid item xs={12} md={6}>
      <Controller
        name="exposedPort"
        control={control}
        rules={{ 
          min: { value: 1, message: 'Port must be > 0' },
          max: { value: 65535, message: 'Port must be < 65536' }
        }}
        render={({ field }) => (
          <TextField
            {...field}
            type="number"
            label="Container Port"
            error={!!errors.exposedPort}
            helperText={
              errors.exposedPort?.message || 
              "Порт, который слушает приложение внутри контейнера"
            }
            fullWidth
          />
        )}
      />
    </Grid>

    {/* ПЕРЕМЕННЫЕ ОКРУЖЕНИЯ ДОСТУПНЫ ДЛЯ CUSTOM IMAGE */}
    <Grid item xs={12}>
      <Typography variant="subtitle1" gutterBottom>
        Environment Variables
        <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
          (опционально)
        </Typography>
      </Typography>
      
      {/* ПОДСКАЗКА ДЛЯ CUSTOM IMAGE */}
{isCustomImage && (
  <Alert severity="info" sx={{ mb: 2 }}>
    <Typography variant="body2">
      Для кастомных образов может потребоваться команда запуска.<br />
      • Добавьте переменную <strong>START_COMMAND</strong> если образ не имеет точки входа<br />
      • Можете удалить или изменить ее, если она не требуется<br />
      • Например: <code>npm start</code>, <code>python app.py</code>, <code>npx serve -s . -l 3000</code>
    </Typography>
  </Alert>
)}
      
      {fields.map((field, index) => {
  const { isRequired, isReserved } = isReservedEnvVar(field.key, containerImage || '');
  
  return (
    <Box key={field.id} display="flex" gap={2} mb={2}>
      <Controller
        name={`environmentVars.${index}.key`}
        control={control}
        render={({ field: controllerField }) => (
          <TextField
            {...controllerField}
            label="Key"
            placeholder="START_COMMAND"
            fullWidth
            // READONLY ДЛЯ ЗАРЕЗЕРВИРОВАННЫХ И ОБЯЗАТЕЛЬНЫХ ПЕРЕМЕННЫХ
            InputProps={{
              readOnly: isRequired || isReserved,
            }}
            sx={{
              '& .MuiInputBase-input.Mui-readOnly': {
                backgroundColor: isRequired || isReserved ? 'action.hover' : 'inherit',
                color: isRequired || isReserved ? 'text.primary' : 'inherit',
                fontWeight: isReserved ? 'bold' : 'normal',
              }
            }}
            helperText={
              isRequired ? 'Обязательная переменная' :
              isReserved ? 'Системная переменная (нельзя изменить)' : ''
            }
          />
        )}
      />
      <Controller
        name={`environmentVars.${index}.value`}
        control={control}
        rules={{
          // ВАЛИДАЦИЯ ДЛЯ ОБЯЗАТЕЛЬНЫХ ПЕРЕМЕННЫХ
          validate: (value) => {
            if (isRequired && !value.trim()) {
              return 'Обязательное поле';
            }
            if (isReserved && !value.trim()) {
              return 'Команда запуска обязательна';
            }
            return true;
          }
        }}
        render={({ field: controllerField, fieldState }) => (
          <TextField
            {...controllerField}
            label="Value"
            placeholder={
              field.key === 'START_COMMAND' ? 
              'npx serve -s . -l 3000' : 
              'strong_password'
            }
            type={controllerField.name.includes('PASSWORD') ? 'password' : 'text'}
            fullWidth
            error={!!fieldState.error}
            helperText={fieldState.error?.message}
            // ПОКАЗЫВАЕМ ПОДСКАЗКУ ДЛЯ START_COMMAND
            InputProps={{
              ...(field.key === 'START_COMMAND' && {
                startAdornment: (
                  <InputAdornment position="start">
                    <Terminal fontSize="small" color="action" />
                  </InputAdornment>
                ),
              }),
            }}
          />
        )}
      />
      <IconButton 
        onClick={() => remove(index)}
        // ЗАПРЕЩАЕМ УДАЛЕНИЕ ОБЯЗАТЕЛЬНЫХ И ЗАРЕЗЕРВИРОВАННЫХ ПЕРЕМЕННЫХ
        disabled={fields.length <= 1 || isRequired || isReserved}
        sx={{ mt: 1 }}
        title={
          isRequired ? "Обязательную переменную нельзя удалить" :
          isReserved ? "Системную переменную нельзя удалить" :
          "Удалить"
        }
      >
        <Remove />
      </IconButton>
    </Box>
  );
})}
 <Button
    startIcon={<Add />}
    onClick={() => append({ key: '', value: '' })}
    variant="outlined"
    size="small"
  >
    Добавить переменную окружения
  </Button>
    </Grid>
  </>
)}
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
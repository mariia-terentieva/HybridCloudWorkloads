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
} from '@mui/material';
import { Info, Add, Remove } from '@mui/icons-material';
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

  const { fields, append, remove } = useFieldArray({
    control,
    name: 'environmentVars',
  });

  // Функция для проверки, является ли переменная обязательной
const isRequiredEnvVar = (key: string, imageName: string): boolean => {
  const image = getImageInfo(imageName);
  if (!image) return false;
  
  // Определим обязательные переменные для каждого типа БД
  const requiredVars: Record<string, string[]> = {
    'postgres': ['POSTGRES_PASSWORD', 'POSTGRES_DB', 'POSTGRES_USER'],
    'mysql': ['MYSQL_ROOT_PASSWORD', 'MYSQL_DATABASE'],
    'mongo': ['MONGO_INITDB_ROOT_USERNAME', 'MONGO_INITDB_ROOT_PASSWORD'],
  };
  
  // Находим тип базы данных
  const dbType = Object.keys(requiredVars).find(type => 
    imageName.toLowerCase().includes(type)
  );
  
  if (dbType) {
    return requiredVars[dbType].includes(key);
  }
  
  return false;
};

// Сохраняем обязательные переменные при смене образа
const handleImageChange = (newImage: string) => {
  const currentVars = watch('environmentVars') || [];
  const currentImage = watch('containerImage');
  
  // Сохраняем обязательные переменные от текущего образа
  const requiredVarsToKeep = currentVars.filter(v => 
    isRequiredEnvVar(v.key, currentImage)
  );
  
  // Устанавливаем новое значение образа
  setValue('containerImage', newImage);
  
  // После установки нового образа добавляем его переменные
  setTimeout(() => {
    const selectedImage = getImageInfo(newImage);
    
    if (selectedImage) {
      setValue('exposedPort', selectedImage.port);
      
      if (selectedImage.envVars && selectedImage.envVars.length > 0) {
        // Объединяем сохраненные обязательные переменные с новыми
        const existingKeys = requiredVarsToKeep.map(v => v.key);
        const newVars = [...requiredVarsToKeep];
        
        selectedImage.envVars.forEach(defaultVar => {
          if (!existingKeys.includes(defaultVar.key)) {
            newVars.push(defaultVar);
          }
        });
        
        // Добавляем пустую строку, если нет переменных
        if (newVars.length === 0) {
          newVars.push({ key: '', value: '' });
        }
        
        setValue('environmentVars', newVars);
      }
    }
  }, 0);
};

  const containerImage = watch('containerImage');

React.useEffect(() => {
  if (containerImage && !workload) {
    const selectedImage = getImageInfo(containerImage);
    
    if (selectedImage) {
      // Автоматически устанавливаем порт
      setValue('exposedPort', selectedImage.port);
      
      // Автоматически заполняем переменные окружения для БД
      if (selectedImage.envVars && selectedImage.envVars.length > 0) {
        // Добавляем обязательные переменные, но не перезаписываем существующие
        const currentVars = watch('environmentVars') || [];
        const existingKeys = currentVars.map(v => v.key);
        
        const newVars = [...currentVars];
        selectedImage.envVars.forEach(defaultVar => {
          if (!existingKeys.includes(defaultVar.key)) {
            newVars.push(defaultVar);
          }
        });
        
        setValue('environmentVars', newVars);
      }
    }
  }
}, [containerImage, setValue, workload, watch]);


  const showDeploymentFields = containerImage !== '';

  // Функция для парсинга JSON строки в массив объектов
const parseEnvironmentVariables = (envJson?: string): EnvironmentVariable[] => {
  if (!envJson) {
    // Если это новая нагрузка и выбран образ с переменными по умолчанию
    const currentImage = watch('containerImage');
    if (currentImage && !workload) {
      const image = getImageInfo(currentImage);
      if (image?.envVars && image.envVars.length > 0) {
        return image.envVars;
      }
    }
    return [{ key: '', value: '' }];
  }
  
  try {
    const parsed = JSON.parse(envJson);
    const entries = Object.entries(parsed);
    
    if (entries.length === 0) {
      return [{ key: '', value: '' }];
    }
    
    return entries.map(([key, value]) => ({
      key,
      value: typeof value === 'string' ? value : String(value)
    }));
  } catch {
    return [{ key: '', value: '' }];
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

  React.useEffect(() => {
    if (workload) {
      const environmentVars = parseEnvironmentVariables(workload.environmentVariables);
      
      reset({
        name: workload.name,
        description: workload.description,
        type: workload.type,
        requiredCpu: workload.requiredCpu,
        requiredMemory: workload.requiredMemory,
        requiredStorage: workload.requiredStorage,
        containerImage: workload.containerImage || 'nginx:latest',
        exposedPort: workload.exposedPort || 80,
        environmentVars,
      });
    } else {
      reset({
        name: '',
        description: '',
        type: 'VirtualMachine',
        requiredCpu: 1,
        requiredMemory: 1,
        requiredStorage: 10,
        containerImage: 'nginx:latest',
        exposedPort: 80,
        environmentVars: [{ key: '', value: '' }],
      });
    }
  }, [workload, reset]);

  const onFormSubmit: SubmitHandler<WorkloadFormData> = (data) => {
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

            <Grid item xs={12} md={6}>
<Controller
  name="containerImage"
  control={control}
  render={({ field }) => (
    <TextField
      {...field}
      select
      label="Container Image"
      fullWidth
      helperText="Select a predefined image or enter custom image"
      // 🔥 Используем наш обработчик при изменении
      onChange={(e) => handleImageChange(e.target.value)}
    >
      {predefinedImages.map((option) => (
        <MenuItem key={option.value || 'custom'} value={option.value}>
          {option.label}
        </MenuItem>
      ))}
    </TextField>
  )}
/>
            </Grid>

            {containerImage === '' && (
              <Grid item xs={12} md={6}>
                <Controller
                  name="containerImage"
                  control={control}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      label="Пользовательский образ"
                      placeholder="мойреестр/мойобраз:тег"
                      fullWidth
                    />
                  )}
                />
              </Grid>
            )}

            {showDeploymentFields && (
              <>
                <Grid item xs={12} md={6}>
                  <Controller
                    name="exposedPort"
                    control={control}
                    rules={{ 
                      min: { value: 1, message: 'Порт должен быть > 0' },
                      max: { value: 65535, message: 'Порт должен быть < 65536' },
                      validate: (value) => Number.isInteger(value) || 'Должно быть целое число'
                    }}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        type="number"
                        label="Открытый порт"
                        error={!!errors.exposedPort}
                        helperText={
                          errors.exposedPort?.message || 
                          "Порт, который слушает приложение внутри контейнера"
                        }
                        fullWidth
                        inputProps={{ 
                          min: 1,
                          max: 65535,
                          step: 1,
                          onKeyDown: allowOnlyDigits
                        }}
                        onChange={(e) => {
                          const value = parseInt(e.target.value);
                          if (!isNaN(value) && value >= 1 && value <= 65535) {
                            field.onChange(value);
                          }
                        }}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12}>
                  <Typography variant="subtitle1" gutterBottom>
                    Переменные окружения
                    {getImageInfo(containerImage)?.envVars?.length > 0 && (
                      <Typography variant="caption" color="primary" sx={{ ml: 1 }}>
        (значения по умолчанию можно редактировать)
                      </Typography>
                    )}
                  </Typography>
  
                  {/* ПРЕДУПРЕЖДЕНИЕ ДЛЯ БАЗ ДАННЫХ */}
                  {getImageInfo(containerImage)?.isWebService === false && (
                    <Alert severity="info" sx={{ mb: 2 }}>
                      <Typography variant="body2">
                        Для корректной работы базы данных заполните обязательные переменные окружения.
                        Значения по умолчанию можно изменить.
                      </Typography>
                    </Alert>
                  )}
  
                  {fields.map((field, index) => {
                    const isRequired = isRequiredEnvVar(field.key, containerImage || '');
    
                    return (
                      <Box key={field.id} display="flex" gap={2} mb={2}>
                        <Controller
                          name={`environmentVars.${index}.key`}
                          control={control}
                          render={({ field: controllerField }) => (
                            <TextField
                              {...controllerField}
              label="Key"
              placeholder="POSTGRES_PASSWORD"
              fullWidth
              // 🔥 Разрешаем редактирование, но делаем readOnly для обязательных
              InputProps={{
                readOnly: isRequired,
              }}
              sx={{
                '& .MuiInputBase-input.Mui-readOnly': {
                  backgroundColor: isRequired ? 'action.hover' : 'inherit',
                  color: isRequired ? 'text.primary' : 'inherit',
                }
              }}
                              helperText={isRequired ? 'Обязательная переменная' : ''}
                            />
                          )}
                        />
                        <Controller
                          name={`environmentVars.${index}.value`}
                          control={control}
                          rules={{
                            // Валидация для обязательных переменных
                            validate: (value) => {
                              if (isRequired && !value.trim()) {
                                return 'Обязательное поле';
                              }
                              return true;
                            }
                          }}
                          render={({ field: controllerField, fieldState }) => (
                            <TextField
                              {...controllerField}
                              label="Value"
                              placeholder="strong_password"
                              type={controllerField.name.includes('PASSWORD') ? 'password' : 'text'}
                              fullWidth
                              error={!!fieldState.error}
                              helperText={fieldState.error?.message}
                              // Всегда разрешаем редактирование значения
                            />
                          )}
                        />
                        <IconButton 
                          onClick={() => remove(index)}
                          //  Запрещаем удаление обязательных переменных
                          disabled={fields.length <= 1 || isRequired}
                          sx={{ mt: 1 }}
                          title={isRequired ? "Обязательную переменную нельзя удалить" : "Удалить"}
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
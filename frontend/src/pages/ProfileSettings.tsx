import React from 'react';
import {
  Container,
  Paper,
  Typography,
  TextField,
  Button,
  Box,
  Alert,
  Grid,
  Divider,
  IconButton,
  InputAdornment,
} from '@mui/material';
import { Visibility, VisibilityOff, Save, Lock, Person, ArrowBack } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { useAuthStore } from '../store/authStore';
import { useNavigate } from 'react-router-dom';
import { authService } from '../services/authService'; 

interface ProfileFormData {
  firstName: string;
  lastName: string;
  email: string;
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export const ProfileSettings: React.FC = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);
    const login = useAuthStore((state) => state.login); 
  const [showCurrentPassword, setShowCurrentPassword] = React.useState(false);
  const [showNewPassword, setShowNewPassword] = React.useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false);
  const [message, setMessage] = React.useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [isUpdatingPassword, setIsUpdatingPassword] = React.useState(false);

  const {
    control,
    handleSubmit,
    watch,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ProfileFormData>({
    defaultValues: {
      firstName: user?.firstName || '',
      lastName: user?.lastName || '',
      email: user?.email || '',
      currentPassword: '',
      newPassword: '',
      confirmNewPassword: '',
    },
  });

  const newPassword = watch('newPassword');

    const onSubmit = async (data: ProfileFormData) => {
    try {
      setMessage(null);
      
      if (isUpdatingPassword) {
        // РЕАЛЬНЫЙ ВЫЗОВ API для смены пароля
        if (data.newPassword !== data.confirmNewPassword) {
          setMessage({ type: 'error', text: 'Новые пароли не совпадают' });
          return;
        }

        await authService.changePassword({
          currentPassword: data.currentPassword,
          newPassword: data.newPassword,
          confirmNewPassword: data.confirmNewPassword,
        });

        setMessage({ type: 'success', text: 'Пароль успешно обновлен' });
        
        // Сбрасываем поля пароля
        reset({
          ...data,
          currentPassword: '',
          newPassword: '',
          confirmNewPassword: '',
        });
        
        setIsUpdatingPassword(false);
      } else {
        // РЕАЛЬНЫЙ ВЫЗОВ API для обновления профиля
        const updatedUser = await authService.updateProfile({
          firstName: data.firstName,
          lastName: data.lastName,
        });

        // Обновляем данные в store
        login({
          token: useAuthStore.getState().token!, // Сохраняем текущий токен
          expiration: new Date().toISOString(), // Требуется по интерфейсу
          user: updatedUser,
        });

        setMessage({ type: 'success', text: 'Профиль успешно обновлен' });
      }
    } catch (error: any) {
      const errorMessage = error.response?.data?.message || 
                          error.response?.data?.errors?.join(', ') || 
                          'Ошибка обновления. Пожалуйста, попробуйте снова.';
      setMessage({ type: 'error', text: errorMessage });
    }
  };

  return (
    <Container maxWidth="md" sx={{ mt: 4, mb: 4 }}>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">
          Настройки профиля
        </Typography>
        <Button
          variant="outlined"
          startIcon={<ArrowBack />}
          onClick={() => navigate('/dashboard')}
        >
          На главную
        </Button>
      </Box>

      <Paper sx={{ p: 3 }}>
        {message && (
          <Alert severity={message.type} sx={{ mb: 3 }}>
            {message.text}
          </Alert>
        )}

        <form onSubmit={handleSubmit(onSubmit)}>
          {/* Личная информация */}
          <Box mb={4}>
            <Box display="flex" alignItems="center" gap={1} mb={3}>
              <Person color="primary" />
              <Typography variant="h6">Личная информация</Typography>
            </Box>

            <Grid container spacing={3}>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="firstName"
                  control={control}
                  rules={{ required: 'Имя обязательно' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Имя"
                      error={!!errors.firstName}
                      helperText={errors.firstName?.message}
                    />
                  )}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <Controller
                  name="lastName"
                  control={control}
                  rules={{ required: 'Фамилия обязательна' }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Фамилия"
                      error={!!errors.lastName}
                      helperText={errors.lastName?.message}
                    />
                  )}
                />
              </Grid>
              <Grid item xs={12}>
                <Controller
                  name="email"
                  control={control}
                  rules={{
                    required: 'Email обязателен',
                    pattern: {
                      value: /^[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}$/i,
                      message: 'Некорректный email адрес',
                    },
                  }}
                  render={({ field }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Email"
                      type="email"
                      disabled
                      helperText="Email нельзя изменить"
                    />
                  )}
                />
              </Grid>
            </Grid>
          </Box>

          <Divider sx={{ my: 4 }} />

          {/* Смена пароля */}
          <Box mb={4}>
            <Box display="flex" alignItems="center" gap={1} mb={3}>
              <Lock color="primary" />
              <Typography variant="h6">Смена пароля</Typography>
            </Box>

            <Button
              variant="outlined"
              onClick={() => setIsUpdatingPassword(!isUpdatingPassword)}
              sx={{ mb: 3 }}
            >
              {isUpdatingPassword ? 'Отменить смену пароля' : 'Сменить пароль'}
            </Button>

            {isUpdatingPassword && (
              <Grid container spacing={3}>
                <Grid item xs={12}>
                  <Controller
                    name="currentPassword"
                    control={control}
                    rules={isUpdatingPassword ? { required: 'Текущий пароль обязателен' } : {}}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        fullWidth
                        label="Текущий пароль"
                        type={showCurrentPassword ? 'text' : 'password'}
                        error={!!errors.currentPassword}
                        helperText={errors.currentPassword?.message}
                        InputProps={{
                          endAdornment: (
                            <InputAdornment position="end">
                              <IconButton
                                onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                                edge="end"
                              >
                                {showCurrentPassword ? <VisibilityOff /> : <Visibility />}
                              </IconButton>
                            </InputAdornment>
                          ),
                        }}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12} sm={6}>
                  <Controller
                    name="newPassword"
                    control={control}
                    rules={isUpdatingPassword ? {
                      required: 'Новый пароль обязателен',
                      minLength: { value: 6, message: 'Пароль должен содержать минимум 6 символов' },
                      pattern: {
                        value: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/,
                        message: 'Должен содержать заглавные, строчные буквы и цифры',
                      },
                    } : {}}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        fullWidth
                        label="Новый пароль"
                        type={showNewPassword ? 'text' : 'password'}
                        error={!!errors.newPassword}
                        helperText={errors.newPassword?.message}
                        InputProps={{
                          endAdornment: (
                            <InputAdornment position="end">
                              <IconButton
                                onClick={() => setShowNewPassword(!showNewPassword)}
                                edge="end"
                              >
                                {showNewPassword ? <VisibilityOff /> : <Visibility />}
                              </IconButton>
                            </InputAdornment>
                          ),
                        }}
                      />
                    )}
                  />
                </Grid>

                <Grid item xs={12} sm={6}>
                  <Controller
                    name="confirmNewPassword"
                    control={control}
                    rules={isUpdatingPassword ? {
                      required: 'Подтвердите новый пароль',
                      validate: value => value === newPassword || 'Пароли не совпадают',
                    } : {}}
                    render={({ field }) => (
                      <TextField
                        {...field}
                        fullWidth
                        label="Подтверждение нового пароля"
                        type={showConfirmPassword ? 'text' : 'password'}
                        error={!!errors.confirmNewPassword}
                        helperText={errors.confirmNewPassword?.message}
                        InputProps={{
                          endAdornment: (
                            <InputAdornment position="end">
                              <IconButton
                                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                edge="end"
                              >
                                {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                              </IconButton>
                            </InputAdornment>
                          ),
                        }}
                      />
                    )}
                  />
                </Grid>
              </Grid>
            )}
          </Box>

          <Divider sx={{ my: 4 }} />

          {/* Действия */}
          <Box display="flex" justifyContent="flex-end" gap={2}>
            <Button
              type="submit"
              variant="contained"
              startIcon={<Save />}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Сохранение...' : 'Сохранить изменения'}
            </Button>
          </Box>
        </form>
      </Paper>
    </Container>
  );
};

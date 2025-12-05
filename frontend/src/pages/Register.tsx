import React from 'react';
import {
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Box,
  Alert,
  FormControl,
  InputLabel,
  OutlinedInput,
  InputAdornment,
  IconButton,
} from '@mui/material';
import { Visibility, VisibilityOff } from '@mui/icons-material';
import { useForm, Controller } from 'react-hook-form';
import { useNavigate, Link } from 'react-router-dom';
import { authService } from '../services/authService';
import { RegisterRequest } from '../types';

export const Register: React.FC = () => {
  const navigate = useNavigate();
  const [showPassword, setShowPassword] = React.useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = React.useState(false);
  const [error, setError] = React.useState<string>('');
  const [success, setSuccess] = React.useState(false);

  const {
    control,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<RegisterRequest>({
    defaultValues: {
      email: '',
      password: '',
      confirmPassword: '',
      firstName: '',
      lastName: '',
    },
  });

  const password = watch('password');
  const confirmPassword = watch('confirmPassword');

  const validatePassword = (value: string) => {
    if (value.length < 6) {
      return 'Пароль должен содержать минимум 6 символов';
    }
    if (!/[A-Z]/.test(value)) {
      return 'Пароль должен содержать хотя бы одну заглавную букву';
    }
    if (!/[a-z]/.test(value)) {
      return 'Пароль должен содержать хотя бы одну строчную букву';
    }
    if (!/\d/.test(value)) {
      return 'Пароль должен содержать хотя бы одну цифру';
    }
    return true;
  };

  const onSubmit = async (data: RegisterRequest) => {
    try {
      setError('');
      await authService.register(data);
      setSuccess(true);
      
      // Перенаправление на вход через 3 секунды
      setTimeout(() => {
        navigate('/login');
      }, 3000);
    } catch (err: any) {
      const errorMessage = err.response?.data?.message || 'Ошибка регистрации';
      if (typeof errorMessage === 'string') {
        setError(errorMessage);
      } else if (Array.isArray(errorMessage)) {
        setError(errorMessage.join(', '));
      } else if (typeof errorMessage === 'object') {
        setError(Object.values(errorMessage).flat().join(', '));
      } else {
        setError('Ошибка регистрации. Пожалуйста, попробуйте снова.');
      }
    }
  };

  const handleClickShowPassword = () => {
    setShowPassword(!showPassword);
  };

  const handleClickShowConfirmPassword = () => {
    setShowConfirmPassword(!showConfirmPassword);
  };

  if (success) {
    return (
      <Container component="main" maxWidth="sm">
        <Box
          sx={{
            marginTop: 8,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Paper elevation={3} sx={{ padding: 4, width: '100%', textAlign: 'center' }}>
            <Alert severity="success" sx={{ mb: 2 }}>
              Регистрация успешна! Вы будете перенаправлены на страницу входа через 3 секунды.
            </Alert>
            <Typography variant="body1" gutterBottom>
              Ваш аккаунт успешно создан.
            </Typography>
            <Button
              component={Link}
              to="/login"
              variant="contained"
              sx={{ mt: 2 }}
            >
              Перейти к входу
            </Button>
          </Paper>
        </Box>
      </Container>
    );
  }

  return (
    <Container component="main" maxWidth="sm">
      <Box
        sx={{
          marginTop: 8,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
        }}
      >
        <Paper elevation={3} sx={{ padding: 4, width: '100%' }}>
          <Typography component="h1" variant="h4" align="center" gutterBottom>
            Создать аккаунт
          </Typography>
          
          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ mt: 1 }}>
            <Box display="flex" gap={2} mb={2}>
              <Controller
                name="firstName"
                control={control}
                rules={{ required: 'Имя обязательно' }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    required
                    fullWidth
                    label="Имя"
                    autoComplete="given-name"
                    error={!!errors.firstName}
                    helperText={errors.firstName?.message}
                  />
                )}
              />

              <Controller
                name="lastName"
                control={control}
                rules={{ required: 'Фамилия обязательна' }}
                render={({ field }) => (
                  <TextField
                    {...field}
                    required
                    fullWidth
                    label="Фамилия"
                    autoComplete="family-name"
                    error={!!errors.lastName}
                    helperText={errors.lastName?.message}
                  />
                )}
              />
            </Box>

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
                  margin="normal"
                  required
                  fullWidth
                  label="Email адрес"
                  autoComplete="email"
                  error={!!errors.email}
                  helperText={errors.email?.message}
                />
              )}
            />

            <Controller
              name="password"
              control={control}
              rules={{ 
                required: 'Пароль обязателен',
                validate: validatePassword
              }}
              render={({ field }) => (
                <FormControl fullWidth margin="normal" variant="outlined">
                  <InputLabel htmlFor="password">Пароль *</InputLabel>
                  <OutlinedInput
                    {...field}
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    error={!!errors.password}
                    endAdornment={
                      <InputAdornment position="end">
                        <IconButton
                          aria-label="переключить видимость пароля"
                          onClick={handleClickShowPassword}
                          edge="end"
                        >
                          {showPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    }
                    label="Пароль"
                  />
                  {errors.password && (
                    <Typography variant="caption" color="error" sx={{ mt: 1 }}>
                      {errors.password.message}
                    </Typography>
                  )}
                  <Typography variant="caption" color="text.secondary" sx={{ mt: 1 }}>
                    Пароль должен содержать минимум 6 символов, заглавные и строчные буквы, цифры.
                  </Typography>
                </FormControl>
              )}
            />

            <Controller
              name="confirmPassword"
              control={control}
              rules={{ 
                required: 'Подтвердите пароль',
                validate: value => value === password || 'Пароли не совпадают'
              }}
              render={({ field }) => (
                <FormControl fullWidth margin="normal" variant="outlined">
                  <InputLabel htmlFor="confirm-password">Подтверждение пароля *</InputLabel>
                  <OutlinedInput
                    {...field}
                    id="confirm-password"
                    type={showConfirmPassword ? 'text' : 'password'}
                    error={!!errors.confirmPassword}
                    endAdornment={
                      <InputAdornment position="end">
                        <IconButton
                          aria-label="переключить видимость подтверждения пароля"
                          onClick={handleClickShowConfirmPassword}
                          edge="end"
                        >
                          {showConfirmPassword ? <VisibilityOff /> : <Visibility />}
                        </IconButton>
                      </InputAdornment>
                    }
                    label="Подтверждение пароля"
                  />
                  {errors.confirmPassword && (
                    <Typography variant="caption" color="error" sx={{ mt: 1 }}>
                      {errors.confirmPassword.message}
                    </Typography>
                  )}
                </FormControl>
              )}
            />

            <Button
              type="submit"
              fullWidth
              variant="contained"
              sx={{ mt: 3, mb: 2 }}
              disabled={isSubmitting}
            >
              {isSubmitting ? 'Создание аккаунта...' : 'Создать аккаунт'}
            </Button>

            <Box textAlign="center">
              <Link to="/login" style={{ textDecoration: 'none' }}>
                <Typography variant="body2" color="primary">
                  Уже есть аккаунт? Войти
                </Typography>
              </Link>
            </Box>
          </Box>
        </Paper>
      </Box>
    </Container>
  );
};

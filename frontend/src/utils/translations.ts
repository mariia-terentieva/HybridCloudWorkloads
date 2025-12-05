// Функции перевода для всего приложения
export const getTypeLabel = (type?: string): string => {
  if (!type) return '';
  
  switch (type) {
    case 'VirtualMachine': return 'Виртуальная машина';
    case 'Database': return 'База данных';
    case 'Container': return 'Контейнер';
    case 'WebService': return 'Веб-сервис';
    case 'BatchJob': return 'Пакетное задание';
    default: return type;
  }
};

export const getStatusLabel = (status?: string): string => {
  if (!status) return '';
  
  switch (status) {
    case 'Running': return 'Работает';
    case 'Deploying': return 'Развертывается';
    case 'Stopped': return 'Остановлено';
    case 'Error': return 'Ошибка';
    case 'NotDeployed': return 'Не развернуто';
    default: return status;
  }
};

export const getImageLabel = (image?: string): string => {
  if (!image) return 'Не указан';
  
  const imageMap: Record<string, string> = {
    'nginx:latest': 'NGINX Веб-сервер',
    'nginx:alpine': 'NGINX Веб-сервер (Alpine)',
    'postgres:15': 'PostgreSQL База данных',
    'postgres:latest': 'PostgreSQL База данных',
    'redis:alpine': 'Redis Кэш',
    'redis:latest': 'Redis Кэш',
    'mysql:8': 'MySQL База данных',
    'mysql:latest': 'MySQL База данных',
    'node:18-alpine': 'Node.js Приложение',
    'node:latest': 'Node.js Приложение',
    'python:3.11-slim': 'Python Приложение',
    'python:latest': 'Python Приложение',
    'httpd:alpine': 'Apache HTTP Сервер',
    'httpd:latest': 'Apache HTTP Сервер',
    'mongo:6': 'MongoDB База данных',
    'mongo:latest': 'MongoDB База данных',
  };
  
  return imageMap[image] || image;
};
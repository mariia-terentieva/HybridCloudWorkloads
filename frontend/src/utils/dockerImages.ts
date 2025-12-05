export interface DockerImage {
  value: string;
  label: string;
  port: number;
  envVars: Array<{ key: string; value: string }>;
  isWebService: boolean;
  description?: string;
}

export const predefinedImages: DockerImage[] = [
  { 
    value: 'nginx:latest', 
    label: 'NGINX Web Server', 
    port: 80,
    envVars: [],
    isWebService: true,
    description: 'Высокопроизводительный веб-сервер'
  },
  { 
    value: 'nginx:alpine', 
    label: 'NGINX (Alpine)', 
    port: 80,
    envVars: [],
    isWebService: true,
    description: 'Облегченная версия NGINX на Alpine Linux'
  },
  { 
    value: 'postgres:15', 
    label: 'PostgreSQL Database', 
    port: 5432,
    envVars: [
      { key: 'POSTGRES_PASSWORD', value: 'postgres' },
      { key: 'POSTGRES_DB', value: 'mydatabase' },
      { key: 'POSTGRES_USER', value: 'postgres' }
    ],
    isWebService: false,
    description: 'Продвинутая реляционная база данных'
  },
  { 
    value: 'redis:alpine', 
    label: 'Redis Cache', 
    port: 6379,
    envVars: [],
    isWebService: false,
    description: 'Ключ-значение хранилище в памяти'
  },
  { 
    value: 'mysql:8', 
    label: 'MySQL Database', 
    port: 3306,
    envVars: [
      { key: 'MYSQL_ROOT_PASSWORD', value: 'root' },
      { key: 'MYSQL_DATABASE', value: 'mydatabase' }
    ],
    isWebService: false,
    description: 'Популярная реляционная база данных'
  },
  { 
    value: 'node:18-alpine', 
    label: 'Node.js Application', 
    port: 3000,
    envVars: [],
    isWebService: true,
    description: 'Среда выполнения JavaScript'
  },
  { 
    value: 'python:3.11-slim', 
    label: 'Python Application', 
    port: 5000,
    envVars: [],
    isWebService: true,
    description: 'Среда выполнения Python'
  },
  { 
    value: 'httpd:alpine', 
    label: 'Apache HTTP Server', 
    port: 80,
    envVars: [],
    isWebService: true,
    description: 'Веб-сервер Apache'
  },
  { 
    value: 'mongo:6', 
    label: 'MongoDB Database', 
    port: 27017,
    envVars: [
      { key: 'MONGO_INITDB_ROOT_USERNAME', value: 'admin' },
      { key: 'MONGO_INITDB_ROOT_PASSWORD', value: 'password' }
    ],
    isWebService: false,
    description: 'Документо-ориентированная база данных'
  },
  { 
    value: '', 
    label: 'Custom Image', 
    port: 80,
    envVars: [],
    isWebService: true,
    description: 'Пользовательский Docker образ'
  },
];

// Функции для работы с образами
export const getImageInfo = (imageName: string): DockerImage | undefined => {
  return predefinedImages.find(img => img.value === imageName);
};

export const getImageLabel = (imageName?: string): string => {
  if (!imageName) return 'Не указан';
  
  const image = getImageInfo(imageName);
  if (image) return image.label;
  
  // Если образ не найден в списке, возвращаем как есть
  return imageName;
};

export const isWebService = (imageName?: string): boolean => {
  if (!imageName) return true; // По умолчанию считаем веб-сервисом
  
  const image = getImageInfo(imageName);
  return image ? image.isWebService : true;
};

export const getDefaultPort = (imageName?: string): number => {
  if (!imageName) return 80;
  
  const image = getImageInfo(imageName);
  return image ? image.port : 80;
};

export const getDefaultEnvVars = (imageName?: string): Array<{ key: string; value: string }> => {
  if (!imageName) return [];
  
  const image = getImageInfo(imageName);
  return image ? [...image.envVars] : [];
};
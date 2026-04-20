interface SlaRequirement {
  maxResponseTimeMs: number;
  allowedDowntimePerMonth: number;
  availabilityTarget: number;
}

interface BusinessHours {
  timezone: string;
  peakHours: { start: string; end: string }[];
  weekendLoadPercent: number;
  workingDays: number[];
}
// функции
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

// НОВЫЕ ФУНКЦИИ ДЛЯ КЛАССИФИКАЦИИ

/**
 * Получить русское название паттерна использования
 */
export const getUsagePatternLabel = (pattern: string): string => {
  switch (pattern) {
    case 'Constant': return 'Постоянная';
    case 'Periodic': return 'Периодическая';
    case 'Burst': return 'Пиковая';
    case 'Unpredictable': return 'Непредсказуемая';
    default: return pattern;
  }
};

/**
 * Получить описание паттерна использования
 */
export const getUsagePatternDescription = (pattern: string): string => {
  switch (pattern) {
    case 'Constant': return 'Нагрузка 24/7 без значительных колебаний';
    case 'Periodic': return 'Нагрузка изменяется по предсказуемому расписанию';
    case 'Burst': return 'Кратковременные пиковые нагрузки';
    case 'Unpredictable': return 'Случайные всплески нагрузки';
    default: return '';
  }
};

/**
 * Получить русское название класса критичности
 */
export const getCriticalityLabel = (criticality: string): string => {
  switch (criticality) {
    case 'MissionCritical': return 'Критическая';
    case 'BusinessEssential': return 'Важная';
    case 'NonCritical': return 'Некритичная';
    default: return criticality;
  }
};

/**
 * Получить описание класса критичности
 */
export const getCriticalityDescription = (criticality: string): string => {
  switch (criticality) {
    case 'MissionCritical': return 'Критически важные для бизнеса системы, простой недопустим';
    case 'BusinessEssential': return 'Важные системы, допустимы кратковременные перерывы';
    case 'NonCritical': return 'Некритичные нагрузки, тестовые среды';
    default: return '';
  }
};

/**
 * Получить цвет для класса критичности (для UI)
 */
export const getCriticalityColor = (criticality: string): 'error' | 'warning' | 'success' | 'default' => {
  switch (criticality) {
    case 'MissionCritical': return 'error';
    case 'BusinessEssential': return 'warning';
    case 'NonCritical': return 'success';
    default: return 'default';
  }
};

/**
 * Получить русское название уровня бюджета
 */
export const getBudgetTierLabel = (budgetTier: string): string => {
  switch (budgetTier) {
    case 'High': return 'Высокий';
    case 'Medium': return 'Средний';
    case 'Low': return 'Низкий';
    default: return budgetTier;
  }
};

/**
 * Получить описание уровня бюджета
 */
export const getBudgetTierDescription = (budgetTier: string): string => {
  switch (budgetTier) {
    case 'High': return 'Приоритет производительности, стоимость вторична';
    case 'Medium': return 'Баланс стоимости и производительности';
    case 'Low': return 'Приоритет экономии, допустимо снижение производительности';
    default: return '';
  }
};

/**
 * Форматировать требования SLA для отображения
 */
export const formatSlaRequirements = (sla?: SlaRequirement): string => {
  if (!sla) return 'Не указаны';
  
  return `${sla.availabilityTarget}% доступности, ` +
         `отклик ≤ ${sla.maxResponseTimeMs}мс, ` +
         `допустимо ${sla.allowedDowntimePerMonth}мин/мес простоя`;
};

/**
 * Форматировать бизнес-часы для отображения
 */
export const formatBusinessHours = (hours?: BusinessHours): string => {
  if (!hours) return 'Круглосуточно';
  
  const daysMap: Record<number, string> = {
    1: 'Пн', 2: 'Вт', 3: 'Ср', 4: 'Чт', 5: 'Пт', 6: 'Сб', 7: 'Вс'
  };
  const days = hours.workingDays.map((d: number) => daysMap[d]).join(', ');
  
  const peakHours = hours.peakHours.map((h: { start: string; end: string }) => 
    `${h.start}-${h.end}`
  ).join(', ');
  
  return `${days}: ${peakHours || 'нет пиковых часов'} (${hours.timezone}), ` +
         `выходные: ${hours.weekendLoadPercent}% нагрузки`;
};
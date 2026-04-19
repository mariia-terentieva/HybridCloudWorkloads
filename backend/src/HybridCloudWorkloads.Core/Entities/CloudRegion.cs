using System;
using System.Collections.Generic;

namespace HybridCloudWorkloads.Core.Entities;


// Регион облачного провайдера

public class CloudRegion
{
    public Guid Id { get; set; }
    
    
    // ID провайдера
    
    public Guid ProviderId { get; set; }
    
    
    // Код региона (eu-west-1, ru-central1)
    
    public string Code { get; set; } = string.Empty;
    
    
    // Название региона
    
    public string Name { get; set; } = string.Empty;
    
    
    // Отображаемое имя
    
    public string DisplayName { get; set; } = string.Empty;
    
    
    // Континент/географическое расположение
    
    public string Continent { get; set; } = string.Empty;
    
    
    // Страна
    
    public string? Country { get; set; }
    
    
    // Город
    
    public string? City { get; set; }
    
    
    // Координаты (lat,lng)
    
    public string? Coordinates { get; set; }
    
    
    // Статус региона
    
    public RegionStatus Status { get; set; } = RegionStatus.Available;
    
    
    // Зоны доступности (количество)
    
    public int AvailabilityZones { get; set; } = 1;
    
    
    // Требования комплаенса (массив строк в JSON)
    
    public string? Compliance { get; set; }
    
    
    // Поддерживаемые сервисы (ID сервисов в JSON)
    
    public string? AvailableServices { get; set; }
    
    
    // Метаданные региона (JSON)
    
    public string? Metadata { get; set; }
    
    
    // Дата создания
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    
    // Дата обновления
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public virtual CloudProvider Provider { get; set; } = null!;
    public virtual ICollection<InstanceType> InstanceTypes { get; set; } = new List<InstanceType>();
}


// Статус региона

public enum RegionStatus
{
    Available = 0,
    Limited = 1,
    Unavailable = 2,
    ComingSoon = 3
}

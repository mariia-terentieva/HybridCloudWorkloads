using System;
using System.Collections.Generic;

namespace HybridCloudWorkloads.Core.Entities;

/// <summary>
/// Тип инстанса (виртуальной машины)
/// </summary>
public class InstanceType
{
    public Guid Id { get; set; }
    
    /// <summary>
    /// ID провайдера
    /// </summary>
    public Guid ProviderId { get; set; }
    
    /// <summary>
    /// ID региона
    /// </summary>
    public Guid RegionId { get; set; }
    
    /// <summary>
    /// ID сервиса (опционально)
    /// </summary>
    public Guid? ServiceId { get; set; }
    
    /// <summary>
    /// Уникальный идентификатор типа (t3.micro, standard-v2)
    /// </summary>
    public string TypeCode { get; set; } = string.Empty;
    
    /// <summary>
    /// Отображаемое имя
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Описание
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Категория инстанса (General Purpose, Compute Optimized, Memory Optimized, etc.)
    /// </summary>
    public string Category { get; set; } = "General Purpose";
    
    /// <summary>
    /// Семейство инстансов (t3, m5, c5)
    /// </summary>
    public string Family { get; set; } = string.Empty;
    
    /// <summary>
    /// Поколение
    /// </summary>
    public int Generation { get; set; } = 1;
    
    /// <summary>
    /// Количество vCPU
    /// </summary>
    public double VcpuCount { get; set; }
    
    /// <summary>
    /// Модель процессора
    /// </summary>
    public string? CpuModel { get; set; }
    
    /// <summary>
    /// Архитектура процессора (x86_64, arm64)
    /// </summary>
    public string CpuArchitecture { get; set; } = "x86_64";
    
    /// <summary>
    /// Частота процессора (GHz)
    /// </summary>
    public double? CpuClockSpeedGhz { get; set; }
    
    /// <summary>
    /// Тип процессора (Burstable, Dedicated, Shared)
    /// </summary>
    public string CpuType { get; set; } = "Dedicated";
    
    /// <summary>
    /// Объем RAM (GB)
    /// </summary>
    public double MemoryGb { get; set; }
    
    /// <summary>
    /// Пропускная способность сети (Gbps)
    /// </summary>
    public double NetworkBandwidthGbps { get; set; }
    
    /// <summary>
    /// Тип сети
    /// </summary>
    public string? NetworkPerformance { get; set; }
    
    /// <summary>
    /// Тип хранилища (EBS, SSD, NVMe)
    /// </summary>
    public string StorageType { get; set; } = "EBS";
    
    /// <summary>
    /// Объем локального хранилища (GB)
    /// </summary>
    public double? LocalStorageGb { get; set; }
    
    /// <summary>
    /// Количество дисков локального хранилища
    /// </summary>
    public int? LocalStorageDisks { get; set; }
    
    /// <summary>
    /// Поддержка EBS оптимизации
    /// </summary>
    public bool EbsOptimized { get; set; }
    
    /// <summary>
    /// Максимальная пропускная способность EBS (Mbps)
    /// </summary>
    public double? MaxEbsBandwidthMbps { get; set; }
    
    /// <summary>
    /// Максимальное количество IOPS
    /// </summary>
    public int? MaxIops { get; set; }
    
    /// <summary>
    /// Поддержка ускорения GPU
    /// </summary>
    public bool HasGpu { get; set; }
    
    /// <summary>
    /// Модель GPU
    /// </summary>
    public string? GpuModel { get; set; }
    
    /// <summary>
    /// Количество GPU
    /// </summary>
    public int? GpuCount { get; set; }
    
    /// <summary>
    /// Память GPU (GB)
    /// </summary>
    public int? GpuMemoryGb { get; set; }
    
    /// <summary>
    /// Поддержка FPGA
    /// </summary>
    public bool HasFpga { get; set; }
    
    /// <summary>
    /// Тип виртуализации (HVM, PV)
    /// </summary>
    public string VirtualizationType { get; set; } = "HVM";
    
    /// <summary>
    /// Поддержка Enhanced Networking
    /// </summary>
    public bool EnhancedNetworking { get; set; }
    
    /// <summary>
    /// Поддержка размещения в placement group
    /// </summary>
    public bool PlacementGroupSupported { get; set; }
    
    /// <summary>
    /// Поддержка выделенных хостов
    /// </summary>
    public bool DedicatedHostSupported { get; set; }
    
    /// <summary>
    /// Физический процессор
    /// </summary>
    public string? PhysicalProcessor { get; set; }
    
    /// <summary>
    /// Характеристики производительности (JSON)
    /// </summary>
    public string? PerformanceCharacteristics { get; set; }
    
    /// <summary>
    /// Статус доступности
    /// </summary>
    public InstanceAvailability Availability { get; set; } = InstanceAvailability.Available;
    
    /// <summary>
    /// Дата создания
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Дата обновления
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Навигационные свойства
    public virtual CloudProvider Provider { get; set; } = null!;
    public virtual CloudRegion Region { get; set; } = null!;
    public virtual CloudService? Service { get; set; }
    public virtual ICollection<InstancePricing> Pricing { get; set; } = new List<InstancePricing>();
}

/// <summary>
/// Доступность инстанса
/// </summary>
public enum InstanceAvailability
{
    Available = 0,
    Limited = 1,
    Deprecated = 2,
    Unavailable = 3
}

/// <summary>
/// Категории инстансов
/// </summary>
public static class InstanceCategories
{
    public const string GeneralPurpose = "General Purpose";
    public const string ComputeOptimized = "Compute Optimized";
    public const string MemoryOptimized = "Memory Optimized";
    public const string StorageOptimized = "Storage Optimized";
    public const string AcceleratedComputing = "Accelerated Computing";
    public const string HighPerformanceComputing = "High Performance Computing";
    public const string Burstable = "Burstable";
}
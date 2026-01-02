//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using Dalamud.Plugin;
//using Dalamud.Plugin.Services;

//namespace BetterMountBGM;

///// <summary>
///// Informações adicionais sobre montarias (origem, tipo, etc)
///// </summary>
//public class MountSourceInfo
//{
//    [JsonPropertyName("name")]
//    public string Name { get; set; } = string.Empty;

//    [JsonPropertyName("type")]
//    public string Type { get; set; } = string.Empty;

//    [JsonPropertyName("acquired_by")]
//    public string AcquiredBy { get; set; } = string.Empty;

//    [JsonPropertyName("patch")]
//    public string Patch { get; set; } = string.Empty;

//    [JsonPropertyName("seats")]
//    public int Seats { get; set; } = 1;

//    [JsonPropertyName("obtainable")]
//    public bool Obtainable { get; set; } = true;

//    [JsonPropertyName("cash_shop")]
//    public bool CashShop { get; set; } = false;

//    [JsonPropertyName("market_board")]
//    public bool MarketBoard { get; set; } = false;
//}

///// <summary>
///// Database de informações de montarias
///// </summary>
//public class MountSourceDatabase
//{
//    [JsonPropertyName("version")]
//    public string Version { get; set; } = string.Empty;

//    [JsonPropertyName("last_updated")]
//    public string LastUpdated { get; set; } = string.Empty;

//    [JsonPropertyName("source")]
//    public string Source { get; set; } = string.Empty;

//    [JsonPropertyName("total_mounts")]
//    public int TotalMounts { get; set; }

//    [JsonPropertyName("note")]
//    public string Note { get; set; } = string.Empty;

//    [JsonPropertyName("mounts")]
//    public Dictionary<string, MountSourceInfo> Mounts { get; set; } = new();
//}

///// <summary>
///// Helper para carregar e acessar informações de aquisição das montarias
///// </summary>
//public static class MountSourceHelper
//{
//    private static MountSourceDatabase? _database = null;
//    private static bool _loaded = false;

//    /// <summary>
//    /// Carrega o database do arquivo JSON
//    /// </summary>
//    public static void LoadDatabase(IDalamudPluginInterface pluginInterface)
//    {
//        if (_loaded) return;

//        try
//        {
//            // Tenta carregar de external_data primeiro
//            var jsonPath = Path.Combine(
//                pluginInterface.AssemblyLocation.Directory?.FullName!,
//                "external_data",
//                "mount_sources_complete.json");

//            // Fallback para o diretório raiz
//            if (!File.Exists(jsonPath))
//            {
//                jsonPath = Path.Combine(
//                    pluginInterface.AssemblyLocation.Directory?.FullName!,
//                    "mount_sources.json");
//            }

//            if (!File.Exists(jsonPath))
//            {
//                Plugin.PluginLog.Warning($"Mount source database not found at: {jsonPath}");
//                _database = new MountSourceDatabase();
//                _loaded = true;
//                return;
//            }

//            var jsonContent = File.ReadAllText(jsonPath);
//            _database = JsonSerializer.Deserialize<MountSourceDatabase>(jsonContent);

//            Plugin.PluginLog.Information($"Loaded mount source database from {jsonPath}: {_database?.TotalMounts ?? 0} mounts");
//            _loaded = true;
//        }
//        catch (Exception ex)
//        {
//            Plugin.PluginLog.Error($"Error loading mount source database: {ex.Message}");
//            _database = new MountSourceDatabase();
//            _loaded = true;
//        }
//    }

//    /// <summary>
//    /// Obtém informação de aquisição de uma montaria pelo nome
//    /// </summary>
//    public static string? GetAcquiredBy(string mountName)
//    {
//        if (_database == null) return null;

//        // Busca por nome (case-insensitive)
//        var entry = _database.Mounts.Values
//            .FirstOrDefault(m => m.Name.Equals(mountName, StringComparison.OrdinalIgnoreCase));

//        return entry?.AcquiredBy;
//    }

//    /// <summary>
//    /// Obtém informação de aquisição de uma montaria pelo ID
//    /// NOTA: Os IDs no JSON são placeholders. Use este método com cuidado.
//    /// É melhor usar GetAcquiredBy(string mountName)
//    /// </summary>
//    public static string? GetAcquiredById(uint mountId)
//    {
//        if (_database == null) return null;

//        var key = mountId.ToString();
//        if (_database.Mounts.TryGetValue(key, out var info))
//        {
//            return info.AcquiredBy;
//        }

//        return null;
//    }

//    /// <summary>
//    /// Obtém informação completa de uma montaria
//    /// </summary>
//    public static MountSourceInfo? GetMountSourceInfo(string mountName)
//    {
//        if (_database == null) return null;

//        return _database.Mounts.Values
//            .FirstOrDefault(m => m.Name.Equals(mountName, StringComparison.OrdinalIgnoreCase));
//    }

//    /// <summary>
//    /// Verifica se a database foi carregada
//    /// </summary>
//    public static bool IsLoaded => _loaded;

//    /// <summary>
//    /// Total de montarias no database
//    /// </summary>
//    public static int TotalMounts => _database?.TotalMounts ?? 0;
//}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dalamud.Plugin;

namespace BetterMountBGM;

/// <summary>
/// Informações de uma montaria vindas da wiki
/// </summary>
public class MountSourceInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("acquired_by")]
    public string AcquiredBy { get; set; } = string.Empty;

    [JsonPropertyName("patch")]
    public string Patch { get; set; } = string.Empty;

    [JsonPropertyName("seats")]
    public int Seats { get; set; } = 1;

    [JsonPropertyName("obtainable")]
    public bool Obtainable { get; set; } = true;

    [JsonPropertyName("cash_shop")]
    public bool CashShop { get; set; } = false;

    [JsonPropertyName("market_board")]
    public bool MarketBoard { get; set; } = false;
}

/// <summary>
/// Database completo do JSON
/// </summary>
public class MountSourceDatabase
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("last_updated")]
    public string LastUpdated { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("total_mounts")]
    public int TotalMounts { get; set; }

    [JsonPropertyName("mounts")]
    public Dictionary<string, MountSourceInfo> Mounts { get; set; } = new();
}

/// <summary>
/// Helper para carregar e buscar informações de montarias da wiki
/// </summary>
public static class MountSourceHelper
{
    private static MountSourceDatabase? _database = null;
    private static bool _loaded = false;

    /// <summary>
    /// Carrega o database do JSON
    /// </summary>
    public static void LoadDatabase(IDalamudPluginInterface pluginInterface)
    {
        if (_loaded) return;

        try
        {
            // Caminho do JSON
            var jsonPath = Path.Combine(
                pluginInterface.AssemblyLocation.Directory?.FullName!,
                "external_data",
                "mount_sources_complete.json");

            if (!File.Exists(jsonPath))
            {
                Plugin.Log.Warning($"Mount source database not found at: {jsonPath}");
                _database = new MountSourceDatabase();
                _loaded = true;
                return;
            }

            var jsonContent = File.ReadAllText(jsonPath);
            _database = JsonSerializer.Deserialize<MountSourceDatabase>(jsonContent);

            Plugin.Log.Information($"Loaded mount source database: {_database?.TotalMounts ?? 0} mounts");
            _loaded = true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading mount source database: {ex.Message}");
            _database = new MountSourceDatabase();
            _loaded = true;
        }
    }

    /// <summary>
    /// Busca informações de uma montaria pelo nome
    /// </summary>
    public static MountSourceInfo? GetMountSourceInfo(string mountName)
    {
        if (_database == null || string.IsNullOrWhiteSpace(mountName))
            return null;

        // Busca por nome exato (case-insensitive)
        var match = _database.Mounts.Values.FirstOrDefault(m =>
            string.Equals(m.Name, mountName, StringComparison.OrdinalIgnoreCase));

        return match;
    }

    /// <summary>
    /// Retorna apenas o texto "Acquired By" de uma montaria
    /// </summary>
    public static string GetAcquiredBy(string mountName)
    {
        var info = GetMountSourceInfo(mountName);
        return info?.AcquiredBy ?? "Unknown";
    }
}
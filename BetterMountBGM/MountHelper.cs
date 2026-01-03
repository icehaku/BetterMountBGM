using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;

namespace BetterMountBGM;

public static class MountHelper
{
    /// <summary>
    /// Obtém a lista de todas as montarias desbloqueadas pelo jogador
    /// </summary>
    public static unsafe List<MountInfo> GetUnlockedMounts(IDataManager dataManager)
    {
        var unlockedMounts = new List<MountInfo>();

        // Acessa o PlayerState do jogo
        var playerState = PlayerState.Instance();
        if (playerState == null)
        {
            Plugin.Log.Warning("PlayerState is null!");
            return unlockedMounts;
        }

        // Busca todas as montarias da planilha do jogo
        var mountSheet = dataManager.GetExcelSheet<Mount>();
        if (mountSheet == null)
        {
            Plugin.Log.Warning("Mount sheet is null!");
            return unlockedMounts;
        }


        // DEBUG: Ver campos disponíveis em MOUNT
        var firstMount = mountSheet.FirstOrDefault();
        Plugin.Log.Information($"Mount fields test:");
        Plugin.Log.Information($"Singular: {firstMount.Singular}");

        foreach (var mount in mountSheet)
        {
            // Pula entradas vazias
            if (mount.RowId == 0)
                continue;

            var mountName = mount.Singular.ToString();
            if (string.IsNullOrEmpty(mountName))
                continue;

            // Verifica se o jogador tem a montaria desbloqueada
            // MÉTODO 1: Usando IsMountUnlocked
            bool isUnlocked = playerState->IsMountUnlocked(mount.RowId);

            if (isUnlocked)
            {
                unlockedMounts.Add(new MountInfo
                {
                    MountId = mount.RowId,
                    Name = mount.Singular.ToString(),
                    Icon = mount.Icon,
                    Order = mount.Order,
                    RideBGM = mount.RideBGM.RowId
                });
            }
        }

        return unlockedMounts.OrderBy(m => m.Order).ToList();
    }

    /// <summary>
    /// Verifica se uma montaria específica está desbloqueada
    /// </summary>
    public static unsafe bool IsMountUnlocked(uint mountId)
    {
        var playerState = PlayerState.Instance();
        if (playerState == null) return false;

        return playerState->IsMountUnlocked(mountId);
    }

    /// <summary>
    /// Obtém informações de uma montaria específica
    /// </summary>
    public static MountInfo? GetMountInfo(IDataManager dataManager, uint mountId)
    {
        var mountSheet = dataManager.GetExcelSheet<Mount>();
        if (mountSheet == null) return null;

        var mount = mountSheet.GetRow(mountId);
        if (mount.RowId == 0) return null;

        return new MountInfo
        {
            MountId = mount.RowId,
            Name = mount.Singular.ToString(),
            Icon = mount.Icon,
            Order = mount.Order
        };
    }

    /// <summary>
    /// Obtém a lista de TODAS as montarias (locked e unlocked)
    /// </summary>
    public static unsafe List<MountInfo> GetAllMounts(IDataManager dataManager)
    {
        var allMountsList = new List<MountInfo>();

        // Acessa o PlayerState do jogo
        var playerState = PlayerState.Instance();
        if (playerState == null)
        {
            Plugin.Log.Warning("PlayerState is null!");
            return allMountsList;
        }

        // Busca todas as montarias da planilha do jogo
        var mountSheet = dataManager.GetExcelSheet<Mount>();
        if (mountSheet == null)
        {
            Plugin.Log.Warning("Mount sheet is null!");
            return allMountsList;
        }

        foreach (var mount in mountSheet)
        {
            // Pula entradas vazias
            if (mount.RowId == 0)
                continue;

            var mountName = mount.Singular.ToString();
            if (string.IsNullOrEmpty(mountName))
                continue;

            // Verifica se está desbloqueada
            bool isUnlocked = playerState->IsMountUnlocked(mount.RowId);

            // Adiciona TODAS as montarias, marcando se está unlocked ou não
            allMountsList.Add(new MountInfo
            {
                MountId = mount.RowId,
                Name = mount.Singular.ToString(),
                Icon = mount.Icon,
                Order = mount.Order,
                IsUnlocked = isUnlocked,
                RideBGM = mount.RideBGM.RowId
            });
        }

        return allMountsList.OrderBy(m => m.Order).ToList();
    }

    /// <summary>
    /// Obtém o nome de uma montaria
    /// </summary>
    public static string GetMountName(IDataManager dataManager, uint mountId)
    {
        var mountSheet = dataManager.GetExcelSheet<Mount>();
        if (mountSheet == null) return $"Mount {mountId}";

        var mount = mountSheet.GetRow(mountId);
        if (mount.RowId == 0) return $"Unknown Mount {mountId}";

        return mount.Singular.ToString();
    }
}

/// <summary>
/// Classe para armazenar informações de montaria
/// </summary>
public class MountInfo
{
    public uint MountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ushort Icon { get; set; }
    public short Order { get; set; }
    public bool IsUnlocked { get; set; } = true; // Por padrão true para compatibilidade
    public uint RideBGM { get; set; } = 0;
}

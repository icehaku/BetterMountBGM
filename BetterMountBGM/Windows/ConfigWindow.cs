using BetterMountBGM;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BetterMountBGM.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    // Cache de montarias
    private List<MountInfo>? unlockedMounts = null;
    private List<MountInfo>? allMounts = null;
    private bool mountsLoaded = false;

    // Filtros e ordenação
    private string searchFilter = string.Empty;
    private SortColumn currentSortColumn = SortColumn.Name;
    private bool sortDescending = false;
    private bool showLockedMounts = false;

    // Dicionário temporário para edição de BGM IDs
    private Dictionary<uint, string> bgmInputs = new();

    private enum SortColumn
    {
        Name,
        Id,
        Unlocked
    }

    public ConfigWindow(Plugin plugin) : base("Mount Music Configuration###MountMusicConfig")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;

        Size = new Vector2(1000, 600);  // Aumentado um pouco para caber a nova coluna
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(800, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        // Carregar database de informações da wiki
        MountSourceHelper.LoadDatabase(Plugin.PluginInterface);
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        if (!mountsLoaded)
        {
            LoadUnlockedMounts();
        }
    }

    public override void Draw()
    {
        if (unlockedMounts == null || unlockedMounts.Count == 0)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Loading mounts...");
            if (ImGui.Button("Retry Load"))
            {
                LoadUnlockedMounts();
            }
            return;
        }

        // ═══════════════════════════════════════════════════════════════
        // CABEÇALHO E CONTROLES
        // ═══════════════════════════════════════════════════════════════

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Mount Music Customization");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1),
            $"  |  Mounts Unlocked: {unlockedMounts.Count} / 337");

        ImGui.Spacing();

        // Campo de busca
        ImGui.SetNextItemWidth(300);
        if (ImGui.InputTextWithHint("##search", "Search mounts...", ref searchFilter, 100))
        {
            // Filtro é aplicado ao renderizar a tabela
        }

        ImGui.SameLine();

        // Dropdown de ordenação
        ImGui.Text("Sort by:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        string sortLabel = currentSortColumn switch
        {
            SortColumn.Name => "Alphabet",
            SortColumn.Id => "Id",
            SortColumn.Unlocked => "Unlocked",
            _ => "Alphabet"
        };

        if (ImGui.BeginCombo("##sortby", sortLabel))
        {
            if (ImGui.Selectable("Alphabet", currentSortColumn == SortColumn.Name))
                currentSortColumn = SortColumn.Name;
            if (ImGui.Selectable("Id", currentSortColumn == SortColumn.Id))
                currentSortColumn = SortColumn.Id;
            if (ImGui.Selectable("Unlocked", currentSortColumn == SortColumn.Unlocked))
                currentSortColumn = SortColumn.Unlocked;

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Reload Mounts"))
        {
            LoadUnlockedMounts();
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
            ImGui.Text("Unlocked Mounts are reloaded every time you open the plugin.");
            ImGui.Text("Use this button only to update if you unlocked a mount");
            ImGui.Text("while the plugin config was open.");
            ImGui.PopTextWrapPos();
            ImGui.EndTooltip();
        }

        ImGui.Spacing();

        // Checkbox para mostrar montarias travadas
        ImGui.Checkbox("Show Locked Mounts", ref showLockedMounts);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ═══════════════════════════════════════════════════════════════
        // TABELA DE MONTARIAS - ATUALIZADA COM NOVA COLUNA
        // ═══════════════════════════════════════════════════════════════

        var filteredMounts = GetFilteredAndSortedMounts();

        ImGuiTableFlags tableFlags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable;

        // NOVA ESTRUTURA: 5 colunas (adicionamos "Acquired By")
        if (ImGui.BeginTable("MountsTable", 5, tableFlags, new Vector2(0, -30)))
        {
            // Setup de colunas
            ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Unlocked", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Acquired By", ImGuiTableColumnFlags.WidthStretch);  // NOVA COLUNA
            ImGui.TableSetupColumn("Custom BGM", ImGuiTableColumnFlags.WidthFixed, 100);

            // Renderiza cada montaria
            foreach (var mount in filteredMounts)
            {
                ImGui.TableNextRow();

                // Coluna 1: Ícone
                ImGui.TableSetColumnIndex(0);
                RenderMountIcon(mount);

                // Coluna 2: Nome
                ImGui.TableSetColumnIndex(1);
                ImGui.AlignTextToFramePadding();
                ImGui.Text(mount.Name);

                // Coluna 3: Status (Unlocked/Locked)
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                if (mount.IsUnlocked)
                {
                    ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1), "Unlocked");
                }
                else
                {
                    ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1), "Locked");
                }

                // Coluna 4: Acquired By (NOVA!)
                ImGui.TableSetColumnIndex(3);
                ImGui.AlignTextToFramePadding();

                var acquiredBy = MountSourceHelper.GetAcquiredBy(mount.Name);

                // Cor diferente para "Unknown"
                if (acquiredBy == "Unknown")
                {
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), acquiredBy);
                }
                else
                {
                    ImGui.TextWrapped(acquiredBy);  // Usa TextWrapped para textos longos
                }

                // Coluna 5: Custom BGM (input field)
                ImGui.TableSetColumnIndex(4);
                RenderCustomBGMInput(mount);
            }

            ImGui.EndTable();
        }

        // ═══════════════════════════════════════════════════════════════
        // RODAPÉ
        // ═══════════════════════════════════════════════════════════════

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1),
            $"💡 Tip: Leave 'Custom BGM' empty to use default mount music  |  Total configured: {configuration.MountMusicOverrides.Count}");
    }

    private void RenderMountIcon(MountInfo mount)
    {
        // Tenta carregar o ícone da montaria
        var icon = Plugin.TextureProvider.GetFromGameIcon((uint)mount.Icon).GetWrapOrDefault();

        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(40, 40));
        }
        else
        {
            // Fallback: quadrado vazio se não carregar
            ImGui.Dummy(new Vector2(40, 40));
        }
    }

    private void RenderCustomBGMInput(MountInfo mount)
    {
        if (!bgmInputs.ContainsKey(mount.MountId))
        {
            if (configuration.MountMusicOverrides.TryGetValue(mount.MountId, out var bgmId))
            {
                bgmInputs[mount.MountId] = bgmId.ToString();
            }
            else
            {
                bgmInputs[mount.MountId] = string.Empty;
            }
        }

        string currentInput = bgmInputs[mount.MountId];
        ImGui.SetNextItemWidth(-1);

        if (ImGui.InputTextWithHint($"##bgm_{mount.MountId}", "Custom BGM ID...",
            ref currentInput, 10, ImGuiInputTextFlags.CharsDecimal))
        {
            bgmInputs[mount.MountId] = currentInput;

            if (string.IsNullOrWhiteSpace(currentInput))
            {
                if (configuration.MountMusicOverrides.ContainsKey(mount.MountId))
                {
                    configuration.MountMusicOverrides.Remove(mount.MountId);
                    configuration.Save();
                    Plugin.Log.Information($"Removed custom BGM for {mount.Name}");
                }
            }
            else if (ushort.TryParse(currentInput, out ushort bgmId) && bgmId > 0)
            {
                configuration.MountMusicOverrides[mount.MountId] = bgmId;
                configuration.Save();
                Plugin.Log.Information($"Set {mount.Name} → BGM {bgmId}");
            }
        }
    }

    private List<MountInfo> GetFilteredAndSortedMounts()
    {
        var sourceList = showLockedMounts ? allMounts : unlockedMounts;
        if (sourceList == null) return new List<MountInfo>();

        var filtered = sourceList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            filtered = filtered.Where(m =>
                m.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                m.MountId.ToString().Contains(searchFilter));
        }

        filtered = currentSortColumn switch
        {
            SortColumn.Name => sortDescending
                ? filtered.OrderByDescending(m => m.Name)
                : filtered.OrderBy(m => m.Name),

            SortColumn.Id => sortDescending
                ? filtered.OrderByDescending(m => m.MountId)
                : filtered.OrderBy(m => m.MountId),

            SortColumn.Unlocked => showLockedMounts
                ? filtered.OrderByDescending(m => m.IsUnlocked).ThenBy(m => m.Name)
                : filtered.OrderBy(m => m.Name),

            _ => filtered.OrderBy(m => m.Name)
        };

        return filtered.ToList();
    }

    private void LoadUnlockedMounts()
    {
        try
        {
            unlockedMounts = MountHelper.GetUnlockedMounts(Plugin.DataManager);
            allMounts = MountHelper.GetAllMounts(Plugin.DataManager);
            mountsLoaded = true;
            bgmInputs.Clear();
            Plugin.Log.Information($"Loaded {unlockedMounts.Count} unlocked mounts and {allMounts.Count} total mounts");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading mounts: {ex.Message}");
            unlockedMounts = new List<MountInfo>();
            allMounts = new List<MountInfo>();
            mountsLoaded = false;
        }
    }
}
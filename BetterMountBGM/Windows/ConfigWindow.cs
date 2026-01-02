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
    private bool showLockedMounts = true;
    private bool showMissingData = false;

    // NOVO: Filtro por Type
    private string selectedTypeFilter = "All Types";
    private List<string> availableTypes = new();

    // Dicionário temporário para edição de BGM IDs
    private Dictionary<uint, string> bgmInputs = new();

    private enum SortColumn
    {
        Name,
        Id,
        Unlocked,
        Type  // NOVO: Ordenação por Type
    }

    public ConfigWindow(Plugin plugin) : base("Better Mount BGM###MountMusicConfig")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;

        Size = new Vector2(1100, 600);  // Aumentado para acomodar nova coluna
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(1400, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        // Carregar database de informações da wiki
        MountSourceHelper.LoadDatabase(Plugin.PluginInterface);

        // Carregar tipos disponíveis
        LoadAvailableTypes();
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

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Mount BGM Customization");
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1),
            $"  |  Mounts Unlocked: {unlockedMounts.Count} / 337");

        ImGui.Spacing();

        // ═══════════════════════════════════════════════════════════════
        // LINHA 1: BUSCA + SORT BY + FILTER TYPE
        // ═══════════════════════════════════════════════════════════════

        // Campo de busca
        ImGui.SetNextItemWidth(250);
        if (ImGui.InputTextWithHint("##search", "Search mounts...", ref searchFilter, 100))
        {
            // Filtro é aplicado ao renderizar a tabela
        }

        ImGui.SameLine();

        // Dropdown de ordenação
        ImGui.Text("Sort by:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        string sortLabel = currentSortColumn switch
        {
            SortColumn.Name => "Alphabet",
            SortColumn.Id => "Id",
            SortColumn.Unlocked => "Unlocked",
            SortColumn.Type => "Type",  // NOVO
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
            if (ImGui.Selectable("Type", currentSortColumn == SortColumn.Type))  // NOVO
                currentSortColumn = SortColumn.Type;

            ImGui.EndCombo();
        }

        ImGui.Separator();
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Filters");
        ImGui.Text("Type:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        if (ImGui.BeginCombo("##filtertype", selectedTypeFilter))
        {
            foreach (var type in availableTypes)
            {
                if (ImGui.Selectable(type, selectedTypeFilter == type))
                {
                    selectedTypeFilter = type;
                }
            }
            ImGui.EndCombo();
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

        // ═══════════════════════════════════════════════════════════════
        // LINHA 2: CHECKBOXES DE FILTRO
        // ═══════════════════════════════════════════════════════════════

        ImGui.Checkbox("Show Locked Mounts", ref showLockedMounts);
        ImGui.SameLine();
        ImGui.Checkbox("Show Missing Data", ref showMissingData);

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Legends");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        

        // ═══════════════════════════════════════════════════════════════
        // TABELA DE MONTARIAS (6 colunas: NOVA coluna Type)
        // ═══════════════════════════════════════════════════════════════

        var filteredMounts = GetFilteredAndSortedMounts();

        ImGuiTableFlags tableFlags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable;

        if (ImGui.BeginTable("MountsTable", 5, tableFlags, new Vector2(0, -30)))  // 6 colunas agora
        {
            // Setup de colunas
            ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 200);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 300);  // NOVA coluna
            ImGui.TableSetupColumn("Acquired By", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Custom BGM", ImGuiTableColumnFlags.WidthFixed, 10);
            ImGui.TableHeadersRow();  // NOVO: Headers visíveis

            // Renderiza cada montaria
            foreach (var mount in filteredMounts)
            {
                ImGui.TableNextRow();

                // Verifica se tem dados no JSON
                bool hasMissingData = MountSourceHelper.GetMountSourceInfo(mount.Name) == null;
                var mountInfo = MountSourceHelper.GetMountSourceInfo(mount.Name);

                // Coluna: Ícone
                ImGui.TableSetColumnIndex(0);
                RenderMountIcon(mount);

                // Coluna: Nome
                ImGui.TableSetColumnIndex(1);
                ImGui.AlignTextToFramePadding();
                ImGui.Text(mount.Name);
                //ImGui.Text($"{mount.Name} (ID: {mount.MountId})");

                // Coluna: Status (Unlocked/Locked)
                //ImGui.TableSetColumnIndex(2);
                //ImGui.AlignTextToFramePadding();
                //if (mount.IsUnlocked)
                //{
                //    ImGui.TextColored(new Vector4(0.2f, 1f, 0.2f, 1), "Unlocked");
                //}
                //else
                //{
                //    ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1), "Locked");
                //}

                // Coluna: Type
                ImGui.TableSetColumnIndex(2);
                ImGui.AlignTextToFramePadding();
                if (hasMissingData || mountInfo == null)
                {
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), "Unknown");
                }
                else
                {
                    // Colorir por categoria para facilitar visualização
                    var typeColor = GetTypeColor(mountInfo.Type);
                    ImGui.TextColored(typeColor, mountInfo.Type);
                }

                // Coluna: Acquired By
                ImGui.TableSetColumnIndex(3);
                ImGui.AlignTextToFramePadding();

                if (hasMissingData)
                {
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), "Unknown");
                }
                else
                {
                    var acquiredBy = MountSourceHelper.GetAcquiredBy(mount.Name);
                    ImGui.TextWrapped(acquiredBy);
                }

                // Coluna: Custom BGM (input field)
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

        var filteredCount = filteredMounts.Count;
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1),
            $"💡 Tip: Leave 'Custom BGM' empty to use default mount music  | Musics Changed: {configuration.MountMusicOverrides.Count}");
    }

    /// <summary>
    /// Retorna cor baseada no tipo de montaria para melhor visualização
    /// </summary>
    private Vector4 GetTypeColor(string type)
    {
        return type switch
        {
            //verde limao
            "Main Scenario" => new Vector4(0.45f, 0.95f, 0.05f, 1f),

            // Verde
            "Quests" => new Vector4(0.2f, 1f, 0.2f, 1),       

            // Dourado (Mogstation)
            "Premium" => new Vector4(1f, 0.84f, 0f, 1),      
            "Campaigns" => new Vector4(1f, 0.84f, 0f, 1),

            // Laranja
            "Limited" => new Vector4(1f, 0.84f, 0f, 1),
            "Seasonal Event" => new Vector4(1f, 0.84f, 0f, 1),  
            "Gold Saucer" => new Vector4(1f, 0.65f, 0f, 1),

            // Amarelo
            "Gil" => new Vector4(1f, 1f, 0f, 1f),            
            "Treasure Hunt" => new Vector4(1f, 1f, 0f, 1f),

            // Azul
            "Dungeons" => new Vector4(0.2f, 0.6f, 1f, 1),
            "V&C Dungeons" => new Vector4(0.2f, 0.6f, 1f, 1),

            // Vermelho
            "Trials" => new Vector4(0.8f, 0.2f, 0.2f, 1),

            // Roxo
            "Raids" => new Vector4(0.6f, 0.2f, 0.8f, 1),
            "Chaotic Alliance Raid" => new Vector4(0.6f, 0.2f, 0.8f, 1),

            // Verde claro
            "Achievements" => new Vector4(0.4f, 0.8f, 0.4f, 1),
            "Achievement Certificates" => new Vector4(0.4f, 0.8f, 0.4f, 1),

            // Roxo escuro
            "Deep Dungeon" => new Vector4(0.5f, 0.3f, 0.7f, 1), 
            "FATE" => new Vector4(0.5f, 0.3f, 0.7f, 1),
            "Shared FATEs" => new Vector4(0.5f, 0.3f, 0.7f, 1),

            // Vermelho claro
            "PvP" => new Vector4(1f, 0.4f, 0.4f, 1),         
            "PvP (Ranked)" => new Vector4(1f, 0.4f, 0.4f, 1),

            // Amarelo escuro
            "Crafting" => new Vector4(0.8f, 0.6f, 0.2f, 1),
            "Gathering" => new Vector4(0.8f, 0.6f, 0.2f, 1),
            "The Hunt" => new Vector4(0.8f, 0.6f, 0.2f, 1),

            // Ciano
            "Occult Crescent" => new Vector4(0.2f, 0.8f, 1f, 1),
            "Bozja" => new Vector4(0.2f, 0.8f, 1f, 1),
            "Ishgardian Restoration" => new Vector4(0.2f, 0.8f, 1f, 1),
            "Cosmic Exploration" => new Vector4(0.2f, 0.8f, 1f, 1),

            // Azul
            "Heaven-on-High" => new Vector4(0f, 0.4f, 1f, 1f),
            "Eureka" => new Vector4(0f, 0.4f, 1f, 1f),
            "Eureka Orthos" => new Vector4(0f, 0.4f, 1f, 1f),
            "Palace of the Dead" => new Vector4(0f, 0.4f, 1f, 1f),
            "Pilgrim's Traverse" => new Vector4(0f, 0.4f, 1f, 1f),           

            // Rosa 
            "Custom Deliveries" => new Vector4(1f, 0.4f, 1f, 1f),
            "Faux Hollows" => new Vector4(1f, 0.4f, 1f, 1f),
            "Wondrous Tails" => new Vector4(1f, 0.4f, 1f, 1f),
            "Allied Societies" => new Vector4(1f, 0.4f, 1f, 1f),
            "Island Sanctuary" => new Vector4(1f, 0.4f, 1f, 1f),

            _ => new Vector4(1f, 1f, 1f, 1)                  // Branco (padrão)
        };
    }

    private void RenderMountIcon(MountInfo mount)
    {
        var icon = Plugin.TextureProvider.GetFromGameIcon((uint)mount.Icon).GetWrapOrDefault();

        if (icon != null)
        {
            // Definir cor da borda baseada no unlock status
            var borderColor = mount.IsUnlocked
                ? new Vector4(0.2f, 1f, 0.2f, 1)   // Verde para unlocked
                : new Vector4(1f, 0.3f, 0.3f, 1);  // Vermelho para locked

            var cursorPos = ImGui.GetCursorScreenPos();
            ImGui.Image(icon.Handle, new Vector2(40, 40));

            // Desenhar borda
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRect(
                cursorPos,
                new Vector2(cursorPos.X + 40, cursorPos.Y + 40),
                ImGui.GetColorU32(borderColor),
                0f,
                ImDrawFlags.None,
                2f  // Espessura da borda
            );
        }
        else
        {
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

        // NOVO: Filtrar apenas montarias com ícone válido
        filtered = filtered.Where(m => m.Icon > 0);

        // FILTRO: Show Missing Data
        if (showMissingData)
        {
            filtered = filtered.Where(m => MountSourceHelper.GetMountSourceInfo(m.Name) == null);
        }

        // FILTRO: Por Type (NOVO)
        if (selectedTypeFilter != "All Types")
        {
            filtered = filtered.Where(m =>
            {
                var info = MountSourceHelper.GetMountSourceInfo(m.Name);
                return info != null && info.Type == selectedTypeFilter;
            });
        }

        // Filtro por busca
        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            filtered = filtered.Where(m =>
                m.Name.Contains(searchFilter, StringComparison.OrdinalIgnoreCase) ||
                m.MountId.ToString().Contains(searchFilter));
        }

        // Ordenação
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

            // NOVO: Ordenação por Type
            SortColumn.Type => filtered.OrderBy(m =>
            {
                var info = MountSourceHelper.GetMountSourceInfo(m.Name);
                return info?.Type ?? "ZZZ_Unknown";  // Unknown vai pro final
            }).ThenBy(m => m.Name),

            _ => filtered.OrderBy(m => m.Name)
        };

        return filtered.ToList();
    }

    /// <summary>
    /// Carrega todos os tipos disponíveis do JSON para o dropdown de filtro
    /// </summary>
    private void LoadAvailableTypes()
    {
        availableTypes.Clear();
        availableTypes.Add("All Types");

        // Tipos REAIS extraídos do JSON (38 tipos únicos)
        var typesFromJson = new List<string>
    {
        "Achievement Certificates",
        "Achievements",
        "Airship Ventures",
        "Allied Societies",
        "Bozja",
        "Campaigns",
        "Chaotic Alliance Raid",
        "Cosmic Exploration",
        "Crafting",
        "Custom Deliveries",
        "Dungeons",
        "Eureka",
        "Eureka Orthos",
        "FATE",
        "Faux Hollows",
        "Gathering",
        "Gil",
        "Gold Saucer",
        "Heaven-on-High",
        "Ishgardian Restoration",
        "Island Sanctuary",
        "Limited",
        "Main Scenario",
        "Occult Crescent",
        "Palace of the Dead",
        "Pilgrim's Traverse",
        "Premium",
        "PvP",
        "PvP (Ranked)",
        "Quests",
        "Raids",
        "Seasonal Event",
        "Shared FATEs",
        "The Hunt",
        "Treasure Hunt",
        "Trials",
        "Unknown",
        "V&C Dungeons",
        "Wondrous Tails"
    };

        availableTypes.AddRange(typesFromJson);
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
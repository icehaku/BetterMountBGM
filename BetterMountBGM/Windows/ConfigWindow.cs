using BetterMountBGM;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace BetterMountBGM.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;

    // xxx
    private readonly string mogImagePath;
    private readonly string boardImagePath;
    private readonly string musicImagePath;

    // type
    private readonly string sharedFatesIconPath;    

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

    // Filtro por Type
    private string selectedTypeFilter = "All Types";
    private List<string> availableTypes = new();

    // Dicionário temporário para edição de BGM IDs
    private Dictionary<uint, string> bgmInputs = new();

    private enum SortColumn
    {
        Name,
        Id,
        Unlocked,
        Type
    }

    public ConfigWindow(Plugin plugin) : base("Better Mount BGM###MountMusicConfig")
    {
        this.plugin = plugin;
        configuration = plugin.Configuration;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(1000, 500),
            MaximumSize = new Vector2(float.MinValue, float.MaxValue)
        };

        //this.mogImagePath = mogImagePath;
        this.mogImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "mogstation_icon.png");
        this.boardImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "market_board_icon.png");
        this.musicImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "music_icon.png");

        // Icones do TYPE
        this.sharedFatesIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Shared_fates_icon1.png");

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

        // xxx
        var mogImage = Plugin.TextureProvider.GetFromFile(mogImagePath).GetWrapOrDefault();
        var boardImage = Plugin.TextureProvider.GetFromFile(boardImagePath).GetWrapOrDefault();
        var musicImage = Plugin.TextureProvider.GetFromFile(musicImagePath).GetWrapOrDefault();

        // types
        var sharedFatesIcon = Plugin.TextureProvider.GetFromFile(sharedFatesIconPath).GetWrapOrDefault();

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
            SortColumn.Type => "Type",
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
            if (ImGui.Selectable("Type", currentSortColumn == SortColumn.Type))
                currentSortColumn = SortColumn.Type;

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.Checkbox("Show Locked Mounts", ref showLockedMounts);
        //ImGui.Checkbox("Show Missing Data", ref showMissingData);

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

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Legends");

        if (mogImage != null)
        {
            ImGui.Image(mogImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            ImGui.Text(" - Mogstation Purchase");
        }

        if (boardImage != null) 
        {
            ImGui.Image(boardImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            ImGui.Text(" - Market Board Purchasable");
        }

        if (musicImage != null)
        {
            ImGui.Image(musicImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            ImGui.Text(" - Has Unique Music");
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        

        // ═══════════════════════════════════════════════════════════════
        // TABELA DE MONTARIAS
        // ═══════════════════════════════════════════════════════════════

        var filteredMounts = GetFilteredAndSortedMounts();

        ImGuiTableFlags tableFlags =
            ImGuiTableFlags.Borders |
            ImGuiTableFlags.RowBg |
            ImGuiTableFlags.ScrollY |
            ImGuiTableFlags.Resizable;

        if (ImGui.BeginTable("MountsTable", 6, tableFlags, new Vector2(0, -30)))
        {
            // Setup de colunas
            ImGui.TableSetupColumn("Icon", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 60);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 150);
            ImGui.TableSetupColumn("Info", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 166);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 50);
            ImGui.TableSetupColumn("Acquired By", ImGuiTableColumnFlags.WidthStretch);  // Pega o resto
            ImGui.TableSetupColumn("Custom BGM", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 120);
            ImGui.TableHeadersRow();

            // Renderiza cada montaria
            foreach (var mount in filteredMounts)
            {
                ImGui.TableNextRow();

                // Verifica se tem dados no JSON
                bool hasMissingData = MountSourceHelper.GetMountSourceInfo(mount.Name) == null;

                // Coluna: Ícone
                ImGui.TableSetColumnIndex(0);
                RenderMountIcon(mount);

                // Coluna: Nome
                ImGui.TableSetColumnIndex(1);
                ImGui.AlignTextToFramePadding();
                ImGui.TextWrapped(ToCamelCase(mount.Name));
                //ImGui.Text($"{mount.Name} (ID: {mount.MountId})");

                // Mostrar seats abaixo do nome
                var mountInfo = MountSourceHelper.GetMountSourceInfo(mount.Name);
                if (mountInfo != null && mountInfo.Seats > 1)
                {
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), $"Seats x{mountInfo.Seats}");
                }

                // Coluna: Shop
                ImGui.TableSetColumnIndex(2);

                // Ícone Mogstation
                if (mogImage != null)
                {
                    var cursorPos = ImGui.GetCursorPos();
                    cursorPos.Y += 5;
                    ImGui.SetCursorPos(cursorPos);

                    var tintMog = (mountInfo != null && mountInfo.CashShop)
                        ? new Vector4(1f, 1f, 1f, 1f)      // Normal (tem na Mogstation)
                        : new Vector4(0.3f, 0.3f, 0.3f, 1f); // Escuro (não tem)

                    ImGui.Image(mogImage.Handle, new Vector2(50, 50), Vector2.Zero, Vector2.One, tintMog);
                }

                ImGui.SameLine();

                // Ícone Market Board
                if (boardImage != null)
                {
                    var tintBoard = (mountInfo != null && mountInfo.MarketBoard)
                        ? new Vector4(1f, 1f, 1f, 1f)      // Normal (vendável no MB)
                        : new Vector4(0.3f, 0.3f, 0.3f, 1f); // Escuro (não vendável)

                    ImGui.Image(boardImage.Handle, new Vector2(50, 50), Vector2.Zero, Vector2.One, tintBoard);
                }

                ImGui.SameLine();

                // Ícone Music (tem BGM customizado configurado?)
                if (musicImage != null)
                {
                    var hasUniqueMusic = mount.RideBGM != 638 && mount.RideBGM != 319 && mount.RideBGM != 895 && mount.RideBGM != 0;

                    var tintMusic = hasUniqueMusic
                        ? new Vector4(1f, 1f, 1f, 1f)      // Claro
                        : new Vector4(0.3f, 0.3f, 0.3f, 1f); // Escuro

                    ImGui.Image(musicImage.Handle, new Vector2(50, 50), Vector2.Zero, Vector2.One, tintMusic);
                }

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
                ImGui.TableSetColumnIndex(3);
                if (sharedFatesIcon != null)
                {
                    var cursorPos = ImGui.GetCursorPos();
                    cursorPos.Y += 5;
                    ImGui.SetCursorPos(cursorPos);

                    ImGui.Image(sharedFatesIcon.Handle, new Vector2(50, 50));

                    if (ImGui.IsItemHovered() && mountInfo != null)
                    {
                        ImGui.BeginTooltip();
                        // Colorir por categoria para dinamisar visualização
                        var typeColor = GetTypeColor(mountInfo.Type);
                        ImGui.TextColored(typeColor, mountInfo.Type);
                        ImGui.EndTooltip();
                    }
                }
                //ImGui.AlignTextToFramePadding();
                //if (hasMissingData || mountInfo == null)
                //{
                //    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1), "Unknown");
                //}
                //else
                //{
                //    // Colorir por categoria para dinamisar visualização
                //    var typeColor = GetTypeColor(mountInfo.Type);
                //    ImGui.TextColored(typeColor, mountInfo.Type);
                //}

                // Coluna: Acquired By
                ImGui.TableSetColumnIndex(4);
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
                ImGui.TableSetColumnIndex(5);
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
            var mountInfo = MountSourceHelper.GetMountSourceInfo(mount.Name);

            // Determinar cor da borda (prioridade: unobtainable > unlock status)
            Vector4 borderColor;
            if (mountInfo != null && mount.IsUnlocked)
            {
                // Verde para unlocked
                borderColor = new Vector4(0.2f, 1f, 0.2f, 1);
            }
            else if (mountInfo != null && mountInfo.Obtainable)
            {
                // Vermelho para locked
                borderColor = new Vector4(1f, 0.3f, 0.3f, 1);
            }
            else
            {
                // Roxo escuro para unobtainable
                borderColor = new Vector4(0.4f, 0.2f, 0.6f, 1);
            }

            var cursorPos = ImGui.GetCursorScreenPos();
            if (mountInfo != null && !mountInfo.Obtainable)
            {
                var tintColor = mountInfo != null && !mountInfo.Obtainable && !mount.IsUnlocked
                    ? new Vector4(0.5f, 0.5f, 0.5f, 1f)  // 50% escuro
                    : new Vector4(1f, 1f, 1f, 1f);        // Normal

                  ImGui.Image(icon.Handle, new Vector2(60, 60), Vector2.Zero, Vector2.One, tintColor);
            } else {
                ImGui.Image(icon.Handle, new Vector2(60, 60));
            }

            // Tooltip no hover ✨
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.EndTooltip();
            }

            // Desenhar borda
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRect(
                cursorPos,
                new Vector2(cursorPos.X + 60, cursorPos.Y + 60),
                ImGui.GetColorU32(borderColor),
                0f,
                ImDrawFlags.None,
                2f
            );
        }
        else
        {
            ImGui.Dummy(new Vector2(60, 60));
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

        // Filtrar apenas montarias com ícone válido
        filtered = filtered.Where(m => m.Icon > 0);

        // FILTRO: Show Missing Data
        if (showMissingData)
        {
            filtered = filtered.Where(m => MountSourceHelper.GetMountSourceInfo(m.Name) == null);
        }

        // FILTRO: Por Type
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

            // Ordenação por Type
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
    private string ToCamelCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w =>
            char.ToUpper(w[0]) + w.Substring(1).ToLower()));
    }
}
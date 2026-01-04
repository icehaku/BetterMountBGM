using BetterMountBGM;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
//using ECommons.ImGuiMethods;

namespace BetterMountBGM.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly Plugin plugin;
    private readonly uint[] defaultBGMs = {
        0, 
        62,  // Eorzea de Chobo - shared by different chocobo mounts
        121, // The Rider's Boon - shared mount bgm
        193, // Big-Boned - shared comical mount bgm (fat chocobo)
        319, // Borderless - shared bgm
        638, // The Merry Wanderes Waltz - shared by different mogstation mounts
        895  // Roads Less Traveled - shared mount bgm
    };

    // xxx
    private readonly string mogImagePath;
    private readonly string boardImagePath;
    private readonly string musicImagePath;

    // type
    private readonly string sharedFatesIconPath; // Shared_fates_icon1.png
    private readonly string achievementCurrencyIconPath; // Achievement_currency_icon1.png
    private readonly string achievementsIconPath; // Achievements_icon1.png
    private readonly string airshipVenturesIconPath; // Airship_ventures_icon1.png
    private readonly string alliedSocietyQuestsIconPath; // Allied_society_quests_icon1.png
    private readonly string bozjaIconPath; // Bozja_icon1.png
    private readonly string campaignsIconPath; // Campaigns_icon1.png
    private readonly string chaoticAllianceRaidIconPath; // Chaotic_alliance_raid_icon1.png
    private readonly string cosmicExplorationIconPath; // Cosmic_exploration_icon1.png
    private readonly string craftingIconPath; // Crafting_icon1.png
    private readonly string customDeliveriesIconPath; // Custom_deliveries_icon1.png
    private readonly string deepDungeonsIconPath; // Deep_dungeons_icon1.png
    private readonly string dungeonsIconPath; // Dungeons_icon1.png
    private readonly string eurekaIconPath; // Eureka_icon1.png
    private readonly string fatesIconPath; // Fates_icon1.png
    private readonly string fauxHollowsIconPath; // Faux_hollows_icon1.png
    private readonly string gatheringIconPath; // Gathering_icon1.png
    private readonly string gilIconPath; // Gil_icon1.png
    private readonly string goldSaucerIconPath; // Gold_saucer_icon1.png
    private readonly string ishgardianRestorationIconPath; // Ishgardian_restoration_icon1.png
    private readonly string islandSanctuaryIconPath; // Island_sanctuary_icon1.png
    private readonly string limitedIconPath; // Limited_icon1.png
    private readonly string mainScenarioIconPath; // Main_scenario_icon1.png
    private readonly string occultCrescentIconPath; // Occult_crescent_icon1.png
    private readonly string premiumIconPath; // Premium_icon1.png
    private readonly string pvpIconPath; // Pvp_icon1.png
    private readonly string questsIconPath; // Quests_icon1.png
    private readonly string raidsIconPath; // Raids_icon1.png
    private readonly string seasonalEventsIconPath; // Seasonal_events_icon1.png
    private readonly string sharedFatesIcon1Path; // Shared_fates_icon1.png
    private readonly string theHuntIconPath; // The_hunt_icon1.png
    private readonly string treasureHuntIconPath; // Treasure_hunt_icon1.png
    private readonly string trialsIconPath; // Trials_icon1.png
    private readonly string unknownIconPath; // Unknown_Icon.png
    private readonly string variantAndCriterionDungeonsIconPath; // Variant_and_criterion_dungeons_icon1.png
    private readonly string wondrousTailsIconPath; // Wondrous_tails_icon1.png

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
    private string typeSearchFilter = string.Empty;
    private bool typeComboJustOpened = false;

    // Dicionário temporário para edição de BGM IDs
    private Dictionary<uint, string> bgmInputs = new();

    // BGM Input
    private List<BGMInfo> bgmList = new();
    private string bgmSearchFilter = string.Empty;

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
        this.achievementCurrencyIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Achievement_currency_icon1.png");
        this.achievementsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Achievements_icon1.png");
        this.airshipVenturesIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Airship_ventures_icon1.png");
        this.alliedSocietyQuestsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Allied_society_quests_icon1.png");
        this.bozjaIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Bozja_icon1.png");
        this.campaignsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Campaigns_icon1.png");
        this.chaoticAllianceRaidIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Chaotic_alliance_raid_icon1.png");
        this.cosmicExplorationIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Cosmic_exploration_icon1.png");
        this.craftingIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Crafting_icon1.png");
        this.customDeliveriesIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Custom_deliveries_icon1.png");
        this.deepDungeonsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Deep_dungeons_icon1.png");
        this.dungeonsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Dungeons_icon1.png");
        this.eurekaIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Eureka_icon1.png");
        this.fatesIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Fates_icon1.png");
        this.fauxHollowsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Faux_hollows_icon1.png");
        this.gatheringIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Gathering_icon1.png");
        this.gilIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Gil_icon1.png");
        this.goldSaucerIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Gold_saucer_icon1.png");
        this.ishgardianRestorationIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Ishgardian_restoration_icon1.png");
        this.islandSanctuaryIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Island_sanctuary_icon1.png");
        this.limitedIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Limited_icon1.png");
        this.mainScenarioIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Main_scenario_icon1.png");
        this.occultCrescentIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Occult_crescent_icon1.png");
        this.premiumIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Premium_icon1.png");
        this.pvpIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Pvp_icon1.png");
        this.questsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Quests_icon1.png");
        this.raidsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Raids_icon1.png");
        this.seasonalEventsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Seasonal_events_icon1.png");
        this.sharedFatesIcon1Path = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Shared_fates_icon1.png");
        this.theHuntIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "The_hunt_icon1.png");
        this.treasureHuntIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Treasure_hunt_icon1.png");
        this.trialsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Trials_icon1.png");
        this.unknownIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Unknown_Icon.png");
        this.variantAndCriterionDungeonsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Variant_and_criterion_dungeons_icon1.png");
        this.wondrousTailsIconPath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "types", "Wondrous_tails_icon1.png");
                
        // Carregar database de informações da wiki
        MountSourceHelper.LoadDatabase(Plugin.PluginInterface);

        // Carregar tipos disponíveis
        LoadAvailableTypes();
        LoadBGMDatabase();
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

        // info badges
        var mogImage = Plugin.TextureProvider.GetFromFile(mogImagePath).GetWrapOrDefault();
        var boardImage = Plugin.TextureProvider.GetFromFile(boardImagePath).GetWrapOrDefault();
        var musicImage = Plugin.TextureProvider.GetFromFile(musicImagePath).GetWrapOrDefault();

        // types
        var sharedFatesIcon = Plugin.TextureProvider.GetFromFile(sharedFatesIconPath).GetWrapOrDefault();
        var achievementCurrencyIcon = Plugin.TextureProvider.GetFromFile(achievementCurrencyIconPath).GetWrapOrDefault();
        var achievementsIcon = Plugin.TextureProvider.GetFromFile(achievementsIconPath).GetWrapOrDefault();
        var airshipVenturesIcon = Plugin.TextureProvider.GetFromFile(airshipVenturesIconPath).GetWrapOrDefault();
        var alliedSocietyQuestsIcon = Plugin.TextureProvider.GetFromFile(alliedSocietyQuestsIconPath).GetWrapOrDefault();
        var bozjaIcon = Plugin.TextureProvider.GetFromFile(bozjaIconPath).GetWrapOrDefault();
        var campaignsIcon = Plugin.TextureProvider.GetFromFile(campaignsIconPath).GetWrapOrDefault();
        var chaoticAllianceRaidIcon = Plugin.TextureProvider.GetFromFile(chaoticAllianceRaidIconPath).GetWrapOrDefault();
        var cosmicExplorationIcon = Plugin.TextureProvider.GetFromFile(cosmicExplorationIconPath).GetWrapOrDefault();
        var craftingIcon = Plugin.TextureProvider.GetFromFile(craftingIconPath).GetWrapOrDefault();
        var customDeliveriesIcon = Plugin.TextureProvider.GetFromFile(customDeliveriesIconPath).GetWrapOrDefault();
        var deepDungeonsIcon = Plugin.TextureProvider.GetFromFile(deepDungeonsIconPath).GetWrapOrDefault();
        var dungeonsIcon = Plugin.TextureProvider.GetFromFile(dungeonsIconPath).GetWrapOrDefault();
        var eurekaIcon = Plugin.TextureProvider.GetFromFile(eurekaIconPath).GetWrapOrDefault();
        var fatesIcon = Plugin.TextureProvider.GetFromFile(fatesIconPath).GetWrapOrDefault();
        var fauxHollowsIcon = Plugin.TextureProvider.GetFromFile(fauxHollowsIconPath).GetWrapOrDefault();
        var gatheringIcon = Plugin.TextureProvider.GetFromFile(gatheringIconPath).GetWrapOrDefault();
        var gilIcon = Plugin.TextureProvider.GetFromFile(gilIconPath).GetWrapOrDefault();
        var goldSaucerIcon = Plugin.TextureProvider.GetFromFile(goldSaucerIconPath).GetWrapOrDefault();
        var ishgardianRestorationIcon = Plugin.TextureProvider.GetFromFile(ishgardianRestorationIconPath).GetWrapOrDefault();
        var islandSanctuaryIcon = Plugin.TextureProvider.GetFromFile(islandSanctuaryIconPath).GetWrapOrDefault();
        var limitedIcon = Plugin.TextureProvider.GetFromFile(limitedIconPath).GetWrapOrDefault();
        var mainScenarioIcon = Plugin.TextureProvider.GetFromFile(mainScenarioIconPath).GetWrapOrDefault();
        var occultCrescentIcon = Plugin.TextureProvider.GetFromFile(occultCrescentIconPath).GetWrapOrDefault();
        var premiumIcon = Plugin.TextureProvider.GetFromFile(premiumIconPath).GetWrapOrDefault();
        var pvpIcon = Plugin.TextureProvider.GetFromFile(pvpIconPath).GetWrapOrDefault();
        var questsIcon = Plugin.TextureProvider.GetFromFile(questsIconPath).GetWrapOrDefault();
        var raidsIcon = Plugin.TextureProvider.GetFromFile(raidsIconPath).GetWrapOrDefault();
        var seasonalEventsIcon = Plugin.TextureProvider.GetFromFile(seasonalEventsIconPath).GetWrapOrDefault();
        var sharedFatesIcon1 = Plugin.TextureProvider.GetFromFile(sharedFatesIcon1Path).GetWrapOrDefault();
        var theHuntIcon = Plugin.TextureProvider.GetFromFile(theHuntIconPath).GetWrapOrDefault();
        var treasureHuntIcon = Plugin.TextureProvider.GetFromFile(treasureHuntIconPath).GetWrapOrDefault();
        var trialsIcon = Plugin.TextureProvider.GetFromFile(trialsIconPath).GetWrapOrDefault();
        var unknownIcon = Plugin.TextureProvider.GetFromFile(unknownIconPath).GetWrapOrDefault();
        var variantAndCriterionDungeonsIcon = Plugin.TextureProvider.GetFromFile(variantAndCriterionDungeonsIconPath).GetWrapOrDefault();
        var wondrousTailsIcon = Plugin.TextureProvider.GetFromFile(wondrousTailsIconPath).GetWrapOrDefault();

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
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Eraser.ToIconString()}##clearmount"))
        {
            searchFilter = String.Empty;
        }
        ImGui.PopFont();
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
        ImGui.SetNextItemWidth(250);

        bool comboOpen = ImGui.BeginCombo("##filtertype", selectedTypeFilter);

        if (comboOpen)
        {
            if (!typeComboJustOpened)
            {
                typeSearchFilter = string.Empty;
                typeComboJustOpened = true;
                ImGui.SetKeyboardFocusHere(0);
            }

            ImGui.InputTextWithHint("##typesearch", "🔍 Search types...", ref typeSearchFilter, 50);

            ImGui.Separator();

            var filteredTypes = availableTypes.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(typeSearchFilter))
            {
                filteredTypes = filteredTypes.Where(t =>
                    t.Contains(typeSearchFilter, StringComparison.OrdinalIgnoreCase));
            }

            var typesToShow = filteredTypes.ToList();

            //if (!string.IsNullOrWhiteSpace(typeSearchFilter))
            //{
            //    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1f),
            //        $"Found: {typesToShow.Count} / {availableTypes.Count}");
            //    ImGui.Separator();
            //}

            foreach (var type in typesToShow)
            {
                bool isSelected = selectedTypeFilter == type;

                if (ImGui.Selectable(type, isSelected))
                {
                    selectedTypeFilter = type;
                    typeSearchFilter = string.Empty;
                    ImGui.CloseCurrentPopup();
                }

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }

            ImGui.EndCombo();
        }
        else
        {
            typeComboJustOpened = false;
        }
        
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Undo.ToIconString()}##cleartype"))
        {
            selectedTypeFilter = "All Types";
        }
        ImGui.PopFont();
        ImGui.Spacing();

        //if (ImGuiEx.IconButton(FontAwesomeIcon.Eraser))
        //{
        //    selectedTypeFilter = "All Types";
        //}

        // ═══════════════════════════════════════════════════════════════
        // LINHA 2: CHECKBOXES DE FILTRO
        // ═══════════════════════════════════════════════════════════════

        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "Legends");

        if (mogImage != null)
        {
            ImGui.Image(mogImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            var cursorPos = ImGui.GetCursorPos();
            cursorPos.Y += 15;
            ImGui.SetCursorPos(cursorPos);
            ImGui.Text(" - Mogstation Purchase");
        }

        if (boardImage != null) 
        {
            ImGui.SameLine();
            ImGui.Image(boardImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            var cursorPos = ImGui.GetCursorPos();
            cursorPos.Y += 15;
            ImGui.SetCursorPos(cursorPos);
            ImGui.Text(" - Market Board Purchasable");
        }

        if (musicImage != null)
        {
            ImGui.SameLine();
            ImGui.Image(musicImage.Handle, new Vector2(50, 50));
            ImGui.SameLine();
            var cursorPos = ImGui.GetCursorPos();
            cursorPos.Y += 15;
            ImGui.SetCursorPos(cursorPos);
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
            ImGui.TableSetupColumn("Custom BGM", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoResize, 250);
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
                    var hasUniqueMusic = !defaultBGMs.Contains(mount.RideBGM);

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

                var iconType = mountInfo?.Type switch
                {
                    "Main Scenario" => mainScenarioIcon,
                    "Quests" => questsIcon,
                    "Premium" => premiumIcon,
                    "Campaigns" => campaignsIcon,
                    "Limited" => limitedIcon,
                    "Seasonal Event" => seasonalEventsIcon,
                    "Gold Saucer" => goldSaucerIcon,
                    "Gil" => gilIcon,
                    "Treasure Hunt" => treasureHuntIcon,
                    "Dungeons" => dungeonsIcon,
                    "V&C Dungeons" => variantAndCriterionDungeonsIcon,
                    "Trials" => trialsIcon,
                    "Raids" => raidsIcon,
                    "Chaotic Alliance Raid" => chaoticAllianceRaidIcon,
                    "Achievements" => achievementsIcon,
                    "Achievement Certificates" => achievementCurrencyIcon,
                    "Deep Dungeon" => deepDungeonsIcon,
                    "FATE" => fatesIcon,
                    "Shared FATEs" => sharedFatesIcon,
                    "PvP" => pvpIcon,
                    "PvP (Ranked)" => pvpIcon,
                    "Crafting" => craftingIcon,
                    "Gathering" => gatheringIcon,
                    "The Hunt" => theHuntIcon,
                    "Occult Crescent" => occultCrescentIcon,
                    "Bozja" => bozjaIcon,
                    "Ishgardian Restoration" => ishgardianRestorationIcon,
                    "Cosmic Exploration" => cosmicExplorationIcon,
                    "Heaven-on-High" => deepDungeonsIcon,
                    "Eureka" => eurekaIcon,
                    "Eureka Orthos" => deepDungeonsIcon,
                    "Palace of the Dead" => deepDungeonsIcon,
                    "Pilgrim's Traverse" => deepDungeonsIcon,
                    "Custom Deliveries" => customDeliveriesIcon,
                    "Faux Hollows" => fauxHollowsIcon,
                    "Wondrous Tails" => wondrousTailsIcon,
                    "Allied Societies" => alliedSocietyQuestsIcon,
                    "Island Sanctuary" => islandSanctuaryIcon,
                    _ => unknownIcon
                };

                if (iconType != null)
                {
                    var cursorPos = ImGui.GetCursorPos();
                    cursorPos.Y += 5;
                    ImGui.SetCursorPos(cursorPos);

                    ImGui.Image(iconType.Handle, new Vector2(50, 50));

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

        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.Lightbulb.ToIconString());
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1),
            $"Leave 'Custom BGM' empty to use default mount music  | Musics Changed: {configuration.MountMusicOverrides.Count}");
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
        ushort currentBGM = 0;
        configuration.MountMusicOverrides.TryGetValue(mount.MountId, out currentBGM);

        var selectedBGM = bgmList.FirstOrDefault(b => b.ID == currentBGM);
        var displayText = selectedBGM != null ? $"{selectedBGM.ID} - {selectedBGM.Title}" : "Select BGM...";

        ImGui.SetNextItemWidth(-40);
        if (ImGui.BeginCombo($"##bgm_{mount.MountId}", displayText))
        {
            ImGui.InputTextWithHint("##bgmsearch", "Search BGM by Name/ID/Location...", ref bgmSearchFilter, 50);
            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
            ImGui.PopFont();
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("The musics Ingame does not have the proper name.");
                ImGui.Text("The information you see here is a community datamine effort, so it won't be 100% acurate.");
                ImGui.Text("You also won't find EVERY music.");
                ImGui.EndTooltip();
            }

            ImGui.Separator();

            var filtered = bgmList.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(bgmSearchFilter))
            {
                filtered = filtered.Where(b =>
                    b.Title.Contains(bgmSearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    b.Locations.Contains(bgmSearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    b.ID.ToString().Contains(bgmSearchFilter));
            }

            foreach (var bgm in filtered)
            {
                var displayName = bgm.Title.Length > 50
                    ? bgm.Title.Substring(0, 50) + "..."
                    : bgm.Title;

                if (ImGui.Selectable($"{bgm.ID} - {displayName}"))
                {
                    configuration.MountMusicOverrides[mount.MountId] = bgm.ID;
                    configuration.Save();
                    bgmSearchFilter = string.Empty;
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text($"Title: {bgm.Title}");
                    if (!string.IsNullOrEmpty(bgm.AltTitle))
                        ImGui.Text($"Alt Title: {bgm.AltTitle}");
                    if (!string.IsNullOrEmpty(bgm.Locations))
                        ImGui.Text($"Location: {bgm.Locations}");
                    ImGui.EndTooltip();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##remove_{mount.MountId}"))
        {
            configuration.MountMusicOverrides.Remove(mount.MountId);
            configuration.Save();
        }
        ImGui.PopFont();

        // Show selected BGM name below
        ushort displayBgmId = 0;
        bool isUserCustom = false;

        // Prioridade 1: Customização do usuário
        if (configuration.MountMusicOverrides.TryGetValue(mount.MountId, out var userBgm))
        {
            displayBgmId = userBgm;
            isUserCustom = true;
        }
        // Prioridade 2: Configuração do autor
        else if (plugin.Configuration.UseAuthorBGMCustomization && plugin.authorBGMConfig != null)
        {
            var authorOverride = plugin.authorBGMConfig.MountBgmOverrides.FirstOrDefault(x => x.MountId == mount.MountId);
            if (authorOverride != null)
            {
                displayBgmId = (ushort)authorOverride.BgmId;
            }
        }

        // Mostrar nome se houver customização
        if (displayBgmId > 0)
        {
            var bgmInfo = bgmList.FirstOrDefault(b => b.ID == displayBgmId);
            if (bgmInfo != null)
            {
                var color = isUserCustom
                    ? new Vector4(1f, 1f, 1f, 1f)      // Branco - usuário
                    : new Vector4(0.6f, 0.8f, 1f, 1f); // Azul - autor

                ImGui.TextColored(color, bgmInfo.Title);

                if (!isUserCustom)
                {
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
                    ImGui.PopFont();
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text("Default customization by author configuration.");
                        ImGui.Text("You can disable this on the settings [Use Author BGM Customization]");
                        ImGui.Text("or just override by selectiong your own music for customization.");

                        ImGui.EndTooltip();
                    }
                }
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

    private void LoadBGMDatabase()
    {
        try
        {
            var csvPath = Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                "external_data",
                "xiv_bgm_en.csv");

            if (!File.Exists(csvPath))
            {
                Plugin.Log.Warning($"BGM CSV not found at: {csvPath}");
                return;
            }

            var lines = File.ReadAllLines(csvPath).Skip(1); // Skip header

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length < 6) continue;

                if (ushort.TryParse(parts[0], out var id))
                {
                    bgmList.Add(new BGMInfo
                    {
                        ID = id,
                        Title = parts[1],
                        AltTitle = parts[2],
                        SpecialModeTitle = parts[3],
                        Locations = parts[4],
                        Comments = parts[5]
                    });
                }
            }

            Plugin.Log.Information($"Loaded {bgmList.Count} BGM entries");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error($"Error loading BGM database: {ex.Message}");
        }
    }
}

public class BGMInfo
{
    public ushort ID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AltTitle { get; set; } = string.Empty;
    public string SpecialModeTitle { get; set; } = string.Empty;
    public string Locations { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
}
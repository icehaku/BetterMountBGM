using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using BetterMountBGM.Windows;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;


namespace BetterMountBGM;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;


    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("BetterMountBGM");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private uint lastMountId = 0;
    public BGMConfiguration? authorBGMConfig = null;

    public Plugin()
    {
        Framework.Update += OnFrameworkUpdate;
        Log.Information($"Better Mount BGM Plugin Loaded!");

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;
        PluginInterface.UiBuilder.OpenMainUi +=  ToggleConfigUi;

        LoadAuthorBGMConfig();

        Log.Information($"Plugin loaded!");
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();
        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        Framework.Update -= OnFrameworkUpdate;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        try
        {
            // Obtém mount atual
            var currentMountId = GetCurrentMountId();

            // Detecta mudança de estado
            if (currentMountId != lastMountId)
            {
                if (currentMountId > 0 && lastMountId == 0)
                {
                    // Acabou de montar
                    var mountInfo = MountHelper.GetMountInfo(DataManager, currentMountId);
                    if(mountInfo != null)
                    Log.Information($"Player mounted: {mountInfo.Name} (Mount ID: {mountInfo.MountId}, RideBGM ID: {mountInfo.RideBGM})");
                    OnPlayerMounted(currentMountId);
                }
                else if (currentMountId == 0 && lastMountId > 0)
                {
                    // Acabou de desmontar
                    OnPlayerDismounted();
                }

                lastMountId = currentMountId;
            }
        }
        catch (Exception ex)
        {
            // Silencia erros para não spammar log
            Log.Information($"Exception: Mount ID {ex}");
        }
    }

    private unsafe uint GetCurrentMountId()
    {
        try
        {
            var player = ObjectTable.LocalPlayer;
            if (player == null) return 0;

            var character = (FFXIVClientStructs.FFXIV.Client.Game.Character.Character*)player.Address;
            if (character == null) return 0;

            return character->Mount.MountId;
        }
        catch
        {
            return 0;
        }
    }

    private void OnPlayerMounted(uint mountId)
    {
        Log.Information($"UseAuthorBGMCustomization: {Configuration.UseAuthorBGMCustomization}");
        Log.Information($"AuthorConfig loaded: {authorBGMConfig != null}");
        Log.Information($"AuthorConfig count: {authorBGMConfig?.MountBgmOverrides.Count ?? 0}");

        // Prioridade 1: Customização do usuário
        if (Configuration.MountMusicOverrides.TryGetValue(mountId, out var customBgmId))
        {
            Log.Information($"Applying user custom BGM {customBgmId}");
            ApplyBGM(customBgmId);
            return;
        }

        // Prioridade 2: Configuração do autor (se habilitada)
        if (Configuration.UseAuthorBGMCustomization && authorBGMConfig != null)
        {
            var authorOverride = authorBGMConfig?.MountBgmOverrides.FirstOrDefault(x => x.MountId == mountId);
            if (authorOverride != null)
            {
                Log.Information($"Applying author BGM {authorOverride.BgmId}");
                ApplyBGM(authorOverride.BgmId);
                return;
            }
        }

        Log.Information($"Using default BGM for mount {mountId}");
    }

    private void OnPlayerDismounted()
    {
        Log.Information("Player dismounted");

        // Opcional: Restaurar música ambiente
        try
        {
            unsafe
            {
                BGMSystem.Instance()->ResetBGM(0);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error resetting BGM: {ex.Message}");
        }
    }

    private void ApplyBGM(ushort bgmId)
    {
        System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
        {
            Framework.RunOnFrameworkThread(() =>
            {
                try
                {
                    unsafe { BGMSystem.SetBGM(bgmId, 0); }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error setting BGM: {ex.Message}");
                }
            });
        });
    }

    private void LoadAuthorBGMConfig()
    {
        try
        {
            var jsonPath = Path.Combine(
                PluginInterface.AssemblyLocation.Directory?.FullName!,
                "external_data",
                "author_bgm_config.json");

            if (!File.Exists(jsonPath))
            {
                Log.Warning("Author BGM config not found");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            authorBGMConfig = JsonSerializer.Deserialize<BGMConfiguration>(json);
            Log.Information($"Loaded author BGM config: {authorBGMConfig?.MountBgmOverrides.Count ?? 0} mounts");
        }
        catch (Exception ex)
        {
            Log.Error($"Error loading author BGM config: {ex.Message}");
        }
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}

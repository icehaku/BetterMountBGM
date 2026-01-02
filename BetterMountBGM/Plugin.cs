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

    public Plugin()
    {
        Framework.Update += OnFrameworkUpdate;
        Log.Information($"Plugin loaded with Mount Music customization!");

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

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
        Log.Information($"Player mounted: Mount ID {mountId}");

        // Verifica se tem override customizado
        if (Configuration.MountMusicOverrides.TryGetValue(mountId, out var customBgmId))
        {
            Log.Information($"Applying custom BGM {customBgmId} to mount {mountId}");

            // Pequeno delay para garantir que mount está estável
            System.Threading.Tasks.Task.Delay(150).ContinueWith(_ =>
            {
                Framework.RunOnFrameworkThread(() =>
                {
                    try
                    {
                        unsafe
                        {
                            BGMSystem.SetBGM(customBgmId, 0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error setting custom BGM: {ex.Message}");
                    }
                });
            });
        }
        else
        {
            Log.Information($"Using default BGM for mount {mountId}");
        }
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

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}

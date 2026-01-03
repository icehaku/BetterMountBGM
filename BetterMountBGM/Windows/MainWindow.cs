using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using System;
using System.IO;
using System.Numerics;

namespace BetterMountBGM.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly string aboutImagePath;
    private readonly Plugin plugin;

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin)
        : base("My Amazing Window##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(920, 520),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.aboutImagePath = Path.Combine(Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!, "images", "haku.jpg"); ;
        this.plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("Show Settings"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        ImGui.Text("About me!");
        var aboutImage = Plugin.TextureProvider.GetFromFile(aboutImagePath).GetWrapOrDefault();
        if (aboutImage != null)
        {
            ImGui.Image(aboutImage.Handle, new Vector2(900, 500));
        }

    }
}

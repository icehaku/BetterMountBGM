using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
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
        : base("Settings##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
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
        //ImGui.Text("0.0");
        var useAuthor = plugin.Configuration.UseAuthorBGMCustomization;
        ImGui.Checkbox("Use Author BGM Customization", ref useAuthor);
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        ImGui.Text(FontAwesomeIcon.InfoCircle.ToIconString());
        ImGui.PopFont();
        if (ImGui.IsItemHovered())
        {
            ImGui.SameLine();
            ImGui.BeginTooltip();
            ImGui.Text("This will set every mount that dosn't have a unique bgm, to a different one choosed by the plugin author!");
            ImGui.Text("If you configure any customization it will always take priorite over this.");
            ImGui.EndTooltip();
        }

        if (ImGui.Button("BGM Customization Menu"))
        {
            plugin.ToggleConfigUi();
        }

        ImGui.Spacing();

        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        ImGui.TextColored(new Vector4(0.3f, 0.8f, 1f, 1f), "About the plugin author:");
        ImGui.Text("Hello, I'm Ice!");
        ImGui.Text("I'm not really a C# dev, I only worked with WebDev(ruby/rails+JS) my entire life, so this here is just an adventure for me.");
        ImGui.Text("Sorry for any bug, feel free to report it or ask for anything on the github issues of the plugin project.");
        var aboutImage = Plugin.TextureProvider.GetFromFile(aboutImagePath).GetWrapOrDefault();
        if (aboutImage != null)
        {
            ImGui.Image(aboutImage.Handle, new Vector2(900, 500));
        }

    }
}

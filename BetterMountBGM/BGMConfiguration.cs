using System.Text.Json.Serialization;
using System.Collections.Generic;

public class BGMConfiguration
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;
    
    [JsonPropertyName("mount_bgm_overrides")]
    public List<MountBGMOverride> MountBgmOverrides { get; set; } = new();
}
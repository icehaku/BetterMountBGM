using System.Text.Json.Serialization;

public class MountBGMOverride
{
    [JsonPropertyName("mount_id")]
    public uint MountId { get; set; }

    [JsonPropertyName("mount_name")]
    public string MountName { get; set; } = string.Empty;

    [JsonPropertyName("bgm_id")]
    public ushort BgmId { get; set; }

    [JsonPropertyName("bgm_name")]
    public string BgmName { get; set; } = string.Empty;

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}


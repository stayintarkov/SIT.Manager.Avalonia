using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class BossLocationSpawn
{
    [JsonIgnore]
    public int Name { get; set; }
    public int BossChance { get; set; }
    public string? BossDifficult { get; set; }
    public string? BossEscortAmount { get; set; }
    public string? BossEscortDifficult { get; set; }
    public string? BossEscortType { get; set; }
    public string? BossName { get; set; }
    public bool BossPlayer { get; set; }
    public string? BossZone { get; set; }
    public int Delay { get; set; }
    public bool ForceSpawn { get; set; }
    public bool IgnoreMaxBots { get; set; }
    public bool RandomTimeSpawn { get; set; }
    public List<Support>? Supports { get; set; }
    public int Time { get; set; }
    public string? TriggerId { get; set; }
    public string? TriggerName { get; set; }
}

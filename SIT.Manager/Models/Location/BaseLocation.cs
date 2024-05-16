using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace SIT.Manager.Models.Location;

public class BaseLocation
{
    public List<object>? AccessKeys { get; set; }
    public List<AirdropParameter>? AirdropParameters { get; set; }
    public float Area { get; set; }
    public int AveragePlayTime { get; set; }
    public int AveragePlayerLevel { get; set; }
    public List<Banner> Banners { get; set; } = [];
    public ObservableCollection<BossLocationSpawn>? BossLocationSpawn { get; set; }
    public int BotAssault { get; set; }
    public int BotEasy { get; set; }
    public int BotHard { get; set; }
    public int BotImpossible { get; set; }
    public BotLocationModifier? BotLocationModifier { get; set; }
    public int BotMarksman { get; set; }
    public int BotMax { get; set; }
    public int BotMaxPlayer { get; set; }
    public int BotMaxTimePlayer { get; set; }
    public int BotNormal { get; set; }
    public int BotSpawnCountStep { get; set; }
    public int BotSpawnPeriodCheck { get; set; }
    public int BotSpawnTimeOffMax { get; set; }
    public int BotSpawnTimeOffMin { get; set; }
    public int BotSpawnTimeOnMax { get; set; }
    public int BotSpawnTimeOnMin { get; set; }
    public int BotStart { get; set; }
    public int BotStartPlayer { get; set; }
    public int BotStop { get; set; }
    public string? Description { get; set; }
    public bool DisabledForScav { get; set; }
    public string? DisabledScavExits { get; set; }
    public bool EnableCoop { get; set; }
    public bool Enabled { get; set; }
    public int EscapeTimeLimit { get; set; }
    public int EscapeTimeLimitCoop { get; set; }
    public bool GenerateLocalLootCache { get; set; }
    public int GlobalContainerChanceModifier { get; set; }
    public double GlobalLootChanceModifier { get; set; }
    public int IconX { get; set; }
    public int IconY { get; set; }
    [JsonPropertyName("Id")]
    public string? MapId { get; set; }
    public bool Insurance { get; set; }
    public bool IsSecret { get; set; }
    public bool Locked { get; set; }
    public List<object>? Loot { get; set; }
    public List<MatchMakerMinPlayersByWaitTime>? MatchMakerMinPlayersByWaitTime { get; set; }
    public int MaxBotPerZone { get; set; }
    public int MaxCoopGroup { get; set; }
    public int MaxDistToFreePoint { get; set; }
    public int MaxPlayers { get; set; }
    public int MinDistToExitPoint { get; set; }
    public int MinDistToFreePoint { get; set; }
    public List<MinMaxBot>? MinMaxBots { get; set; }
    public int MinPlayerLvlAccessKeys { get; set; }
    public int MinPlayers { get; set; }
    public string? Name { get; set; }
    public bool NewSpawn { get; set; }
    public NonWaveGroupScenario? NonWaveGroupScenario { get; set; }
    public bool OcculsionCullingEnabled { get; set; }
    public bool OfflineNewSpawn { get; set; }
    public bool OfflineOldSpawn { get; set; }
    public bool OldSpawn { get; set; }
    public string? OpenZones { get; set; }
    public int PlayersRequestCount { get; set; }
    public int PmcMaxPlayersInGroup { get; set; }
    public Bundle? Preview { get; set; }
    public int RequiredPlayerLevelMax { get; set; }
    public int RequiredPlayerLevelMin { get; set; }
    public string? Rules { get; set; }
    public bool SafeLocation { get; set; }
    public int ScavMaxPlayersInGroup { get; set; }
    public Bundle? Scene { get; set; }
    public List<SpawnPointParam>? SpawnPointParams { get; set; }
    public int UnixDateTime { get; set; }
    [JsonPropertyName("_Id")]
    public string? Id { get; set; }
    [JsonPropertyName("doors")]
    public List<object>? Doors { get; set; }
    [JsonPropertyName("exit_access_time")]
    public int ExitAccessTime { get; set; }
    [JsonPropertyName("exit_count")]
    public int ExitCount { get; set; }
    [JsonPropertyName("exit_time")]
    public int ExitTime { get; set; }
    [JsonPropertyName("exits")]
    public List<Exit>? Exits { get; set; }
    [JsonPropertyName("filter_ex")]
    public List<object>? FilterEx { get; set; }
    [JsonPropertyName("limits")]
    public List<object>? Limits { get; set; }
    [JsonPropertyName("matching_min_seconds")]
    public int MatchingMinSeconds { get; set; }
    [JsonPropertyName("maxItemCountInLocation")]
    public List<MaxItemCountInLocation>? MaxItemCountInLocation { get; set; }
    [JsonPropertyName("sav_summon_seconds")]
    public int SavSummonSeconds { get; set; }
    [JsonPropertyName("tmp_location_field_remove_me")]
    public int TmpLocationFieldRemoveMe { get; set; }
    [JsonPropertyName("users_gather_seconds")]
    public int UsersGatherSeconds { get; set; }
    [JsonPropertyName("users_spawn_seconds_n")]
    public int UsersSpawnSecondsN { get; set; }
    [JsonPropertyName("users_spawn_seconds_n2")]
    public int UsersSpawnSecondsN2 { get; set; }
    [JsonPropertyName("users_summon_seconds")]
    public int UsersSummonSeconds { get; set; }
    [JsonPropertyName("waves")]
    public ObservableCollection<Wave>? Waves { get; set; }
}

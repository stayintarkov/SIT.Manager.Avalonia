using System.Collections.Generic;

namespace SIT.Manager.Models.Location;

public class SpawnPointParam
{
    public string? BotZoneName { get; set; }
    public List<string>? Categories { get; set; }
    public ColliderParams? ColliderParams { get; set; }
    public int CorePointId { get; set; }
    public int DelayToCanSpawnSec { get; set; }
    public string? Id { get; set; }
    public string? Infiltration { get; set; }
    public Position? Position { get; set; }
    public double Rotation { get; set; }
    public List<string>? Sides { get; set; }
}

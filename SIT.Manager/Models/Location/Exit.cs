namespace SIT.Manager.Models.Location;

public class Exit
{
    public int Chance { get; set; }
    public int Count { get; set; }
    public string? EntryPoints { get; set; }
    public bool EventAvailable { get; set; }
    public int ExfiltrationTime { get; set; }
    public string? ExfiltrationType { get; set; }
    public string? Id { get; set; }
    public int MaxTime { get; set; }
    public int MinTime { get; set; }
    public string? Name { get; set; }
    public string? PassageRequirement { get; set; }
    public int PlayersCount { get; set; }
    public string? RequirementTip { get; set; }
}

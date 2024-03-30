using System;

namespace SIT.Manager.Classes;

[Serializable]
public struct TarkovLaunchConfig
{
    private const string GAME_VERSION = "live";
    public string BackendUrl { get; init; }
    public readonly string Version => GAME_VERSION;
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SIT.Manager.Avalonia.Classes
{
    [Serializable]
    public struct TarkovLaunchConfig
    {
        private const string GAME_VERSION = "live";
        public string BackendUrl { get; init; }
        public readonly string Version => GAME_VERSION;
    }
}

using System;

namespace SIT.Manager.Services.Caching;

public class EvictedEventArgs(string key) : EventArgs
{
    public string Key { get; private set; } = key;
}

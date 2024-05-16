using Avalonia.Reactive;
using System;

namespace SIT.Manager.Theme.Assists;

public static class Observable
{
    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> action)
    {
        return source.Subscribe(new AnonymousObserver<T>(action));
    }
}

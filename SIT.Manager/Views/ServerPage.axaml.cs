using Avalonia;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using SIT.Manager.Theme.Controls;
using SIT.Manager.ViewModels;
using System;

namespace SIT.Manager.Views;

public partial class ServerPage : ActivatableUserControl
{
    private const string ConsoleScrollerName = "ConsoleLogScroller";
    private readonly ScrollViewer _consoleLogScroller;
    private bool _autoScroll = true;

    public ServerPage()
    {
        DataContext = App.Current.Services.GetService<ServerPageViewModel>();

        InitializeComponent();
        ScrollViewer? scrollViewer = this.FindControl<ScrollViewer>(ConsoleScrollerName);
        _consoleLogScroller = scrollViewer ??
                              throw new Exception(
                                  $"Could not find a {nameof(ScrollViewer)} control with name {ConsoleScrollerName}");
    }

    private void ConsoleLogScroller_ScrollChanged(object? _, ScrollChangedEventArgs e)
    {
        // User scroll event : set or unset auto-scroll mode
        if (e.ExtentDelta == Vector.Zero)
        {
            _autoScroll = Math.Abs(_consoleLogScroller.Offset.Y - _consoleLogScroller.ScrollBarMaximum.Y) < 0.01f;
        }
        else
        {
            if (!_autoScroll) return;
            _consoleLogScroller?.ScrollToEnd();
        }
    }
}

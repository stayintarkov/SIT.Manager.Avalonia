using Avalonia;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using SIT.Manager.Avalonia.ViewModels;

namespace SIT.Manager.Avalonia.Views
{
    public partial class ServerPage : ReactiveUserControl<ServerPageViewModel>
    {
        private bool _autoScroll = true;
        private readonly ScrollViewer? _consoleLogScroller;

        public ServerPage()
        {
            DataContext = App.Current.Services.GetService<ServerPageViewModel>();

            InitializeComponent();
            _consoleLogScroller = this.FindControl<ScrollViewer>("ConsoleLogScroller");

            // ServerPageViewModel's WhenActivated block will also get called.
            this.WhenActivated(disposables => { /* Handle view activation etc. */ });
        }

        private void ConsoleLogScroller_ScrollChanged(object? sender, ScrollChangedEventArgs e) {
            // User scroll event : set or unset auto-scroll mode
            if (e.ExtentDelta == Vector.Zero) {
                // Content unchanged : user scroll event
                if (_consoleLogScroller?.Offset.Y == _consoleLogScroller?.ScrollBarMaximum.Y) {
                    // Scroll bar is in bottom
                    // Set auto-scroll mode
                    _autoScroll = true;
                }
                else {
                    // Scroll bar isn't in bottom
                    // Unset auto-scroll mode
                    _autoScroll = false;
                }
            }

            // Content scroll event : auto-scroll eventually
            if (_autoScroll && e.ExtentDelta != Vector.Zero) {
                // Content changed and auto-scroll mode set
                // Autoscroll
                _consoleLogScroller?.ScrollToEnd();
            }
        }
    }
}

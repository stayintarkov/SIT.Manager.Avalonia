using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SIT.Manager.Controls;

public class ActivatableUserControl : UserControl
{
    public ObservableRecipient? ViewModel => DataContext is ObservableRecipient dc ? dc : default;

    public ActivatableUserControl()
    {
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (ViewModel != null)
        {
            ViewModel.IsActive = true;
            OnActivated();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (ViewModel != null)
        {
            ViewModel.IsActive = false;
            OnDeactivated();
        }
    }

    protected virtual void OnActivated() { }

    protected virtual void OnDeactivated() { }
}

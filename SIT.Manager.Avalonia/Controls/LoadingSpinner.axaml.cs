using Avalonia;
using Avalonia.Controls;

namespace SIT.Manager.Avalonia.Controls
{
    public partial class LoadingSpinner : UserControl
    {
        public static new readonly StyledProperty<int> HeightProperty =
            AvaloniaProperty.Register<LoadingSpinner, int>(nameof(Height), defaultValue: 150);

        public new int Height {
            get { return GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        public static new readonly StyledProperty<int> WidthProperty =
            AvaloniaProperty.Register<LoadingSpinner, int>(nameof(Width), defaultValue: 150);

        public new int Width {
            get { return GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public static readonly StyledProperty<int> StrokeWidthProperty =
            AvaloniaProperty.Register<LoadingSpinner, int>(nameof(StrokeWidth), defaultValue: 16);

        public int StrokeWidth {
            get { return GetValue(StrokeWidthProperty); }
            set { SetValue(StrokeWidthProperty, value); }
        }

        public LoadingSpinner() {
            InitializeComponent();
        }
    }
}

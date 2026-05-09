using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NodeHiveCenter.Controls;

public partial class NetworkDiagramControl : UserControl
{
    // ── Dependency Property ──────────────────────────────────────────────────
    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(NetworkDiagramControl),
            new PropertyMetadata(false, OnIsActiveChanged));

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NetworkDiagramControl ctrl)
            ctrl.UpdateAnimations((bool)e.NewValue);
    }

    // ── Fields ───────────────────────────────────────────────────────────────
    private Storyboard? _marchA;
    private Storyboard? _marchB;
    private Storyboard? _glowA;
    private Storyboard? _glowB;
    private Storyboard? _glowCenter;

    private static readonly Color AccentColor = Color.FromRgb(0, 245, 196);
    private static readonly Color DimColor    = Color.FromRgb(30, 58, 49);

    public NetworkDiagramControl()
    {
        InitializeComponent();
        Loaded += (_, _) => CacheStoryboards();
    }

    private void CacheStoryboards()
    {
        _marchA     = (Storyboard)Resources["MarchLineA"];
        _marchB     = (Storyboard)Resources["MarchLineB"];
        _glowA      = (Storyboard)Resources["GlowA"];
        _glowB      = (Storyboard)Resources["GlowB"];
        _glowCenter = (Storyboard)Resources["GlowCenter"];
    }

    private void UpdateAnimations(bool active)
    {
        if (active)
        {
            // Set line colors to accent
            SetLineColor(LineA, AccentColor);
            SetLineColor(LineB, AccentColor);

            // Start marching ants
            _marchA?.Begin(DiagramCanvas, true);
            _marchB?.Begin(DiagramCanvas, true);

            // Start glow pulses
            _glowA?.Begin(DiagramCanvas, true);
            _glowB?.Begin(DiagramCanvas, true);
            _glowCenter?.Begin(DiagramCanvas, true);
        }
        else
        {
            // Stop all animations
            _marchA?.Stop(DiagramCanvas);
            _marchB?.Stop(DiagramCanvas);
            _glowA?.Stop(DiagramCanvas);
            _glowB?.Stop(DiagramCanvas);
            _glowCenter?.Stop(DiagramCanvas);

            // Dim the lines
            SetLineColor(LineA, DimColor);
            SetLineColor(LineB, DimColor);
        }
    }

    private static void SetLineColor(System.Windows.Shapes.Path path, Color color)
        => path.Stroke = new SolidColorBrush(color);
}

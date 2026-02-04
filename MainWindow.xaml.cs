using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;
using RTS_Tactical_Overlay.Native;
using RTS_Tactical_Overlay.ViewModels;

namespace RTS_Tactical_Overlay;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        // Ensure window doesn't steal focus
        Loaded += MainWindow_Loaded;
        KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Position window at top of screen
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;

        // Set overlay bar width to 80% of screen width (responsive design)
        double screenWidth = SystemParameters.PrimaryScreenWidth;
        double targetWidth = screenWidth * 0.8;

        // Clamp between MinWidth (800) and MaxWidth (1200)
        targetWidth = Math.Max(800, Math.Min(1200, targetWidth));
        OverlayBar.Width = targetWidth;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Apply WS_EX_NOACTIVATE to prevent focus stealing
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = Win32Methods.GetWindowLong(hwnd, Win32Constants.GWL_EXSTYLE);

        // Add WS_EX_NOACTIVATE and WS_EX_TOOLWINDOW
        Win32Methods.SetWindowLong(hwnd, Win32Constants.GWL_EXSTYLE,
            extendedStyle | Win32Constants.WS_EX_NOACTIVATE | Win32Constants.WS_EX_TOOLWINDOW);
    }

    protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        // Prevent window from taking focus when clicked
        e.Handled = true;
        base.OnMouseDown(e);
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // F5: Load sample profile
        if (e.Key == Key.F5)
        {
            LoadSampleProfile();
            e.Handled = true;
        }
    }

    private void LoadSampleProfile()
    {
        var samplePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sample-profile.json");
        if (File.Exists(samplePath))
        {
            _viewModel.LoadProfileCommand.Execute(samplePath);
        }
    }

    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Grid grid) return;
        if (DataContext is not MainViewModel viewModel) return;

        // Get node index from Tag
        if (grid.Tag is not int nodeIndex) return;

        // Capture mouse to receive events even when cursor leaves the element
        grid.CaptureMouse();

        // Get mouse position relative to the canvas
        var canvas = FindParentCanvas(grid);
        if (canvas == null) return;

        Point mousePos = e.GetPosition(canvas);
        viewModel.StartNodeDrag(nodeIndex, mousePos.X);

        e.Handled = true;
    }

    private void Node_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not Grid grid) return;
        if (DataContext is not MainViewModel viewModel) return;
        if (!viewModel.IsDraggingNode) return;

        // Get mouse position relative to the canvas
        var canvas = FindParentCanvas(grid);
        if (canvas == null) return;

        Point mousePos = e.GetPosition(canvas);
        viewModel.UpdateNodeDrag(mousePos.X);

        e.Handled = true;
    }

    private void Node_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Grid grid) return;
        if (DataContext is not MainViewModel viewModel) return;

        grid.ReleaseMouseCapture();
        viewModel.EndNodeDrag();

        e.Handled = true;
    }

    private Canvas? FindParentCanvas(DependencyObject child)
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is Canvas canvas)
                return canvas;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }
}

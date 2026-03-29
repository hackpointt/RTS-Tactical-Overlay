# Edge Glow Breathing Effect Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add breathing glow effect on left/right screen edges during Stage execution, with user-configurable speed, intensity, and width.

**Architecture:** Create two independent transparent SideGlowWindow instances managed by EdgeGlowService. The service responds to Stage changes from MainViewModel, controlling animation start/stop/pause/resume with color synced to current node.

**Tech Stack:** WPF .NET 8, CommunityToolkit.Mvvm, Microsoft.Extensions.DependencyInjection

---

## File Structure

### New Files
| File | Responsibility |
|------|---------------|
| `Models/EdgeGlowSettings.cs` | Configuration model (breath speed, intensity, width, enabled) |
| `Services/IEdgeGlowService.cs` | Service interface for DI |
| `Services/EdgeGlowService.cs` | Manages SideGlowWindow instances, animation control |
| `Views/SideGlowWindow.xaml` | Transparent edge glow window UI |
| `Views/SideGlowWindow.xaml.cs` | Window logic, animation, WS_EX_NOACTIVATE |

### Modified Files
| File | Changes |
|------|---------|
| `Models/TacticalProfile.cs` | Add EdgeGlowSettings property |
| `App.xaml.cs` | Register IEdgeGlowService in DI container |
| `ViewModels/MainViewModel.cs` | Inject IEdgeGlowService, call on Stage events |

---

### Task 1: Create EdgeGlowSettings Model

**Files:**
- Create: `Models/EdgeGlowSettings.cs`

**Explanation:** This model stores user configuration for the edge glow effect. It will be embedded in TacticalProfile and saved to JSON. Default values provide a balanced experience.

- [ ] **Step 1: Create EdgeGlowSettings.cs**

```csharp
using System.Text.Json.Serialization;

namespace RTS_Tactical_Overlay.Models;

/// <summary>
/// Configuration for the edge glow breathing effect
/// </summary>
public class EdgeGlowSettings
{
    /// <summary>
    /// Breathing cycle duration in seconds (0.5 - 5.0)
    /// Lower = faster breathing, higher = slower
    /// </summary>
    public double BreathSpeedSeconds { get; set; } = 2.0;

    /// <summary>
    /// Maximum brightness/opacity (0.1 - 1.0)
    /// Controls how bright the glow can get at peak
    /// </summary>
    public double MaxIntensity { get; set; } = 0.6;

    /// <summary>
    /// Width of the glow band in pixels (10 - 100)
    /// Thicker = more visible, thinner = subtle
    /// </summary>
    public int GlowWidthPixels { get; set; } = 30;

    /// <summary>
    /// Whether the edge glow feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Creates default settings
    /// </summary>
    [JsonIgnore]
    public static EdgeGlowSettings Default => new EdgeGlowSettings();

    /// <summary>
    /// Clamps all values to valid ranges
    /// Called when loading from JSON to ensure safety
    /// </summary>
    public void ClampValues()
    {
        BreathSpeedSeconds = Math.Clamp(BreathSpeedSeconds, 0.5, 5.0);
        MaxIntensity = Math.Clamp(MaxIntensity, 0.1, 1.0);
        GlowWidthPixels = Math.Clamp(GlowWidthPixels, 10, 100);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Models/EdgeGlowSettings.cs
git commit -m "feat: add EdgeGlowSettings configuration model

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 2: Update TacticalProfile with EdgeGlowSettings

**Files:**
- Modify: `Models/TacticalProfile.cs`

**Explanation:** We embed EdgeGlowSettings into the existing profile model so settings are saved per-profile. Users can have different glow preferences for different tactical configurations.

- [ ] **Step 1: Add EdgeGlowSettings property to TacticalProfile**

Add this property after the `Loop` property (around line 40):

```csharp
    /// <summary>
    /// Edge glow breathing effect settings
    /// Null means use default settings
    /// </summary>
    public EdgeGlowSettings? EdgeGlowSettings { get; set; }
```

- [ ] **Step 2: Update IsValid method to clamp EdgeGlowSettings**

Replace the `IsValid()` method with this version that clamps settings:

```csharp
    /// <summary>
    /// Validates that the profile is valid
    /// Also clamps EdgeGlowSettings to safe values
    /// </summary>
    public bool IsValid()
    {
        // Clamp edge glow settings if present
        if (EdgeGlowSettings != null)
        {
            EdgeGlowSettings.ClampValues();
        }

        return !string.IsNullOrWhiteSpace(Name) &&
               Stages.Count > 0 &&
               Stages.All(s => s.IsValid());
    }
```

- [ ] **Step 3: Commit**

```bash
git add Models/TacticalProfile.cs
git commit -m "feat: add EdgeGlowSettings property to TacticalProfile

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 3: Create IEdgeGlowService Interface

**Files:**
- Create: `Services/IEdgeGlowService.cs`

**Explanation:** Interface for dependency injection. Defines the contract for controlling edge glow animation. MainViewModel will call these methods on Stage events.

- [ ] **Step 1: Create IEdgeGlowService.cs**

```csharp
using System.Windows.Media;
using RTS_Tactical_Overlay.Models;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service interface for managing edge glow breathing effect
/// </summary>
public interface IEdgeGlowService
{
    /// <summary>
    /// Starts the breathing glow with specified color and settings
    /// Called when execution starts or Stage changes
    /// </summary>
    /// <param name="color">Glow color (from current node)</param>
    /// <param name="settings">User configuration for the effect</param>
    void StartGlow(Color color, EdgeGlowSettings settings);

    /// <summary>
    /// Updates the glow color (instant, no animation restart)
    /// Called when switching to a new Stage
    /// </summary>
    /// <param name="color">New glow color</param>
    void UpdateColor(Color color);

    /// <summary>
    /// Pauses the breathing animation at current brightness
    /// Called when execution is paused
    /// </summary>
    void PauseGlow();

    /// <summary>
    /// Resumes the breathing animation from current brightness
    /// Called when execution resumes
    /// </summary>
    void ResumeGlow();

    /// <summary>
    /// Stops the glow and fades out
    /// Called when execution stops or completes
    /// </summary>
    void StopGlow();

    /// <summary>
    /// Initializes the glow windows (called on startup)
    /// </summary>
    void Initialize();

    /// <summary>
    /// Cleans up resources (called on exit)
    /// </summary>
    void Dispose();
}
```

- [ ] **Step 2: Commit**

```bash
git add Services/IEdgeGlowService.cs
git commit -m "feat: add IEdgeGlowService interface

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 4: Create SideGlowWindow XAML

**Files:**
- Create: `Views/SideGlowWindow.xaml`

**Explanation:** This XAML defines the transparent window that renders the edge glow. LinearGradientBrush creates the fade effect from edge toward center. The glow border animates opacity for breathing effect.

- [ ] **Step 1: Create SideGlowWindow.xaml**

```xml
<Window x:Class="RTS_Tactical_Overlay.Views.SideGlowWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Edge Glow"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ResizeMode="NoResize"
        WindowStartupLocation="Manual">

    <Grid x:Name="RootGrid">
        <!-- Gradient glow band -->
        <!-- For left edge: gradient goes from color (left) to transparent (right) -->
        <!-- For right edge: gradient goes from transparent (left) to color (right) -->
        <Border x:Name="GlowBorder">
            <Border.Background>
                <LinearGradientBrush x:Name="GlowGradient" StartPoint="0,0" EndPoint="1,0">
                    <GradientStop x:Name="ColorStop" Offset="0" Color="Transparent"/>
                    <GradientStop x:Name="TransparentStop" Offset="1" Color="Transparent"/>
                </LinearGradientBrush>
            </Border.Background>
        </Border>
    </Grid>
</Window>
```

- [ ] **Step 2: Commit**

```bash
git add Views/SideGlowWindow.xaml
git commit -m "feat: add SideGlowWindow XAML with gradient glow

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 5: Create SideGlowWindow Code-Behind

**Files:**
- Create: `Views/SideGlowWindow.xaml.cs`

**Explanation:** This code-beind handles:
1. WS_EX_NOACTIVATE to prevent focus stealing (same as MainWindow)
2. Positioning: left edge at x=0, right edge at x=ScreenWidth-Width
3. Gradient direction reversal for right edge
4. Breathing animation via DoubleAnimation on opacity
5. Pause/resume support via holding current opacity

- [ ] **Step 1: Create SideGlowWindow.xaml.cs**

```csharp
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using RTS_Tactical_Overlay.Native;

namespace RTS_Tactical_Overlay.Views;

/// <summary>
/// Transparent window for edge glow breathing effect
/// Two instances: one for left edge, one for right edge
/// </summary>
public partial class SideGlowWindow : Window
{
    private readonly bool _isLeftEdge;
    private Color _currentGlowColor;
    private double _maxIntensity;
    private double _breathSpeedSeconds;
    private Storyboard? _breathingStoryboard;
    private DoubleAnimation? _breathingAnimation;
    private bool _isAnimating;

    /// <summary>
    /// Creates a SideGlowWindow for specified edge
    /// </summary>
    /// <param name="isLeftEdge">true for left edge, false for right edge</param>
    public SideGlowWindow(bool isLeftEdge)
    {
        InitializeComponent();
        _isLeftEdge = isLeftEdge;

        // Set initial position based on which edge
        PositionWindow();

        // Setup gradient direction based on edge
        SetupGradientDirection();
    }

    /// <summary>
    /// Positions the window at the correct edge
    /// </summary>
    private void PositionWindow()
    {
        Top = 0;
        Height = SystemParameters.PrimaryScreenHeight;

        if (_isLeftEdge)
        {
            Left = 0;
        }
        else
        {
            // Right edge: position at screen width minus window width
            // Width will be set later via SetWidth()
            Left = SystemParameters.PrimaryScreenWidth - Width;
        }
    }

    /// <summary>
    /// Sets the gradient direction based on which edge
    /// Left edge: color at left (Offset 0), transparent at right (Offset 1)
    /// Right edge: transparent at left (Offset 0), color at right (Offset 1)
    /// </summary>
    private void SetupGradientDirection()
    {
        if (_isLeftEdge)
        {
            // Left edge: glow fades from left to right
            ColorStop.Offset = 0;
            TransparentStop.Offset = 1;
        }
        else
        {
            // Right edge: glow fades from right to left
            ColorStop.Offset = 1;
            TransparentStop.Offset = 0;
        }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Apply WS_EX_NOACTIVATE to prevent focus stealing
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = Win32Methods.GetWindowLong(hwnd, Win32Constants.GWL_EXSTYLE);
        Win32Methods.SetWindowLong(hwnd, Win32Constants.GWL_EXSTYLE,
            extendedStyle | Win32Constants.WS_EX_NOACTIVATE | Win32Constants.WS_EX_TOOLWINDOW);
    }

    /// <summary>
    /// Sets the glow width and repositions if right edge
    /// </summary>
    public void SetWidth(double width)
    {
        Width = width;
        if (!_isLeftEdge)
        {
            // Reposition right edge window after width change
            Left = SystemParameters.PrimaryScreenWidth - width;
        }
    }

    /// <summary>
    /// Starts the breathing animation with specified parameters
    /// </summary>
    /// <param name="color">Glow color</param>
    /// <param name="breathSpeedSeconds">Breathing cycle duration</param>
    /// <param name="maxIntensity">Maximum opacity</param>
    public void StartBreathing(Color color, double breathSpeedSeconds, double maxIntensity)
    {
        _currentGlowColor = color;
        _breathSpeedSeconds = breathSpeedSeconds;
        _maxIntensity = maxIntensity;

        // Update gradient color with initial opacity
        UpdateGradientColor(0.3); // Start at low brightness

        // Create and start breathing animation
        CreateBreathingAnimation();
        _breathingStoryboard?.Begin();
        _isAnimating = true;
    }

    /// <summary>
    /// Updates the glow color instantly without restarting animation
    /// </summary>
    public void UpdateColor(Color color)
    {
        _currentGlowColor = color;

        // Get current opacity from animation or border
        double currentOpacity = _breathingAnimation?.CurrentValue ?? GlowBorder.Opacity;

        // Apply new color at current opacity
        UpdateGradientColor(currentOpacity);
    }

    /// <summary>
    /// Pauses the breathing animation at current brightness
    /// </summary>
    public void PauseBreathing()
    {
        if (_isAnimating && _breathingStoryboard != null)
        {
            _breathingStoryboard.Pause();
        }
    }

    /// <summary>
    /// Resumes the breathing animation from current brightness
    /// </summary>
    public void ResumeBreathing()
    {
        if (_isAnimating && _breathingStoryboard != null)
        {
            _breathingStoryboard.Resume();
        }
    }

    /// <summary>
    /// Stops the breathing and fades out
    /// </summary>
    public void StopBreathing()
    {
        if (_breathingStoryboard != null)
        {
            _breathingStoryboard.Stop();
            _isAnimating = false;
        }

        // Fade out animation
        var fadeOutAnimation = new DoubleAnimation
        {
            From = GlowBorder.Opacity,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.3),
            FillBehavior = FillBehavior.Stop
        };

        fadeOutAnimation.Completed += (s, e) =>
        {
            UpdateGradientColor(0);
            GlowBorder.Opacity = 0;
        };

        GlowBorder.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }

    /// <summary>
    /// Updates the gradient color with specified opacity
    /// Creates color with alpha channel based on opacity and maxIntensity
    /// </summary>
    private void UpdateGradientColor(double opacity)
    {
        // Calculate final opacity (opacity is 0-1, multiply by maxIntensity)
        double finalOpacity = opacity * _maxIntensity;
        byte alpha = (byte)(finalOpacity * 255);

        // Create color with alpha
        Color colorWithAlpha = Color.FromArgb(alpha, _currentGlowColor.R, _currentGlowColor.G, _currentGlowColor.B);

        // Update the color gradient stop
        ColorStop.Color = colorWithAlpha;
    }

    /// <summary>
    /// Creates the breathing animation storyboard
    /// Animates opacity from 0.1 to 1.0 (relative to maxIntensity)
    /// </summary>
    private void CreateBreathingAnimation()
    {
        // Stop any existing animation
        _breathingStoryboard?.Stop();

        // Create new animation
        _breathingAnimation = new DoubleAnimation
        {
            From = 0.1,
            To = 1.0,
            Duration = TimeSpan.FromSeconds(_breathSpeedSeconds / 2), // Half cycle (up)
            AutoReverse = true, // Creates full cycle (up then down)
            RepeatBehavior = RepeatBehavior.Forever,
            FillBehavior = FillBehavior.Stop
        };

        // Create storyboard
        _breathingStoryboard = new Storyboard();
        _breathingStoryboard.Children.Add(_breathingAnimation);

        // Target the GlowBorder's Opacity property
        Storyboard.SetTarget(_breathingAnimation, GlowBorder);
        Storyboard.SetTargetProperty(_breathingAnimation, new PropertyPath(OpacityProperty));

        // When opacity changes, update the gradient color
        // We use a custom approach: animate a proxy value and update color
        // Alternative: bind to Opacity and use a converter
        _breathingAnimation.CurrentTimeInvalidated += (s, e) =>
        {
            if (_breathingAnimation?.CurrentValue != null)
            {
                double currentOpacity = (double)_breathingAnimation.CurrentValue;
                UpdateGradientColor(currentOpacity);
            }
        };
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Views/SideGlowWindow.xaml.cs
git commit -m "feat: add SideGlowWindow code-behind with breathing animation

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 6: Implement EdgeGlowService

**Files:**
- Create: `Services/EdgeGlowService.cs`

**Explanation:** This service orchestrates the two SideGlowWindow instances. It:
1. Creates and positions left/right windows on Initialize()
2. Starts/stops/pauses/resumes animation on both windows simultaneously
3. Updates color when Stage changes
4. Handles errors gracefully (falls back to default color, logs errors)

- [ ] **Step 1: Create EdgeGlowService.cs**

```csharp
using System;
using System.Windows;
using System.Windows.Media;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Views;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service that manages edge glow breathing effect
/// Controls two SideGlowWindow instances (left and right)
/// </summary>
public class EdgeGlowService : IEdgeGlowService, IDisposable
{
    private SideGlowWindow? _leftWindow;
    private SideGlowWindow? _rightWindow;
    private EdgeGlowSettings _currentSettings;
    private Color _currentColor;
    private bool _isGlowing;
    private bool _isPaused;

    // Fallback color if invalid color is provided
    private static readonly Color FallbackColor = Color.FromRgb(0x4A, 0x90, 0xD9); // Light blue

    public EdgeGlowService()
    {
        _currentSettings = EdgeGlowSettings.Default;
        _currentColor = FallbackColor;
        _isGlowing = false;
        _isPaused = false;
    }

    /// <summary>
    /// Initializes the glow windows
    /// Called during application startup
    /// </summary>
    public void Initialize()
    {
        try
        {
            // Create left edge window
            _leftWindow = new SideGlowWindow(isLeftEdge: true);
            _leftWindow.SetWidth(_currentSettings.GlowWidthPixels);

            // Create right edge window
            _rightWindow = new SideGlowWindow(isLeftEdge: false);
            _rightWindow.SetWidth(_currentSettings.GlowWidthPixels);

            // Show windows but keep them hidden (opacity 0)
            _leftWindow.Show();
            _rightWindow.Show();

            Console.WriteLine("[EdgeGlowService] Windows initialized");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to initialize: {ex.Message}");
            // Disable feature but don't crash
            _leftWindow?.Close();
            _rightWindow?.Close();
            _leftWindow = null;
            _rightWindow = null;
        }
    }

    /// <summary>
    /// Starts the breathing glow with specified color and settings
    /// </summary>
    public void StartGlow(Color color, EdgeGlowSettings settings)
    {
        if (settings == null || !settings.IsEnabled)
        {
            // Feature disabled, stop any existing glow
            StopGlow();
            return;
        }

        // Validate and clamp settings
        settings.ClampValues();
        _currentSettings = settings;

        // Validate color (use fallback if invalid)
        _currentColor = ValidateColor(color);

        // Update window widths
        _leftWindow?.SetWidth(settings.GlowWidthPixels);
        _rightWindow?.SetWidth(settings.GlowWidthPixels);

        // Start breathing on both windows
        try
        {
            _leftWindow?.StartBreathing(_currentColor, settings.BreathSpeedSeconds, settings.MaxIntensity);
            _rightWindow?.StartBreathing(_currentColor, settings.BreathSpeedSeconds, settings.MaxIntensity);
            _isGlowing = true;
            _isPaused = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to start glow: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates the glow color instantly
    /// </summary>
    public void UpdateColor(Color color)
    {
        if (!_isGlowing) return;

        _currentColor = ValidateColor(color);

        try
        {
            _leftWindow?.UpdateColor(_currentColor);
            _rightWindow?.UpdateColor(_currentColor);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to update color: {ex.Message}");
        }
    }

    /// <summary>
    /// Pauses the breathing animation
    /// </summary>
    public void PauseGlow()
    {
        if (!_isGlowing || _isPaused) return;

        try
        {
            _leftWindow?.PauseBreathing();
            _rightWindow?.PauseBreathing();
            _isPaused = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to pause glow: {ex.Message}");
        }
    }

    /// <summary>
    /// Resumes the breathing animation
    /// </summary>
    public void ResumeGlow()
    {
        if (!_isGlowing || !_isPaused) return;

        try
        {
            _leftWindow?.ResumeBreathing();
            _rightWindow?.ResumeBreathing();
            _isPaused = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to resume glow: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the glow and fades out
    /// </summary>
    public void StopGlow()
    {
        if (!_isGlowing) return;

        try
        {
            _leftWindow?.StopBreathing();
            _rightWindow?.StopBreathing();
            _isGlowing = false;
            _isPaused = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EdgeGlowService] Failed to stop glow: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates color and returns fallback if invalid
    /// </summary>
    private Color ValidateColor(Color color)
    {
        // Check if color has valid values (not all zeros, not invalid)
        if (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0)
        {
            return FallbackColor;
        }
        return color;
    }

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        // Stop animations first
        StopGlow();

        // Close windows
        _leftWindow?.Close();
        _rightWindow?.Close();
        _leftWindow = null;
        _rightWindow = null;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add Services/EdgeGlowService.cs
git commit -m "feat: implement EdgeGlowService for managing glow windows

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 7: Register EdgeGlowService in DI Container

**Files:**
- Modify: `App.xaml.cs`

**Explanation:** We need to register IEdgeGlowService in the dependency injection container so MainViewModel can receive it. Also call Initialize() on startup to create the glow windows early.

- [ ] **Step 1: Add EdgeGlowService registration**

Add this line after `services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();` (around line 31):

```csharp
        services.AddSingleton<IEdgeGlowService, EdgeGlowService>();
```

- [ ] **Step 2: Add using statement for EdgeGlowService**

Add this using at the top of the file (after existing usings):

```csharp
// EdgeGlowService is in the same namespace, no new using needed
```

Actually, since EdgeGlowService is in `RTS_Tactical_Overlay.Services` namespace and that namespace is already imported (line 5), no new using is needed.

- [ ] **Step 3: Initialize EdgeGlowService on startup**

Modify the OnStartup method to initialize the glow service. Add after the service registration block (after `_serviceProvider = services.BuildServiceProvider();`):

```csharp
        // Initialize edge glow service
        var edgeGlowService = _serviceProvider.GetRequiredService<IEdgeGlowService>();
        edgeGlowService.Initialize();
```

Full updated OnStartup method:

```csharp
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Allocate console for debug output
        AllocConsole();
        Console.WriteLine("=== RTS Tactical Overlay Debug Console ===");

        var services = new ServiceCollection();

        // Register services
        services.AddSingleton<INativeInputSimulator, NativeInputSimulator>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IStageExecutor, StageExecutor>();
        services.AddSingleton<IGlobalHotkeyService, GlobalHotkeyService>();
        services.AddSingleton<IEdgeGlowService, EdgeGlowService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();

        _serviceProvider = services.BuildServiceProvider();

        // Initialize edge glow service
        var edgeGlowService = _serviceProvider.GetRequiredService<IEdgeGlowService>();
        edgeGlowService.Initialize();

        // Show main window
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
```

- [ ] **Step 4: Commit**

```bash
git add App.xaml.cs
git commit -m "feat: register EdgeGlowService in DI container and initialize on startup

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 8: Integrate EdgeGlowService into MainViewModel

**Files:**
- Modify: `ViewModels/MainViewModel.cs`

**Explanation:** Now we connect the EdgeGlowService to MainViewModel. We:
1. Inject IEdgeGlowService in constructor
2. Call StartGlow when execution starts (StartExecution command)
3. Update color when Stage changes (OnStageChanged handler)
4. Pause/Resume when execution pauses/resumes
5. Stop when execution stops or completes

- [ ] **Step 1: Add IEdgeGlowService field and injection**

Add field declaration after `_hotkeyService` (around line 16):

```csharp
    private readonly IEdgeGlowService _edgeGlowService;
```

Modify constructor to inject and store IEdgeGlowService. Replace the constructor (lines 69-88) with:

```csharp
    public MainViewModel(
        INativeInputSimulator inputSimulator,
        IProfileService profileService,
        IStageExecutor stageExecutor,
        IGlobalHotkeyService hotkeyService,
        IEdgeGlowService edgeGlowService)
    {
        _inputSimulator = inputSimulator;
        _profileService = profileService;
        _stageExecutor = stageExecutor;
        _hotkeyService = hotkeyService;
        _edgeGlowService = edgeGlowService;

        _stageExecutor.StageChanged += OnStageChanged;
        _stageExecutor.ExecutionCompleted += OnExecutionCompleted;
        _stageExecutor.ProgressUpdated += OnProgressUpdated;
        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _hotkeyService.Start();

        // Initialize profile system asynchronously
        _ = InitializeProfileSystemAsync();
    }
```

- [ ] **Step 2: Add using for EdgeGlowSettings**

The EdgeGlowSettings is in `RTS_Tactical_Overlay.Models` namespace which is already imported. No new using needed.

- [ ] **Step 3: Modify StartExecution to start glow**

Modify the StartExecution method (lines 154-167) to start edge glow:

```csharp
    [RelayCommand]
    private void StartExecution()
    {
        if (_profileService.CurrentProfile == null)
        {
            StatusText = "No profile loaded";
            return;
        }

        // Get settings (use default if null)
        var glowSettings = _profileService.CurrentProfile.EdgeGlowSettings ?? EdgeGlowSettings.Default;

        // Get initial color (first stage)
        var initialColor = NodeColorPalette.GetColorForIndex(0);

        // Start edge glow
        _edgeGlowService.StartGlow(initialColor, glowSettings);

        _stageExecutor.Start(_profileService.CurrentProfile);
        IsExecuting = true;
        IsPaused = false;
        StatusText = "Executing...";
    }
```

- [ ] **Step 4: Modify StopExecution to stop glow**

Modify the StopExecution method (lines 169-178) to stop edge glow:

```csharp
    [RelayCommand]
    private void StopExecution()
    {
        _stageExecutor.Stop();
        _edgeGlowService.StopGlow();
        IsExecuting = false;
        IsPaused = false;
        CurrentStageIndex = -1;
        StatusText = "Stopped";
        UpdateTimelineActiveState();
    }
```

- [ ] **Step 5: Modify PauseExecution to pause glow**

Modify the PauseExecution method (lines 180-186) to pause edge glow:

```csharp
    [RelayCommand]
    private void PauseExecution()
    {
        _stageExecutor.Pause();
        _edgeGlowService.PauseGlow();
        IsPaused = true;
        StatusText = "Paused";
    }
```

- [ ] **Step 6: Modify ResumeExecution to resume glow**

Modify the ResumeExecution method (lines 188-194) to resume edge glow:

```csharp
    [RelayCommand]
    private void ResumeExecution()
    {
        _stageExecutor.Resume();
        _edgeGlowService.ResumeGlow();
        IsPaused = false;
        StatusText = "Executing...";
    }
```

- [ ] **Step 7: Modify OnStageChanged to update glow color**

Modify the OnStageChanged method (lines 323-327) to update glow color:

```csharp
    private void OnStageChanged(object? sender, int stageIndex)
    {
        CurrentStageIndex = stageIndex;
        UpdateTimelineActiveState();

        // Update edge glow color
        if (IsExecuting && _profileService.CurrentProfile != null)
        {
            var color = NodeColorPalette.GetColorForIndex(stageIndex);
            _edgeGlowService.UpdateColor(color);
        }
    }
```

- [ ] **Step 8: Modify OnExecutionCompleted to stop glow**

Modify the OnExecutionCompleted method (lines 329-336) to stop glow:

```csharp
    private void OnExecutionCompleted(object? sender, EventArgs e)
    {
        _edgeGlowService.StopGlow();
        IsExecuting = false;
        IsPaused = false;
        CurrentStageIndex = -1;
        StatusText = "Completed";
        UpdateTimelineActiveState();
    }
```

- [ ] **Step 9: Commit**

```bash
git add ViewModels/MainViewModel.cs
git commit -m "feat: integrate EdgeGlowService into MainViewModel for Stage events

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

### Task 9: Build and Test

**Files:**
- None (testing only)

**Explanation:** Build the project and verify no compilation errors. Manual testing of the edge glow feature during execution.

- [ ] **Step 1: Build project**

Run: `dotnet build`

Expected: Build succeeds with no errors

- [ ] **Step 2: Run application**

Run: `dotnet run`

Expected:
1. Application starts
2. Edge glow windows are created (invisible initially)
3. When execution starts (F6), left/right edges show breathing glow
4. When Stage changes, glow color changes
5. When paused (F7), glow pauses at current brightness
6. When stopped (F8), glow fades out

- [ ] **Step 3: Final commit if needed**

```bash
git status
# If any remaining changes:
git add -A
git commit -m "feat: complete edge glow breathing effect implementation

Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>"
```

---

## Self-Review Checklist

**1. Spec Coverage:**
- [x] EdgeGlowSettings model with all properties (Task 1)
- [x] Embedded in TacticalProfile (Task 2)
- [x] IEdgeGlowService interface (Task 3)
- [x] SideGlowWindow XAML with gradient (Task 4)
- [x] SideGlowWindow code-behind with animation (Task 5)
- [x] EdgeGlowService orchestration (Task 6)
- [x] DI registration and initialization (Task 7)
- [x] MainViewModel integration for all Stage events (Task 8)
- [x] Build and test (Task 9)

**2. Placeholder Scan:**
- No TBD, TODO, or placeholder patterns found
- All code blocks contain complete implementation

**3. Type Consistency:**
- EdgeGlowSettings used consistently across TacticalProfile, IEdgeGlowService, EdgeGlowService, MainViewModel
- Color type from System.Windows.Media used consistently
- Method names match: StartGlow/StopGlow/PauseGlow/ResumeGlow/UpdateColor

---

**Plan complete. Ready for execution.**
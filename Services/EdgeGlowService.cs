using System;
using System.Windows.Media;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Views;

namespace RTS_Tactical_Overlay.Services;

/// <summary>
/// Service that manages edge glow breathing effect
/// Controls two SideGlowWindow instances (left and right)
/// </summary>
public class EdgeGlowService : IEdgeGlowService
{
    // ============== 字段定义 ==============

    /// <summary>
    /// Window instance for left screen edge
    /// </summary>
    private SideGlowWindow? _leftWindow;

    /// <summary>
    /// Window instance for right screen edge
    /// </summary>
    private SideGlowWindow? _rightWindow;

    /// <summary>
    /// Current glow settings (from user profile)
    /// </summary>
    private EdgeGlowSettings _currentSettings;

    /// <summary>
    /// Current glow color
    /// </summary>
    private Color _currentColor;

    /// <summary>
    /// Whether glow is currently active
    /// </summary>
    private bool _isGlowing;

    /// <summary>
    /// Whether glow is paused
    /// </summary>
    private bool _isPaused;

    /// <summary>
    /// Fallback color used when invalid color is provided
    /// </summary>
    private static readonly Color FallbackColor = Color.FromRgb(0x4A, 0x90, 0xD9); // Light blue

    // ============== 构造函数 ==============

    public EdgeGlowService()
    {
        // Initialize with default settings
        _currentSettings = EdgeGlowSettings.Default;
        _currentColor = FallbackColor;
        _isGlowing = false;
        _isPaused = false;
    }

    // ============== 初始化 ==============

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

            // Show windows (they start invisible because opacity = 0)
            _leftWindow.Show();
            _rightWindow.Show();

            Console.WriteLine("[EdgeGlowService] Windows initialized successfully");
        }
        catch (Exception ex)
        {
            // If window creation fails, log error but don't crash the app
            Console.WriteLine($"[EdgeGlowService] Failed to initialize: {ex.Message}");

            // Clean up any partially created windows
            _leftWindow?.Close();
            _rightWindow?.Close();
            _leftWindow = null;
            _rightWindow = null;
        }
    }

    // ============== 核心方法 ==============

    /// <summary>
    /// Starts the breathing glow with specified color and settings
    /// </summary>
    public void StartGlow(Color color, EdgeGlowSettings settings)
    {
        // Check if feature is disabled
        if (settings == null || !settings.IsEnabled)
        {
            StopGlow();
            return;
        }

        // Validate and clamp settings to safe values
        settings.ClampValues();
        _currentSettings = settings;

        // Validate color (use fallback if invalid)
        _currentColor = ValidateColor(color);

        // Update window widths based on settings
        _leftWindow?.SetWidth(settings.GlowWidthPixels);
        _rightWindow?.SetWidth(settings.GlowWidthPixels);

        // Start breathing animation on both windows
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
        // Don't update if not currently glowing
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

    // ============== 辅助方法 ==============

    /// <summary>
    /// Validates color and returns fallback if invalid
    /// </summary>
    private Color ValidateColor(Color color)
    {
        // Check if color is all zeros (invalid/uninitialized)
        // A valid color should have at least one non-zero component
        if (color.A == 0 && color.R == 0 && color.G == 0 && color.B == 0)
        {
            return FallbackColor;
        }
        return color;
    }

    // ============== 资源清理 ==============

    /// <summary>
    /// Cleans up resources
    /// </summary>
    public void Dispose()
    {
        // Stop any active animation first
        StopGlow();

        // Close and release window references
        _leftWindow?.Close();
        _rightWindow?.Close();
        _leftWindow = null;
        _rightWindow = null;
    }
}
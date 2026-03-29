---
name: Edge Glow Breathing Effect
description: Screen edge breathing glow effect during Stage execution for enhanced visual feedback
type: project
---

# Edge Glow Breathing Effect Design Spec

## Overview

Add a breathing glow effect on the left and right screen edges during Stage execution. The glow color syncs with the current timeline node color, providing enhanced visual feedback to users. The effect parameters (speed, intensity, width) are user-configurable.

## Architecture

### New Files

```
Views/
└── SideGlowWindow.xaml / .cs    # Left/right edge glow window (same class, two instances)

Services/
├── IEdgeGlowService.cs          # Edge glow service interface
└── EdgeGlowService.cs           # Manages window instances, animation control

Models/
└── EdgeGlowSettings.cs          # Configuration model (breath speed, intensity, width)
```

### Component Relationships

```
StageExecutor ──(StageChanged/ProgressUpdated)──► MainViewModel
                                                      │
                                                      ├──► EdgeGlowService
                                                      │         │
                                                      │         ├──► SideGlowWindow (left)
                                                      │         └──► SideGlowWindow (right)
                                                      │
NodeColorPalette ──(current node color)──────────────────┘
```

### Responsibilities

| Component | Responsibility |
|-----------|---------------|
| `SideGlowWindow` | Independent transparent window rendering single-edge breathing animation |
| `EdgeGlowService` | Manage two window instances, respond to Stage changes, control animation start/stop |
| `EdgeGlowSettings` | Store user configuration, embedded in Profile |

## Data Flow

```
[StageExecutor executing]
        │
        ▼ StageChanged event (stageIndex)
[MainViewModel]
        │
        │ 1. Get current node color (NodeColorPalette.GetColorForIndex(stageIndex))
        │ 2. Call EdgeGlowService.SetGlow(color, settings)
        │
        ▼
[EdgeGlowService]
        │
        ├─► Left SideGlowWindow.StartBreathing(color, speed, intensity)
        └─► Right SideGlowWindow.StartBreathing(color, speed, intensity)

[Execution stop/complete]
        │
        ▼
[EdgeGlowService.StopGlow()]
        │
        └─► Both windows stop animation, hide edge glow
```

## Animation Control Logic

| Event | Action |
|-------|--------|
| `StartExecution` | Start breathing glow, color = current node color |
| `StageChanged` | Update breathing glow color to new node color |
| `PauseExecution` | Animation pauses (brightness fixed at current value) |
| `ResumeExecution` | Animation resumes |
| `StopExecution` | Breathing glow stops and fades out |

### Animation Implementation

- Use WPF `DoubleAnimation` to control Border `Opacity`
- Cycle: `AutoReverse = true`, `RepeatBehavior = RepeatBehavior.Forever`
- Brightness range: 0.0 ~ `MaxIntensity` (user-configurable upper limit)
- Pause: Stop animation at current value
- Resume: Restart animation from current value

## User Configuration

### EdgeGlowSettings Properties

| Property | Type | Default | Range | Description |
|----------|------|---------|-------|-------------|
| `BreathSpeedSeconds` | double | 2.0 | 0.5-5.0 | Breathing cycle duration (seconds) |
| `MaxIntensity` | double | 0.6 | 0.1-1.0 | Maximum brightness (Opacity upper limit) |
| `GlowWidthPixels` | int | 30 | 10-100 | Edge glow band width |
| `IsEnabled` | bool | true | - | Feature toggle |

### Storage Location

- Embedded in existing `TacticalProfile` model as `EdgeGlowSettings` property
- Saved together with Profile JSON

### UI Entry

Integrate into existing settings module:
- Add EdgeGlowSettings section to settings panel
- Sliders for breath speed, intensity, width
- Checkbox for enable/disable toggle
- Changes apply immediately, saved via debounced save mechanism

## SideGlowWindow Implementation

### Window Properties

| Property | Value | Description |
|----------|-------|-------------|
| `WindowStyle` | None | No border |
| `AllowsTransparency` | true | Transparent background |
| `Background` | Transparent | Fully transparent |
| `Topmost` | true | Top-level display |
| `ShowInTaskbar` | false | No taskbar entry |
| `Width` | User-configured (default 30px) | Edge glow band width |
| `Height` | Screen height | Full screen height coverage |
| `Left` | 0 (left) / ScreenWidth - Width (right) | Positioning |
| `Top` | 0 | Top-aligned |
| `WS_EX_NOACTIVATE` | Applied | No focus stealing (same as main Overlay) |

### XAML Structure

```xml
<Window ...>
    <Grid>
        <!-- Gradient glow band: fades from edge toward center -->
        <Border x:Name="GlowBorder">
            <Border.Background>
                <LinearGradientBrush x:Name="GlowGradient" StartPoint="0,0" EndPoint="1,0">
                    <!-- Left window: GradientStop at 0 = glow color, at 1 = transparent -->
                    <!-- Right window: GradientStop at 0 = transparent, at 1 = glow color -->
                </LinearGradientBrush>
            </Border.Background>
        </Border>
    </Grid>
</Window>
```

### Visual Effect

- `LinearGradientBrush` creates fade effect from screen edge toward center
- Animation controls the color's opacity via gradient stop or border opacity
- Window uses `WS_EX_NOACTIVATE` to prevent focus stealing

## Error Handling

### Exception Handling

| Scenario | Handling |
|----------|----------|
| Window creation failed | Log error, disable edge glow feature, main Overlay unaffected |
| Invalid color value | Use fallback color (light blue #4A90D9) |
| Animation resource leak | Explicitly stop animation and release Storyboard in `StopGlow()` |
| Config value out of range | Auto clamp to valid range, no exception thrown |

### Edge Cases

| Scenario | Behavior |
|----------|----------|
| User disables feature | Immediately stop animation and hide window |
| Switch Profile | Use new Profile's EdgeGlowSettings |
| Profile has no config | Use default values |
| Rapid Stage switching | Immediately switch color, no wait for animation cycle |
| Execution paused | Animation pauses at current brightness |
| Program exit | Stop animation before closing window, prevent resource leak |

## Implementation Notes

1. EdgeGlowService should be registered in App.xaml DI container
2. MainViewModel should inject IEdgeGlowService and call it on Stage events
3. SideGlowWindow should implement IDisposable for proper cleanup
4. All animation Storyboards should be named and explicitly stopped on cleanup
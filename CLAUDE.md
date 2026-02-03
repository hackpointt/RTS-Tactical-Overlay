# RTS-Tactical-Overlay

A WPF .NET 8 transparent overlay application for RTS games that provides automated unit group cycling with visual timeline feedback.

## Project Overview

This application displays a non-focus-stealing overlay at the top of the screen, showing a timeline of unit group selections. It uses native keyboard simulation (SendInput) to cycle through unit groups at precise intervals, helping players maintain tactical awareness in real-time strategy games.

## Core Architecture

**Framework**: .NET 8 WPF with MVVM pattern  
**UI Toolkit**: CommunityToolkit.Mvvm  
**Native Interop**: P/Invoke for SendInput keyboard simulation

## File Structure

```
D:\Dev\Cmd\
├── RTS-Tactical-Overlay.csproj    # Project definition
├── App.xaml / App.xaml.cs         # DI container setup
├── MainWindow.xaml / .cs          # Transparent overlay UI
│
├── Models/
│   ├── Stage.cs                   # Execution stage (key + duration)
│   ├── TacticalProfile.cs         # Profile with stages list
│   └── TimelineNode.cs            # UI node with animation state
│
├── ViewModels/
│   └── MainViewModel.cs           # Main logic & timeline management
│
├── Services/
│   ├── INativeInputSimulator.cs   # Keyboard simulation interface
│   ├── NativeInputSimulator.cs    # SendInput implementation
│   ├── ProfileService.cs          # JSON profile loading/saving
│   ├── StageExecutor.cs           # High-precision timer execution
│   └── NodeColorPalette.cs        # 20-color palette for nodes
│
├── Converters/
│   ├── NodeColorProgressConverter.cs   # Node color animation
│   └── LineColorProgressConverter.cs   # Line gradient animation
│
└── Native/
    ├── Win32Constants.cs          # WS_EX_NOACTIVATE, etc.
    ├── Win32Structures.cs         # INPUT, KEYBDINPUT structs
    └── Win32Methods.cs            # SendInput, SetWindowLong
```

## Key Features

- **Focus-Free Overlay**: Uses `WS_EX_NOACTIVATE` to prevent stealing focus
- **60 FPS Animation**: Smooth node lighting and line progression
- **Native Input**: SendInput for reliable keyboard simulation
- **Profile System**: JSON-based stage configuration

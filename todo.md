# RTS-Tactical-Overlay - TODO List

## ✅ Completed (Step 1)
- [x] Project structure and .csproj configuration
- [x] Native P/Invoke layer (Win32 API for SendInput, window styling)
- [x] NativeInputSimulator service (keyboard simulation infrastructure)
- [x] Basic MVVM architecture with DI container
- [x] Transparent overlay window (no focus stealing, always on top)
- [x] Visual placeholder (semi-transparent red bar)
- [x] Build and compile successfully
- [x] Verify focus-prevention behavior

---

## ✅ Completed (Phase 2)

### 1. Data Models
- [x] `Stage.cs` - Define stage structure
  - Stage ID
  - Unit key (e.g., "1", "2", "3")
  - Duration in seconds
  - Description/name
  - DoubleTap configuration
  
- [x] `TacticalProfile.cs` - Define profile structure
  - Profile name
  - Profile description
  - List of stages
  - Total duration calculation
  - Loop mode support

- [x] `TimelineNode.cs` - UI representation of stages
  - Reference to Stage
  - Position X (for rendering)
  - IsActive flag
  - Width for visualization
  - Observable properties

### 2. Profile Management (JSON)
- [x] `IProfileService.cs` - Interface for profile operations
  - LoadProfileAsync(filePath)
  - SaveProfileAsync(profile, filePath)
  - GetCurrentProfile()
  - SetCurrentProfile(profile)

- [x] `ProfileService.cs` - Implementation
  - JSON serialization/deserialization
  - Profile validation logic
  - Error handling

- [x] Create default profile JSON example
  - Example: 5-stage cycle for RTS game
  - Saved to `sample-profile.json`

### 3. Stage Execution Engine
- [x] `IStageExecutor.cs` - Interface for stage cycling
  - Start(profile)
  - Pause()
  - Resume()
  - Stop()
  - Events: StageChanged, ExecutionCompleted

- [x] `StageExecutor.cs` - Implementation
  - High-resolution timer with Stopwatch
  - Stage transition logic
  - Pause/resume with time preservation
  - Background task execution
  - Event notifications

### 4. Keyboard Simulation Enhancement
- [x] Implement "double-tap" key simulation
  - Press key down
  - Release key
  - Small delay (configurable, default 50ms)
  - Press key down again
  - Release key
  
- [ ] Test with actual RTS games
  - Verify key timing works with game engines
  - Adjust delays if needed for reliability

### 5. Timeline Visualization
- [x] Design timeline UI layout
  - Horizontal timeline bar
  - Stage nodes as rectangles
  - Width represents duration
  - Active stage highlighting
  
- [x] Implement timeline rendering
  - ItemsControl with Canvas for node positioning
  - Data binding to TimelineNode collection
  - Visual states (pending, active)
  - Color coding for active stages

- [x] Implement scrolling for long timelines
  - ScrollViewer for horizontal scrolling
  - Manual scrolling support

### 6. Interactive Timeline Features
- [ ] Draggable nodes for duration adjustment
  - Drag node left/right to change duration
  - Real-time duration update
  - Preserve adjacent node spacing
  - Visual feedback during drag

- [ ] Click to pause at specific stage
  - Click node to jump to that stage
  - Pause execution at clicked stage
  - Resume from paused position

- [ ] Hover tooltips
  - Show stage details on hover
  - Display: name, key, duration, description

### 7. Pause/Resume Functionality
- [x] Implement pause logic in StageExecutor
  - Freeze timer
  - Preserve current position
  - Resume from paused position

- [x] Add keyboard shortcuts (Global Hotkeys)
  - F5: Load profile
  - F6: Start execution
  - F7: Pause/Resume
  - F8: Stop execution

- [x] Hotkey configuration system
  - JSON-based hotkey configuration
  - Customizable key bindings
  - Modifier support (Ctrl, Alt, Shift)

### 8. Enhanced UI/UX
- [x] Improve overlay appearance
  - Replaced red placeholder with dark semi-transparent bar
  - Added proper styling
  - Timeline visualization with colored nodes

- [x] Add status indicators
  - Current profile name display
  - Status text (Ready, Loaded, Executing, Paused, etc.)
  - Timeline with active stage highlighting

- [ ] Add settings panel (collapsible)
  - Opacity adjustment
  - Position adjustment (top/bottom)
  - Enable/disable sound notifications
  - Hotkey customization

### 9. Profile Switching
- [ ] UI for profile selection
  - Dropdown menu with available profiles
  - Load profile on selection
  - Show profile description

- [ ] Profile management UI
  - Create new profile button
  - Edit current profile button
  - Delete profile button
  - Duplicate profile button

### 10. Advanced Features
- [ ] Loop mode
  - Automatically restart from stage 1 after completion
  - Configurable loop count or infinite loop
  - Loop counter display

- [ ] Sound notifications
  - Play sound on stage transition
  - Different sounds for different stages
  - Volume control

- [ ] Hotkey for emergency pause
  - Global hotkey (e.g., Ctrl+Shift+P)
  - Works even when game has focus
  - Register/unregister hotkey on startup/shutdown

- [ ] Stage action enhancements
  - Support for key combinations (Ctrl+1, Shift+2)
  - Support for multiple actions per stage
  - Conditional actions based on time

### 11. Configuration & Settings
- [ ] Settings persistence
  - Save user preferences to JSON
  - Load on startup
  - Settings: opacity, position, hotkeys, sound

- [ ] Profile editor UI
  - Add/remove stages
  - Edit stage properties
  - Reorder stages (drag & drop)
  - Preview timeline

### 12. Testing & Validation
- [ ] Unit tests for core services
  - NativeInputSimulator tests
  - StageExecutor tests
  - ProfileService tests

- [ ] Integration tests
  - End-to-end profile loading and execution
  - Keyboard simulation verification
  - Timer accuracy tests

- [ ] Performance optimization
  - Ensure <50MB RAM usage
  - Ensure <1% CPU when idle
  - Optimize rendering for smooth 60 FPS

### 13. Documentation
- [ ] README.md
  - Project description
  - Installation instructions
  - Usage guide
  - Configuration examples

- [ ] User guide
  - How to create profiles
  - How to use the overlay
  - Keyboard shortcuts reference
  - Troubleshooting section

- [ ] Developer documentation
  - Architecture overview
  - Code structure explanation
  - How to extend/modify

### 14. Deployment & Distribution
- [ ] Create installer
  - MSI or setup.exe
  - Include .NET 8 runtime check
  - Create desktop shortcut

- [ ] Auto-update mechanism
  - Check for updates on startup
  - Download and install updates
  - Version management

---

## 🎯 Recommended Implementation Order

### Phase 2: Core Functionality (Next)
1. Data Models (Stage, TacticalProfile, TimelineNode)
2. ProfileService (JSON loading/saving)
3. Create default profile example
4. StageExecutor (timing engine)
5. Implement double-tap key simulation
6. Test with hardcoded profile

### Phase 3: Timeline Visualization
1. Design timeline UI layout
2. Implement timeline rendering
3. Add visual states and animations
4. Implement sliding window

### Phase 4: Interactive Features
1. Pause/Resume functionality
2. Keyboard shortcuts
3. Draggable nodes
4. Click to jump to stage

### Phase 5: Polish & Enhancement
1. Improve UI appearance
2. Add settings panel
3. Profile switching UI
4. Sound notifications

### Phase 6: Advanced Features
1. Loop mode
2. Global hotkeys
3. Profile editor UI
4. Advanced stage actions

### Phase 7: Testing & Documentation
1. Unit tests
2. Integration tests
3. Performance optimization
4. Documentation

### Phase 8: Deployment
1. Create installer
2. Auto-update mechanism
3. Release preparation

---

## 📝 Notes
- Focus on getting a minimal working prototype first (Phase 2)
- Test keyboard simulation with actual RTS games early
- Iterate based on real-world usage feedback
- Keep performance targets in mind throughout development

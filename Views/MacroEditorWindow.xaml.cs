using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RTS_Tactical_Overlay.Models;
using RTS_Tactical_Overlay.Services;
using RTS_Tactical_Overlay.ViewModels;

namespace RTS_Tactical_Overlay.Views;

public partial class MacroEditorWindow : Window
{
    private readonly ObservableCollection<MacroActionViewModel> _actions = new();
    private readonly Stage _stage;
    private MacroRecorderService? _recorder;

    public MacroEditorWindow(Stage stage)
    {
        InitializeComponent();
        _stage = stage;

        ActionsListBox.ItemsSource = _actions;

        // Load existing macro if present
        if (stage.Macro != null)
        {
            MacroNameTextBox.Text = stage.Macro.Name ?? "";
            foreach (var action in stage.Macro.Actions)
            {
                _actions.Add(MacroActionViewModel.FromMacroAction(action));
            }
        }

        UpdatePreview();
    }

    private void ActionTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Guard against calls during XAML initialization
        if (KeyInputTextBox == null) return;

        if (ActionTypeCombo.SelectedIndex == 3) // Delay
        {
            KeyInputTextBox.IsEnabled = false;
            CtrlCheckBox.IsEnabled = false;
            ShiftCheckBox.IsEnabled = false;
            AltCheckBox.IsEnabled = false;
        }
        else
        {
            KeyInputTextBox.IsEnabled = true;
            CtrlCheckBox.IsEnabled = true;
            ShiftCheckBox.IsEnabled = true;
            AltCheckBox.IsEnabled = true;
        }
    }

    private void AddAction_Click(object sender, RoutedEventArgs e)
    {
        var actionType = (MacroActionType)ActionTypeCombo.SelectedIndex;
        int.TryParse(DelayTextBox.Text, out int delayMs);
        if (delayMs <= 0) delayMs = 50;

        var vm = new MacroActionViewModel
        {
            Type = actionType,
            KeyText = KeyInputTextBox.Text.Trim(),
            DelayMs = delayMs,
            UseCtrl = CtrlCheckBox.IsChecked == true,
            UseShift = ShiftCheckBox.IsChecked == true,
            UseAlt = AltCheckBox.IsChecked == true
        };

        // Validate
        if (actionType != MacroActionType.Delay && string.IsNullOrEmpty(vm.KeyText))
        {
            MessageBox.Show("Please enter a key.", "Validation", MessageBoxButton.OK);
            return;
        }

        if (_isEditing && _editingIndex >= 0)
        {
            // Replace the action being edited
            _actions[_editingIndex] = vm;
            _isEditing = false;
            _editingIndex = -1;
            AddActionButton.Content = "Add";
            CancelEditButton.Visibility = System.Windows.Visibility.Collapsed;
        }
        else
        {
            _actions.Add(vm);
        }

        UpdatePreview();
        ClearInputFields();
    }

    private void ClearInputFields()
    {
        KeyInputTextBox.Text = "";
        CtrlCheckBox.IsChecked = false;
        ShiftCheckBox.IsChecked = false;
        AltCheckBox.IsChecked = false;
        ActionTypeCombo.SelectedIndex = 0;
        DelayTextBox.Text = "50";
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        int index = ActionsListBox.SelectedIndex;
        if (index > 0)
        {
            var item = _actions[index];
            _actions.RemoveAt(index);
            _actions.Insert(index - 1, item);
            ActionsListBox.SelectedIndex = index - 1;
            UpdatePreview();
        }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        int index = ActionsListBox.SelectedIndex;
        if (index >= 0 && index < _actions.Count - 1)
        {
            var item = _actions[index];
            _actions.RemoveAt(index);
            _actions.Insert(index + 1, item);
            ActionsListBox.SelectedIndex = index + 1;
            UpdatePreview();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        int index = ActionsListBox.SelectedIndex;
        if (index >= 0)
        {
            _actions.RemoveAt(index);
            UpdatePreview();
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        EditSelectedAction();
    }

    private void ActionsListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        EditSelectedAction();
    }

    private void EditSelectedAction()
    {
        int index = ActionsListBox.SelectedIndex;
        if (index < 0) return;

        // Cancel any previous edit
        if (_isEditing && _editingIndex >= 0 && _editingIndex < _actions.Count)
        {
            _actions[_editingIndex].IsEditing = false;
        }

        var action = _actions[index];

        // Load action into edit controls
        ActionTypeCombo.SelectedIndex = (int)action.Type;
        KeyInputTextBox.Text = action.KeyText;
        DelayTextBox.Text = action.DelayMs.ToString();
        CtrlCheckBox.IsChecked = action.UseCtrl;
        ShiftCheckBox.IsChecked = action.UseShift;
        AltCheckBox.IsChecked = action.UseAlt;

        // Mark as editing (keep in list)
        action.IsEditing = true;
        _editingIndex = index;
        _isEditing = true;

        // Update UI to show editing mode
        AddActionButton.Content = "Update";
        CancelEditButton.Visibility = System.Windows.Visibility.Visible;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (_isEditing && _editingIndex >= 0 && _editingIndex < _actions.Count)
        {
            _actions[_editingIndex].IsEditing = false;
        }

        _isEditing = false;
        _editingIndex = -1;
        AddActionButton.Content = "Add";
        CancelEditButton.Visibility = System.Windows.Visibility.Collapsed;
        ClearInputFields();
    }

    private int _editingIndex = -1;
    private bool _isEditing;

    private void ClearMacro_Click(object sender, RoutedEventArgs e)
    {
        _actions.Clear();
        MacroNameTextBox.Text = "";
        _stage.Macro = null;
        UpdatePreview();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (_actions.Count > 0)
        {
            _stage.Macro = new Macro
            {
                Name = MacroNameTextBox.Text.Trim(),
                Actions = _actions.Select(a => a.ToMacroAction()).ToList()
            };
        }
        else
        {
            _stage.Macro = null;
        }

        DialogResult = true;
        Close();
    }

    private void UpdatePreview()
    {
        if (_actions.Count == 0)
        {
            PreviewText.Text = "(No actions - will use default UnitKey)";
            return;
        }

        var parts = _actions.Select(a => a.DisplayText);
        PreviewText.Text = string.Join(" -> ", parts);
    }

    private void Record_Click(object sender, RoutedEventArgs e)
    {
        if (_recorder?.IsRecording == true)
        {
            StopRecording();
        }
        else
        {
            StartRecording();
        }
    }

    private void StartRecording()
    {
        _actions.Clear();
        UpdatePreview();

        _recorder = new MacroRecorderService();
        _recorder.KeyRecorded += OnKeyRecorded;
        _recorder.RecordingStopped += OnRecordingStopped;

        RecordButton.Content = "Stop (Esc)";
        RecordButton.Background = new SolidColorBrush(Color.FromRgb(0xAA, 0x22, 0x22));
        PreviewText.Text = "Recording... Press keys, then Esc to stop";

        _recorder.StartRecording();
    }

    private void StopRecording()
    {
        if (_recorder == null) return;

        var actions = _recorder.StopRecording();
        _recorder.KeyRecorded -= OnKeyRecorded;
        _recorder.RecordingStopped -= OnRecordingStopped;
        _recorder.Dispose();
        _recorder = null;

        // Convert to view models
        foreach (var action in actions)
        {
            _actions.Add(MacroActionViewModel.FromMacroAction(action));
        }

        RecordButton.Content = "Record";
        RecordButton.Background = new SolidColorBrush(Color.FromRgb(0x88, 0x22, 0x22));
        UpdatePreview();
    }

    private void OnKeyRecorded(object? sender, RecordedKey key)
    {
        Dispatcher.Invoke(() =>
        {
            PreviewText.Text = $"Recording... {key.DisplayText}";
        });
    }

    private void OnRecordingStopped(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            RecordButton.Content = "Record";
            RecordButton.Background = new SolidColorBrush(Color.FromRgb(0x88, 0x22, 0x22));
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        _recorder?.Dispose();
        base.OnClosed(e);
    }
}

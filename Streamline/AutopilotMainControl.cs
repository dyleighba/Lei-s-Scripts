using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyGridTerminalSystem = Sandbox.ModAPI.Ingame.IMyGridTerminalSystem;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;


namespace IngameScript
{
    public class AutopilotMainControl
    {
        private static readonly string[] ControlStrings = { "2:Edit 3:AP",  "2:Cancel 3:AP", "1:OK 2:Cancel 3:AP"};
        private static readonly string[] AutopilotSuffixes = { "m", "°", "m/s", "m/s" };
        private static readonly string[] ButtonSchemeAPModules = { "ALT", "HDG", "SPD", "VS" };
        public enum ButtonType
        {
            SB1, SB2, SB3, BB1, BB2, BB3, BB4 
        }
        private enum MenuState{
            Main,
            EditSelection,
            Editing
        }
        
        private Autopilot _autopilot;
        private readonly DisplayController _display;
        private readonly ButtonBar _buttonBar;
        
        private bool editMode = false;
        private Autopilot.Module editType = Autopilot.Module.None;
        private double editMulti = 1;
        private double editTemp = 0;

        
        public AutopilotMainControl(Autopilot autopilot, DisplayController displayController, ButtonBar buttonBar)
        {
            _autopilot = autopilot;
            _display = displayController;
            _buttonBar = buttonBar;
            // Input setup is here in js, but needs to be setup manually in SE
            SwitchToMainState();
        }
        
        // New function layout
        // Helpers
        private MenuState GetMenuState()
        {
            if (editMode)
            {
                if (editType != Autopilot.Module.None) return MenuState.Editing;
                return MenuState.EditSelection;
            }
            return MenuState.Main;
        }
        private void RenameButtons(string[] buttonLabels)
        {
            _buttonBar.RenameButtons(buttonLabels);
        }
        // Input handling
        public void HandleInput(ButtonType buttonType)
        {
            if (buttonType == ButtonType.SB3)
            {
                _autopilot.AutopilotEnabled = !_autopilot.AutopilotEnabled;
                RefreshDisplay();
            }
            else {
                switch (GetMenuState()) {
                    case MenuState.Main:
                        HandleInputMain(buttonType);
                        break;
                    case MenuState.EditSelection:
                        HandleInputEditSelection(buttonType);
                        break;
                    case MenuState.Editing:
                        HandleInputEditing(buttonType);
                        break;
                    default:
                        this.RenderError($"Unknown menu state for processing input: [{buttonType}]");
                        break;
                }
            }
        }
        private void HandleInputMain(ButtonType buttonType)
        {
            switch (buttonType) {
                case ButtonType.BB1: // ALT
                    _autopilot.ToggleModule(Autopilot.Module.Altitude);
                    RefreshDisplay();
                    break;
                case ButtonType.BB2: // HDG
                    _autopilot.ToggleModule(Autopilot.Module.Heading);
                    RefreshDisplay();
                    break;
                case ButtonType.BB3: // SPD
                    _autopilot.ToggleModule(Autopilot.Module.Speed);
                    RefreshDisplay();
                    break;
                case ButtonType.BB4: // VS
                    _autopilot.ToggleModule(Autopilot.Module.VerticalSpeed);
                    RefreshDisplay();
                    break;
                case ButtonType.SB2: // Edit
                    SwitchToEditSelectionState();
                    break;
                case ButtonType.SB1:
                    // Not used in this state
                    break;
                default:
                    RenderError($"Unknown button press: [{buttonType}]");
                    break;
            }
        }
        private void HandleInputEditSelection(ButtonType buttonType)
        {
            switch (buttonType) {
                case ButtonType.BB1: // ALT
                    editType = Autopilot.Module.Altitude;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB2: // HDG
                    editType = Autopilot.Module.Heading;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB3: // SPD
                    editType = Autopilot.Module.Speed;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB4: // VS
                    editType = Autopilot.Module.VerticalSpeed;
                    SwitchToEditingState();
                    break;
                case ButtonType.SB2: // Edit
                    SwitchToMainState();
                    break;
                case ButtonType.SB1:
                    // Not used in this state
                    break;
                default:
                    RenderError($"Unknown button press: [{buttonType}]");
                    break;
            }
        }
        private void HandleInputEditing(ButtonType buttonType)
        {
            // TODO add special cases for heading
            switch (buttonType) {
                case ButtonType.BB1: // --
                    editTemp = editTemp - (editMulti * 10);
                    if (editType == Autopilot.Module.VerticalSpeed)
                    {
                        editTemp = Math.Max(editTemp, -100);
                    }
                    else
                    {
                        editTemp = Math.Max(editTemp, 0);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.BB2: // -
                    editTemp = editTemp - editMulti;
                    if (editType == Autopilot.Module.VerticalSpeed)
                    {
                        editTemp = Math.Max(editTemp, -100);
                    }
                    else
                    {
                        editTemp = Math.Max(editTemp, 0);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.BB3: // +
                    editTemp = editTemp + editMulti;
                    if (editType == Autopilot.Module.VerticalSpeed || editType == Autopilot.Module.Speed)
                    {
                        editTemp = Math.Min(100, editTemp);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.BB4: // ++
                    editTemp = editTemp + (editMulti * 10);
                    if (editType == Autopilot.Module.VerticalSpeed || editType == Autopilot.Module.Speed)
                    {
                        editTemp = Math.Min(100, editTemp);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.SB2: // Edit
                    SwitchToMainState();
                    break;
                case ButtonType.SB1:
                    _autopilot.SetTarget(editType, editTemp);
                    SwitchToMainState();
                    break;
                default:
                    RenderError($"Unknown button press: [{buttonType}]");
                    break;
            }
        }
        // State switching
        private void SwitchToMainState()
        {
            editType = Autopilot.Module.None;
            editMode = false;
            RenameButtons(ButtonSchemeAPModules);
            RefreshDisplay();
        }
        private void SwitchToEditSelectionState()
        {
            editType = Autopilot.Module.None;
            editMode = true;
            RenameButtons(ButtonSchemeAPModules);
            RefreshDisplay();
        }
        private void SwitchToEditingState()
        {
            editMulti = 1;
            editTemp = _autopilot.GetTarget(editType);
            if (editType == Autopilot.Module.Altitude)
            {
                editMulti = 100;
            }

            RenameButtons(new string[]
            {
                $"- {editMulti * 10} ",
                $"- {editMulti} ",
                $"+ {editMulti} ",
                $"+ {editMulti * 10} "
            });
            RefreshDisplay();
        }
        // Rendering
        private void RenderError(string message)
        {
            this._display.NewBuffer();
            this._display.WriteLine($"Error: \n{message} \n");
            this._display.FlipDisplay();
            throw new Exception(message);
        }
        private void DrawHeader()
        {
            Func<bool, string, string> hideIfFalse = (conditionResult, text) =>
            {
                if (conditionResult)
                {
                    return text;
                }
                else
                {
                    return new string(' ', text.Length);
                }
            };
            
            string apEnabledString = _autopilot.AutopilotEnabled ? "AP ON       " : "AP OFF      ";
            var functionsEnabled = $"{apEnabledString} {hideIfFalse(_autopilot.AltitudeEnabled, "ALT")} {hideIfFalse(_autopilot.HeadingEnabled, "HDG")} {hideIfFalse(_autopilot.SpeedEnabled, "SPD")} {hideIfFalse(_autopilot.VerticalSpeedEnabled, "VS")}";
            _display.SeekLine(0);
            _display.WriteLine($"{functionsEnabled}\n{new string('-', _display.Width)}");
        }
        private void DrawControls(MenuState menuState)
        {
            string outputStr = ControlStrings[(int) menuState].PadLeft(_display.Width);
            _display.SeekLine(12);
            _display.WriteLine(outputStr);
        }
        private void DrawAutopilotReadout()
        {
            string altitudeReal = Math.Round(_autopilot.CurrentAltitude).ToString().PadLeft(5);
            string altitudeTarget = _autopilot.AltitudeTarget.ToString().PadLeft(5);
            var altitudeError = "N/A".PadLeft(6);
            if (_autopilot.AltitudeEnabled)
                altitudeError = Math.Round(_autopilot.AltitudeError).ToString()
                    .PadLeft(6);

            string headingReal = (Math.Round(_autopilot.CurrentHeading)).ToString().PadLeft(3);
            string headingTarget = _autopilot.HeadingTarget.ToString().PadLeft(5);
            string headingError = "N/A".PadLeft(6);
            if (_autopilot.HeadingEnabled)
                headingError = Math.Round(_autopilot.HeadingError).ToString()
                    .PadLeft(6);

            string hSpeedReal = (Math.Round(_autopilot.CurrentSpeed)).ToString().PadLeft(3);
            string hSpeedTarget = _autopilot.SpeedTarget.ToString().PadLeft(5);
            string hSpeedError = "N/A".PadLeft(6);
            if (_autopilot.SpeedEnabled)
                hSpeedError = Math.Round(_autopilot.SpeedError).ToString()
                    .PadLeft(6);

            string vSpeedReal = (Math.Round(_autopilot.CurrentVerticalSpeed)).ToString().PadLeft(3);
            string vSpeedTarget = _autopilot.VerticalSpeedTarget.ToString().PadLeft(5);
            string vSpeedError = "N/A".PadLeft(6);
            if (_autopilot.VerticalSpeedEnabled)
                vSpeedError = Math.Round(_autopilot.VerticalSpeedError).ToString()
                    .PadLeft(6);

            string pitchReal = Math.Round(_autopilot.CurrentPitch).ToString().PadLeft(4);
            string rollReal = Math.Round(_autopilot.CurrentRoll).ToString().PadLeft(4);
            
            _display.SeekLine(2);
            _display.WriteLine("      Real  Error  Goal");
            _display.WriteLine($"Alt: {altitudeReal} {altitudeError} {altitudeTarget} m");
            _display.WriteLine($"Hdg:   {headingReal} {headingError} {headingTarget} °");
            _display.WriteLine($"Spd:   {hSpeedReal} {hSpeedError} {hSpeedTarget} m/s");
            _display.WriteLine($" Vs:   {vSpeedReal} {vSpeedError} {vSpeedTarget} m/s");
            _display.WriteLine($"Pitch: {pitchReal}° Roll: {rollReal}°");
        }
        public void RefreshDisplay()
        {
            MenuState menuState = GetMenuState();
            _display.NewBuffer();
            DrawHeader();
            DrawAutopilotReadout();

            if (menuState == MenuState.EditSelection || menuState == MenuState.Editing)
            {
                string editingTooltop = (editType != Autopilot.Module.None) ? editType.ToString() : "___";
                _display.SeekLine(8);
                _display.WriteLine($"Edit Target: {editingTooltop}".PadRight(_display.Width));
            }
            if (menuState == MenuState.Editing) {
                _display.WriteLine($"Current: {_autopilot.GetTarget(editType).ToString().PadRight(6)} {AutopilotSuffixes[(int) editType]}");
                _display.WriteLine($"    New: {editTemp.ToString().PadRight(6)} {AutopilotSuffixes[(int) editType]}");
            }

            DrawControls(menuState);
            _display.FlipDisplay();
        }
    }
}
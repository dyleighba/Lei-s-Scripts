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
        
        private AutopilotData _apData = new AutopilotData();
        private readonly DisplayController _display;
        private readonly ButtonBar _buttonBar;
        
        private bool editMode = false;
        private Autopilot.APModule editType = Autopilot.APModule.None;
        private double editMulti = 1;
        private double editTemp = 0;

        
        public AutopilotMainControl(DisplayController displayController, ButtonBar buttonBar)
        {
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
                if (editType != Autopilot.APModule.None) return MenuState.Editing;
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
            if (buttonType == ButtonType.SB3) {
                _apData.ToggleAP();
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
                    _apData.ToggleModule(Autopilot.APModule.ALT);
                    RefreshDisplay();
                    break;
                case ButtonType.BB2: // HDG
                    _apData.ToggleModule(Autopilot.APModule.HDG);
                    RefreshDisplay();
                    break;
                case ButtonType.BB3: // SPD
                    _apData.ToggleModule(Autopilot.APModule.SPD);
                    RefreshDisplay();
                    break;
                case ButtonType.BB4: // VS
                    _apData.ToggleModule(Autopilot.APModule.VS);
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
                    editType = Autopilot.APModule.ALT;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB2: // HDG
                    editType = Autopilot.APModule.HDG;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB3: // SPD
                    editType = Autopilot.APModule.SPD;
                    SwitchToEditingState();
                    break;
                case ButtonType.BB4: // VS
                    editType = Autopilot.APModule.VS;
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
                    if (editType == Autopilot.APModule.VS)
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
                    if (editType == Autopilot.APModule.VS)
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
                    if (editType == Autopilot.APModule.VS || editType == Autopilot.APModule.SPD)
                    {
                        editTemp = Math.Min(100, editTemp);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.BB4: // ++
                    editTemp = editTemp + (editMulti * 10);
                    if (editType == Autopilot.APModule.VS || editType == Autopilot.APModule.SPD)
                    {
                        editTemp = Math.Min(100, editTemp);
                    }
                    RefreshDisplay();
                    break;
                case ButtonType.SB2: // Edit
                    SwitchToMainState();
                    break;
                case ButtonType.SB1:
                    _apData.SetTarget(editType, editTemp);
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
            editType = Autopilot.APModule.None;
            editMode = false;
            RenameButtons(ButtonSchemeAPModules);
            RefreshDisplay();
        }
        private void SwitchToEditSelectionState()
        {
            editType = Autopilot.APModule.None;
            editMode = true;
            RenameButtons(ButtonSchemeAPModules);
            RefreshDisplay();
        }
        private void SwitchToEditingState()
        {
            editMulti = 1;
            editTemp = _apData.GetTarget(editType);
            if (editType == Autopilot.APModule.ALT)
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
            
            string apEnabledString = _apData.AutopilotEnabled ? "AP ON       " : "AP OFF      ";
            var functionsEnabled = $"{apEnabledString} {hideIfFalse(_apData.AltitudeEnabled, "ALT")} {hideIfFalse(_apData.HeadingEnabled, "HDG")} {hideIfFalse(_apData.SpeedEnabled, "SPD")} {hideIfFalse(_apData.VerticalSpeedEnabled, "VS")}";
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
            string altitudeReal = Math.Round(_apData.AltitudeCurrent).ToString().PadLeft(5);
            string altitudeTarget = _apData.AltitudeTarget.ToString().PadLeft(5);
            var altitudeError = "N/A".PadLeft(6);
            if (_apData.AltitudeEnabled)
                altitudeError = Math.Round(_apData.AltitudeError).ToString()
                    .PadLeft(6);

            string headingReal = (Math.Round(_apData.HeadingCurrent)).ToString().PadLeft(3);
            string headingTarget = _apData.HeadingTarget.ToString().PadLeft(5);
            string headingError = "N/A".PadLeft(6);
            if (_apData.HeadingEnabled)
                headingError = Math.Round(_apData.HeadingError).ToString()
                    .PadLeft(6);

            string hSpeedReal = (Math.Round(_apData.SpeedCurrent)).ToString().PadLeft(3);
            string hSpeedTarget = _apData.SpeedTarget.ToString().PadLeft(5);
            string hSpeedError = "N/A".PadLeft(6);
            if (_apData.SpeedEnabled)
                hSpeedError = Math.Round(_apData.SpeedError).ToString()
                    .PadLeft(6);

            string vSpeedReal = (Math.Round(_apData.VerticalSpeedCurrent)).ToString().PadLeft(3);
            string vSpeedTarget = _apData.VerticalSpeedTarget.ToString().PadLeft(5);
            string vSpeedError = "N/A".PadLeft(6);
            if (_apData.VerticalSpeedEnabled)
                vSpeedError = Math.Round(_apData.VerticalSpeedError).ToString()
                    .PadLeft(6);

            string pitchReal = Math.Round(_apData.PitchCurrent).ToString().PadLeft(4);
            string rollReal = Math.Round(_apData.RollCurrent).ToString().PadLeft(4);
            
            _display.SeekLine(2);
            _display.WriteLine("      Real  Error  Goal");
            _display.WriteLine($"Alt: {altitudeReal} {altitudeError} {altitudeTarget} m");
            _display.WriteLine($"Hdg:   {headingReal} {headingError} {headingTarget} °");
            _display.WriteLine($"Spd:   {hSpeedReal} {hSpeedError} {hSpeedTarget} m/s");
            _display.WriteLine($" Vs:   {vSpeedReal} {vSpeedError} {vSpeedTarget} m/s");
            _display.WriteLine($"Pitch: {pitchReal}° {rollReal}°");
        }
        public void RefreshDisplay()
        {
            MenuState menuState = GetMenuState();
            _display.NewBuffer();
            DrawHeader();
            DrawAutopilotReadout();

            if (menuState == MenuState.EditSelection || menuState == MenuState.Editing)
            {
                string editingTooltop = (editType != Autopilot.APModule.None) ? editType.ToString() : "___";
                _display.SeekLine(8);
                _display.WriteLine($"Edit Target: {editingTooltop}".PadRight(_display.Width));
            }
            if (menuState == MenuState.Editing) {
                _display.WriteLine($"Current: {_apData.GetTarget(editType).ToString().PadRight(6)} {AutopilotSuffixes[(int) editType]}");
                _display.WriteLine($"    New: {editTemp.ToString().PadRight(6)} {AutopilotSuffixes[(int) editType]}");
            }

            DrawControls(menuState);
            _display.FlipDisplay();
        }

        public void RefreshDisplayWithNewAPData(AutopilotData autopilotData)
        {
            _apData = autopilotData;
            RefreshDisplay();
        }
    }
}
﻿using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyThrust = Sandbox.ModAPI.Ingame.IMyThrust;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string ShipControllerBlockName = "[AP] Remote Control";
        private const string MenuDisplayBlockName = "[AP] Inset Button Panel";
        private const string ButtonBarBlockName = "[AP] Sci-Fi Four-Button Panel";
        


        private Autopilot _autopilot;
        private DisplayController _displayController;
        private ButtonBar _buttonBar;
        private AutopilotMainControl _autopilotMainControl;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            _autopilot = GetAutopilot(ShipControllerBlockName);
            _autopilot.AutopilotEnabled = true;
            _displayController = GetDisplayController(MenuDisplayBlockName);
            _buttonBar = GetButtonBar(ButtonBarBlockName);
            _autopilotMainControl = new AutopilotMainControl(_autopilot, _displayController, _buttonBar);
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
            try
            {
                if (((UpdateType)((uint)UpdateType.Update1 & (uint)updateSource) == UpdateType.Update1))
                {
                    _autopilot.Update(Runtime.LastRunTimeMs);
                    _autopilotMainControl.RefreshDisplay();
                    // Could possibly cause display to refresh twice if there is also an argument
                }

                if (argument.Length > 0)
                {
                    string[] arguments = argument.ToUpper().Split(' ');
                    if (arguments.Length > 3)
                    {
                        Me.CustomData += $"Invalid Argument: [{argument}]\n";
                        //throw new ArgumentException("Invalid argument");
                    }

                    if (arguments[0] == "INPUT_EVENT")
                    {
                        AutopilotMainControl.ButtonType buttonType;
                        if (!AutopilotMainControl.ButtonType.TryParse(arguments[1], out buttonType))
                        {
                            throw new ArgumentException("Invalid button type");
                        }

                        Me.CustomData = $"{Me.CustomData}\nType: {buttonType} AP Status: {_autopilot.AutopilotEnabled}";
                        Echo($"Input event: {buttonType}");
                        _autopilotMainControl.HandleInput(buttonType);
                    }
                }
            }
            catch (Exception ex)
            {
                Echo("Exception occured. See CustomData.");
                Me.CustomData = ex.ToString();
                _autopilot.AutopilotEnabled = false;
            }
        }

        private Autopilot GetAutopilot(string blockName)
        {
            IMyShipController shipController = GridTerminalSystem.GetBlockWithName(blockName) as IMyShipController;
            if (shipController == null)
            {
                throw new Exception("GetAutopilot: received null block");
            }
            List<IMyThrust> thrusters = new List<IMyThrust>();
            List<IMyGyro> gyros = new List<IMyGyro>();
            GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrusters);
            GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyros);
            
            
            Autopilot ap = new Autopilot(shipController, thrusters, gyros);
            return ap;
        }

        private DisplayController GetDisplayController(string blockName)
        {
            IMyTextSurfaceProvider textSurfaceProvider =
                GridTerminalSystem.GetBlockWithName(blockName) as IMyTextSurfaceProvider;
            if (textSurfaceProvider == null)
            {
                throw new Exception("GetDisplayController: textSurfaceProvider is null");
            }

            IMyTextSurface textSurface = textSurfaceProvider.GetSurface(0); // For main panel on inset button panel
            if (textSurface == null)
            {
                throw new Exception("GetDisplayController: surf is null");
            }

            DisplayController display = new DisplayController(textSurface);
            return display;
        }

        private ButtonBar GetButtonBar(string blockName)
        {
            IMyTextSurfaceProvider textSurfaceProvider =
                GridTerminalSystem.GetBlockWithName(blockName) as IMyTextSurfaceProvider;
            if (textSurfaceProvider == null)
            {
                throw new Exception("GetButtonBar: textSurfaceProvider is null");
            }

            ButtonBar buttonBar = new ButtonBar(textSurfaceProvider);
            return buttonBar;
        }
    }
}
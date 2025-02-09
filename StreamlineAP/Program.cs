using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private const string ShipControllerBlockName = "[AP] Remote Control";
        
        private static string APControllerBlockName = "[AP] Autopilot Controller";
        
        private IMyProgrammableBlock _apController;

        private Autopilot ap;
        
        private int _updateCounter = 0;
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            _apController = GridTerminalSystem.GetBlockWithName(APControllerBlockName) as IMyProgrammableBlock;
            if (_apController == null)
            {
                throw new NullReferenceException("Could not find an Autopilot Controller block");
            }
            ap = GetAutopilot(ShipControllerBlockName);
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
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
            try
            {
                if ((UpdateType)((uint)UpdateType.Update1 & (uint)updateSource) == UpdateType.Update1)
                {
                    ap.Update();
                    _updateCounter++;
                    if (_updateCounter >= 4)
                    {
                        SendAutopilotData();
                    }
                    // Could possibly cause display to refresh twice if there is also an argument
                }

                if (argument.Length > 0)
                {
                    string[] arguments = argument.ToUpper().Split(' ');
                    if (arguments.Length >= 2)
                    {
                        throw new ArgumentException("Invalid argument");
                    }

                    if (arguments[0] == "APCONTROL")
                    {
                        DoAutopilotCommand(argument.Substring(arguments[0].Length + 2));
                    }
                    else if (arguments[0] == "ABORT")
                    {
                        ap.ToggleAP = false;
                    }
                }
            }
            catch (Exception ex)
            {
                Echo("Exception occured. See CustomData.");
                Me.CustomData = ex.ToString();
                ap.ToggleAP = false;
            }
        }
        
        private Autopilot GetAutopilot(string blockName)
        {
            IMyShipController shipController = GridTerminalSystem.GetBlockWithName(blockName) as IMyShipController;
            if (shipController == null)
            {
                throw new Exception("GetAutopilot: received null block");
            }

            Autopilot ap = new Autopilot(shipController);
            return ap;
        }
        
        private void SendAutopilotData()
        {
            if (_apController == null)
            {
                throw new Exception("_apController is null");
            }
            List<TerminalActionParameter> tapList = new List<TerminalActionParameter>();
            tapList.Add(TerminalActionParameter.Get("apdata"));
            tapList.Add(TerminalActionParameter.Get(AutopilotData.SerializeFromAutopilot(ap)));
            _apController.ApplyAction("Run", tapList);
        }

        private void DoAutopilotCommand(string command)
        {
            
        }
    }
}
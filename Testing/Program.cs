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
        // Constants for block group and block names
        
        // Block references
        IMyTextSurface _textSurface;
        
        IMyShipController _shipController;
        
        public Program()
        {
            _shipController = GridTerminalSystem.GetBlockWithName("[AP] Remote Control") as IMyShipController;
            if (_shipController == null)
            {
                LogError("_shipController was null");
            }
            _textSurface = Me.GetSurface(0);
            if (_textSurface == null)
            {
                LogError("_textSurface was null");
            }
            _textSurface.Font = "Monospace";
            
            _textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
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
            // The main entry point of the script, invoked every time
            // one of the programmable block's Run actions are invoked,
            // or the script updates itself. The updateSource argument
            // describes where the update came from. Be aware that the
            // updateSource is a  bitfield  and might contain more than 
            // one update type.
            // 
            // The method itself is required, but the arguments above
            // can be removed if not needed.
            Log("Starting program");
            Me.CustomData = "";
            Vector3D gravity = _shipController.GetNaturalGravity().Normalized();
            Vector3D shipForward = _shipController.WorldMatrix.Forward;
            Vector3D shipUp = _shipController.WorldMatrix.Up;
            _textSurface.WriteText($"Gravity: {gravity}\nShip Forward: {shipForward}\nShip Up: {shipUp}");
            Log($"Gravity: {gravity}\nShip Forward: {shipForward}\nShip Up: {shipUp}");
        }
        
        void Log(string message)
        {
            string currentTime = DateTime.Now.ToString("hh:mm:ss");
            string formattedMessage = $"{Me.CustomData}{currentTime}: {message}\n";
            Me.CustomData = formattedMessage;
            Echo(formattedMessage);
        }
        
        void LogError(string message)
        {
            Log(message);
            throw new Exception(message);
        }
    }
}
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
        const string SiloWelderprefix = "Silo Welder";
        const string SiloPistonPrefix = "Silo Piston";
        const string SiloMergeBlockName = "Silo Merge Block";
        const string SiloProjectorBlockName = "Silo Projector";
        const string GuidanceProgrammableBlockName = "Ion Torpedo Guidance";
        private const float PistonSpeed = 3f;
        
        // Block references
        List<IMyShipWelder> _welders = new List<IMyShipWelder>();
        List<IMyPistonBase> _pistons = new List<IMyPistonBase>();
        IMyShipMergeBlock _mergeBlock;
        IMyTextSurface _textPanel; // Changed to IMyTextSurface for accessing text panel surfaces
        IMyProjector _projector;

        public Program()
        {
            InitializeBlocks();
            Log("Program initialized");
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
        }
        
        void InitializeBlocks()
        {
            List<IMyTerminalBlock> allBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocks(allBlocks);

            foreach (IMyTerminalBlock block in allBlocks)
            {
                /*if (block.CustomName.Length > 0)
                {
                    Log($"Block: {block.CustomName}");
                }*/
                
                if (block.CustomName.StartsWith(SiloWelderprefix))
                {
                    _welders.Add(block as IMyShipWelder);
                    Log("Found Silo Welder");
                }
                else if (block.CustomName.StartsWith(SiloPistonPrefix))
                {
                    _pistons.Add(block as IMyPistonBase);
                    Log("Found Silo Piston");
                }
                else if (block.CustomName.StartsWith(SiloMergeBlockName))
                {
                    _mergeBlock = (block as IMyShipMergeBlock);
                    Log("Found Silo Merge Block");
                }
                else if (block.CustomName.StartsWith(SiloProjectorBlockName))
                {
                    _projector = (block as IMyProjector);
                    Log("Found Silo Projector");
                }
            }
            
            _textPanel = Me.GetSurface(0) as IMyTextSurface; // Changed to access the text surface of the programmable block


            if (_welders == null)
            {
                Log("Error: Silo Welders group not found.");
            }
            if (_pistons == null)
            {
                Log("Error: Silo Pistons group not found.");
            }
            if (_mergeBlock == null)
            {
                Log("Error: Silo Merge Block not found.");
                Log($"Merge Dbg: {_mergeBlock.CustomName}");
            }
            if (_textPanel == null)
            {
                Log("Error: Text Panel not found.");
            }
            if (_projector == null)
            {
                Log("Error: Projector not found.");
            }

            Log($"{_textPanel != null}");
            if (_textPanel != null)
            {
                _textPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                _textPanel.WriteText("SE Test");
            }

            if (_welders == null || _pistons == null || _mergeBlock == null || _textPanel == null)
            {
                Log("Initialization failed. Fix block references.");
            }
        }
        
        void Log(string message)
        {
            string currentTime = DateTime.Now.ToString("hh:mm:ss");
            string formattedMessage = $"{Me.CustomData}{currentTime}: {message}\n";
            Me.CustomData = formattedMessage;
            Echo(formattedMessage);
        }
    }
}
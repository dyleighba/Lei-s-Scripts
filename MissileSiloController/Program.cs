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
using Sandbox.ModAPI;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;
using IMyBlockGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;
using IMyGyro = Sandbox.ModAPI.Ingame.IMyGyro;
using IMyMechanicalConnectionBlock = Sandbox.ModAPI.Ingame.IMyMechanicalConnectionBlock;
using IMyPistonBase = Sandbox.ModAPI.Ingame.IMyPistonBase;
using IMyProgrammableBlock = Sandbox.ModAPI.Ingame.IMyProgrammableBlock;
using IMyProjector = Sandbox.ModAPI.Ingame.IMyProjector;
using IMyRemoteControl = Sandbox.ModAPI.Ingame.IMyRemoteControl;
using IMyShipWelder = Sandbox.ModAPI.Ingame.IMyShipWelder;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using IMyTextPanel = Sandbox.ModAPI.Ingame.IMyTextPanel;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyThrust = Sandbox.ModAPI.Ingame.IMyThrust;
using IMyWarhead = Sandbox.ModAPI.Ingame.IMyWarhead;

namespace IngameScript
{
    enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
    enum ScriptState
    {
        Setup, // Initial state on script start
        Ready, // Everything is setup ready to build
        Build, // craft currently being built
        ReadyForLaunch, // craft build and ready for launch
        Launch,
        Error
    }
    
    partial class Program : MyGridProgram {
        // Constants for block group and block names
        const string SiloWelderPrefix = "Silo Welder";
        const string SiloPistonPrefix = "Silo Piston";
        const string SiloMergeBlockName = "Silo Merge Block";
        const string SiloProjectorBlockName = "Silo Projector";
        const string GuidanceProgrammableBlockName = "Ion Torpedo Guidance";
        const string LCDBlockName = "Silo LCD";
        private const float PistonSpeed = 1f;
        static LogLevel LoggingLevel = LogLevel.Debug;
        
        // Block references
        List<IMyShipWelder> _welders = new List<IMyShipWelder>();
        List<IMyPistonBase> _pistons = new List<IMyPistonBase>();
        IMyShipMergeBlock _mergeBlock;
        IMyTextSurface _textPanel; // Changed to IMyTextSurface for accessing text panel surfaces
        IMyProjector _projector;
        
        private ScriptState _scriptState = ScriptState.Setup;
        private bool _stateJustChanged = true;
        
        private string _outputMessage = "";

        Program()
        {
            Log("Started script");
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            InitializeBlocks();
        }
        
        void Main(string argument, UpdateType updateSource)
        {
            
            if (updateSource == UpdateType.Update10) // Will only run this code during a normal tick
            {
                switch (_scriptState)
                {
                    case ScriptState.Setup:
                        RunnerSetup(_stateJustChanged);
                        break;
                    case ScriptState.Ready:
                        break;
                    case ScriptState.Build:
                        RunnerBuild(_stateJustChanged);
                        break;
                    case ScriptState.ReadyForLaunch:
                        break;
                    case ScriptState.Launch:
                        RunnerLaunch(_stateJustChanged);
                        break;
                    case ScriptState.Error:
                        break;
                    default:
                        LogError("Invalid State");
                        break;
                }
                if (_stateJustChanged)
                {
                    _stateJustChanged = false;
                }

                _textPanel.WriteText($"Missle Silo Controller\nState: {_scriptState}\n{_outputMessage}");
            }
            
            if (argument.Length == 0) return;
            string lowerArgument = argument.ToLower();
            if (lowerArgument.Equals("build") && _scriptState == ScriptState.Ready)
            {
                SetState(ScriptState.Build);
            }
            else if (lowerArgument.Equals("launch") && _scriptState == ScriptState.ReadyForLaunch)
            {
                SetState(ScriptState.Launch);
            }
            else
            {
                Log($"Unknown argument: {lowerArgument}", LogLevel.Warning);
            }
        }

        void SetState(ScriptState state)
        {
            _scriptState = state;
            _stateJustChanged = true;
        }

        void PrintScr(string message)
        {
            _outputMessage = message;
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
                
                if (block.CustomName.StartsWith(SiloWelderPrefix))
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
                else if (block.CustomName.StartsWith(LCDBlockName))
                {
                    _textPanel = block as IMyTextSurface;
                    Log("Found LCD Panel");
                }
            }
            
            //_textPanel = Me.GetSurface(0) as IMyTextSurface; // Changed to access the text surface of the programmable block


            if (_welders == null)
            {
                LogError("Silo Welders group not found.");
            }
            if (_pistons == null)
            {
                LogError("Silo Pistons group not found.");
            }
            if (_mergeBlock == null)
            {
                LogError("Silo Merge Block not found.");
            }
            if (_textPanel == null)
            {
                LogError("Text Panel not found.");
            }
            if (_projector == null)
            {
                LogError("Projector not found.");
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
        
        // --------------------------------------------------
        // Main looping functions
        void RunnerSetup(bool justChanged)
        {
            foreach (var welder in _welders)
            {
                welder.Enabled = false;
            }
            // 2. Fully retract pistons
            foreach (var piston in _pistons)
            {
                piston.Velocity = -PistonSpeed; // Retract
            }

            if (IsFullyRetracted())
            {
                SetState(ScriptState.Ready);
            }
        }

        void RunnerBuild(bool justChanged)
        {
            if (justChanged)
            {
                _mergeBlock.Enabled = true;
                _projector.Enabled = true;
                foreach (var welder in _welders)
                {
                    welder.Enabled = true;
                }
            }
            
            PrintScr($"{_projector.TotalBlocks - _projector.RemainingBlocks}/{_projector.TotalBlocks}");
            
            if (_projector.RemainingBlocks == 0)
            {
                SetState(ScriptState.ReadyForLaunch);
                _projector.Enabled = false;
                foreach (var welder in _welders)
                {
                    welder.Enabled = false;
                }
            }
        }

        void RunnerLaunch(bool justChanged)
        {
            if (justChanged)
            {
                foreach (var piston in _pistons)
                {
                    piston.Velocity = PistonSpeed;
                }
            }

            if (IsFullyExtended()) // Maximum extension
            {
                IMyProgrammableBlock missileGuidance = GridTerminalSystem.GetBlockWithName(GuidanceProgrammableBlockName) as IMyProgrammableBlock;
                if (missileGuidance == null)
                {
                    PrintScr("Unable to find torpedo guidance computer.");
                    SetState(ScriptState.Error);
                    return;
                }
                PrintScr("Launch in progress...");
                Log($"launch command: {missileGuidance.TryRun("launch")}");
                _mergeBlock.Enabled = false;
                SetState(ScriptState.Setup);
                PrintScr("");
            }
        }
        
        void Log(string message, LogLevel level = LogLevel.Info)
        {
            if (message.ToLower().Length == 0)
            {
                Log("Sent empty message", LogLevel.Warning);
                return;
            }
            if (level < LoggingLevel) return;
            string currentTime = DateTime.Now.ToString("hh:mm:ss");
            string formattedMessage = $"{Me.CustomData}[{level.ToString()}]{currentTime}: {message}\n";
            Me.CustomData = formattedMessage;
            if (level < LogLevel.Warning)
            Echo(formattedMessage);
        }
        
        void LogError(string message)
        {
            Log(message, LogLevel.Error);
            throw new Exception(message);
        }

        bool IsFullyRetracted()
        {
            return _pistons.TrueForAll(p => p.CurrentPosition <= (p.MinLimit + 0.01));
        }
        bool IsFullyExtended()
        {
            return _pistons.TrueForAll(p => p.CurrentPosition >= (p.MaxLimit - 0.01));
        }
    }
}
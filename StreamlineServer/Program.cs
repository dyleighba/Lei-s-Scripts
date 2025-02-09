using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Text;
using Sandbox.ModAPI.Interfaces.Terminal;
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
        private static readonly List<string> MenuDefault = new List<string> { "Lighting", "Helm Control", "Security", "Atmospherics" };


        private readonly Dictionary<string, StreamlineTerminal> _streamlineTerminals = new Dictionary<string, StreamlineTerminal>();

        private const float DeltaTime = 1.6666667f; // Change if runtime frequency changes, this is for 100 ticks
        private const string ShipName = "USS Armstrong";

        void DecomposeBlockName(string blockName, out string ship, out string location, out string blockType)
        {
            List<string> parts = blockName.Split(',').ToList();
            if (parts.Count >= 1)
            {
                ship = parts[0].Trim();
            }
            else
            {
                ship = null;
            }

            if (parts.Count >= 2)
            {
                location = parts[1].Trim();
            }
            else
            {
                location = null;
            }

            if (parts.Count >= 3)
            {
                blockType = parts[2].Trim();
            }
            else
            {
                blockType = null;
            }
        }

        private void PopulateStreamlineTerminals()
        {
            List<IMyTextSurfaceProvider> blocks = new List<IMyTextSurfaceProvider>();
            GridTerminalSystem.GetBlocksOfType<IMyTextSurfaceProvider>(blocks);
            foreach (var block in blocks)
            {
                string ship;
                string location;
                string blockType;
                IMyTerminalBlock tBlock = block as IMyTerminalBlock;
                if (tBlock == null)
                {
                    continue;
                }
                DecomposeBlockName(tBlock.CustomName, out ship, out location, out blockType);
                if (ship == ShipName && blockType == "Streamline Terminal")
                {
                    if (_streamlineTerminals.ContainsKey(location))
                    {
                        Echo("Detected second streamline terminal at: " + location);
                    }
                    else
                    {
                        StreamlineTerminal terminal = new StreamlineTerminal(tBlock, block.GetSurface(0), location);
                        terminal.ReplaceMenu(MenuDefault);
                        terminal.UpdateDisplay();
                        _streamlineTerminals[location] = terminal;    
                    }
                }
            }
        }
        
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            
            PopulateStreamlineTerminals();
            Echo($"Found {_streamlineTerminals.Count} terminal/s.");
            
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
            if (updateSource == UpdateType.Update100 || updateSource == UpdateType.Update10 || updateSource == UpdateType.Update1)
            {
                foreach (var terminal in _streamlineTerminals.Values) {
                    terminal.ActionIdleTimer(DeltaTime);
                }
            }
            else if (argument.Trim().Length > 0)
            {
                // button_press,Lower Bridge,Return
                List<string> parts = argument.Trim().Split(',').ToList();
                if (parts[0].Trim() != "button_press") return;
                string location = parts[1].Trim();
                if (_streamlineTerminals.ContainsKey(location))
                {
                    string buttonType = parts[2].Trim();
                    switch (buttonType)
                    {
                        case "Return":
                            _streamlineTerminals[location].ActionReturn();
                            break;
                        case "Up":
                            _streamlineTerminals[location].ActionUp();
                            break;
                        case "Down":
                            _streamlineTerminals[location].ActionDown();
                            break;
                        default:
                            Echo("Unknown button type: " + buttonType);
                            break;
                    }
                }
            }
        }

    }
    
    
    
    class StreamlineTerminal
    {
        private readonly IMyTerminalBlock _terminalBlock;
        private readonly IMyTextSurface _textSurface;
        private float _lastActionDelta = 0;
        private bool _isIdle = true;

        private const float _idleCutoff = 10.0f; // In seconds earliest Idle trigger, 
        // resolution limited to runtime frequency
        
        List<string> menuItems = new List<string>();
        int highlightedIndex = 0;
        private string terminalLocation = "";

        public StreamlineTerminal(IMyTerminalBlock streamlineTerminalBlock, IMyTextSurface streamlineTerminalTextSurface, string terminalLocation)
        {
            _terminalBlock = streamlineTerminalBlock;
            _textSurface = streamlineTerminalTextSurface;
            this.terminalLocation = terminalLocation;
        }

        public void ReplaceMenu(List<string> newMenuItems)
        {
            menuItems = new List<string>(newMenuItems);
            highlightedIndex = 0;
        }
        
        public void ActionIdleTimer(float deltaTime)
        {
            if (_isIdle) return;
            _lastActionDelta += deltaTime;
            if (_idleCutoff < _lastActionDelta)
            {
                _isIdle = true;
                _lastActionDelta = 0;
                UpdateDisplay();
            }
        }
        
        public void ActionReturn()
        {
            _lastActionDelta = 0;
            if (_isIdle)
            {
                _isIdle = false;
            }
            else
            {
                // place to put action code like changing something on the ship or navigating to another menu
            }
            UpdateDisplay();
        }

        public void ActionUp()
        {
            _lastActionDelta = 0;
            if (_isIdle)
            {
                _isIdle = false;
            }
            else
            {
                highlightedIndex = (highlightedIndex - 1) % menuItems.Count;
            }
            UpdateDisplay();
        }

        public void ActionDown()
        {
            _lastActionDelta = 0;
            if (_isIdle)
            {
                _isIdle = false;
            }
            else
            {
                highlightedIndex = (highlightedIndex + 1) % menuItems.Count;
            }
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (_isIdle)
            {
                RenderHelper.RenderIdleScreen(_textSurface, terminalLocation);
            }
            else
            {
                RenderHelper.RenderVerticalMenu(_textSurface, menuItems, highlightedIndex, terminalLocation);
            }
        }
        
    }

    static class RenderHelper
    {
        private static Color ThemeBlack = new Color(0.03125f, 0.03125f, 0.03125f, 1.0f);
        private static Color ThemeBlue = new Color(0, 0.5882352941176470f, 1f, 1.0f);
        private static Color ThemeWhite = new Color(1f, 1f, 1f, 1.0f);

        private static void SetupDisplay(IMyTextSurface surface)
        {
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.Alignment = TextAlignment.CENTER;
            surface.Font = "Monospace";
            surface.FontSize = 0.9f;
            surface.FontColor = ThemeWhite;
            surface.BackgroundColor = ThemeBlack;
        }
        public static void RenderIdleScreen(IMyTextSurface surface, string terminalLocation)
        {
            // 14 lines at 0.9f FontSize
            SetupDisplay(surface);
            surface.FontSize = 1.5f;
            
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("");
            builder.AppendLine("USS Armstrong");
            builder.AppendLine("");
            builder.AppendLine(terminalLocation);
            builder.AppendLine("");
            surface.WriteText(builder, false);
        }
        
        public static void RenderVerticalMenu(IMyTextSurface surface, List<string> menuItems, int highlightedIndex, string terminalLocation)
        {
            if (surface == null || menuItems == null || menuItems.Count == 0)
                return;

            // Configure surface for sprites
            SetupDisplay(surface);
            
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("");
            builder.AppendLine("USS Armstrong");
            builder.AppendLine("");
            builder.AppendLine(terminalLocation);
            builder.AppendLine("");
            builder.AppendLine("");
            builder.AppendLine("");
            int maxLength = 0;
            for (int i = 0; i < menuItems.Count; i++)
            {
                if (menuItems[i].Length > maxLength) maxLength = menuItems[i].Length;
            }
            for (int i = 0; i < menuItems.Count; i++)
            {
                string item = menuItems[i];
                string prefix = "   ";
                string suffix = "   ";
                if (i == highlightedIndex)
                {
                    prefix = ">  ";
                    suffix = "   ";
                }
                builder.AppendLine($"{prefix}{item.PadRight(maxLength)}{suffix}");
            }
            surface.WriteText(builder, false);
        }
    }
}
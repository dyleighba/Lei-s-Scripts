using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using EmptyKeys.UserInterface.Generated.ContractsBlockView_Gamepad_Bindings;
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
    public class DisplayController
    {
        private static int _width = 27;
        private static int _height = 13;
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        private readonly IMyTextSurface textSurface;
        private int _lineIndex = 0;
        private string[] lines = new string[_height];

        public DisplayController(IMyTextSurface textSurface)
        {
            textSurface.Font = "Monospace";
            textSurface.ContentType = ContentType.TEXT_AND_IMAGE;
            textSurface.FontSize = 0.95f;
            textSurface.TextPadding = 0.2f;
            textSurface.Alignment = TextAlignment.LEFT;
            textSurface.BackgroundColor = new Color(0.03f, 0.03f, 0.03f); // Dull grey for screen effect
            textSurface.FontColor = new Color(0f, 0.588f, 1f); // Starfleet blue
            this.textSurface = textSurface;
            NewBuffer();
        }
        public void FlipDisplay()
        {
            string outputString = "";
            foreach (var line in lines)
            {
                if (line.Length != _width)
                {
                    ErrorOut("flipDisplay: line length is incorrect, probably something was written wrong");
                }

                string maybeNL = "";
                if (outputString.Length != 0)
                {
                    maybeNL = "\n";
                }

                outputString = $"{outputString}{maybeNL}{line}";
            }
            
            if (textSurface == null)
            {
                ErrorOut("flipDisplay: textSurface is null");
            }
            textSurface.WriteText(outputString, false);
        }
        public void WriteLine(string line)
        {
            string fixedLine = line.Replace("\n", "").PadRight(_width).Substring(0, _width);
            lines[_lineIndex] = fixedLine;
            _lineIndex = (_lineIndex + 1) % _height;
        }
        public void NewBuffer()
        {
            for (int i = 0; i < _height; i++)
            {
                lines[i] = new string(' ', _width);
            }
            _lineIndex = 0;
        }
        public void SeekLine(int lineIndex)
        {
            if (lineIndex < 0 || lineIndex >= _height)
            {
                ErrorOut($"seekLine: requested invalid index {lineIndex}");
            }
            _lineIndex = (lineIndex) % _height;
        }
        private void ErrorOut(string message)
        {
            string errMsg = $"Error in DisplayController!\n\n{message}";
            textSurface.WriteText(errMsg);
            throw new Exception(errMsg);
        }
    }
}
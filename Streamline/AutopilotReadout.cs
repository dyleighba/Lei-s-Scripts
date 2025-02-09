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
    public class AutopilotReadout
    {
        private IMyGridTerminalSystem _grid;
        private Autopilot _ap;
        private IMyTextSurface _readoutHeading;
        private IMyTextSurface _readoutAltitude;
        private IMyTextSurface _readoutSpeed;
        private IMyTextSurface _readoutVSpeed;
        private IMyTextSurface _HUDLeft;
        private IMyTextSurface _HUDCenter;
        private IMyTextSurface _HUDRight;

        private void VerifyBlocks()
        {
            if (_grid == null) throw new NullReferenceException("_grid is null");
            if (_ap == null) throw new NullReferenceException("_ap is null");
            if (_readoutHeading == null) throw new NullReferenceException("_readoutHeading is null");
            SetupSurface(_readoutHeading);
            if (_readoutAltitude == null) throw new NullReferenceException("_readoutAltitude is null");
            SetupSurface(_readoutAltitude);
            if (_readoutSpeed == null) throw new NullReferenceException("_readoutSpeed is null");
            SetupSurface(_readoutSpeed);
            if (_readoutVSpeed == null) throw new NullReferenceException("_readoutVSpeed is null");
            SetupSurface(_readoutVSpeed);
            if (_HUDLeft == null) throw new NullReferenceException("_HUDLeft is null");
            if (_HUDCenter == null) throw new NullReferenceException("_HUDCenter is null");
            if (_HUDRight == null) throw new NullReferenceException("_HUDRight is null");
        }
        
        private void SetupSurface(IMyTextSurface surface)
        {
            surface.Font = "Monospace";
            surface.ContentType = ContentType.TEXT_AND_IMAGE;
            surface.FontSize = 1.9f;
            surface.TextPadding = 0.5f;
            surface.Alignment = TextAlignment.CENTER;
            surface.BackgroundColor = new Color(0.03f, 0.03f, 0.03f);
            surface.FontColor = new Color(1f, 1f, 1f);
        }
        
        public AutopilotReadout(IMyGridTerminalSystem grid, Autopilot ap)
        {
            _grid = grid;
            _ap = ap;
            foreach (var taggedBlock in TaggedBlock.GetAllTaggedBlocks(_grid))
            {
                //taggedBlock.Block.CustomData = $"{taggedBlock.Block.CustomData}\n{taggedBlock.Block.CustomName} - [{string.Join(", ", taggedBlock.Tags)}] - {taggedBlock.Location}\nAutopilot match: {taggedBlock.HasTag("Autopilot")}\n";
                if (taggedBlock.HasTag("Autopilot"))
                {
                    if (taggedBlock.HasTag("Heading"))
                    {
                        _readoutHeading = (taggedBlock.Block as IMyTextSurfaceProvider).GetSurface(0);
                    }
                    else if (taggedBlock.HasTag("Altitude"))
                    {
                        _readoutAltitude = (taggedBlock.Block as IMyTextSurfaceProvider).GetSurface(0);
                    }
                    else if (taggedBlock.HasTag("Speed"))
                    {
                        _readoutSpeed = (taggedBlock.Block as IMyTextSurfaceProvider).GetSurface(0);
                    }
                    else if (taggedBlock.HasTag("VSpeed"))
                    {
                        _readoutVSpeed = (taggedBlock.Block as IMyTextSurfaceProvider).GetSurface(0);
                    }
                    else if (taggedBlock.HasTag("HUD"))
                    {
                        if (taggedBlock.HasTag("Left"))
                        {
                            _HUDLeft = taggedBlock.Block as IMyTextSurface;
                        }
                        else if (taggedBlock.HasTag("Center"))
                        {
                            _HUDCenter = taggedBlock.Block as IMyTextSurface;
                        }
                        else if (taggedBlock.HasTag("Right"))
                        {
                            _HUDRight = taggedBlock.Block as IMyTextSurface;
                        }
                    }
                }
            }
            VerifyBlocks();
        }

        private void WriteReadout(IMyTextSurface surface, string label, string suffix, float targetValue, float currentValue)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("");
            sb.AppendLine($"{label}");
            sb.AppendLine("");
            sb.AppendLine($" Target: {Math.Round(targetValue, 2)}{suffix}");
            sb.AppendLine($"Current: {Math.Round(currentValue, 2)}{suffix}");
            sb.AppendLine("");
            sb.AppendLine($"  Error: {Math.Round(currentValue - targetValue, 2)}{suffix}");
            surface.WriteText(sb.ToString(), false);
        }
        
        private string GetPowerStatus(bool state) => state ? "On" : "Off";
        
        public void RefreshReadouts()
        {
            VerifyBlocks();
            WriteReadout(_readoutHeading, $"Heading - {GetPowerStatus(_ap.ToggleHDG)}","*", _ap.TargetHDG, _ap.CurrentHDG);
            WriteReadout(_readoutAltitude, $"Altitude - {GetPowerStatus(_ap.ToggleALT)}", " m", _ap.TargetALT, _ap.CurrentALT);
            WriteReadout(_readoutSpeed, $"Horizontal Speed - {GetPowerStatus(_ap.ToggleSPD)}"," m/s", _ap.TargetSPD, _ap.CurrentSPD);
            WriteReadout(_readoutVSpeed, $"Vertical Speed - {GetPowerStatus(_ap.ToggleVS)}"," m/s", _ap.TargetVS, _ap.CurrentVS);
        }
    }
}
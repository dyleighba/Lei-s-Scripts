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
using LitJson;
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
    public struct AutopilotData
    {
        public bool AutopilotEnabled { get; set; }
        public double PitchCurrent { get; set; }
        public double RollCurrent { get; set; }

        public bool AltitudeEnabled { get; set; }
        public double AltitudeCurrent { get; set; }
        public double AltitudeTarget { get; set; }
        public double AltitudeError { get; set; }

        public bool HeadingEnabled { get; set; }
        public double HeadingCurrent { get; set; }
        public double HeadingTarget { get; set; }
        public double HeadingError { get; set; }

        public bool SpeedEnabled { get; set; }
        public double SpeedCurrent { get; set; }
        public double SpeedTarget { get; set; }
        public double SpeedError { get; set; }

        public bool VerticalSpeedEnabled { get; set; }
        public double VerticalSpeedCurrent { get; set; }
        public double VerticalSpeedTarget { get; set; }
        public double VerticalSpeedError { get; set; }

        public static AutopilotData ParseAutopilotData(string serializedData)
        {
            var ini = new MyIni();
            if (!ini.TryParse(serializedData))
                throw new ArgumentException("Invalid INI format");

            return new AutopilotData
            {
                AutopilotEnabled = ini.Get("Autopilot", "Enabled").ToBoolean(),
                PitchCurrent = ini.Get("Pitch", "Current").ToDouble(),
                RollCurrent = ini.Get("Roll", "Current").ToDouble(),
                
                AltitudeEnabled = ini.Get("Altitude", "Enabled").ToBoolean(),
                AltitudeCurrent = ini.Get("Altitude", "Current").ToDouble(),
                AltitudeTarget = ini.Get("Altitude", "Target").ToDouble(),
                AltitudeError = ini.Get("Altitude", "Error").ToDouble(),
                
                HeadingEnabled = ini.Get("Heading", "Enabled").ToBoolean(),
                HeadingCurrent = ini.Get("Heading", "Current").ToDouble(),
                HeadingTarget = ini.Get("Heading", "Target").ToDouble(),
                HeadingError = ini.Get("Heading", "Error").ToDouble(),
                
                SpeedEnabled = ini.Get("Speed", "Enabled").ToBoolean(),
                SpeedCurrent = ini.Get("Speed", "Current").ToDouble(),
                SpeedTarget = ini.Get("Speed", "Target").ToDouble(),
                SpeedError = ini.Get("Speed", "Error").ToDouble(),
                
                VerticalSpeedEnabled = ini.Get("VerticalSpeed", "Enabled").ToBoolean(),
                VerticalSpeedCurrent = ini.Get("VerticalSpeed", "Current").ToDouble(),
                VerticalSpeedTarget = ini.Get("VerticalSpeed", "Target").ToDouble(),
                VerticalSpeedError = ini.Get("VerticalSpeed", "Error").ToDouble()
            };
        }

        public string Serialize()
        {
            var ini = new MyIni();
            ini.Set("Autopilot", "Enabled", AutopilotEnabled);
            ini.Set("Pitch", "Current", PitchCurrent);
            ini.Set("Roll", "Current", RollCurrent);
            
            ini.Set("Altitude", "Enabled", AltitudeEnabled);
            ini.Set("Altitude", "Current", AltitudeCurrent);
            ini.Set("Altitude", "Target", AltitudeTarget);
            ini.Set("Altitude", "Error", AltitudeError);
            
            ini.Set("Heading", "Enabled", HeadingEnabled);
            ini.Set("Heading", "Current", HeadingCurrent);
            ini.Set("Heading", "Target", HeadingTarget);
            ini.Set("Heading", "Error", HeadingError);
            
            ini.Set("Speed", "Enabled", SpeedEnabled);
            ini.Set("Speed", "Current", SpeedCurrent);
            ini.Set("Speed", "Target", SpeedTarget);
            ini.Set("Speed", "Error", SpeedError);
            
            ini.Set("VerticalSpeed", "Enabled", VerticalSpeedEnabled);
            ini.Set("VerticalSpeed", "Current", VerticalSpeedCurrent);
            ini.Set("VerticalSpeed", "Target", VerticalSpeedTarget);
            ini.Set("VerticalSpeed", "Error", VerticalSpeedError);
            
            return ini.ToString();
        }

        public static string SerializeFromAutopilot(Autopilot ap)
        {
            var ini = new MyIni();
            ini.Set("Autopilot", "Enabled", ap.ToggleAP);
            ini.Set("Pitch", "Current", ap.CurrentPTH);
            ini.Set("Roll", "Current", ap.CurrentROL);
            
            ini.Set("Altitude", "Enabled", ap.ToggleALT);
            ini.Set("Altitude", "Current", ap.CurrentALT);
            ini.Set("Altitude", "Target", ap.TargetALT);
            ini.Set("Altitude", "Error", ap.ErrorALT);
            
            ini.Set("Heading", "Enabled", ap.ToggleHDG);
            ini.Set("Heading", "Current", ap.CurrentHDG);
            ini.Set("Heading", "Target", ap.TargetHDG);
            ini.Set("Heading", "Error", ap.ErrorHDG);
            
            ini.Set("Speed", "Enabled", ap.ToggleSPD);
            ini.Set("Speed", "Current", ap.CurrentSPD);
            ini.Set("Speed", "Target", ap.TargetSPD);
            ini.Set("Speed", "Error", ap.ErrorSPD);
            
            ini.Set("VerticalSpeed", "Enabled", ap.ToggleVS);
            ini.Set("VerticalSpeed", "Current", ap.CurrentVS);
            ini.Set("VerticalSpeed", "Target", ap.TargetVS);
            ini.Set("VerticalSpeed", "Error", ap.ErrorVS);
            
            return ini.ToString();
        }
    }
}
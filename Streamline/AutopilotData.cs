using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    public struct AutopilotData
    {
        public bool AutopilotEnabled { get; private set; }
        public double PitchCurrent { get; private set; }
        public double RollCurrent { get; private set; }

        public bool AltitudeEnabled { get; private set; }
        public double AltitudeCurrent { get; private set; }
        public double AltitudeTarget { get; private set; }
        public double AltitudeError { get; private set; }

        public bool HeadingEnabled { get; private set; }
        public double HeadingCurrent { get; private set; }
        public double HeadingTarget { get; private set; }
        public double HeadingError { get; private set; }

        public bool SpeedEnabled { get; private set; }
        public double SpeedCurrent { get; private set; }
        public double SpeedTarget { get; private set; }
        public double SpeedError { get; private set; }

        public bool VerticalSpeedEnabled { get; private set; }
        public double VerticalSpeedCurrent { get; private set; }
        public double VerticalSpeedTarget { get; private set; }
        public double VerticalSpeedError { get; private set; }
        
        public IMyProgrammableBlock AutopilotBlock { get; private set; }

        public static AutopilotData ParseAutopilotData(string serializedData, IMyProgrammableBlock autopilotBlock)
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
                VerticalSpeedError = ini.Get("VerticalSpeed", "Error").ToDouble(),
                    
                AutopilotBlock = autopilotBlock
            };
        }
        
        private void SendAutopilotCommand(string command)
        {
            if (AutopilotBlock == null)
            {
                throw new NullReferenceException("SendAutopilotCommand: AutopilotBlock is null");
            }
            List<TerminalActionParameter> tapList = new List<TerminalActionParameter>();
            tapList.Add(TerminalActionParameter.Get("apcontrol"));
            tapList.Add(TerminalActionParameter.Get(command));
            AutopilotBlock.ApplyAction("Run", tapList);
        }
        
        public bool GetToggle(Autopilot.APModule apModule)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    return AltitudeEnabled;
                case Autopilot.APModule.HDG:
                    return HeadingEnabled;
                case Autopilot.APModule.SPD:
                    return SpeedEnabled;
                case Autopilot.APModule.VS:
                    return VerticalSpeedEnabled;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public double GetCurrent(Autopilot.APModule apModule)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    return AltitudeCurrent;
                case Autopilot.APModule.HDG:
                    return HeadingCurrent;
                case Autopilot.APModule.SPD:
                    return SpeedCurrent;
                case Autopilot.APModule.VS:
                    return VerticalSpeedCurrent;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public double GetTarget(Autopilot.APModule apModule)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    return AltitudeTarget;
                case Autopilot.APModule.HDG:
                    return HeadingTarget;
                case Autopilot.APModule.SPD:
                    return SpeedTarget;
                case Autopilot.APModule.VS:
                    return VerticalSpeedTarget;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public double GetError(Autopilot.APModule apModule)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    return AltitudeError;
                case Autopilot.APModule.HDG:
                    return HeadingError;
                case Autopilot.APModule.SPD:
                    return SpeedError;
                case Autopilot.APModule.VS:
                    return VerticalSpeedError;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        public void SetTarget(Autopilot.APModule apModule, double target)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    AltitudeTarget = target;
                    SendAutopilotCommand($"SetTarget ALT {Math.Round(target)}");
                    break;
                case Autopilot.APModule.HDG:
                    HeadingTarget = target;
                    SendAutopilotCommand($"SetTarget HDG {Math.Round(target)}");
                    break;
                case Autopilot.APModule.SPD:
                    SpeedTarget = target;
                    SendAutopilotCommand($"SetTarget SPD {Math.Round(target)}");
                    break;
                case Autopilot.APModule.VS:
                    VerticalSpeedTarget = target;
                    SendAutopilotCommand($"SetTarget VS {Math.Round(target)}");
                    break;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public void ToggleModule(Autopilot.APModule apModule)
        {
            switch (apModule)
            {
                case Autopilot.APModule.ALT:
                    AltitudeEnabled = !AltitudeEnabled;
                    SendAutopilotCommand($"SetEnabled ALT {AltitudeEnabled}");
                    break;
                case Autopilot.APModule.HDG:
                    HeadingEnabled = !HeadingEnabled;
                    SendAutopilotCommand($"SetEnabled HDG {HeadingEnabled}");
                    break;
                case Autopilot.APModule.SPD:
                    SpeedEnabled = !SpeedEnabled;
                    SendAutopilotCommand($"SetEnabled SPD {SpeedEnabled}");
                    break;
                case Autopilot.APModule.VS:
                    VerticalSpeedEnabled = !VerticalSpeedEnabled;
                    SendAutopilotCommand($"SetEnabled VS {VerticalSpeedEnabled}");
                    break;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }

        public void ToggleAP()
        {
            AutopilotEnabled = !AutopilotEnabled;
            SendAutopilotCommand($"SetAutopilot {SpeedEnabled}");
        }
    }
}
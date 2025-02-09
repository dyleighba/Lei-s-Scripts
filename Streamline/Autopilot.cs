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
    public class Autopilot
    {
        public enum Module
        {
            Altitude,
            Heading,
            Speed,
            VerticalSpeed,
            None
        }

        private readonly IMyShipController _shipController;
        private readonly AutopilotOperations _autopilotOperations;

        public bool AutopilotEnabled;
        public bool AltitudeEnabled;
        public bool VerticalSpeedEnabled;
        public bool HeadingEnabled;
        public bool SpeedEnabled;

        public double AltitudeTarget;
        public double VerticalSpeedTarget;
        public double HeadingTarget;
        public double SpeedTarget;

        public Autopilot(IMyShipController shipController)
        {
            _shipController = shipController;
            _autopilotOperations = new AutopilotOperations(this);
        }

        public bool GetModuleState(Module module)
        {
            switch (module)
            {
                case Module.Altitude: return AltitudeEnabled;
                case Module.Heading: return HeadingEnabled;
                case Module.Speed: return SpeedEnabled;
                case Module.VerticalSpeed: return VerticalSpeedEnabled;
                default: throw new Exception("Unknown Autopilot Module");
            }
        }

        public double GetCurrentValue(Module module)
        {
            switch (module)
            {
                case Module.Altitude: return CurrentAltitude;
                case Module.Heading: return CurrentHeading;
                case Module.Speed: return CurrentSpeed;
                case Module.VerticalSpeed: return CurrentVerticalSpeed;
                default: throw new Exception("Unknown Autopilot Module");
            }
        }

        public double GetTarget(Module module)
        {
            switch (module)
            {
                case Module.Altitude: return AltitudeTarget;
                case Module.Heading: return HeadingTarget;
                case Module.Speed: return SpeedTarget;
                case Module.VerticalSpeed: return VerticalSpeedTarget;
                default: throw new Exception("Unknown Autopilot Module");
            }
        }

        public double GetError(Module module)
        {
            switch (module)
            {
                case Module.Altitude: return AltitudeError;
                case Module.Heading: return HeadingError;
                case Module.Speed: return SpeedError;
                case Module.VerticalSpeed: return VerticalSpeedError;
                default: throw new Exception("Unknown Autopilot Module");
            }
        }

        public void ToggleModule(Module module)
        {
            switch (module)
            {
                case Module.Altitude:
                    AltitudeEnabled = !AltitudeEnabled;
                    break;
                case Module.Heading:
                    HeadingEnabled = !HeadingEnabled;
                    break;
                case Module.Speed:
                    SpeedEnabled = !SpeedEnabled;
                    break;
                case Module.VerticalSpeed:
                    VerticalSpeedEnabled = !VerticalSpeedEnabled;
                    break;
                default:
                    throw new Exception("Unknown Autopilot Module");
            }
        }

        public void SetTarget(Module module, double target)
        {
            switch (module)
            {
                case Module.Altitude:
                    AltitudeTarget = target;
                    break;
                case Module.Heading:
                    HeadingTarget = target;
                    break;
                case Module.Speed:
                    SpeedTarget = target;
                    break;
                case Module.VerticalSpeed:
                    VerticalSpeedTarget = target;
                    break;
                default:
                    throw new Exception("Unknown Autopilot Module");
            }
        }

        public double CurrentAltitude {
            get
            {
                double altitude;
                if (_shipController.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out altitude))
                {
                    return altitude;
                }
                return -1;
            }
        }
    
        public double CurrentVerticalSpeed => -Vector3D.Dot(_shipController.GetShipVelocities().LinearVelocity, Vector3D.Normalize(_shipController.GetNaturalGravity()));
        public double CurrentSpeed => _shipController.GetShipVelocities().LinearVelocity.Length();
        
        public double CurrentHeading
        {
            get
            {
                Vector3D gravity = _shipController.GetNaturalGravity();
                if (gravity.LengthSquared() == 0) return -1;
                
                gravity = Vector3D.Normalize(gravity);
                Vector3D forward = _shipController.WorldMatrix.Forward;
                Vector3D planetEast = Vector3D.Normalize(Vector3D.Cross(Vector3D.Up, gravity));
                Vector3D planetNorth = Vector3D.Normalize(Vector3D.Cross(gravity, planetEast));
                Vector3D projectedForward = Vector3D.Normalize(Vector3D.Reject(forward, gravity));
                
                double headingRad = Math.Acos(MathHelper.Clamp(Vector3D.Dot(projectedForward, planetNorth), -1.0, 1.0));
                double headingDeg = MathHelper.ToDegrees(headingRad);
                if (Vector3D.Dot(projectedForward, planetEast) > 0)
                    headingDeg = 360 - headingDeg;
                
                return ((headingDeg - 180) % 360 + 360) % 360;
            }
        }
        
        public double AltitudeError => CurrentAltitude - AltitudeTarget;
        public double HeadingError => (CurrentHeading - HeadingTarget + 180) % 360 - 180;
        public double SpeedError => CurrentSpeed - SpeedTarget;
        public double VerticalSpeedError => CurrentVerticalSpeed - VerticalSpeedTarget;
        
        public double CurrentPitch => -1;
        public double CurrentRoll => -1;
        
        
        // Autopilot operations
        public void Update(double deltaTime) => _autopilotOperations.Update(deltaTime);
    }
}
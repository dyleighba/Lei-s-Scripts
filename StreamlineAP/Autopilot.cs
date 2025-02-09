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
        public enum APModule
        {
            ALT,
            HDG,
            SPD,
            VS,
            None
        }
        
        private readonly IMyShipController _shipController;
        
        public bool ToggleAP;
        public bool ToggleALT;
        public bool ToggleVS;
        public bool ToggleHDG;
        public bool ToggleSPD;
        
        public float TargetALT;
        public float TargetVS;
        public float TargetHDG;
        public float TargetSPD;
        
        public Autopilot(IMyShipController shipController)
        {
            _shipController = shipController;
        }
        
        public bool GetToggle(APModule apModule)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    return ToggleALT;
                case APModule.HDG:
                    return ToggleHDG;
                case APModule.SPD:
                    return ToggleSPD;
                case APModule.VS:
                    return ToggleVS;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public float GetCurrent(APModule apModule)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    return CurrentALT;
                case APModule.HDG:
                    return CurrentHDG;
                case APModule.SPD:
                    return CurrentSPD;
                case APModule.VS:
                    return CurrentVS;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public float GetTarget(APModule apModule)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    return TargetALT;
                case APModule.HDG:
                    return TargetHDG;
                case APModule.SPD:
                    return TargetSPD;
                case APModule.VS:
                    return TargetVS;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public float GetError(APModule apModule)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    return ErrorALT;
                case APModule.HDG:
                    return ErrorHDG;
                case APModule.SPD:
                    return ErrorSPD;
                case APModule.VS:
                    return ErrorVS;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public void ToggleModule(APModule apModule)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    ToggleALT = !ToggleALT;
                    break;
                case APModule.HDG:
                    ToggleHDG = !ToggleHDG;
                    break;
                case APModule.SPD:
                    ToggleSPD  = !ToggleSPD;
                    break;
                case APModule.VS:
                    ToggleVS = !ToggleVS;
                    break;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }
        
        public void SetTarget(APModule apModule, float target)
        {
            switch (apModule)
            {
                case APModule.ALT:
                    TargetALT = target;
                    break;
                case APModule.HDG:
                    TargetHDG = target;
                    break;
                case APModule.SPD:
                    TargetSPD = target;
                    break;
                case APModule.VS:
                    TargetVS = target;
                    break;
                default:
                    throw new Exception("Unknown AP Module");
            }
        }

        public float CurrentALT
        {
            get
            {
                // TODO Return actual altitude
                MyPlanetElevation elevationType = MyPlanetElevation.Sealevel;
                double result;
                double altitude = _shipController.GetNaturalGravity().LengthSquared() > 0 
                    ? _shipController.TryGetPlanetElevation(elevationType, out result) ? result : -1 
                    : -1;
                return (float) altitude;
            }
        }
        
        private float GetAngleBetweenVectors(Vector3D vecA, Vector3D vecB)
        {
            vecA = Vector3D.Normalize(vecA); // Normalize to get only direction
            vecB = Vector3D.Normalize(vecB);

            double dotProduct = Vector3D.Dot(vecA, vecB);
            double angleRad = Math.Acos(MathHelper.Clamp(dotProduct, -1.0, 1.0)); // Prevent NaN errors from precision issues
            double angleDeg = MathHelper.ToDegrees(angleRad); // Convert to degrees

            return (float) angleDeg;
        }
        
        public float CurrentVS
        {
            get
            {
                // Get the gravity direction (normalized)
                Vector3D gravity = _shipController.GetNaturalGravity();
                if (gravity.LengthSquared() == 0) 
                    return 0; // No gravity present

                gravity = Vector3D.Normalize(gravity);

                // Get the velocity of the craft
                Vector3D velocity = _shipController.GetShipVelocities().LinearVelocity;

                // Project velocity onto gravity vector to get the vertical component
                double verticalVelocity = -Vector3D.Dot(velocity, gravity);

                return (float) verticalVelocity; // Positive if ascending, negative if descending
            }
        }

        public float CurrentHDG
        {
            get
            {
                // TODO fix slight offset to the right
                // Get the gravity direction (normalized)
                Vector3D gravity = _shipController.GetNaturalGravity();
                if (gravity.LengthSquared() == 0) 
                    return -1; // Not in a gravity well

                gravity = Vector3D.Normalize(gravity);

                // Get the ship's forward direction
                Vector3D forward = _shipController.WorldMatrix.Forward;

                // Compute the East vector (perpendicular to gravity and world 'Up')
                Vector3D planetEast = Vector3D.Normalize(Vector3D.Cross(Vector3D.Up, gravity));

                // Compute the North vector (flipped order to correct direction)
                Vector3D planetNorth = Vector3D.Normalize(Vector3D.Cross(gravity, planetEast));

                // Project the forward vector onto the planet’s surface
                Vector3D projectedForward = Vector3D.Normalize(Vector3D.Reject(forward, gravity));

                // Compute the heading angle relative to north
                double headingRad = Math.Acos(MathHelper.Clamp(Vector3D.Dot(projectedForward, planetNorth), -1.0, 1.0));
                double headingDeg = MathHelper.ToDegrees(headingRad);

                // Correct the direction (ensure left decreases, right increases)
                if (Vector3D.Dot(projectedForward, planetEast) > 0)
                    headingDeg = 360 - headingDeg;
                float finalHeading = (float)(headingDeg - 180) % 360;
                if (finalHeading < 0)
                {
                    finalHeading = 360 + finalHeading;
                }
                return finalHeading;
            }
        }

        public float CurrentSPD
        {
            get
            {
                return (float) _shipController.GetShipVelocities().LinearVelocity.Length();
            }
        }
        
        public float ErrorALT
        {
            get
            {
                return CurrentALT - TargetALT;
            }
        }
        
        public float ErrorHDG
        {
            get
            {
                float currentError = CurrentHDG - TargetHDG;
                if (currentError > 180)
                {
                    currentError -= 360;
                }
                return currentError;
            }
        }
        
        public float ErrorSPD
        {
            get
            {
                return CurrentSPD - TargetSPD;
            }
        }
        
        public float ErrorVS
        {
            get
            {
                return CurrentVS - TargetVS;
            }
        }
        
        public float CurrentPTH // Error == Current as target is always 0 == level
        {
            get
            {
                return -42;
            }
        }
        
        public float CurrentROL // Error == Current as target is always 0 == level
        {
            get
            {
                return 0;
            }
        }

        public void Update()
        {
            
        }
    }
}
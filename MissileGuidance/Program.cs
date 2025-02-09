using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
using IMyGyro = Sandbox.ModAPI.Ingame.IMyGyro;
using IMyRadioAntenna = Sandbox.ModAPI.Ingame.IMyRadioAntenna;
using IMyRemoteControl = Sandbox.ModAPI.Ingame.IMyRemoteControl;
using IMyTerminalBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using IMyTextPanel = Sandbox.ModAPI.Ingame.IMyTextPanel;
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
    enum GuidanceStage
    {
        Park,
        Climb,
        Cruise,
        Dive,
        DiveArmed,
        Detonate
    }
    
    // ###########################################
    // Program class
    partial class Program : MyGridProgram
    {
        private readonly IMyRemoteControl _cockpit;
        private readonly List<IMyThrust> _thrusters = new List<IMyThrust>();
        private readonly List<IMyGyro> _gyros = new List<IMyGyro>();
        private readonly List<IMyWarhead> _warheads = new List<IMyWarhead>();
        private readonly IMyTextPanel _debugDisplay;
        private readonly IMyRadioAntenna _antenna;

        
        private GuidanceStage _guidanceStage = GuidanceStage.Park;
        private bool _guidanceModeJustSwitched = true;
        private int _airTime = 0;
        
        private PDController _gyroController;
        private double _headingTarget = 0;
        private double _pitchTarget = 90;
        private double _headingTargetError = 0;
        private double _pitchTargetError = 0;
        private LogLevel LoggingLevel = LogLevel.Debug;
        
        // -------------------------------------------
        // Required/Core functions
        public Program()
        {
            Log("Program started");
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            _gyroController = new PDController(0.8, 0.2);
            _debugDisplay = GridTerminalSystem.GetBlockWithName("Debug Panel") as IMyTextPanel;
            _antenna = GridTerminalSystem.GetBlockWithName("Antenna") as IMyRadioAntenna;
            if (_antenna == null)
            {
                Log("No antenna found, remote loggin disabled.", LogLevel.Warning);
            }
            
            List<IMyRemoteControl> cockpits = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(cockpits);
            if (cockpits.Count == 0)
            {
                LogError("No cockpits found");
            }
            this._cockpit = cockpits[0] as IMyRemoteControl;
            
            GridTerminalSystem.GetBlocksOfType(_thrusters);
            if (_thrusters.Count == 0)
            {
                LogError("No thrusters found");
            }
            
            GridTerminalSystem.GetBlocksOfType(_gyros);
            if (_gyros.Count == 0)
            {
                LogError("No gyros found");
            }
            
            GridTerminalSystem.GetBlocksOfType(this._warheads);
            if (this._warheads.Count == 0)
            {
                LogError("No warheads found");
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            try
            {
                if (updateSource == UpdateType.Update1) // Will only run this code during a normal tick
                {
                    switch (this._guidanceStage)
                    {
                        case GuidanceStage.Park:
                            GuidancePark(_guidanceModeJustSwitched);
                            break;
                        case GuidanceStage.Climb:
                            GuidanceClimb(_guidanceModeJustSwitched);
                            break;
                        case GuidanceStage.Cruise:
                            GuidanceCruise(_guidanceModeJustSwitched);
                            break;
                        case GuidanceStage.Dive:
                            GuidanceDive(_guidanceModeJustSwitched);
                            break;
                        case GuidanceStage.DiveArmed:
                            GuidanceDiveArmed(_guidanceModeJustSwitched);
                            break;
                        case GuidanceStage.Detonate:
                            GuidanceDetonate(_guidanceModeJustSwitched);
                            break;
                        default:
                            LogError("Unknown Stage");
                            break;
                    }

                    if (this._guidanceStage != GuidanceStage.Park)
                    {
                        _airTime++;
                        AlignToGravity(_headingTarget, _pitchTarget);
                    }

                    double heading;
                    double pitch;
                    GetShipHeadingAndPitch(out heading, out pitch);
                    _debugDisplay.WriteText($"Guidance Stage: {this._guidanceStage}\nHeading: {Math.Round(heading)}\nPitch: {Math.Round(pitch)}\nAir Time: {this._airTime}\nHDG: {_headingTarget}\nPTH: {_pitchTarget}\nHDG Err: {_headingTargetError}\nPTH Err: {_pitchTargetError}");
                    if (_guidanceModeJustSwitched)
                    {
                        _guidanceModeJustSwitched = false;
                    }
                }
                else if (argument.Length > 0)
                {
                    if (argument.Equals("launch", StringComparison.OrdinalIgnoreCase))
                    {
                        if (_guidanceStage == GuidanceStage.Park)
                        {
                            Log("Launching missile");
                            StageMissle();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Dump the exception content to the 
                LogError($"{e.ToString()}\n");

                // Rethrow the exception to make the programmable block halt execution properly
                throw;
            }
        }
        
        
        
        // -------------------------------------------
        // Utility functions
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
            if (_antenna != null)
            {
                IGC.SendBroadcastMessage("LogChannel", message, TransmissionDistance.TransmissionDistanceMax);
            }
        }
        
        void LogError(string message)
        {
            Log(message, LogLevel.Error);
            //throw new Exception(message);
        }
        
        private void AlignToGravity(double targetHeadingDegrees, double targetPitchDegrees)
        {
            // Get gravity vector from the cockpit
            Vector3D gravity = _cockpit.GetNaturalGravity();

            // Check if gravity exists
            if (gravity.LengthSquared() < 1e-6) // Small threshold to account for near-zero gravity
            {
                Log("Error: No gravity detected!", LogLevel.Warning);
                return;
            }

            // Normalize gravity to define the "up" direction
            Vector3D desiredUp = Vector3D.Normalize(-gravity);

            // Get the ship's current heading and pitch
            double currentHeading, currentPitch;
            GetShipHeadingAndPitch(out currentHeading, out currentPitch);

            // Calculate errors
            double headingError = targetHeadingDegrees - currentHeading;
            double pitchError = targetPitchDegrees - currentPitch;

            // Normalize heading error to [-180, 180] for minimal rotation
            if (headingError > 180) headingError -= 360;
            if (headingError < -180) headingError += 360;

            // Convert errors into desired angular velocities (control signals)
            double headingCorrection = MathHelper.ToRadians(headingError);
            double pitchCorrection = MathHelper.ToRadians(pitchError);

            // Calculate control vector for gyroscopes
            Vector3D correctionVector =
                new Vector3D(pitchCorrection, headingCorrection, 0); // Assuming roll is not corrected

            // Apply corrections to gyroscopes
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = true;

                // Transform correctionVector to gyroscope's local coordinate system
                Vector3D localCorrection =
                    Vector3D.TransformNormal(correctionVector, MatrixD.Transpose(gyro.WorldMatrix));

                // Apply corrections to the gyroscope
                gyro.Pitch = (float)localCorrection.X;
                gyro.Yaw = (float)localCorrection.Y;
                gyro.Roll = (float)localCorrection.Z;
            }

            // Display status
            //Echo($"Aligning... Heading Error: {headingError:F2}° | Pitch Error: {pitchError:F2}°");

            // Stop gyroscopes when alignment is achieved
            if (Math.Abs(headingError) < 0.5 && Math.Abs(pitchError) < 0.5)
            {
                foreach (var gyro in _gyros)
                {
                    gyro.GyroOverride = false;
                }

                Log("Aligned to gravity!", LogLevel.Debug);
            }
        }

        private void GetHeadingAndPitch(Vector3D targetPosition, out double heading, out double pitch)
        {
            // Get the ship's current position
            Vector3D shipPosition = _cockpit.GetPosition();

            // Calculate the direction vector to the target
            Vector3D direction = Vector3D.Normalize(targetPosition - shipPosition);

            // Get the ship's gravity vector to define the "up" direction
            Vector3D gravity = _cockpit.GetNaturalGravity();
            if (gravity.LengthSquared() < 1e-6) // No gravity present
            {
                throw new InvalidOperationException("Error: No gravity detected!");
            }
            Vector3D up = Vector3D.Normalize(-gravity);

            // Compute heading
            Vector3D forward = _cockpit.WorldMatrix.Forward;
            Vector3D right = _cockpit.WorldMatrix.Right;
            Vector3D directionHorizontal = direction - Vector3D.Dot(direction, up) * up;
            directionHorizontal = Vector3D.Normalize(directionHorizontal);

            double headingRadians = Math.Acos(Vector3D.Dot(directionHorizontal, forward));
            heading = MathHelper.ToDegrees(headingRadians);
            if (Vector3D.Dot(directionHorizontal, right) < 0)
            {
                heading = 360 - heading;
            }

            // Compute pitch
            Vector3D forwardVertical = Vector3D.Dot(forward, up) * up;
            Vector3D forwardHorizontal = forward - forwardVertical;

            pitch = MathHelper.ToDegrees(Math.Atan2(Vector3D.Dot(forward, up), forwardHorizontal.Length()));
        }

        
        // Method to get the current heading and pitch relative to the horizon
        public void GetShipHeadingAndPitch(out double heading, out double pitch)
        {
            // Use the ship's current forward vector as the direction to check
            Vector3D shipForward = _cockpit.WorldMatrix.Forward;

            // Call the helper function to compute the values
            GetHeadingAndPitch(shipForward, out heading, out pitch);
        }
        
        public void StageMissle()
        {
            _guidanceStage = (GuidanceStage)Enum.ToObject(typeof(GuidanceStage),
                Math.Min((int)_guidanceStage + 1, (int)GuidanceStage.Detonate));
            _guidanceModeJustSwitched = true;
            Log($"Stage: {_guidanceStage}");
        }
        
        public void Detonate()
        {
            Log("Detonation triggered");
            foreach (var warhead in _warheads)
            {
                warhead.Detonate();
            }
        }
        
        // -------------------------------------------
        // Stage logic loops
        private void GuidancePark(bool justSwitched)
        {
            foreach (var thruster in _thrusters)
            {
                thruster.ThrustOverridePercentage = 0.001f;
            }
        }
        
        private void GuidanceClimb(bool justSwitched)
        {
            if (justSwitched)
            {
                GetShipHeadingAndPitch(out _headingTarget, out _pitchTarget);
                _pitchTarget = 90;
                _gyroController.Reset();
                _cockpit.DampenersOverride = false;
            }
            foreach (var thruster in _thrusters)
            {
                thruster.ThrustOverridePercentage = 1f;
            }
            if (_airTime > 240)
            {
                StageMissle();
            }
        }
        
        private void GuidanceCruise(bool justSwitched)
        {
            if (justSwitched)
            {
                _pitchTarget = 20;
                _gyroController.Reset();
                _cockpit.DampenersOverride = false;
            }
            if (_airTime > 480)
            {
                StageMissle();
            }
        }
        
        private void GuidanceDive(bool justSwitched)
        {
            if (justSwitched)
            {
                _pitchTarget = -60;
                _gyroController.Reset();
                _cockpit.DampenersOverride = false;
            }
            if (_airTime > 550)
            {
                StageMissle();
            }
        }
        
        private void GuidanceDiveArmed(bool justSwitched)
        {
            if (justSwitched)
            {
                foreach (var warhead in _warheads)
                {
                    warhead.IsArmed = true;
                }
                // No PDController reset or change in behaviour from Dive
            }
            // if x metres from target explode
            // rotate to target
            // full thrust
            if (_airTime > 700)
            {
                StageMissle();
            }
        }
        
        private void GuidanceDetonate(bool justSwitched)
        {
            Detonate();
        }
    }
    
    
    
    // ###########################################
    // PDController class
    // ReSharper disable once InconsistentNaming
    public class PDController
    {
        private readonly double _kp;
        private readonly double _kd;
        private Vector3D _previousError;

        public PDController(double proportionalGain, double derivativeGain)
        {
            _kp = proportionalGain;
            _kd = derivativeGain;
            _previousError = Vector3D.Zero;
        }

        public Vector3D CalculateControlSignal(Vector3D error, double deltaTime)
        {
            Vector3D derivative = (error - _previousError) / deltaTime;
            _previousError = error;
            return _kp * error + _kd * derivative;
        }
        
        public void Reset()
        {
            _previousError = Vector3D.Zero;
        }
    }
}
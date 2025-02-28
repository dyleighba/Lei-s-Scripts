using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.AI.Pathfinding.Obsolete;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRageMath;
using VRageRender.Import;

namespace IngameScript
{
    public class AutopilotOperations
    {
        private Autopilot _autopilot;
        
        private PDController _rollController;
        private PDController _pitchController;
        private PDController _altitudeController;
        private PDController _headingController;
        private PDController _speedController;
        private PDController _verticalSpeedController;
        
        private double _derivedVerticalSpeedTarget = 0;
        private double _derivedRollTarget = 0;
        
        
        private List<IMyThrust> _thrusters;
        private List<IMyThrust> _gravityFightingThrusters = new List<IMyThrust>();
        private ShipGyroController _gyros;
        
        public AutopilotOperations(Autopilot autopilot, List<IMyThrust> thrusters, ShipGyroController gyros)
        {
            _autopilot = autopilot;
            _thrusters = thrusters;
            _gyros = gyros;
            if (_thrusters == null || _thrusters.Count < 1)
            {
                throw new Exception("AutopilotOperations requires at least one thruster");
            }

            if (_gyros == null)
            {
                throw new Exception("AutopilotOperations requires a gyro controller");
            }
            

            _rollController = new PDController(1.0, 2.0);
            _pitchController = new PDController(1.0, 2.0);
            _altitudeController = new PDController(10, 30);
            _headingController = new PDController(0.02, 0.02);
            _speedController = new PDController(1.0, 2.0);
            _verticalSpeedController = new PDController(2.0, 1.0);
        }

        public void Update(double deltaTime)
        {
            if (!_autopilot.AutopilotEnabled)
            {
                ReleaseAllControlLocks();
                return;
            };
            _gyros.Yaw = 0;
            _gyros.Pitch = 0;
            _gyros.Roll = 0;
            CorrectRoll(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectPitch(deltaTime, !_autopilot.AutopilotEnabled);

            if (Math.Abs(_autopilot.CurrentRoll) + Math.Abs(_autopilot.CurrentPitch) > 10)
            {
                _gyros.UpdateGyroRotation();
                return;
            }
            PopulateDownwardsThrustersIfNeeded();
            
            CorrectAltitude(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectHeading(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectSpeed(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectVerticalSpeed(deltaTime, !_autopilot.AutopilotEnabled);
            _gyros.UpdateGyroRotation();
            
        }

        public void ReleaseAllControlLocks()
        {
            _gyros.Yaw = 0;
            _gyros.Pitch = 0;
            _gyros.Roll = 0;
            _gyros.UpdateGyroRotation();
            _gyros.GyroOverride = false;

            ReleaseThrusterControlLocks();
            _autopilot.DampenersOverride = true;
        }
        
        private void ReleaseThrusterControlLocks()
        {
            foreach (var thruster in _thrusters)
            {
                thruster.ThrustOverridePercentage = 0;
            }
        }

        private void PopulateDownwardsThrustersIfNeeded()
        {
            if (_gravityFightingThrusters.Count > 0) return;
            foreach (var thruster in _thrusters)
            {
                if (Vector3D.Dot(thruster.WorldMatrix.Forward, _autopilot.Gravity) > 0.9)
                {
                    _gravityFightingThrusters.Add(thruster);
                }
            }
        }
        
        private void CorrectRoll(double deltaTime, bool forceOff = false)
        {
            if (forceOff || !_autopilot.AutopilotEnabled)
            {
                _rollController.Reset();
                return;
            }
            double desiredError = _autopilot.CurrentRoll;
            if (_autopilot.HeadingEnabled)
            {
                desiredError = _autopilot.CurrentRoll - _derivedRollTarget;
            }
            double correction = _rollController.Compute(0, desiredError, deltaTime);
            // do gyro stuff
            _gyros.GyroOverride = true;
            _gyros.Roll = (float)correction;
        }

        private void CorrectPitch(double deltaTime, bool forceOff = false)
        {
            if (forceOff || !_autopilot.AutopilotEnabled)
            {
                _rollController.Reset();
                return;
            }
            double correction = _pitchController.Compute(0, -_autopilot.CurrentPitch, deltaTime);
            // do gyro stuff
            _gyros.GyroOverride = true;
            _gyros.Pitch = -(float)correction;
        }
        
        private void CorrectHeading(double deltaTime, bool forceOff = false)
        {
            _derivedRollTarget = 0;
            if (forceOff || !_autopilot.HeadingEnabled)
            {
                _headingController.Reset();
                return;
            }
            
            if (Math.Abs(_autopilot.HeadingError) > 10)
            {
                if (_autopilot.HeadingError < 0)
                {
                    _derivedRollTarget = -0.1;
                }
                else
                {
                    _derivedRollTarget = 0.1;
                }
            }
            double correction = _headingController.Compute(_autopilot.HeadingTarget, _autopilot.HeadingError, deltaTime);
            // do gyro stuff
            _gyros.GyroOverride = true;
            _gyros.Yaw = (float)correction;
        }

        private void CorrectAltitude(double deltaTime, bool forceOff = false)
        {
            _derivedVerticalSpeedTarget = 0;
            if (forceOff || !_autopilot.AltitudeEnabled || !_autopilot.VerticalSpeedEnabled)
            {
                _altitudeController.Reset();
                // TODO don't run if possible
                return;
            }
            
            // let the game handle it once settled to stop oscillation
            if (Math.Round(_autopilot.AltitudeError) == 0)
            {
                _derivedVerticalSpeedTarget = 0;
                return;
            }
            double correction = _altitudeController.Compute(0, -_autopilot.AltitudeError, deltaTime);
            double slowDownDistance = (Math.Abs(_autopilot.CurrentVerticalSpeed) / 30) * 100; // Missile barge quick fix
            double realVerticalSpeedTarget = _autopilot.VerticalSpeedTarget;
            if (Math.Abs(_autopilot.AltitudeError) < slowDownDistance)
            {
                realVerticalSpeedTarget = Math.Min(10.0f, realVerticalSpeedTarget);
            }
            // do thruster stuff
            double derivedTarget = realVerticalSpeedTarget * Math.Max(-1, Math.Min(1, correction));
            _derivedVerticalSpeedTarget = derivedTarget;
        }

        private void CorrectSpeed(double deltaTime, bool forceOff = false)
        {
            if (forceOff || !_autopilot.SpeedEnabled)
            {
                _speedController.Reset();
                return;
            }
            double correction = _speedController.Compute(0, _autopilot.SpeedError, deltaTime);
            // do thruster stuff
        }

        private void CorrectVerticalSpeed(double deltaTime, bool forceOff = false)
        {
            if (forceOff || !_autopilot.VerticalSpeedEnabled)
            {
                _verticalSpeedController.Reset();
                foreach (var thruster in _gravityFightingThrusters)
                {
                    thruster.ThrustOverridePercentage = 0;
                }
                return;
            }
            double desiredError = _autopilot.VerticalSpeedError;
            if (_autopilot.AltitudeEnabled)
            {
                desiredError = _autopilot.CurrentVerticalSpeed - _derivedVerticalSpeedTarget;
            }
            
            // let the game handle it once settled to stop oscillation
            if (Math.Abs(desiredError) < 0.1)
            {
                foreach (var thruster in _gravityFightingThrusters)
                {
                    thruster.ThrustOverridePercentage = 0;
                }
                return;
            };

            float thrustRatio = 0;
            //if (!(desiredError < 1.0f && _autopilot.CurrentVerticalSpeed < 1.0f))
            //{
                double correction = _verticalSpeedController.Compute(0, -desiredError, deltaTime);
                thrustRatio = MathHelper.Clamp((float)correction, 0.0001f, 1.0f);
            //}
            Vector3D gravity = _autopilot.Gravity;
            // do thruster stuff
            foreach (var thruster in _gravityFightingThrusters)
            {
                thruster.ThrustOverridePercentage = thrustRatio;
            }
        }
    }
}
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
        private List<IMyGyro> _gyros;
        
        public AutopilotOperations(Autopilot autopilot, List<IMyThrust> thrusters, List<IMyGyro> gyros)
        {
            _autopilot = autopilot;
            _thrusters = thrusters;
            _gyros = gyros;
            if (_thrusters == null || _thrusters.Count < 1)
            {
                throw new Exception("AutopilotOperations requires at least one thruster");
            }

            if (_gyros == null || _gyros.Count < 1)
            {
                throw new Exception("AutopilotOperations requires at least one gyro");
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
            CorrectRoll(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectPitch(deltaTime, !_autopilot.AutopilotEnabled);
                
            if (Math.Abs(_autopilot.CurrentRoll) + Math.Abs(_autopilot.CurrentPitch) > 10) return;
            PopulateDownwardsThrustersIfNeeded();
            
            CorrectAltitude(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectHeading(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectSpeed(deltaTime, !_autopilot.AutopilotEnabled);
            CorrectVerticalSpeed(deltaTime, !_autopilot.AutopilotEnabled);
        }

        public void ReleaseAllControlLocks()
        {
            foreach (IMyGyro gyro in _gyros)
            {
                gyro.GyroOverride = false;
            }

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
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = true;
                gyro.Roll = (float)correction;
            }
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
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = true;
                gyro.Pitch = (float)correction;
                gyro.Yaw = -(float)correction;
            }
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
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = true;
                gyro.Yaw = gyro.Yaw + (float)correction;
                gyro.Pitch = gyro.Pitch + (float)correction;
            }
        }

        private void CorrectAltitude(double deltaTime, bool forceOff = false)
        {
            _derivedVerticalSpeedTarget = 0;
            if (forceOff || !_autopilot.AltitudeEnabled || !_autopilot.VerticalSpeedEnabled)
            {
                _altitudeController.Reset();
                return;
            }
            if (_autopilot.VerticalSpeedTarget < 0) return;
            double correction = _altitudeController.Compute(0, -_autopilot.AltitudeError, deltaTime);
            double slowDownDistance = (Math.Abs(_autopilot.CurrentVerticalSpeed) / 30) * 100; // Missile barge quick fix
            if (_autopilot.AltitudeError < slowDownDistance && _autopilot.AltitudeError > 50 && _autopilot.CurrentVerticalSpeed < 0)
            {
                correction =  1;
            }
            // do thruster stuff
            double derivedTarget = _autopilot.VerticalSpeedTarget * Math.Max(-1, Math.Min(1, correction));
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
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        private FlightControl flightControl;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            
            IMyCockpit cockpit = GridTerminalSystem.GetBlockWithName("Cockpit") as IMyCockpit;
            if (cockpit == null)
            {
                Echo("Error: Cockpit not found!");
                return;
            }

            flightControl = new FlightControl(GridTerminalSystem, cockpit);
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (argument.Equals("enable", StringComparison.OrdinalIgnoreCase))
            {
                flightControl.EnablePilot(true);
            }
            else if (argument.Equals("disable", StringComparison.OrdinalIgnoreCase))
            {
                flightControl.EnablePilot(false);
            }
            else if (argument.StartsWith("set_speed", StringComparison.OrdinalIgnoreCase))
            {
                double speed;
                if (double.TryParse(argument.Split(' ')[1], out speed))
                {
                    flightControl.SetTargetSpeed(speed);
                }
                else
                {
                    Echo("Invalid speed input.");
                }
            }
            else if (argument.Equals("level", StringComparison.OrdinalIgnoreCase))
            {
                flightControl.SetLeveling(true);
            }
            else if (argument.Equals("no_level", StringComparison.OrdinalIgnoreCase))
            {
                flightControl.SetLeveling(false);
            }

            flightControl.Update();
        }
    }

    public class FlightControl
    {
        private readonly IMyGridTerminalSystem gridTerminalSystem;
        private readonly IMyCockpit cockpit;
        private readonly List<IMyGyro> gyros = new List<IMyGyro>();
        private readonly List<IMyThrust> thrusters = new List<IMyThrust>();

        private bool isPilotEnabled = false;
        private bool shouldLevel = false;
        private double targetSpeed = 0;

        private PDController pdController;

        public FlightControl(IMyGridTerminalSystem gridTerminalSystem, IMyCockpit cockpit)
        {
            this.gridTerminalSystem = gridTerminalSystem;
            this.cockpit = cockpit;

            CacheBlocks();
            pdController = new PDController(1.0, 0.1);
        }

        private void CacheBlocks()
        {
            gridTerminalSystem.GetBlocksOfType(gyros);
            gridTerminalSystem.GetBlocksOfType(thrusters);
        }

        public void EnablePilot(bool enable)
        {
            isPilotEnabled = enable;
        }

        public void SetTargetSpeed(double speed)
        {
            targetSpeed = speed;
        }

        public void SetLeveling(bool level)
        {
            shouldLevel = level;
        }

        public void Update()
        {
            if (!isPilotEnabled) return;

            Vector3D gravity = cockpit.GetNaturalGravity();
            if (gravity.LengthSquared() < 0.01)
            {
                cockpit.GetSurface(0).WriteText("No gravity detected.");
                return;
            }

            gravity.Normalize();

            if (shouldLevel)
            {
                AlignToGravity(gravity);
            }

            if (targetSpeed != 0)
            {
                MaintainSpeed(gravity);
            }
        }

        private void AlignToGravity(Vector3D gravity)
        {
            Vector3D desiredUp = gravity;
            Vector3D shipUp = cockpit.WorldMatrix.Up;
            Vector3D correction = Vector3D.Cross(shipUp, desiredUp);

            if (correction.Length() > 0.01)
            {
                Vector3D controlSignal = pdController.CalculateControlSignal(correction, 1.0 / 60.0);
                foreach (var gyro in gyros)
                {
                    gyro.GyroOverride = true;
                    Vector3D localSignal = Vector3D.TransformNormal(controlSignal, MatrixD.Transpose(gyro.WorldMatrix));
                    gyro.Pitch = (float)localSignal.X;
                    gyro.Yaw = (float)localSignal.Y;
                    gyro.Roll = (float)localSignal.Z;
                }
            }
            else
            {
                foreach (var gyro in gyros)
                {
                    gyro.GyroOverride = false;
                }
            }
        }

        private void MaintainSpeed(Vector3D gravity)
        {
            Vector3D velocity = cockpit.GetShipVelocities().LinearVelocity;
            double currentSpeed = Vector3D.Dot(velocity, -gravity);
            double speedError = targetSpeed - currentSpeed;

            double thrustAdjustment = pdController.CalculateControlSignal(new Vector3D(speedError, 0, 0), 1.0 / 60.0).X;
            float thrustRatio = MathHelper.Clamp((float)thrustAdjustment, 0.0f, 1.0f);

            foreach (var thruster in thrusters)
            {
                if (Vector3D.Dot(thruster.WorldMatrix.Forward, gravity) > 0.9)
                {
                    thruster.ThrustOverridePercentage = thrustRatio;
                }
                else
                {
                    thruster.ThrustOverridePercentage = 0;
                }
            }
        }
    }

    public class PDController
    {
        private double Kp;
        private double Kd;
        private Vector3D previousError;

        public PDController(double proportionalGain, double derivativeGain)
        {
            Kp = proportionalGain;
            Kd = derivativeGain;
            previousError = Vector3D.Zero;
        }

        public Vector3D CalculateControlSignal(Vector3D error, double deltaTime)
        {
            Vector3D derivative = (error - previousError) / deltaTime;
            previousError = error;
            return Kp * error + Kd * derivative;
        }

        public void Reset()
        {
            previousError = Vector3D.Zero;
        }
    }
}

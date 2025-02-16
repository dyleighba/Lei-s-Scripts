using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class ShipGyroController
    {
        private List<IMyGyro> _gyros = new List<IMyGyro>();
        private Vector3D _virtualGyroRotation = Vector3D.Zero; 
        
        public ShipGyroController(IMyGridTerminalSystem gridTerminalSystem)
        {
            gridTerminalSystem.GetBlocksOfType<IMyGyro>(_gyros);
            UpdateGyroRotation();
        }

        public void UpdateGyroRotation()
        {
            Vector3D controlSignal = new Vector3D(Yaw, Pitch, Roll);
            foreach (var gyro in _gyros)
            {
                gyro.GyroOverride = true;
                Vector3D localSignal = Vector3D.TransformNormal(controlSignal, MatrixD.Transpose(gyro.WorldMatrix));
                gyro.Pitch = (float)localSignal.X;
                gyro.Yaw = (float)localSignal.Y;
                gyro.Roll = (float)localSignal.Z;
            }
        }

        public double Pitch
        {
            get { return _virtualGyroRotation.X; }
            set { _virtualGyroRotation.X = value; }
        }

        public double Roll
        {
            get { return _virtualGyroRotation.Y; }
            set { _virtualGyroRotation.Y = value; }
        }
        
        public double Yaw
        {
            get { return _virtualGyroRotation.Z; }
            set { _virtualGyroRotation.Z = value; }
        }

        public bool GyroOverride
        {
            set
            {
                foreach (var gyro in _gyros)
                {
                    gyro.GyroOverride = value;
                }
            }
        }
    }
}
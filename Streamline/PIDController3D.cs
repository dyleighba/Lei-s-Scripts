using VRageMath;

namespace IngameScript
{
    public class PIDController3D
    {
        private double Kp;
        private double Ki;
        private double Kd;
        private Vector3D integral;
        private Vector3D previousError;
        private double integralLimit;
    
        public PIDController3D(double proportionalGain, double integralGain, double derivativeGain, double integralLimit = 10.0)
        {
            Kp = proportionalGain;
            Ki = integralGain;
            Kd = derivativeGain;
            this.integralLimit = integralLimit;
            integral = Vector3D.Zero;
            previousError = Vector3D.Zero;
        }

        public Vector3D Compute(Vector3D error, double deltaTime)
        {
            // Compute integral with windup protection
            integral += error * deltaTime;
            integral = Vector3D.ClampToSphere(integral, integralLimit);
        
            // Compute derivative
            Vector3D derivative = (error - previousError) / deltaTime;
            previousError = error;

            // Compute raw output
            return (Kp * error) + (Ki * integral) + (Kd * derivative);
        }

        public void Reset()
        {
            integral = Vector3D.Zero;
            previousError = Vector3D.Zero;
        }
    }

}
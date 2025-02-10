namespace IngameScript
{
    public class PDController
    {
        private double proportionalGain;
        private double derivativeGain;
        private double lastError;

        public PDController(double pGain, double dGain)
        {
            proportionalGain = pGain;
            derivativeGain = dGain;
            lastError = 0;
        }

        public double Compute(double setpoint, double error, double deltaTime)
        {
            double derivative = (error - lastError) / deltaTime;
            lastError = error;
            return (proportionalGain * error) + (derivativeGain * derivative);
        }

        public void Reset()
        {
            lastError = 0;
        }
    }
}
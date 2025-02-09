namespace IngameScript
{
    public class AutopilotOperations
    {
        private Autopilot _autopilot;
        
        public AutopilotOperations(Autopilot autopilot)
        {
            _autopilot = autopilot;
        }

        public void Update(double deltaTime)
        {
            
        }

        public void DisableAutopilot()
        {
            
        }

        public void ReleaseAllControlLocks()
        {
            // go though thrusters and gyros to remove any overrides and enable inertial dampening
            // as soon as Update() is called controls will be taken again
        }
    }
}
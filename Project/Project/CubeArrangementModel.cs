using Silk.NET.Vulkan;

namespace Project
{
    internal class CubeArrangementModel
    {
        /// <summary>
        /// Gets or sets wheather the animation should run or it should be frozen.
        /// </summary>
        public bool AnimationEnabeld { get; set; } = false;

        /// <summary>
        /// The time of the simulation. It helps to calculate time dependent values.
        /// </summary>
        private double Time { get; set; } = 0;

        private double OldTime { get; set; } = 0;

        public double OldDirection { get; set; } = 0;

        /// <summary>
        /// The value by which the center cube is scaled. It varies between 0.8 and 1.2 with respect to the original size.
        /// </summary>
        public double CenterCubeScale { get; private set; } = 1;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleOwnRevolution { get; private set; } = 0;

        /// <summary>
        /// The angle with which the diamond cube is rotated around the diagonal from bottom right front to top left back.
        /// </summary>
        public double DiamondCubeAngleRevolutionOnGlobalY { get; private set; } = 0;

        internal void AdvanceTime(double deltaTime, int rollDirection)
        {
            if (Math.Abs(Time - OldTime) >= 90.0f && AnimationEnabeld)
            {
                AnimationEnabeld = false;
                OldTime = Time;
            }
            // we do not advance the simulation when animation is stopped
            if (!AnimationEnabeld)
                return;

            if (Time == 360.0f && rollDirection == 1)
            {
                Time = deltaTime = 0;
            }
            else if (Time == 0.0f && rollDirection == 0)
            {
                Time = deltaTime = 360;
            }
            // set a simulation time
            Time += (3.0f * rollDirection);

            // lets produce an oscillating scale in time
            CenterCubeScale = 1 + 0.2 * Math.Sin(1.5 * Time);

            // the rotation angle is time x angular velocity;
            DiamondCubeAngleRevolutionOnGlobalY = Time * 1;

            DiamondCubeAngleOwnRevolution = (Math.PI / 180) * Time;
        }
    }
}


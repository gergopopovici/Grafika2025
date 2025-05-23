using Silk.NET.Maths;

namespace pgim2289_project

{
    internal class CameraDescriptor
    {
        private double DistanceToOrigin = 30;

        private double AngleToZYPlane = Math.PI / 180 * 36 * 5;

        private double AngleToZXPlane = Math.PI / 180 * 2 * 5;

        private const double DistanceScaleFactor = 1.1;

        private const double AngleChangeStepSize = Math.PI / 180 * 5;

        private float CameraHeightOffset = 5f;

        public Vector3D<float> Target = Vector3D<float>.Zero;

        /// <summary>
        /// Gets the position of the camera.
        /// </summary>
        public Vector3D<float> Position
        {
            get
            {
                var position = Target + GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane);
                return new Vector3D<float>(position.X, position.Y + CameraHeightOffset, position.Z);
            }
        }


        /// <summary>
        /// Gets the up vector of the camera.
        /// </summary>
        public Vector3D<float> UpVector
        {
            get
            {
                return Vector3D.Normalize(GetPointFromAngles(DistanceToOrigin, AngleToZYPlane, AngleToZXPlane + Math.PI / 2));
            }
        }

        /// <summary>
        /// Gets the target point of the camera view.
        /// </summary>


        public void SetCameraOffsetHeight(float height)
        {
            CameraHeightOffset = height;
        }

        public void SetDistanceToOrigin(float distance)
        {
            DistanceToOrigin = distance;
        }
        public void IncreaseZXAngle()
        {
            AngleToZXPlane += AngleChangeStepSize;
        }

        public void IncreaseZYAngle(double angles)
        {
            AngleToZYPlane += angles;
        }

        public void ResetZYAngle()
        {
            AngleToZYPlane = Math.PI / 180 * 36 * 5;
        }

        public void DecreaseZXAngle()
        {
            AngleToZXPlane -= AngleChangeStepSize;
        }

        public void IncreaseZYAngle()
        {
            AngleToZYPlane += AngleChangeStepSize;

        }

        public void DecreaseZYAngle()
        {
            AngleToZYPlane -= AngleChangeStepSize;
        }

        public void IncreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin * DistanceScaleFactor;
        }

        public void DecreaseDistance()
        {
            DistanceToOrigin = DistanceToOrigin / DistanceScaleFactor;
        }

        public void SetTarget(Vector3D<float> PlayerPosition)
        {
            Target = PlayerPosition;
        }

        private static Vector3D<float> GetPointFromAngles(double distanceToOrigin, double angleToMinZYPlane, double angleToMinZXPlane)
        {
            var x = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Sin(angleToMinZYPlane);
            var z = distanceToOrigin * Math.Cos(angleToMinZXPlane) * Math.Cos(angleToMinZYPlane);
            var y = distanceToOrigin * Math.Sin(angleToMinZXPlane);

            return new Vector3D<float>((float)x, (float)y, (float)z);
        }
    }
}

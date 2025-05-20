using Silk.NET.Maths;

namespace pgim2289_project
{
    internal class BoundingBox
    {
        private Vector3D<float> Min;
        private Vector3D<float> Max;
        public BoundingBox(Vector3D<float> min, Vector3D<float> max)
        {
            Min = min;
            Max = max;
        }

        public bool Intersects(BoundingBox other)
        {
            bool xOverlap = !(Max.X < other.Min.X || Min.X > other.Max.X);
            bool yOverlap = !(Max.Y < other.Min.Y || Min.Y > other.Max.Y);
            bool zOverlap = !(Max.Z < other.Min.Z || Min.Z > other.Max.Z);

            return xOverlap && yOverlap && zOverlap;
        }

        public void Update(Vector3D<float> position, Vector3D<float> dimensions)
        {
            Vector3D<float> halfExtents = dimensions / 2f;
            Min = position - halfExtents;
            Max = position + halfExtents;
        }
    }
}


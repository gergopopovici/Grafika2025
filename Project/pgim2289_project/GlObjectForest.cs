using Silk.NET.OpenGL;
using Silk.NET.Maths;

namespace pgim2289_project
{
    internal class GlObjectForest
    {
        public float maxSpeed;              
        public float acceleration;           
        public float speed;                 
        public float steeringAngle;
        public float wheelBase;
        public float Orientation;
        public float DeltaOrientation;
        public float scaleSize;
        public Matrix4X4<float> Scale;
        public Matrix4X4<float> Translation;
        public Matrix4X4<float> Rotation;
        public Matrix4X4<float> ModelMatrix;
        public Vector3D<float> Position;
        private GlObject objectBase;
        public BoundingBox boundingBox;
        Vector3D<float> BoundingBoxDimensions;

        public GlObjectForest(GL Gl, string carName)
        {
            Position = new Vector3D<float>(0f, 0f, 0f);
            Orientation = 0f;
            scaleSize = 1.0f;
            Scale = Matrix4X4.CreateScale(1f);
            Translation = Matrix4X4.CreateTranslation(0f, 0f, 0f);
            Rotation = Matrix4X4.CreateRotationY(0f);
            ModelMatrix = Scale * Translation * Rotation;
            objectBase = ObjectResourceReader.CreateObjectWithTextureFromResource(Gl, "objects." + carName + ".obj", "objects." + carName + ".png");
            BoundingBoxDimensions = new Vector3D<float>(1f, 1f, 1f);
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox = new BoundingBox(Position, BoundingBoxDimensions);
        }

        public void UpdateSteering(float deltaTime)
        {
            float steeringAngleInRadians = MathF.PI / 180 * steeringAngle;
            float turnRadius = wheelBase / MathF.Tan(steeringAngleInRadians);
            float angularVelocity = speed / turnRadius;

            Orientation += angularVelocity * deltaTime;
            DeltaOrientation = angularVelocity * deltaTime;
            if (Orientation < 0)
                Orientation += 2 * MathF.PI;
            else if (Orientation >= 2 * MathF.PI)
                Orientation -= 2 * MathF.PI;

            Vector3D<float> forwardDirection = new Vector3D<float>(MathF.Sin(Orientation), 0f, MathF.Cos(Orientation));
            Position += forwardDirection * speed * deltaTime;

            ModelMatrix = Scale * Matrix4X4.CreateRotationY(Orientation) * Matrix4X4.CreateTranslation(Position);
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public double degreesToRadians(double degrees)
        {
            return Math.PI / 180 * degrees;
        }

        public unsafe void Release()
        {
            objectBase.ReleaseGlObject();
        }

        public unsafe void SetPosition(Vector3D<float> position)
        {
            Position = position;
            Translation = Matrix4X4.CreateTranslation(Position);
            ModelMatrix = Scale * Rotation * Translation;
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public unsafe void SetRotation(float rotation)
        {
            Orientation = rotation;
            Rotation = Matrix4X4.CreateRotationY(rotation);
            ModelMatrix = Scale * Rotation * Translation;
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public unsafe void UpdateState()
        {
            Translation = Matrix4X4.CreateTranslation(Position);
            Rotation = Matrix4X4.CreateRotationY(Orientation);
            ModelMatrix = Scale * Rotation * Translation;
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public unsafe void SetScale(float scale)
        {
            scaleSize = scale;
            BoundingBoxDimensions *= scaleSize;
            Scale = Matrix4X4.CreateScale(scale);
            ModelMatrix = Scale * Rotation * Translation;
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public unsafe void Render(uint program, string textureUniformVariableName,
            string ModelMatrixVariableName, string NormalMatrixVariableName)
        {
            objectBase.Render(program, textureUniformVariableName, ModelMatrixVariableName, NormalMatrixVariableName);
        }

        public unsafe void updatePosition(Vector3D<float> positionChange)
        {
            Position += positionChange;
            Translation = Matrix4X4.CreateTranslation(Position);
            ModelMatrix = Scale * Matrix4X4.CreateRotationY(Orientation) * Matrix4X4.CreateTranslation(Position);
            objectBase.ModelMatrix = ModelMatrix;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }

        public unsafe void SetBoundingBoxDimensions(float width, float length, float height)
        {
            BoundingBoxDimensions = new Vector3D<float>(width, height, length) * scaleSize;
            boundingBox.Update(Position, BoundingBoxDimensions);
        }
    }
}
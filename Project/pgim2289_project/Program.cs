using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace pgim2289_project
{
    internal static class Program
    {
        private static CameraDescriptor cameraDescriptor1 = new();
        private static CameraDescriptor cameraDescriptor2 = new();
        private static CameraDescriptor activeCamera = new();

        private static int activeCameraNum = 1;

        private static IWindow window;

        private static IInputContext inputContext;

        private static GL Gl;

        private static ImGuiController controller;

        private static uint program;

        private static GlObjectForest squirrel;
        private static GlObjectForest apple;
        private static GlObjectForest pear;
        private static float appleTimer;
        private static float pearTimer;
        private static float car1RadiusA;
        private static float car1RadiusB;
        private static float car2RadiusA;
        private static float car2RadiusB;

        private static IKeyboard primaryKeyboard;

        private static PlayerSquirrelModel playerSquirrelModel = new();
        private static GlObject raceTrack;

        private static GlCube skyBox;

        private static float Shininess = 50;

        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private const string TextureUniformVariableName = "uTexture";

        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private const string ShininessVariableName = "shininess";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "pgim2289_project";
            windowOptions.Size = new Vector2D<int>(1920, 1280);

            // on some systems there is no depth buffer by default, so we need to make sure one is created
            windowOptions.PreferredDepthBufferBits = 24;

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // set up input handling

            inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                primaryKeyboard = keyboard;
            }

            if (primaryKeyboard != null)
            {
                primaryKeyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            // Handle resizes
            window.FramebufferResize += s =>
            {
                // Adjust the viewport to the new window size
                Gl.Viewport(s);
            };

            playerSquirrelModel.isGoing = false;
            playerSquirrelModel.isTurning = false;

            cameraDescriptor2.SetCameraOffsetHeight(100.0f);
            cameraDescriptor2.SetDistanceToOrigin(300.0f);


            Gl.ClearColor(System.Drawing.Color.Black);

            SetUpObjects();

            LinkProgram();

            // Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void LinkProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, ReadShader("VertexShader.vert"));
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, ReadShader("FragmentShader.frag"));
            Gl.CompileShader(fshader);

            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }
            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static string ReadShader(string shaderFileName)
        {
            using (Stream shaderStream = typeof(Program).Assembly.GetManifestResourceStream("pgim2289_project.Shaders." + shaderFileName))
            using (StreamReader shaderReader = new StreamReader(shaderStream))
                return shaderReader.ReadToEnd();
        }

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.F5:
                    if (activeCameraNum == 1)
                    {
                        activeCameraNum = 2;
                        activeCamera = cameraDescriptor2;
                    }
                    else
                    {
                        activeCameraNum = 1;
                        activeCamera = cameraDescriptor1;
                    }; break;

            }
        }

        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            playerSquirrelModel.isGoing = false;
            playerSquirrelModel.isTurning = false;
            appleTimer += (float)deltaTime;
            pearTimer += (float)deltaTime;
            if (primaryKeyboard.IsKeyPressed(Key.A))
            {
                playerSquirrelModel.isTurning = true;
                squirrel.steeringAngle = 10.0f;
            }
            else if (primaryKeyboard.IsKeyPressed(Key.D))
            {
                playerSquirrelModel.isTurning = true;
                squirrel.steeringAngle = -10.0f;
            }
            if (primaryKeyboard.IsKeyPressed(Key.W))
            {
                playerSquirrelModel.isGoing = true;
                squirrel.speed = Math.Min(squirrel.speed + squirrel.acceleration * (float)deltaTime, squirrel.maxSpeed);
            }
            else if (primaryKeyboard.IsKeyPressed(Key.S))
            {
                playerSquirrelModel.isGoing = true;
                squirrel.speed = Math.Max(squirrel.speed - squirrel.acceleration * (float)deltaTime, -squirrel.maxSpeed / 2);
            }
            else
            {
                squirrel.speed *= 0.98f;
                if (squirrel.speed < 2)
                {
                    squirrel.speed = 0;
                }
            }

            if (playerSquirrelModel.isTurning && playerSquirrelModel.isGoing)
            {
                squirrel.UpdateSteering((float)deltaTime);
                cameraDescriptor1.IncreaseZYAngle(squirrel.DeltaOrientation);
            }
            Vector3D<float> Direction = new Vector3D<float>(MathF.Sin(squirrel.Orientation), 0f, MathF.Cos(squirrel.Orientation));
            squirrel.updatePosition(Direction * squirrel.speed * (float)deltaTime);
            cameraDescriptor1.SetTarget(squirrel.Position);
            if (activeCameraNum == 1)
            {
                activeCamera = cameraDescriptor1;
            }

            appleTimer += 0.01f * (float)deltaTime;
            float x1 = car1RadiusA * MathF.Cos(appleTimer * 0.3f);
            float z1 = car1RadiusB * MathF.Sin(appleTimer * 0.3f);
            apple.Position = new Vector3D<float>(x1, 1.5f, z1);

            float dx1 = -car1RadiusA * MathF.Sin(appleTimer * 0.3f);
            float dz1 = car1RadiusB * MathF.Cos(appleTimer * 0.3f);
            apple.Orientation = MathF.Atan2(dz1, dx1);
            apple.UpdateState();

            pearTimer += 0.012f * (float)deltaTime;
            float x2 = car2RadiusA * MathF.Cos(pearTimer * 0.3f);
            float z2 = car2RadiusB * MathF.Sin(pearTimer * 0.3f);
            pear.Position = new Vector3D<float>(x2, 5f, z2);

            float dx2 = -car2RadiusA * MathF.Sin(pearTimer * 0.3f);
            float dz2 = car2RadiusB * MathF.Cos(pearTimer * 0.3f);
            pear.Orientation = MathF.Atan2(dz2, dx2);
            pear.UpdateState();

            if (squirrel.boundingBox.Intersects(apple.boundingBox) || squirrel.boundingBox.Intersects(pear.boundingBox))
            {
                Vector3D<float> position = new Vector3D<float>(26f, 1.8f, 0f);
                squirrel.SetPosition(position);
                squirrel.SetRotation(0f);
                cameraDescriptor1.SetTarget(squirrel.Position);
                cameraDescriptor1.ResetZYAngle();
                if (activeCameraNum == 1)
                {
                    activeCamera = cameraDescriptor1;
                }
            }
            controller.Update((float)deltaTime);

        }


        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // GL here
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);


            Gl.UseProgram(program);

            SetViewMatrix();
            SetProjectionMatrix();

            SetLightColor();
            SetLightPosition();
            SetViewerPosition();
            SetShininess();

            raceTrack.Render(program, TextureUniformVariableName, ModelMatrixVariableName, NormalMatrixVariableName);

            squirrel.Render(program, TextureUniformVariableName, ModelMatrixVariableName, NormalMatrixVariableName);
            apple.Render(program, TextureUniformVariableName, ModelMatrixVariableName, NormalMatrixVariableName);
            pear.Render(program, TextureUniformVariableName, ModelMatrixVariableName, NormalMatrixVariableName);


            DrawSkyBox();

            //ImGuiNET.ImGui.ShowDemoWindow();
            ImGuiNET.ImGui.Begin("Car Properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGui.Text($"Speed: {(squirrel.speed * 3.6):F2} km/h");
            ImGuiNET.ImGui.End();


            controller.Render();
        }

        private static unsafe void DrawSkyBox()
        {
            Matrix4X4<float> modelMatrix = Matrix4X4.CreateScale(1000f);
            SetModelMatrix(modelMatrix);
            Gl.BindVertexArray(skyBox.Vao);

            int textureLocation = Gl.GetUniformLocation(program, TextureUniformVariableName);
            if (textureLocation == -1)
            {
                throw new Exception($"{TextureUniformVariableName} uniform not found on shader.");
            }
            // set texture 0
            Gl.Uniform1(textureLocation, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)GLEnum.Linear);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)GLEnum.Linear);
            Gl.BindTexture(TextureTarget.Texture2D, skyBox.Texture.Value);

            Gl.DrawElements(GLEnum.Triangles, skyBox.IndexArrayLength, GLEnum.UnsignedInt, null);
            Gl.BindVertexArray(0);

            CheckError();
            Gl.BindTexture(TextureTarget.Texture2D, 0);
            CheckError();
        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 1f, 1f, 1f);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, 0f, 10f, 0f);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, activeCamera.Position.X, activeCamera.Position.Y, activeCamera.Position.Z);
            CheckError();
        }

        private static unsafe void SetShininess()
        {
            int location = Gl.GetUniformLocation(program, ShininessVariableName);

            if (location == -1)
            {
                throw new Exception($"{ShininessVariableName} uniform not found on shader.");
            }

            Gl.Uniform1(location, Shininess);
            CheckError();
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError();

            var modelMatrixWithoutTranslation = new Matrix4X4<float>(modelMatrix.Row1, modelMatrix.Row2, modelMatrix.Row3, modelMatrix.Row4);
            modelMatrixWithoutTranslation.M41 = 0;
            modelMatrixWithoutTranslation.M42 = 0;
            modelMatrixWithoutTranslation.M43 = 0;
            modelMatrixWithoutTranslation.M44 = 1;

            Matrix4X4<float> modelInvers;
            Matrix4X4.Invert<float>(modelMatrixWithoutTranslation, out modelInvers);
            Matrix3X3<float> normalMatrix = new Matrix3X3<float>(Matrix4X4.Transpose(modelInvers));
            location = Gl.GetUniformLocation(program, NormalMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{NormalMatrixVariableName} uniform not found on shader.");
            }
            Gl.UniformMatrix3(location, 1, false, (float*)&normalMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {

            squirrel = new GlObjectForest(Gl, "squirrel");
            squirrel.SetPosition(new Vector3D<float>(26f, 1.8f, 0f));
            squirrel.SetScale(0.2f);
            squirrel.maxSpeed = 30.0f;
            squirrel.speed = 0f;
            squirrel.acceleration = 5.0f;
            squirrel.steeringAngle = 10.0f;
            squirrel.wheelBase = 2.8f;
            squirrel.SetBoundingBoxDimensions(1.3f, 2.5f, 6.4f);

            apple = new GlObjectForest(Gl, "apple");
            apple.SetScale(10.0f);
            apple.SetRotation(90.0f * MathF.PI / 180); // Y-axis rotation
            apple.SetPosition(new Vector3D<float>(33.0f,1.5f, 0f));
            apple.speed = 25.0f;
            car1RadiusA = 35.0f;
            car1RadiusB = 155.0f;
            apple.SetBoundingBoxDimensions(0.7f, 1.25f, 3.20f);

            pear = new GlObjectForest(Gl, "10197_Pear");
            Vector3D<float> opponent1Position2 = new Vector3D<float>(39.5f, 2.0f, 0f);
            pear.SetScale(0.5f);
            pear.SetPosition(opponent1Position2);
            pear.speed = 27f;
            car2RadiusA = 40.0f;
            car2RadiusB = 170.0f;
            pear.SetBoundingBoxDimensions(2.7f, 1.15f, 0.7f);

            GlObject glObject = ObjectResourceReader.CreateObjectWithTextureFromResource(Gl, "terrain.RaceTrack.obj", "terrain.RaceTrack.png");
            raceTrack = new GlObject(glObject.Vao, glObject.Vertices, glObject.Colors, glObject.Indices, glObject.IndexArrayLength, Gl, glObject.Texture.Value);
            raceTrack.Scale = Matrix4X4.CreateScale(1.5f);
            raceTrack.ModelMatrix *= raceTrack.Scale;

            skyBox = GlCube.CreateInteriorCube(Gl, "");
        }




        private static void Window_Closing()
        {
            raceTrack.ReleaseGlObject();
            squirrel.Release();
            apple.Release();
            pear.Release();
            // glCubeRotating.ReleaseGlObject();
        }

        private static unsafe void SetProjectionMatrix()
        {
            var projectionMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)Math.PI / 4f, 1024f / 768f, 0.1f, 1000);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&projectionMatrix);
            CheckError();
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(activeCamera.Position, activeCamera.Target, activeCamera.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        public static void CheckError()
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("GL.GetError() returned " + error.ToString());
        }
    }
}
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL.Extensions.ImGui;

namespace Project
{
    internal static class Program
    {
        private static ImGuiController controller;
        private static int rollDirecttion = 1;
        private static CameraDescriptor cameraDescriptor = new();

        private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static GlCube[] cubeComponents = new GlCube[27];
        private static uint vertices;
        private static uint colors;
        private static uint indices;
        private static uint indexLength;

        private static uint program;


        private const string ModelMatrixVariableName = "uModel";
        private const string NormalMatrixVariableName = "uNormal";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";
        private const string LightColorVariableName = "lightColor";
        private const string LightPositionVariableName = "lightPos";
        private const string ViewPosVariableName = "viewPos";
        private static float LightColorRed = 1.0f;
        private static float LightColorGreen = 1.0f;
        private static float LightColorBlue = 1.0f;
        private static float LightPositionX = 0.0f;
        private static float LightPositionY = 0.0f;
        private static float LightPositionZ = 2.0f;
        private static Vector3 LightPosition = new Vector3(LightPositionX, LightPositionY, LightPositionZ);

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;
        layout (location = 2) in vec3 vNorm;

        uniform mat4 uModel;
        uniform mat3 uNormal;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        out vec3 outNormal;
        out vec3 outWorldPosition;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
            outNormal = uNormal*vNorm;
            outWorldPosition = vec3(uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0));
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        
        uniform vec3 lightColor;
        uniform vec3 lightPos;
        uniform vec3 viewPos;

        out vec4 FragColor;

		in vec4 outCol;
        in vec3 outNormal;
        in vec3 outWorldPosition;

        void main()
        {
            float shininess = 50;
            float ambientStrength = 0.2;
            vec3 ambient = ambientStrength * lightColor;

            float diffuseStrength = 0.3;
            vec3 norm = normalize(outNormal);
            vec3 lightDir = normalize(lightPos - outWorldPosition);
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = diff * lightColor * diffuseStrength;

            float specularStrength = 0.5;
            vec3 viewDir = normalize(viewPos - outWorldPosition);
            vec3 reflectDir = reflect(-lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), shininess) / max(dot(norm,viewDir), -dot(norm,lightDir));
            vec3 specular = specularStrength * spec * lightColor;  

            vec3 result = (ambient + diffuse + specular) * outCol.xyz;
            FragColor = vec4(result, outCol.w);
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab3_3";
            windowOptions.Size = new Vector2D<int>(1500, 1500);
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

            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }

            Gl = window.CreateOpenGL();

            controller = new ImGuiController(Gl, window, inputContext);

            Gl.ClearColor(System.Drawing.Color.White);

            SetUpObjects();

            CreateProgram();

            Gl.Enable(EnableCap.CullFace);

            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);
        }

        private static void CreateProgram()
        {
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            Gl.ShaderSource(fshader, FragmentShaderSource);
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

        private static void Keyboard_KeyDown(IKeyboard keyboard, Key key, int arg3)
        {
            switch (key)
            {
                case Key.Left:
                    cameraDescriptor.DecreaseZYAngle();
                    break;
                    ;
                case Key.Right:
                    cameraDescriptor.IncreaseZYAngle();
                    break;
                case Key.Down:
                    cameraDescriptor.IncreaseDistance();
                    break;
                case Key.Up:
                    cameraDescriptor.DecreaseDistance();
                    break;
                case Key.U:
                    cameraDescriptor.IncreaseZXAngle();
                    break;
                case Key.D:
                    cameraDescriptor.DecreaseZXAngle();
                    break;
                case Key.Space:
                    cubeArrangementModel.AnimationEnabeld = true;
                    cubeArrangementModel.OldDirection = rollDirecttion;
                    rollDirecttion = 1;
                    break;
                case Key.Backspace:
                    cubeArrangementModel.AnimationEnabeld = true;
                    cubeArrangementModel.OldDirection = rollDirecttion;
                    rollDirecttion = -1;
                    break;
            }
        }


        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            cubeArrangementModel.AdvanceTime(deltaTime, rollDirecttion);
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

            DrawCubeComponents();

            ImGuiNET.ImGui.Begin("Lighting properties",
                ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar);
            ImGuiNET.ImGui.SliderFloat("Light Color Red", ref LightColorRed, 0.0f, 1.0f);
            ImGuiNET.ImGui.SliderFloat("Light Color Green", ref LightColorGreen, 0.0f, 1.0f);
            ImGuiNET.ImGui.SliderFloat("Light Color Blue", ref LightColorBlue, 0.0f, 1.0f);
            ImGuiNET.ImGui.InputFloat3("Light Origin Cooridinates", ref LightPosition);
            //ImGuiNET.ImGui.Button("Rotate Top Left");
            //ImGuiNET.ImGui.Button("Rotate Top Right");
            if (ImGui.Button("Rotate Top Left"))
            {
                cubeArrangementModel.AnimationEnabeld = true;
                cubeArrangementModel.OldDirection = rollDirecttion;
                rollDirecttion = -1;
            }

            if (ImGui.Button("Rotate Top Right"))
            {
                cubeArrangementModel.AnimationEnabeld = true;
                cubeArrangementModel.OldDirection = rollDirecttion;
                rollDirecttion = 1;
            }

            ImGuiNET.ImGui.End();
            controller.Render();

        }

        private static unsafe void SetLightColor()
        {
            int location = Gl.GetUniformLocation(program, LightColorVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightColorVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, LightColorRed, LightColorGreen, LightColorBlue);
            CheckError();
        }

        private static unsafe void SetLightPosition()
        {
            int location = Gl.GetUniformLocation(program, LightPositionVariableName);

            if (location == -1)
            {
                throw new Exception($"{LightPositionVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, LightPosition.X, LightPosition.Y, LightPosition.Z);
            CheckError();
        }

        private static unsafe void SetViewerPosition()
        {
            int location = Gl.GetUniformLocation(program, ViewPosVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewPosVariableName} uniform not found on shader.");
            }

            Gl.Uniform3(location, cameraDescriptor.Position.X, cameraDescriptor.Position.Y, cameraDescriptor.Position.Z);
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

        private static unsafe void DrawCubeComponents()
        {
            for (int i = 0; i < 3; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    for (int k = 0; k < 3; ++k)
                    {
                        Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(1.0f);
                        Matrix4X4<float> trans = Matrix4X4.CreateTranslation((k - 1) * 1.05f, (i - 1) * 1.05f, (j - 1) * 1.05f);
                        Matrix4X4<float> modelMatrix;
                        if (i == 2)
                        {
                            Matrix4X4<float> rotx = Matrix4X4.CreateRotationX((float)Math.PI / 4f);
                            Matrix4X4<float> rotz = Matrix4X4.CreateRotationZ((float)Math.PI / 4f);
                            Matrix4X4<float> rotGlobY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleRevolutionOnGlobalY);
                            Matrix4X4<float> rotLocY = Matrix4X4.CreateRotationY((float)cubeArrangementModel.DiamondCubeAngleOwnRevolution);
                            modelMatrix = diamondScale * trans * rotLocY;
                            SetModelMatrix(modelMatrix);
                        }
                        else
                        {
                            modelMatrix = diamondScale * trans;
                            SetModelMatrix(modelMatrix);
                        }

                        SetModelMatrix(modelMatrix);
                        Gl.BindVertexArray(cubeComponents[i * 9 + j * 3 + k].Vao);
                        Gl.DrawElements(GLEnum.Triangles, cubeComponents[i * 9 + j * 3 + k].IndexArrayLength, GLEnum.UnsignedInt, null);
                        Gl.BindVertexArray(0);
                    }
                }
            }
        }

        private static unsafe void SetViewMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetUpObjects()
        {

            float[][] faceColorsContainer =
            {
                new float[] { 1.0f, 0.0f, 0.0f, 1.0f },
                new float[] { 0.0f, 1.0f, 0.0f, 1.0f },
                new float[] { 0.0f, 0.0f, 1.0f, 1.0f },
                new float[] { 1.0f, 0.0f, 1.0f, 1.0f },
                new float[] { 0.0f, 1.0f, 1.0f, 1.0f },
                new float[] { 1.0f, 1.0f, 0.0f, 1.0f },
                new float[] { 0.0f, 0.0f, 0.0f, 1.0f }
            };

            float[][] faceColors =
            {
                new float[4],
                new float[4],
                new float[4],
                new float[4],
                new float[4],
                new float[4],
            };

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        for (int colorIndex = 0; colorIndex < 6; colorIndex++)
                        {
                            faceColors[colorIndex] = faceColorsContainer[colorIndex];
                        }
                        if (i <= 1)
                        {
                            faceColors[0] = faceColorsContainer[6];
                        }
                        if (i >= 1)
                        {
                            faceColors[3] = faceColorsContainer[6];
                        }
                        if (j <= 1)
                        {
                            faceColors[1] = faceColorsContainer[6];
                        }
                        if (j >= 1)
                        {
                            faceColors[4] = faceColorsContainer[6];
                        }
                        if (k >= 1)
                        {
                            faceColors[2] = faceColorsContainer[6];
                        }
                        if (k <= 1)
                        {
                            faceColors[5] = faceColorsContainer[6];
                        }
                        cubeComponents[i * 9 + j * 3 + k] = GlCube.CreateCubeWithFaceColors(Gl, faceColors[0], faceColors[1], faceColors[2], faceColors[3], faceColors[4], faceColors[5]);
                    }
                }
            }

        }

        private static void Window_Closing()
        {
            for (int i = 0; i < 27; i++)
            {
                cubeComponents[i].ReleaseGlCube();
            }
        }

        private static unsafe void SetProjectionMatrix()
        {
            var viewMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>((float)(Math.PI / 2), 1024f / 768f, 0.1f, 100);
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);

            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError();
        }

        private static unsafe void SetCameraMatrix()
        {
            var viewMatrix = Matrix4X4.CreateLookAt(cameraDescriptor.Position, cameraDescriptor.Target, cameraDescriptor.UpVector);

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

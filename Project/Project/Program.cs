using lab2_1;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Project
{
    internal static class Program
    {

        private static CameraDescriptor cameraDescriptor = new();

        //private static CubeArrangementModel cubeArrangementModel = new();

        private static IWindow window;

        private static GL Gl;

        private static uint[] vao = new uint[27];
        private static uint vertices;
        private static uint colors;
        private static uint indices;
        private static uint indexLength;

        private static uint program;


        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;


        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";


        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;

        void main()
        {
            FragColor = outCol;
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "lab2_1";
            windowOptions.Size = new Vector2D<int>(500, 500);

            window = Window.Create(windowOptions);

            window.Load += Window_Load;
            window.Update += Window_Update;
            window.Render += Window_Render;
            window.Closing += Window_Closing;

            window.Run();
        }
        public static void CheckError(String func_name)
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("Error(" + func_name + ") GL.GetError() returned " + error.ToString());
        }

        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            cameraDescriptor.SetDistance(4.56f);
            cameraDescriptor.SetZXAngle((float)Math.PI / 180 * 30);
            cameraDescriptor.SetZYAngle((float)Math.PI / 180 * 30);

            Gl = window.CreateOpenGL();

            Gl.ClearColor(System.Drawing.Color.White);

            SetUpModelObjects();

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


        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // multithreaded
            // make sure it is threadsafe
            // NO GL calls
            //cubeArrangementModel.AdvanceTime(deltaTime);
        }

        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {

            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError("SetModelMatrix");
        }

        private static unsafe void SetUpModelObjects()
        {

            // counter clockwise is front facing
            float[] vertexArray = new float[] {
                -0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,

                -0.5f, 0.5f, 0.5f,
                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, 0.5f, 0.5f,

                -0.5f, 0.5f, 0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, 0.5f,

                -0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, 0.5f,
                0.5f, -0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, -0.5f,
                -0.5f, 0.5f, -0.5f,
                -0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,

                0.5f, 0.5f, 0.5f,
                0.5f, 0.5f, -0.5f,
                0.5f, -0.5f, -0.5f,
                0.5f, -0.5f, 0.5f,

            };

            /*
            float[] colorArray = new float[] {
                //top
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,
                1.0f, 0.0f, 0.0f, 1.0f,

                //front
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,
                0.0f, 1.0f, 0.0f, 1.0f,

                //left
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,
                0.0f, 0.0f, 1.0f, 1.0f,

                //bottom
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,
                1.0f, 0.0f, 1.0f, 1.0f,

                //back
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,
                0.0f, 1.0f, 1.0f, 1.0f,

                //right
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
                1.0f, 1.0f, 0.0f, 1.0f,
            };*/


            float[][] colorArray = new float[27][];
            for (int i = 0; i < 27; i++)
            {
                colorArray[i] = new float[96];
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; ++j)
                {
                    for (int k = 0; k < 3; ++k)
                    {
                        for (int l = 0; l < 96; l += 4)
                        {
                            colorArray[i * 9 + j * 3 + k][l + 3] = 1.0f;
                            if (l < 16)
                            {
                                if (i > 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                            else if (l < 32)
                            {
                                if (j > 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                            else if (l < 48)
                            {
                                if (k < 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                            else if (l < 64)
                            {
                                if (i < 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                            else if (l < 80)
                            {
                                if (j < 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                            else if (l < 96)
                            {
                                if (k > 1)
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                                else
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f;
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f;
                                }
                            }
                        }
                    }
                }
            }

            uint[] indexArray = new uint[] {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                10, 11, 8,

                12, 14, 13,
                12, 15, 14,

                17, 16, 19,
                17, 19, 18,

                20, 22, 21,
                20, 23, 22
            };

            for (int i = 0; i < 27; i++)
            {
                vao[i] = Gl.GenVertexArray();
                Gl.BindVertexArray(vao[i]);
                vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(0);

                colors = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, colors);

                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray[i].AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(1);

                indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            }

            Gl.BindVertexArray(0);

            indexLength = (uint)indexArray.Length;
        }
    }
}
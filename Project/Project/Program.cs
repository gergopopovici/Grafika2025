using Project;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Project
{
    internal static class Program
    {
        // Camera setup to control viewpoint
        private static CameraDescriptor cameraDescriptor = new();

        //private static CubeArrangementModel cubeArrangementModel = new();

        // Main window instance
        private static IWindow window;

        // OpenGL context
        private static GL Gl;

        private static int rollDirecttion = 1;  // Direction of cube rotation (1 or -1)

        private static CubeArrangementModel cubeArrangementModel = new();  // Model for cube animation state

        // Array of Vertex Array Objects - one for each of the 27 cubes (3x3x3)
        private static uint[] vao = new uint[27];
        private static uint vertices;  // Buffer for vertex positions
        private static uint colors;    // Buffer for color data
        private static uint indices;   // Buffer for indices/element data
        private static uint indexLength;  // Number of indices to draw

        private static uint program;

        private const string ModelMatrixVariableName = "uModel";
        private const string ViewMatrixVariableName = "uView";
        private const string ProjectionMatrixVariableName = "uProjection";

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
		layout (location = 1) in vec4 vCol;

        uniform mat4 uModel;      // Model transformation
        uniform mat4 uView;       // View/camera transformation
        uniform mat4 uProjection; // Projection transformation

		out vec4 outCol;
        
        void main()
        {
			outCol = vCol;  // Pass color to fragment shader
            gl_Position = uProjection*uView*uModel*vec4(vPos.x, vPos.y, vPos.z, 1.0);  // Transform vertex
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
		
		in vec4 outCol;  // Color received from vertex shader

        void main()
        {
            FragColor = outCol;  // Use the color from vertex shader
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

        // Helper function to check for OpenGL errors
        public static void CheckError(String func_name)
        {
            var error = (ErrorCode)Gl.GetError();
            if (error != ErrorCode.NoError)
                throw new Exception("Error(" + func_name + ") GL.GetError() returned " + error.ToString());
        }

        // Called when window is created - initialize OpenGL resources
        private static void Window_Load()
        {
            //Console.WriteLine("Load");

            // Set up camera position and angles
            cameraDescriptor.SetDistance(4.56f);
            cameraDescriptor.SetZXAngle((float)Math.PI / 180 * 30);  // 30 degrees in radians
            cameraDescriptor.SetZYAngle((float)Math.PI / 180 * 30);  // 30 degrees in radians

            // Get OpenGL context
            Gl = window.CreateOpenGL();

            // Set background color to white
            Gl.ClearColor(System.Drawing.Color.White);

            // Create all the cubes' geometry
            SetUpModelObjects();

            // Compile and link shaders
            CreateProgram();

            // Enable back-face culling (don't render faces pointing away from camera)
            Gl.Enable(EnableCap.CullFace);

            // Enable depth testing (closer objects obscure farther ones)
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Lequal);

            IInputContext inputContext = window.CreateInput();
            foreach (var keyboard in inputContext.Keyboards)
            {
                keyboard.KeyDown += Keyboard_KeyDown;
            }
        }

        // Compile and link shader program
        private static void CreateProgram()
        {
            // Create shader objects
            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            // Set vertex shader source and compile
            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + Gl.GetShaderInfoLog(vshader));

            // Set fragment shader source and compile
            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);

            // Create program, attach shaders, and link
            program = Gl.CreateProgram();
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            Gl.LinkProgram(program);
            Gl.GetProgram(program, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                Console.WriteLine($"Error linking shader {Gl.GetProgramInfoLog(program)}");
            }

            // Clean up shader objects after linking
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

        // Called each frame before rendering - for updates that don't require OpenGL
        private static void Window_Update(double deltaTime)
        {
            //Console.WriteLine($"Update after {deltaTime} [s].");
            // This method runs in a separate thread
            // Don't make any OpenGL calls here - they're not thread-safe
            //cubeArrangementModel.AdvanceTime(deltaTime);  // Would handle cube rotations/animations
        }

        // Called each frame to render the scene
        private static unsafe void Window_Render(double deltaTime)
        {
            //Console.WriteLine($"Render after {deltaTime} [s].");

            // Clear the color and depth buffers
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Clear(ClearBufferMask.DepthBufferBit);

            // Use our shader program for rendering
            Gl.UseProgram(program);

            // Set camera (view) and projection matrices in the shader
            SetCameraMatrix();
            SetProjectionMatrix();

            // Render each of the 27 cubes in the 3x3x3 arrangement
            for (int i = 0; i < 3; i++)       // Y axis (up/down)
            {
                for (int j = 0; j < 3; j++)   // Z axis (front/back)
                {
                    for (int k = 0; k < 3; k++)  // X axis (left/right)
                    {
                        // Create model matrix for this cube
                        Matrix4X4<float> diamondScale = Matrix4X4.CreateScale(1.0f);  // Unit scale
                        // Position cube with small gaps between (1.05 instead of 1.0)
                        Matrix4X4<float> trans = Matrix4X4.CreateTranslation(
                            (k - 1) * 1.05f,  // X position (-1.05, 0, or 1.05)
                            (i - 1) * 1.05f,  // Y position (-1.05, 0, or 1.05)
                            (j - 1) * 1.05f   // Z position (-1.05, 0, or 1.05)
                        );
                        Matrix4X4<float> modelMatrix = diamondScale * trans;
                        SetModelMatrix(modelMatrix);

                        // Bind this cube's VAO and draw it
                        Gl.BindVertexArray(vao[i * 9 + j * 3 + k]);  // Calculate index in the VAO array
                        Gl.DrawElements(GLEnum.Triangles, indexLength, GLEnum.UnsignedInt, null);
                    }
                }
            }

            // Unbind VAO
            Gl.BindVertexArray(0);
        }

        // Set the model matrix uniform in the shader
        private static unsafe void SetModelMatrix(Matrix4X4<float> modelMatrix)
        {
            // Get location of model matrix uniform
            int location = Gl.GetUniformLocation(program, ModelMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ModelMatrixVariableName} uniform not found on shader.");
            }

            // Upload matrix data to shader
            Gl.UniformMatrix4(location, 1, false, (float*)&modelMatrix);
            CheckError("SetModelMatrix");
        }

        // Create all vertex, color, and index buffers for the cubes
        private static unsafe void SetUpModelObjects()
        {
            // Define cube vertices (8 corners, repeated for each face to support different colors per face)
            // counter clockwise winding is front facing
            float[] vertexArray = new float[] {
                // Top face (y = 0.5)
                -0.5f, 0.5f, 0.5f,    // top-front-left
                0.5f, 0.5f, 0.5f,     // top-front-right
                0.5f, 0.5f, -0.5f,    // top-back-right
                -0.5f, 0.5f, -0.5f,   // top-back-left

                // Front face (z = 0.5)
                -0.5f, 0.5f, 0.5f,    // top-front-left
                -0.5f, -0.5f, 0.5f,   // bottom-front-left
                0.5f, -0.5f, 0.5f,    // bottom-front-right
                0.5f, 0.5f, 0.5f,     // top-front-right

                // Left face (x = -0.5)
                -0.5f, 0.5f, 0.5f,    // top-front-left
                -0.5f, 0.5f, -0.5f,   // top-back-left
                -0.5f, -0.5f, -0.5f,  // bottom-back-left
                -0.5f, -0.5f, 0.5f,   // bottom-front-left

                // Bottom face (y = -0.5)
                -0.5f, -0.5f, 0.5f,   // bottom-front-left
                0.5f, -0.5f, 0.5f,    // bottom-front-right
                0.5f, -0.5f, -0.5f,   // bottom-back-right
                -0.5f, -0.5f, -0.5f,  // bottom-back-left

                // Back face (z = -0.5)
                0.5f, 0.5f, -0.5f,    // top-back-right
                -0.5f, 0.5f, -0.5f,   // top-back-left
                -0.5f, -0.5f, -0.5f,  // bottom-back-left
                0.5f, -0.5f, -0.5f,   // bottom-back-right

                // Right face (x = 0.5)
                0.5f, 0.5f, 0.5f,     // top-front-right
                0.5f, 0.5f, -0.5f,    // top-back-right
                0.5f, -0.5f, -0.5f,   // bottom-back-right
                0.5f, -0.5f, 0.5f,    // bottom-front-right
            };

            // Create color arrays for each of the 27 cubes
            float[][] colorArray = new float[27][];
            for (int i = 0; i < 27; i++)
            {
                colorArray[i] = new float[96];  // 24 vertices * 4 color components (RGBA)
            }

            // Set colors for each cube face
            // i = Y position (0-2)
            // j = Z position (0-2)
            // k = X position (0-2)
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; ++j)
                {
                    for (int k = 0; k < 3; ++k)
                    {
                        // For each vertex (24 vertices * 4 color components)
                        for (int l = 0; l < 96; l += 4)
                        {
                            // Set alpha to 1.0 for all vertices
                            colorArray[i * 9 + j * 3 + k][l + 3] = 1.0f;

                            // Top face (red if on top layer, black otherwise)
                            if (l < 16)
                            {
                                if (i > 1)  // Top layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                                else  // Not on top layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                            // Front face (green if on front layer, black otherwise)
                            else if (l < 32)
                            {
                                if (j > 1)  // Front layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                                else  // Not on front layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                            // Left face (blue if on left layer, black otherwise)
                            else if (l < 48)
                            {
                                if (k < 1)  // Left layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f; // B
                                }
                                else  // Not on left layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                            // Bottom face (magenta if on bottom layer, black otherwise)
                            else if (l < 64)
                            {
                                if (i < 1)  // Bottom layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f; // B
                                }
                                else  // Not on bottom layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                            // Back face (cyan if on back layer, black otherwise)
                            else if (l < 80)
                            {
                                if (j < 1)  // Back layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 1.0f; // B
                                }
                                else  // Not on back layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                            // Right face (yellow if on right layer, black otherwise)
                            else if (l < 96)
                            {
                                if (k > 1)  // Right layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 1.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 1.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                                else  // Not on right layer
                                {
                                    colorArray[i * 9 + j * 3 + k][l] = 0.0f;     // R
                                    colorArray[i * 9 + j * 3 + k][l + 1] = 0.0f; // G
                                    colorArray[i * 9 + j * 3 + k][l + 2] = 0.0f; // B
                                }
                            }
                        }
                    }
                }
            }

            // Define indices for the triangles of each face (6 faces * 2 triangles * 3 vertices)
            uint[] indexArray = new uint[] {
                // Top face (2 triangles)
                0, 1, 2,    // First triangle
                0, 2, 3,    // Second triangle

                // Front face (2 triangles)
                4, 5, 6,
                4, 6, 7,

                // Left face (2 triangles)
                8, 9, 10,
                10, 11, 8,

                // Bottom face (2 triangles)
                12, 14, 13,
                12, 15, 14,

                // Back face (2 triangles)
                17, 16, 19,
                17, 19, 18,

                // Right face (2 triangles)
                20, 22, 21,
                20, 23, 22
            };

            // Create and set up VAOs and buffers for each of the 27 cubes
            for (int i = 0; i < 27; i++)
            {
                // Create and bind VAO
                vao[i] = Gl.GenVertexArray();
                Gl.BindVertexArray(vao[i]);

                // Create, bind, and fill vertex buffer
                vertices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, vertices);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(0);

                // Create, bind, and fill color buffer
                colors = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ArrayBuffer, colors);
                Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)colorArray[i].AsSpan(), GLEnum.StaticDraw);
                Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(1);

                // Create, bind, and fill index buffer
                indices = Gl.GenBuffer();
                Gl.BindBuffer(GLEnum.ElementArrayBuffer, indices);
                Gl.BufferData(GLEnum.ElementArrayBuffer, (ReadOnlySpan<uint>)indexArray.AsSpan(), GLEnum.StaticDraw);
            }

            // Unbind VAO
            Gl.BindVertexArray(0);

            // Store the number of indices for DrawElements calls
            indexLength = (uint)indexArray.Length;
        }

        // Clean up OpenGL resources when window is closing
        private static void Window_Closing()
        {
            // Always unbind the vertex buffer first to avoid partial rendering
            Gl.DeleteBuffer(vertices);
            Gl.DeleteBuffer(colors);
            Gl.DeleteBuffer(indices);

            // Delete all VAOs
            for (int i = 0; i < 27; i++)
            {
                Gl.DeleteVertexArray(vao[i]);
            }
        }

        // Set projection matrix uniform in shader
        private static unsafe void SetProjectionMatrix()
        {
            // Create perspective projection matrix
            var projMatrix = Matrix4X4.CreatePerspectiveFieldOfView<float>(
                (float)(Math.PI / 2),  // 90 degree field of view
                1024f / 768f,          // Aspect ratio
                0.1f,                  // Near clip plane
                100                    // Far clip plane
            );

            // Get location of projection matrix uniform
            int location = Gl.GetUniformLocation(program, ProjectionMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ProjectionMatrixVariableName} uniform not found on shader.");
            }

            // Upload matrix data to shader
            Gl.UniformMatrix4(location, 1, false, (float*)&projMatrix);
            CheckError("SetProjectionMatrix");
        }

        // Set view (camera) matrix uniform in shader
        private static unsafe void SetCameraMatrix()
        {
            // Create view matrix from camera descriptor
            var viewMatrix = Matrix4X4.CreateLookAt(
                cameraDescriptor.Position,   // Camera position
                cameraDescriptor.Target,     // Look-at target
                cameraDescriptor.UpVector    // Up vector
            );

            // Get location of view matrix uniform
            int location = Gl.GetUniformLocation(program, ViewMatrixVariableName);
            if (location == -1)
            {
                throw new Exception($"{ViewMatrixVariableName} uniform not found on shader.");
            }

            // Upload matrix data to shader
            Gl.UniformMatrix4(location, 1, false, (float*)&viewMatrix);
            CheckError("SetCameraMatrix");
        }
    }
}
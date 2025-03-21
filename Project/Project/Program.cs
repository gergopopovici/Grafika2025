using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;

namespace Project
{
    internal static class Program
    {
        private static IWindow graphicWindow;
        private static GL Gl;
        private static uint program;

        private static readonly string VertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 vPos;
        layout (location = 1) in vec4 vCol;
        out vec4 outCol;
        void main()
        {
            outCol = vCol
            gl_Position = vec4(vPos.x, vPos.y, vPos.z, 1.0);
        }
        ";

        private static readonly string FragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        in vec4 outCol;
        void main()
        {
            FragColor = outCol + 1.0; // ERROR: Invalid operation on vec4
        }
        ";

        static void Main(string[] args)
        {
            WindowOptions windowOptions = WindowOptions.Default;
            windowOptions.Title = "Lab1-1 Teszt";
            windowOptions.Size = new Silk.NET.Maths.Vector2D<int>(500, 500);
            graphicWindow = Window.Create(windowOptions);
            graphicWindow.Load += GraphicWindow_Load;
            graphicWindow.Update += GraphicWindow_Update;
            graphicWindow.Render += GraphicWindow_Render;
            graphicWindow.Run();
        }

        private static void CheckGLError(string stage)
        {
            var error = Gl.GetError();
            if (error != GLEnum.NoError)
            {
                Console.WriteLine($"OpenGL Error after {stage}: {error}");
            }
        }

        private static void GraphicWindow_Load()
        {
            Gl = graphicWindow.CreateOpenGL();
            Gl.ClearColor(System.Drawing.Color.White);

            uint vshader = Gl.CreateShader(ShaderType.VertexShader);
            uint fshader = Gl.CreateShader(ShaderType.FragmentShader);

            Gl.ShaderSource(vshader, VertexShaderSource);
            Gl.CompileShader(vshader);
            CheckGLError("Vertex Shader Compilation");

            Gl.GetShader(vshader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
            {
                Console.WriteLine($"Vertex shader compilation failed: {Gl.GetShaderInfoLog(vshader)}");
            }

            Gl.ShaderSource(fshader, FragmentShaderSource);
            Gl.CompileShader(fshader);
            CheckGLError("Fragment Shader Compilation");

            Gl.GetShader(fshader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
            {
                Console.WriteLine($"Fragment shader compilation failed: {Gl.GetShaderInfoLog(fshader)}");
            }

            program = Gl.CreateProgram();
            Gl.LinkProgram(program);
            Gl.AttachShader(program, vshader);
            Gl.AttachShader(program, fshader);
            CheckGLError("Shader Program Linking");

            Gl.DetachShader(program, vshader);
            Gl.DetachShader(program, fshader);
            Gl.DeleteShader(vshader);
            Gl.DeleteShader(fshader);
        }

        private static void GraphicWindow_Update(double deltaTime)
        {
            // Placeholder for update logic
        }

        private static unsafe void GraphicWindow_Render(double deltaTime)
        {
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            CheckGLError("Clearing Buffer");

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            float[] vertexArray = new float[] {
                -0.5f, -0.5f, 0.0f,
                +0.5f, -0.5f, 0.0f,
                0.0f, +0.5f, 0.0f,
                999.0f
            };

            uint vertices = Gl.GenBuffer();
            // Gl.BindBuffer(GLEnum.ArrayBuffer, vertices); 
            Gl.BufferData(GLEnum.ArrayBuffer, (ReadOnlySpan<float>)vertexArray.AsSpan(), GLEnum.StaticDraw);
            CheckGLError("Uploading Vertex Buffer Data");

            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, null);
            Gl.EnableVertexAttribArray(2);
            CheckGLError("Vertex Attribute Setup");

            Gl.UseProgram(program);
            Gl.DrawArrays(GLEnum.Triangles, 0, 3);
            CheckGLError("Drawing Triangles");

            Gl.DeleteBuffer(vertices);
            Gl.DeleteVertexArray(vao);
        }
    }
}
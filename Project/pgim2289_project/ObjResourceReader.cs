using Silk.NET.Maths;
using Silk.NET.OpenGL;
using StbImageSharp;
using System.Globalization;

namespace pgim2289_project
{
    internal class ObjVertexTransformationData
    {
        private static bool hasFaceNormals { get; set; } = false;
        public readonly Vector3D<float> Coordinates;
        public readonly Vector2D<float> TextureCoords;
        public Vector3D<float> Normal { get; internal set; }

        private int aggregatedFaceCount;

        public ObjVertexTransformationData(Vector3D<float> coordinates, Vector3D<float> initialNormal, Vector2D<float> textureCoords, int aggregatedFaceCount)
        {
            this.Coordinates = coordinates;
            this.TextureCoords = textureCoords;
            this.Normal = Vector3D.Normalize(initialNormal);
            this.aggregatedFaceCount = aggregatedFaceCount;
        }

        internal void UpdateNormalWithContributionFromAFace(Vector3D<float> normal)
        {
            var newNormalToNormalize = aggregatedFaceCount == 0 ? normal : (aggregatedFaceCount * Normal + normal) / (aggregatedFaceCount + 1);
            var newNormal = Vector3D.Normalize(newNormalToNormalize);
            Normal = newNormal;
            ++aggregatedFaceCount;
        }
    }

    struct ObjFace
    {
        public int coordsIndex;
        public int textureCoordsIndex;
        public int normalsIndex;
    }

    internal class ObjectResourceReader
    {
        private static bool voltTextura = false;
        private static bool voltNormalis = false;
        private static int osszesFaceDrb = 0;
        private static bool hasFaceNormals;
        private static void ParseFaceElement(string element, int[] face, int[] faceNormal, int index)
        {
            var parts = element.Split('/');
            face[index] = int.Parse(parts[0]);
            if (parts.Length == 3)
            {
                hasFaceNormals = true;
                faceNormal[index] = int.Parse(parts[2]);
            }
        }

        public static unsafe GlObject CreateObjectWithTextureFromResource(GL Gl, string resourceName, string textureName, float[]? defaultColor = null)
        {
            voltNormalis = false;
            voltTextura = false;
            List<float[]> objVertices = new List<float[]>();
            List<float[]> objNormalVectors = new List<float[]>();
            List<float[]> objTextureCoords = new List<float[]>();
            List<ObjFace[]> objFaces = new List<ObjFace[]>();

            string fullResourceName = "pgim2289_project.Resources." + resourceName;
            using (var objStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream(fullResourceName))
            using (var objReader = new StreamReader(objStream))
            {
                while (!objReader.EndOfStream)
                {
                    var line = objReader.ReadLine();

                    if (string.IsNullOrWhiteSpace(line) || line.Length == 1)
                        continue;

                    var lineClassifier = line.Substring(0, line.IndexOf(' '));
                    var lineData = line.Substring(line.IndexOf(" ")).Trim().Split(' ');

                    switch (lineClassifier)
                    {
                        case "v":
                            float[] vertex = new float[3];
                            for (int i = 0; i < vertex.Length; ++i)
                                vertex[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objVertices.Add(vertex);
                            break;
                        case "f":
                            List<ObjFace> face = new List<ObjFace>();
                            if (line.Contains("//") || line.Contains("/"))
                            {
                                for (int i = 0; i < lineData.Length; ++i)
                                {
                                    var data = lineData[i].Trim().Split(new string[] { "//", "/" }, StringSplitOptions.RemoveEmptyEntries);
                                    ObjFace facedata = new();
                                    switch (data.Length)
                                    {
                                        case 1:
                                            facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
                                            break;
                                        case 2:
                                            facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
                                            facedata.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture);
                                            break;
                                        case 3:
                                            facedata.coordsIndex = int.Parse(data[0], CultureInfo.InvariantCulture);
                                            facedata.textureCoordsIndex = int.Parse(data[1], CultureInfo.InvariantCulture);
                                            facedata.normalsIndex = int.Parse(data[2], CultureInfo.InvariantCulture);
                                            break;
                                    }
                                    face.Add(facedata);
                                }
                            }
                            else
                            {
                                ObjFace facedata = new();
                                for (int i = 0; i < lineData.Length; ++i)
                                {
                                    facedata.coordsIndex = int.Parse(lineData[i], CultureInfo.InvariantCulture);
                                    face.Add(facedata);
                                }
                            }

                            if (face.Count >= 3)
                            {
                                for (int i = 1; i < face.Count - 1; i++)
                                {
                                    ObjFace[] haromszog = new ObjFace[3];
                                    haromszog[0] = face[0];
                                    haromszog[1] = face[i];
                                    haromszog[2] = face[i + 1];
                                    objFaces.Add(haromszog);
                                    osszesFaceDrb += 3;
                                }
                            }
                            break;
                        case "vn":
                            voltNormalis = true;
                            float[] normalVektorok = new float[3];
                            for (int i = 0; i < normalVektorok.Length; ++i)
                                normalVektorok[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objNormalVectors.Add(normalVektorok);
                            break;
                        case "vt":
                            voltTextura = true;
                            float[] texCoord = new float[lineData.Length];
                            for (int i = 0; i < texCoord.Length; ++i)
                                texCoord[i] = float.Parse(lineData[i], CultureInfo.InvariantCulture);
                            objTextureCoords.Add(texCoord);
                            break;
                        default:
                            continue;
                    }
                }
            }

            List<float> glVertices = new List<float>();
            List<float> glColors = new List<float>();
            foreach (ObjFace[] faces in objFaces)
            {
                foreach (ObjFace face in faces)
                {
                    glVertices.Add(objVertices[face.coordsIndex - 1][0]);
                    glVertices.Add(objVertices[face.coordsIndex - 1][1]);
                    glVertices.Add(objVertices[face.coordsIndex - 1][2]);

                    if (voltNormalis)
                    {
                        glVertices.Add(objNormalVectors[face.normalsIndex - 1][0]);
                        glVertices.Add(objNormalVectors[face.normalsIndex - 1][1]);
                        glVertices.Add(objNormalVectors[face.normalsIndex - 1][2]);
                    }

                    if (voltTextura)
                    {
                        glVertices.Add(objTextureCoords[face.textureCoordsIndex - 1][0]);
                        glVertices.Add(objTextureCoords[face.textureCoordsIndex - 1][1]);
                    }

                    if (defaultColor != null)
                    {
                        glColors.AddRange(defaultColor);
                    }
                    else
                    {
                        glColors.AddRange([1f, 0f, 0f, 1f]);
                    }
                }
            }

            List<uint> glIndexArray = new List<uint>();
            for (int i = 0; i < osszesFaceDrb; i++)
            {
                glIndexArray.Add((uint)(i));
            }

            uint vao = Gl.GenVertexArray();
            Gl.BindVertexArray(vao);

            uint offsetPos = 0;
            uint offsetNormals = 0;
            if (voltNormalis)
            {
                offsetNormals = offsetPos + 3 * sizeof(float);
            }
            uint offsetTexture = offsetNormals + (3 * sizeof(float));
            uint vertexSize = offsetTexture + (2 * sizeof(float));

            uint vertices = Gl.GenBuffer();
            Gl.BindBuffer(BufferTargetARB.ArrayBuffer, vertices);
            Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glVertices.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
            Gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetPos);
            Gl.EnableVertexAttribArray(0);
            if (voltNormalis)
            {
                Gl.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, true, vertexSize, (void*)offsetNormals);
                Gl.EnableVertexAttribArray(2);
            }

            if (defaultColor != null)
            {
                uint colors = Gl.GenBuffer();
                Gl.BindBuffer(BufferTargetARB.ArrayBuffer, colors);
                Gl.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)glColors.ToArray().AsSpan(), BufferUsageARB.StaticDraw);
                Gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, null);
                Gl.EnableVertexAttribArray(1);

                uint indices = Gl.GenBuffer();
                Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
                Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

                Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

                uint indexArrayLength = (uint)glIndexArray.Count;

                Gl.BindVertexArray(0);

                return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl);
            }
            else
            {
                uint colors = Gl.GenBuffer();
                uint texture = Gl.GenTexture();
                Gl.ActiveTexture(TextureUnit.Texture0);
                Gl.BindTexture(TextureTarget.Texture2D, texture);

                var skyboxImageResult = ReadTextureImage(textureName);
                var textureBytes = (ReadOnlySpan<byte>)skyboxImageResult.Data.AsSpan();
                Gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)skyboxImageResult.Width,
                    (uint)skyboxImageResult.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, textureBytes);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
                Gl.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
                Gl.BindTexture(TextureTarget.Texture2D, 0);

                Gl.EnableVertexAttribArray(3);
                Gl.VertexAttribPointer(3, 2, VertexAttribPointerType.Float, false, vertexSize, (void*)offsetTexture);

                uint indices = Gl.GenBuffer();
                Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, indices);
                Gl.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)glIndexArray.ToArray().AsSpan(), BufferUsageARB.StaticDraw);

                Gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);

                uint indexArrayLength = (uint)glIndexArray.Count;

                Gl.BindVertexArray(0);

                return new GlObject(vao, vertices, colors, indices, indexArrayLength, Gl, texture);
            }
        }

        private static unsafe ImageResult ReadTextureImage(string textureResource)
        {
            ImageResult result;
            using (Stream textureStream = typeof(ObjectResourceReader).Assembly.GetManifestResourceStream("pgim2289_project.Resources." + textureResource))
            {
                result = ImageResult.FromStream(textureStream, ColorComponents.RedGreenBlueAlpha);
            }

            int width = result.Width;
            int height = result.Height;
            int comp = 4;
            byte[] flippedData = new byte[width * height * comp];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int c = 0; c < comp; c++)
                    {
                        flippedData[(y * width + x) * comp + c] = result.Data[((height - 1 - y) * width + x) * comp + c];
                    }
                }
            }

            result.Data = flippedData;
            return result;
        }
    }
}
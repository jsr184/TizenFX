﻿
using System;
using System.Runtime.InteropServices;
using Tizen.NUI.BaseComponents;
using System.Collections.Generic;

namespace Tizen.NUI.Samples
{
    // copied from https://github.com/hinohie/nui-demo/blob/geotest/Mesh/Mesh.cs
    public class DisposeTest : IExample
    {

        public struct Vec2
        {
            float x;
            float y;
            public Vec2(float xIn, float yIn)
            {
                x = xIn;
                y = yIn;
            }
        }
        public struct Vec3
        {
            float x;
            float y;
            float z;
            public Vec3(float xIn, float yIn, float zIn)
            {
                x = xIn;
                y = yIn;
                z = zIn;
            }
        }

        struct TexturedQuadVertex
        {
            public Vec3 position;
            public Vec3 normal;
            public Vec2 texcoord;
        }

        static readonly string VERTEX_SHADER =
        "attribute mediump vec3 aPosition;\n" +
        "attribute mediump vec3 aNormal;\n" +
        "attribute mediump vec2 aTexCoord;\n" +
        "uniform mediump mat4 uMvpMatrix;\n" +
        "uniform mediump mat3 uNormalMatrix;\n" +
        "uniform mediump vec3 uSize;\n" +
        "varying mediump vec3 vNormal;\n" +
        "varying mediump vec2 vTexCoord;\n" +
        "varying mediump vec3 vPosition;\n" +
        "void main()\n" +
        "{\n" +
        //"    vec4 pos = vec4(aPosition, 1.0)*vec4(uSize,1.0);\n"+
        "    vec4 pos = vec4(aPosition, 1.0)*vec4(uSize.xy, 400.0 ,1.0);\n" +
        "    gl_Position = uMvpMatrix*pos;\n" +
        "    vPosition = aPosition;\n" +
        "    vNormal   = normalize(uNormalMatrix * aNormal);\n" +
        "    vTexCoord = aTexCoord;\n" +
        "}\n";

        static readonly string FRAGMENT_SHADER =
        "uniform lowp vec4 uColor;\n" +
        "uniform sampler2D sTexture;\n" +
        "varying mediump vec3 vNormal;\n" +
        "varying mediump vec2 vTexCoord;\n" +
        "varying mediump vec3 vPosition;\n" +
        "mediump vec3 uLightDir = vec3(2.0, 0.5, 1.0);\n" +
        "mediump vec3 uViewDir  = vec3(0.0, 0.0, 1.0);\n" +
        "mediump vec3 uAmbientColor = vec3(0.2, 0.2, 0.2);\n" +
        "mediump vec3 uDiffuseColor = vec3(0.8, 0.8, 0.8);\n" +
        "mediump vec3 uSpecularColor = vec3(0.5, 0.5, 0.5);\n" +
        "void main()\n" +
        "{\n" +
        "    mediump vec3 lightdir = normalize(uLightDir);\n" +
        "    mediump vec3 eyedir   = normalize(uViewDir);\n" +
        "    mediump vec4 texColor = texture2D( sTexture, vTexCoord ) * uColor;\n" +
        "    mediump float diffuse = min(max(-dot(vNormal, lightdir) + 0.1, 0.0), 1.0);\n" +
        "    mediump vec3 reflectdir = reflect(-lightdir, vNormal);\n" +
        "    mediump float specular = pow(max(0.0, dot(reflectdir, eyedir)), 50.0);\n" +
        "    mediump vec4 color = texColor * vec4(uAmbientColor + uDiffuseColor * diffuse, 1.0) + vec4(uSpecularColor, 0.0) * specular;\n" +
        "    gl_FragColor = color;\n" +
        "}\n";

        // Copy from dali-toolkit/internal/visuals/primitive/primitive-visual.cpp
        private global::System.IntPtr SphereVertexDataPtr()
        {
            TexturedQuadVertex[] vertices = new TexturedQuadVertex[SPHERE_VERTEX_NUMBER];

            const int slices = SPHERE_SLICES;
            const int stacks = SPHERE_STACKS;
            // Build start.
            {

                int vertexIndex = 0; //Track progress through vertices.
                float x;
                float y;
                float z;

                //Top stack.
                vertices[vertexIndex].position = new Vec3(0.0f, 0.5f, 0.0f);
                vertices[vertexIndex].normal = new Vec3(0.0f, 1.0f, 0.0f);
                vertices[vertexIndex].texcoord = new Vec2(0.5f, 1.0f);
                vertexIndex++;

                //Middle stacks.
                for (int i = 1; i < stacks; i++)
                {
                    for (int j = 0; j < slices; j++, vertexIndex++)
                    {
                        float cos_j = (float)Math.Cos(2.0f * (float)Math.PI * j / (float)slices);
                        float sin_j = (float)Math.Sin(2.0f * (float)Math.PI * j / (float)slices);
                        float cos_i = (float)Math.Cos((float)Math.PI * i / (float)stacks);
                        float sin_i = (float)Math.Sin((float)Math.PI * i / (float)stacks);
                        x = cos_j * sin_i;
                        y = cos_i;
                        z = sin_j * sin_i;

                        vertices[vertexIndex].position = new Vec3(x / 2.0f, y / 2.0f, z / 2.0f);
                        vertices[vertexIndex].normal = new Vec3(x, y, z);
                        vertices[vertexIndex].texcoord = new Vec2((float)j / (float)slices, 1.0f - (float)i / (float)stacks);
                    }
                }

                //Bottom stack.
                vertices[vertexIndex].position = new Vec3(0.0f, -0.5f, 0.0f);
                vertices[vertexIndex].normal = new Vec3(0.0f, -1.0f, 0.0f);
                vertices[vertexIndex].texcoord = new Vec2(0.5f, 0.0f);
            }
            // Build done.

            int length = Marshal.SizeOf(vertices[0]);
            global::System.IntPtr pA = Marshal.AllocHGlobal(length * SPHERE_VERTEX_NUMBER);

            for (int i = 0; i < SPHERE_VERTEX_NUMBER; i++)
            {
                Marshal.StructureToPtr(vertices[i], pA + i * length, true);
            }

            return pA;
        }

        private ushort[] SphereIndexData()
        {
            ushort[] indices = new ushort[SPHERE_INDEX_NUMBER];
            const int slices = SPHERE_SLICES;
            const int stacks = SPHERE_STACKS;

            // Build start.
            {
                int indiceIndex = 0; //Used to keep track of progress through indices.
                int previousCycleBeginning = 1; //Stores the index of the vertex that started the cycle of the previous stack.
                int currentCycleBeginning = 1 + slices;

                //Top stack. Loop from index 1 to index slices, as not counting the very first vertex.
                for (int i = 1; i <= slices; i++, indiceIndex += 3)
                {
                    indices[indiceIndex] = 0;
                    if (i == slices)
                    {
                        //End, so loop around.
                        indices[indiceIndex + 1] = 1;
                    }
                    else
                    {
                        indices[indiceIndex + 1] = (ushort)(i + 1);
                    }
                    indices[indiceIndex + 2] = (ushort)i;
                }

                //Middle Stacks. Want to form triangles between the top and bottom stacks, so loop up to the number of stacks - 2.
                for (int i = 0; i < stacks - 2; i++, previousCycleBeginning += slices, currentCycleBeginning += slices)
                {
                    for (int j = 0; j < slices; j++, indiceIndex += 6)
                    {
                        if (j == slices - 1)
                        {
                            //End, so loop around.
                            indices[indiceIndex] = (ushort)(previousCycleBeginning + j);
                            indices[indiceIndex + 1] = (ushort)previousCycleBeginning;
                            indices[indiceIndex + 2] = (ushort)(currentCycleBeginning + j);
                            indices[indiceIndex + 3] = (ushort)(currentCycleBeginning + j);
                            indices[indiceIndex + 4] = (ushort)previousCycleBeginning;
                            indices[indiceIndex + 5] = (ushort)currentCycleBeginning;
                        }
                        else
                        {
                            indices[indiceIndex] = (ushort)(previousCycleBeginning + j);
                            indices[indiceIndex + 1] = (ushort)(previousCycleBeginning + 1 + j);
                            indices[indiceIndex + 2] = (ushort)(currentCycleBeginning + j);
                            indices[indiceIndex + 3] = (ushort)(currentCycleBeginning + j);
                            indices[indiceIndex + 4] = (ushort)(previousCycleBeginning + 1 + j);
                            indices[indiceIndex + 5] = (ushort)(currentCycleBeginning + 1 + j);
                        }
                    }
                }

                //Bottom stack. Loop around the last stack from the previous loop, and go up to the penultimate vertex.
                for (int i = 0; i < slices; i++, indiceIndex += 3)
                {
                    indices[indiceIndex] = (ushort)(previousCycleBeginning + slices);
                    indices[indiceIndex + 1] = (ushort)(previousCycleBeginning + i);
                    if (i == slices - 1)
                    {
                        //End, so loop around.
                        indices[indiceIndex + 2] = (ushort)previousCycleBeginning;
                    }
                    else
                    {
                        indices[indiceIndex + 2] = (ushort)(previousCycleBeginning + i + 1);
                    }
                }
            }
            // Build done.

            return indices;
        }
        const int NUMBER_OF_SPHERES_ROW = 3;
        const int NUMBER_OF_SPHERES_COLUMN = 2;
        const int NUMBER_OF_SPHERES = NUMBER_OF_SPHERES_ROW * NUMBER_OF_SPHERES_COLUMN;
        const int SPHERE_SLICES = 150; // >= 3
        const int SPHERE_STACKS = 90; // >= 1
        const int SPHERE_VERTEX_NUMBER = SPHERE_SLICES * (SPHERE_STACKS - 1) + 2;
        const int SPHERE_INDEX_NUMBER = 6 * SPHERE_SLICES * (SPHERE_STACKS - 1);


        private const int MaxObject = 5;
        private Window win;
        private View root;
        private Timer timer;
        private bool toggle = false;
        private string resource;
        private List<View> views;

        public void Activate()
        {
            win = NUIApplication.GetDefaultWindow();
            resource = Tizen.Applications.Application.Current.DirectoryInfo.Resource;
            root = new View()
            {
                Name = "root",
                Size = new Size(10, 10),
                BackgroundColor = Color.Blue,
            };
            win.Add(root);

            views = new List<View>();

            AddManyViews();
            toggle = true;

            timer = new Timer(3000); //3s
            timer.Tick += OnTimerTick;
            timer.Start();
        }

        private bool OnTimerTick(object source, Timer.TickEventArgs e)
        {
            toggle = !toggle;
            if (toggle)
            {
                AddManyViews();
            }
            else
            {
                RemoveAllViews();
                FullGC();
            }
            return true;
        }

        private Geometry GenerateGeometry()
        {
            PropertyMap vertexFormat = new PropertyMap();
            vertexFormat.Add("aPosition", new PropertyValue((int)PropertyType.Vector3));
            vertexFormat.Add("aNormal", new PropertyValue((int)PropertyType.Vector3));
            vertexFormat.Add("aTexCoord", new PropertyValue((int)PropertyType.Vector2));
            PropertyBuffer vertexBuffer = new PropertyBuffer(vertexFormat);

            vertexBuffer.SetData(SphereVertexDataPtr(), SPHERE_VERTEX_NUMBER);

            ushort[] indexBuffer = SphereIndexData();

            Geometry geometry = new Geometry();
            geometry.AddVertexBuffer(vertexBuffer);
            geometry.SetIndexBuffer(indexBuffer, SPHERE_INDEX_NUMBER);
            geometry.SetType(Geometry.Type.TRIANGLES);
            return geometry;
        }

        private void AddManyViews()
        {
            Random rand = new Random();

            for (int i = 0; i < MaxObject; i++)
            {
                int viewSize = 150;
                var view = new View()
                {
                    Size = new Size(viewSize, viewSize, viewSize),
                    Position = new Position(rand.Next(10, 600), rand.Next(10, 600)),
                };
                root.Add(view);

                PixelData pixelData = PixelBuffer.Convert(ImageLoading.LoadImageFromFile(
                    resource + "/images/PopupTest/circle.jpg",
                    new Size2D(),
                    FittingModeType.ScaleToFill
                ));
                Texture texture = new Texture(
                    TextureType.TEXTURE_2D,
                    pixelData.GetPixelFormat(),
                    pixelData.GetWidth(),
                    pixelData.GetHeight()
                );
                texture.Upload(pixelData);
                TextureSet textureSet = new TextureSet();
                textureSet.SetTexture(0u, texture);
                Renderer renderer = new Renderer(GenerateGeometry(), new Shader(VERTEX_SHADER, FRAGMENT_SHADER));
                renderer.SetTextures(textureSet);
                view.AddRenderer(renderer);
            }
            
            for (int i = 0; i < MaxObject; i++)
            {
                int viewSize = 150;
                var view = new View()
                {
                    Size = new Size(viewSize, viewSize, viewSize),
                    Position = new Position(rand.Next(10, 600), rand.Next(10, 600)),
                };
                root.Add(view);
                views.Add(view);

                PixelData pixelData = PixelBuffer.Convert(ImageLoading.LoadImageFromFile(
                    resource + "/images/PaletteTest/red2.jpg",
                    new Size2D(),
                    FittingModeType.ScaleToFill
                ));
                Texture texture = new Texture(
                    TextureType.TEXTURE_2D,
                    pixelData.GetPixelFormat(),
                    pixelData.GetWidth(),
                    pixelData.GetHeight()
                );
                texture.Upload(pixelData);
                TextureSet textureSet = new TextureSet();
                textureSet.SetTexture(0u, texture);
                Renderer renderer = new Renderer(GenerateGeometry(), new Shader(VERTEX_SHADER, FRAGMENT_SHADER));
                renderer.SetTextures(textureSet);
                view.AddRenderer(renderer);
            }

        }
        private void RemoveAllViews()
        {
            uint cnt = root.ChildCount;
            for (int i = (int)(cnt - 1); i >= 0; i--)
            {
                root.Remove(root.GetChildAt((uint)i));
            }
            foreach(var view in views)
            {
                var renderer = view.GetRendererAt(0);
                renderer.Dispose();
                view.Dispose();
            }
            views.Clear();
        }

        private void FullGC()
        {
            global::System.GC.Collect();
            global::System.GC.WaitForPendingFinalizers();
            global::System.GC.Collect();
        }

        public void Deactivate()
        {
            timer.Stop();
            RemoveAllViews();
            root.Unparent();
            root.Dispose();
        }
    }
}
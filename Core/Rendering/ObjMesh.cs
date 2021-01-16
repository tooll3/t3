using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SharpDX;
using T3.Core.Logging;

namespace T3.Core.Rendering
{
    public class ObjMesh
    {
        public readonly List<SharpDX.Vector3> Vertices = new List<SharpDX.Vector3>();
        public readonly List<SharpDX.Vector2> TexCoords = new List<SharpDX.Vector2>();
        public readonly List<SharpDX.Vector3> Normals = new List<SharpDX.Vector3>();
        public readonly List<Face> Faces = new List<Face>();
        
        public ObjMesh(string objFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(objFilePath) || !(new FileInfo(objFilePath).Exists))
                    return;
            }
            catch (Exception e)
            {
                Log.Warning("Failed to load object path:" + objFilePath + "\\n" + e);
                return;
            }
            
            using (var stream = new StreamReader(objFilePath))
            {
                try
                {
                    string line;
                    while ((line = stream.ReadLine()) != null)
                    {
                        var lineEntries = line.Split(' ');
                        switch (lineEntries[0])
                        {
                            case "v":
                            {
                                float x = float.Parse(lineEntries[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(lineEntries[2], CultureInfo.InvariantCulture);
                                float z = float.Parse(lineEntries[3], CultureInfo.InvariantCulture);
                                Vertices.Add(new Vector3(x, y, z));
                                break;
                            }
                            case "vt":
                            {
                                float u = float.Parse(lineEntries[1], CultureInfo.InvariantCulture);
                                float v = float.Parse(lineEntries[2], CultureInfo.InvariantCulture);
                                TexCoords.Add(new Vector2(u, v));
                                break;
                            }
                            case "vn":
                            {
                                float x = float.Parse(lineEntries[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(lineEntries[2], CultureInfo.InvariantCulture);
                                float z = float.Parse(lineEntries[3], CultureInfo.InvariantCulture);
                                Normals.Add(new Vector3(x, y, z));
                                break;
                            }
                            case "f":
                            {
                                var v0 = lineEntries[1];
                                var v0entries = v0.Split('/');
                                int v0v = int.Parse(v0entries[0], CultureInfo.InvariantCulture) - 1;
                                int v0t = int.Parse(v0entries[1], CultureInfo.InvariantCulture) - 1;
                                int v0n = int.Parse(v0entries[2], CultureInfo.InvariantCulture) - 1;

                                var v1 = lineEntries[2];
                                var v1entries = v1.Split('/');
                                int v1v = int.Parse(v1entries[0], CultureInfo.InvariantCulture) - 1;
                                int v1t = int.Parse(v1entries[1], CultureInfo.InvariantCulture) - 1;
                                int v1n = int.Parse(v1entries[2], CultureInfo.InvariantCulture) - 1;

                                var v2 = lineEntries[3];
                                var v2entries = v2.Split('/');
                                int v2v = int.Parse(v2entries[0], CultureInfo.InvariantCulture) - 1;
                                int v2t = int.Parse(v2entries[1], CultureInfo.InvariantCulture) - 1;
                                int v2n = int.Parse(v2entries[2], CultureInfo.InvariantCulture) - 1;

                                Faces.Add(new Face(v0v, v0n, v0t, v1v, v1n, v1t, v2v, v2n, v2t));
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Failed to load point cloud:" + e.Message);
                }
            }
        }

        public struct Face
        {
            public Face(int v0, int v0n, int v0t, int v1, int v1n, int v1t, int v2, int v2n, int v2t)
            {
                V0 = v0;
                V0n = v0n;
                V0t = v0t;

                V1 = v1;
                V1n = v1n;
                V1t = v1t;

                V2 = v2;
                V2n = v2n;
                V2t = v2t;
            }

            public int V0;
            public int V0n;
            public int V0t;
            public int V1;
            public int V1n;
            public int V1t;
            public int V2;
            public int V2n;
            public int V2t;
        }    
    }
}
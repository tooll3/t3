using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using T3.Core.Logging;

namespace T3.Core.Rendering
{
    public class ObjMesh
    {
        public readonly List<SharpDX.Vector3> Positions = new List<SharpDX.Vector3>();
        public readonly List<SharpDX.Vector4> Colors = new List<Vector4>();
        
        public readonly List<SharpDX.Vector3> Normals = new List<SharpDX.Vector3>();
        public readonly List<SharpDX.Vector2> TexCoords = new List<SharpDX.Vector2>();
        public readonly List<Face> Faces = new List<Face>();
        public readonly List<Line> Lines = new List<Line>();

        public static ObjMesh LoadFromFile(string objFilePath)
        {
            try
            {
                if (string.IsNullOrEmpty(objFilePath) || !(new FileInfo(objFilePath).Exists))
                    return null;
            }
            catch (Exception e)
            {
                Log.Warning("Failed to load object path:" + objFilePath + "\\n" + e);
                return null;
            }

            var mesh = new ObjMesh();

            using (var stream = new StreamReader(objFilePath))
            {
                var line = "";
                try
                {
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
                                mesh.Positions.Add(new Vector3(x, y, z));
                                var vertexIncludesColor = lineEntries.Length == 7;
                                if (vertexIncludesColor)
                                {
                                    float r = float.Parse(lineEntries[4], CultureInfo.InvariantCulture);
                                    float g = float.Parse(lineEntries[5], CultureInfo.InvariantCulture);
                                    float b = float.Parse(lineEntries[6], CultureInfo.InvariantCulture);
                                    mesh.Colors.Add(new Vector4(r, g, b, 1));
                                }
                                break;
                            }
                            case "vt":
                            {
                                float u = float.Parse(lineEntries[1], CultureInfo.InvariantCulture);
                                float v = float.Parse(lineEntries[2], CultureInfo.InvariantCulture);
                                mesh.TexCoords.Add(new Vector2(u, v));
                                break;
                            }
                            case "vn":
                            {
                                float x = float.Parse(lineEntries[1], CultureInfo.InvariantCulture);
                                float y = float.Parse(lineEntries[2], CultureInfo.InvariantCulture);
                                float z = float.Parse(lineEntries[3], CultureInfo.InvariantCulture);
                                mesh.Normals.Add(new Vector3(x, y, z));
                                break;
                            }
                            case "f":
                            {
                                SplitFaceIndices(lineEntries[1], out var v0v, out var v0t, out var v0n);
                                SplitFaceIndices(lineEntries[2], out var v1v, out var v1t, out var v1n);
                                SplitFaceIndices(lineEntries[3], out var v2v, out var v2t, out var v2n);
                                mesh.Faces.Add(new Face( v0v, v0n, v0t, v1v, v1n, v1t, v2v, v2n, v2t));                                
                                break;
                            }
                            case "l":
                            {
                                mesh.Lines.Add(new Line(int.Parse(lineEntries[1], CultureInfo.InvariantCulture) - 1,
                                                        int.Parse(lineEntries[2], CultureInfo.InvariantCulture) - 1
                                                       ));
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to load point cloud:{e.Message} '{line}'");
                    return null;
                }

                if (mesh.Colors.Count > 0 && mesh.Colors.Count != mesh.Positions.Count)
                {
                    Log.Warning("Optional OBJ color information not defined for all vertices");
                }
            }

            return mesh;
        }

        

        private static void SplitFaceIndices(string v0, out int positionIndex, out int textureIndex, out int normalIndex)
        {
            var v0Entries = v0.Split('/');
            positionIndex = int.Parse(v0Entries[0], CultureInfo.InvariantCulture) - 1;
            if (string.IsNullOrEmpty(v0Entries[1]))
            {
                textureIndex = 0;
                normalIndex = int.Parse(v0Entries[2], CultureInfo.InvariantCulture) - 1;
            }
            else
            {
                textureIndex = int.Parse(v0Entries[1], CultureInfo.InvariantCulture) - 1;
                normalIndex = int.Parse(v0Entries[2], CultureInfo.InvariantCulture) - 1;
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

        public struct Line
        {
            public Line(int v0, int v2)
            {
                V0 = v0;
                V2 = v2;
            }

            public int V0;
            public int V2;
        }

        #region joining vertices
        public List<Vertex> DistinctDistinctVertices
        {
            get
            {
                if (_distinctVertices == null)
                {
                    InitializeVertices();
                    UpdateVertexSorting(SortDirections.ZForward);
                }
                return _distinctVertices;
            }
        }
        
        
        

        public int GetVertexIndex(int positionIndex, int normalIndex, int textureCoordsIndex)
        {
            var hash = Vertex.GetHashForIndices(positionIndex, normalIndex, textureCoordsIndex);

            if (VertexIndicesByHash.TryGetValue(hash, out var index))
            {
                return index;
            }

            return -1;
        }

        public struct Vertex
        {
            public readonly int PositionIndex;
            public readonly int NormalIndex;
            public readonly int TextureCoordsIndex;
            public int PresortIndex;    // An internal helper

            //public readonly long Hash;

            public Vertex(int positionIndex, int normalIndex, int textureCoordsIndex, int index)
            {
                PositionIndex = positionIndex;
                NormalIndex = normalIndex;
                TextureCoordsIndex = textureCoordsIndex;
                PresortIndex = index;
                //Hash = GetHashForIndices(positionIndex, normalIndex, textureCoordsIndex);
            }

            public static long GetHashForIndices(int pos, int normal, int textureCoords)
            {
                return (long)pos << 42 | (long)normal << 21 | (long)textureCoords;
            }
        }

        private void InitializeVertices( )
        {
            if (TexCoords.Count == 0)
            {
                TexCoords.Add(Vector2.Zero);
            }
            _distinctVertices = new List<Vertex>();
            for (int faceIndex = 0; faceIndex < Faces.Count; faceIndex++)
            {
                var face = Faces[faceIndex];

                SortInMergedVertex(face.V0, face.V0n, face.V0t, ref face);
                SortInMergedVertex(face.V1, face.V1n, face.V1t, ref face);
                SortInMergedVertex(face.V2, face.V2n, face.V2t, ref face);
            }
        }



        private int SortInMergedVertex(int posIndex, int normalIndex, int texCoordIndex, ref Face face)
        {
            var vertHash = Vertex.GetHashForIndices(posIndex, normalIndex, texCoordIndex);

            if (VertexIndicesByHash.TryGetValue(vertHash, out var index))
            {
                return index;
            }

            Vector3 tangent, bitangent;
            MeshUtils.CalcTBNSpace(p0: Positions[face.V0],
                                   uv0: TexCoords[face.V0t],
                                   p1: Positions[face.V1],
                                   uv1: TexCoords[face.V1t],
                                   p2: Positions[face.V2],
                                   uv2: TexCoords[face.V2t],
                                   normal: Normals[normalIndex],
                                   tangent: out tangent,
                                   bitangent: out bitangent);

            var newIndex = _distinctVertices.Count;
            var vert = new Vertex(posIndex, normalIndex, texCoordIndex, newIndex);
            VertexIndicesByHash[vertHash] = newIndex;
            VertexBinormals.Add(bitangent);
            VertexTangents.Add(tangent);
            _distinctVertices.Add(vert);
            return newIndex;
        }
        
        /// <summary>
        /// Vertex sorting is implement through an index look-up table
        /// </summary>
        public void UpdateVertexSorting(SortDirections sortDirection)
        {
            SortedVertexIndices = Enumerable.Range(0, _distinctVertices.Count).ToList();
            switch (sortDirection)
            {
                case SortDirections.XForward:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v1].PositionIndex].X.
                                                 CompareTo(Positions[_distinctVertices[v2].PositionIndex].X));
                    break;
                case SortDirections.XBackwards:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v2].PositionIndex].X.
                                                 CompareTo(Positions[_distinctVertices[v1].PositionIndex].X));
                    break;
                case SortDirections.YForward:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v1].PositionIndex].Y.
                                                 CompareTo(Positions[_distinctVertices[v2].PositionIndex].Y));
                    break;
                case SortDirections.YBackwards:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v2].PositionIndex].Y.
                                                 CompareTo(Positions[_distinctVertices[v1].PositionIndex].Y));
                    break;
                case SortDirections.ZForward:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v1].PositionIndex].Z.
                                                 CompareTo(Positions[_distinctVertices[v2].PositionIndex].Z));
                    break;
                case SortDirections.ZBackwards:
                    SortedVertexIndices.Sort((v1, v2) =>   Positions[_distinctVertices[v2].PositionIndex].Z.
                                                 CompareTo(Positions[_distinctVertices[v1].PositionIndex].Z));
                    break;
            }
        }

        public enum SortDirections
        {
            XForward,
            XBackwards,
            YForward,
            YBackwards,
            ZForward,
            ZBackwards,
        }

        private List<Vertex> _distinctVertices;
        public readonly List<SharpDX.Vector3> VertexTangents = new List<Vector3>();
        public readonly List<SharpDX.Vector3> VertexBinormals = new List<Vector3>();
        public List<int> SortedVertexIndices;
        private readonly Dictionary<long, int> VertexIndicesByHash = new Dictionary<long, int>();
        #endregion
    }
}
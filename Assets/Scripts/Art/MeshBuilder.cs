using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BlackMarket.Art
{
    /// <summary>
    /// Builds stylised low-poly meshes from simple shapes (boxes, lathes, spheres, tori, gems) in code.
    /// Every shape is coloured per vertex, so a whole character or prop becomes ONE mesh drawn with ONE
    /// material: cheap on phones and visually consistent.
    ///
    /// Vertex colour alpha encodes glow: pass glow 0..1 and the Toon shader makes that part self-lit.
    ///
    /// Shapes are placed with a transform stack: Push(position, rotation, scale) ... Pop().
    /// Triangle winding is corrected automatically from the expected outward normal, so shapes always
    /// face outward even under mirrored or squashed transforms.
    /// </summary>
    public sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector3> normals = new List<Vector3>();
        private readonly List<Vector2> uvs = new List<Vector2>();
        private readonly List<Color32> colors = new List<Color32>();
        private readonly List<int> triangles = new List<int>();
        private readonly List<Matrix4x4> stack = new List<Matrix4x4>();

        private Matrix4x4 matrix = Matrix4x4.identity;
        private Matrix4x4 normalMatrix = Matrix4x4.identity;

        public int VertexCount => vertices.Count;
        public bool IsEmpty => vertices.Count == 0;

        // ------------------------------------------------------------------ Transform stack

        public MeshBuilder Push(Vector3 position, Vector3 eulerDegrees, Vector3 scale)
        {
            stack.Add(matrix);
            SetMatrix(matrix * Matrix4x4.TRS(position, Quaternion.Euler(eulerDegrees), scale));
            return this;
        }

        public MeshBuilder Push(Vector3 position, Vector3 eulerDegrees) => Push(position, eulerDegrees, Vector3.one);

        public MeshBuilder Push(Vector3 position) => Push(position, Vector3.zero, Vector3.one);

        public MeshBuilder Pop()
        {
            if (stack.Count == 0) throw new InvalidOperationException("MeshBuilder.Pop without matching Push.");
            SetMatrix(stack[stack.Count - 1]);
            stack.RemoveAt(stack.Count - 1);
            return this;
        }

        private void SetMatrix(Matrix4x4 m)
        {
            matrix = m;
            normalMatrix = m.inverse.transpose;
        }

        // ------------------------------------------------------------------ Shapes

        /// <summary>Axis-aligned box (in the current transform). UVs are in metres, so textures tile evenly.</summary>
        public void AddBox(Vector3 center, Vector3 size, Color color, float glow = 0f)
        {
            Vector3 h = size * 0.5f;
            AddBoxFace(center, h, Vector3.right, Vector3.up, size.z, size.y, color, glow);
            AddBoxFace(center, h, Vector3.left, Vector3.up, size.z, size.y, color, glow);
            AddBoxFace(center, h, Vector3.forward, Vector3.up, size.x, size.y, color, glow);
            AddBoxFace(center, h, Vector3.back, Vector3.up, size.x, size.y, color, glow);
            AddBoxFace(center, h, Vector3.up, Vector3.forward, size.x, size.z, color, glow);
            AddBoxFace(center, h, Vector3.down, Vector3.forward, size.x, size.z, color, glow);
        }

        private void AddBoxFace(Vector3 center, Vector3 h, Vector3 n, Vector3 up, float width, float height, Color color, float glow)
        {
            Vector3 right = Vector3.Cross(up, -n);
            Vector3 c = center + Vector3.Scale(n, h);
            Vector3 r = Vector3.Scale(right, h);
            Vector3 u = Vector3.Scale(up, h);
            r = right * Mathf.Abs(Vector3.Dot(r, right));
            u = up * Mathf.Abs(Vector3.Dot(u, up));

            int i0 = AddVertex(c - r - u, n, new Vector2(0, 0), color, glow);
            int i1 = AddVertex(c - r + u, n, new Vector2(0, height), color, glow);
            int i2 = AddVertex(c + r + u, n, new Vector2(width, height), color, glow);
            int i3 = AddVertex(c + r - u, n, new Vector2(width, 0), color, glow);
            AddTriangle(i0, i1, i2, n);
            AddTriangle(i0, i2, i3, n);
        }

        /// <summary>
        /// Revolves a profile (x = radius, y = height) around the Y axis. Start and end the profile on the
        /// axis (radius 0) for a closed solid. Corners sharper than smoothAngle stay crisp.
        /// </summary>
        public void AddLathe(IList<Vector2> profile, int segments, Color color, float glow = 0f, float smoothAngle = 40f, Vector3 offset = default)
        {
            if (profile == null || profile.Count < 2) return;
            segments = Mathf.Max(3, segments);

            int count = profile.Count;
            var segNormals = new Vector2[count - 1];
            for (int i = 0; i < count - 1; i++)
            {
                Vector2 d = profile[i + 1] - profile[i];
                segNormals[i] = d.sqrMagnitude > 1e-10f ? new Vector2(d.y, -d.x).normalized : Vector2.up;
            }

            // Build rings: each ring is (profile index, 2D normal, connects to next ring).
            var ringPoint = new List<int>();
            var ringNormal = new List<Vector2>();
            var ringConnects = new List<bool>();
            float cosLimit = Mathf.Cos(smoothAngle * Mathf.Deg2Rad);

            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    AddRing(ringPoint, ringNormal, ringConnects, i, segNormals[0], true);
                }
                else if (i == count - 1)
                {
                    AddRing(ringPoint, ringNormal, ringConnects, i, segNormals[i - 1], false);
                }
                else if (Vector2.Dot(segNormals[i - 1], segNormals[i]) < cosLimit)
                {
                    AddRing(ringPoint, ringNormal, ringConnects, i, segNormals[i - 1], false); // end of lower segment
                    AddRing(ringPoint, ringNormal, ringConnects, i, segNormals[i], true);      // start of upper segment
                }
                else
                {
                    AddRing(ringPoint, ringNormal, ringConnects, i, (segNormals[i - 1] + segNormals[i]).normalized, true);
                }
            }

            // V coordinate: distance along the profile, in metres.
            var vCoord = new float[count];
            for (int i = 1; i < count; i++) vCoord[i] = vCoord[i - 1] + Vector2.Distance(profile[i - 1], profile[i]);

            var ringStart = new int[ringPoint.Count];
            for (int r = 0; r < ringPoint.Count; r++)
            {
                ringStart[r] = vertices.Count;
                Vector2 p = profile[ringPoint[r]];
                Vector2 n2 = ringNormal[r];
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                    var pos = new Vector3(p.x * cos, p.y, p.x * sin) + offset;
                    var nrm = new Vector3(n2.x * cos, n2.y, n2.x * sin);
                    AddVertex(pos, nrm, new Vector2(a * Mathf.Max(p.x, 0.05f), vCoord[ringPoint[r]]), color, glow);
                }
            }

            for (int r = 0; r < ringPoint.Count - 1; r++)
            {
                if (!ringConnects[r]) continue;
                int a0 = ringStart[r], b0 = ringStart[r + 1];
                for (int s = 0; s < segments; s++)
                {
                    float angle = (s + 0.5f) / segments * Mathf.PI * 2f;
                    Vector2 n2 = (ringNormal[r] + ringNormal[r + 1]).normalized;
                    var expected = new Vector3(n2.x * Mathf.Cos(angle), n2.y, n2.x * Mathf.Sin(angle));
                    AddTriangle(b0 + s, b0 + s + 1, a0 + s + 1, expected);
                    AddTriangle(b0 + s, a0 + s + 1, a0 + s, expected);
                }
            }
        }

        private static void AddRing(List<int> points, List<Vector2> normals, List<bool> connects, int index, Vector2 normal, bool connectsToNext)
        {
            points.Add(index);
            normals.Add(normal);
            connects.Add(connectsToNext);
        }

        /// <summary>Smooth ellipsoid.</summary>
        public void AddSphere(Vector3 center, Vector3 radii, Color color, float glow = 0f, int segments = 14)
        {
            int rings = Mathf.Max(4, segments / 2 + 1);
            var profile = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = -Mathf.PI * 0.5f + Mathf.PI * i / rings;
                profile[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }

            if (profile.Length > 0)
            {
                profile[0].x = 0f;
                profile[rings].x = 0f;
            }

            Push(center, Vector3.zero, radii);
            AddLathe(profile, segments, color, glow, 89f);
            Pop();
        }

        public void AddSphere(Vector3 center, float radius, Color color, float glow = 0f, int segments = 14)
        {
            AddSphere(center, Vector3.one * radius, color, glow, segments);
        }

        /// <summary>Capped cylinder or cone from bottom centre upward. radiusTop 0 makes a cone.</summary>
        public void AddCylinder(Vector3 bottomCenter, float radiusBottom, float radiusTop, float height, Color color,
            float glow = 0f, int segments = 12)
        {
            var profile = new List<Vector2> { new Vector2(0f, 0f), new Vector2(radiusBottom, 0f), new Vector2(radiusTop, height) };
            if (radiusTop > 0f) profile.Add(new Vector2(0f, height));
            AddLathe(profile, segments, color, glow, 30f, bottomCenter);
        }

        /// <summary>Ring lying flat in the XZ plane (axis = Y).</summary>
        public void AddTorus(Vector3 center, float majorRadius, float minorRadius, Color color, float glow = 0f,
            int majorSegments = 18, int minorSegments = 8)
        {
            int start = vertices.Count;
            for (int i = 0; i <= majorSegments; i++)
            {
                float phi = i / (float)majorSegments * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
                for (int j = 0; j <= minorSegments; j++)
                {
                    float theta = j / (float)minorSegments * Mathf.PI * 2f;
                    Vector3 n = dir * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
                    Vector3 p = center + dir * majorRadius + n * minorRadius;
                    AddVertex(p, n, new Vector2(phi * majorRadius, theta * minorRadius), color, glow);
                }
            }

            int row = minorSegments + 1;
            for (int i = 0; i < majorSegments; i++)
            {
                for (int j = 0; j < minorSegments; j++)
                {
                    int a = start + i * row + j;
                    int b = a + row;
                    float phi = (i + 0.5f) / majorSegments * Mathf.PI * 2f;
                    float theta = (j + 0.5f) / minorSegments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(phi), 0f, Mathf.Sin(phi));
                    Vector3 expected = dir * Mathf.Cos(theta) + Vector3.up * Mathf.Sin(theta);
                    AddTriangle(a, b, b + 1, expected);
                    AddTriangle(a, b + 1, a + 1, expected);
                }
            }
        }

        /// <summary>Faceted double pyramid: crystals, gems, blades, plumes.</summary>
        public void AddGem(Vector3 center, float radius, float top, float bottom, int sides, Color color, float glow = 0f)
        {
            sides = Mathf.Max(3, sides);
            var ring = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i / (float)sides * Mathf.PI * 2f;
                ring[i] = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            }

            Vector3 apexTop = center + Vector3.up * top;
            Vector3 apexBottom = center + Vector3.down * bottom;
            for (int i = 0; i < sides; i++)
            {
                Vector3 a = ring[i], b = ring[(i + 1) % sides];
                AddFlatTriangle(apexTop, a, b, center, color, glow);
                if (bottom > 0f) AddFlatTriangle(apexBottom, b, a, center, color, glow);
            }
        }

        /// <summary>A flat-shaded triangle facing away from 'inside'.</summary>
        public void AddFlatTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 inside, Color color, float glow = 0f)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            if (Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f) n = -n;
            int i0 = AddVertex(a, n, new Vector2(0, 0), color, glow);
            int i1 = AddVertex(b, n, new Vector2(1, 0), color, glow);
            int i2 = AddVertex(c, n, new Vector2(0, 1), color, glow);
            AddTriangle(i0, i1, i2, n);
        }

        /// <summary>A capsule-like limb between two points (cylinder with rounded ends).</summary>
        public void AddLimb(Vector3 from, Vector3 to, float radiusFrom, float radiusTo, Color color, int segments = 10)
        {
            Vector3 dir = to - from;
            float length = dir.magnitude;
            if (length < 1e-4f) return;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir / length);
            Push(from, rot.eulerAngles);
            AddCylinder(Vector3.zero, radiusFrom, radiusTo, length, color, 0f, segments);
            Pop();
            AddSphere(from, radiusFrom, color, 0f, segments);
            AddSphere(to, radiusTo, color, 0f, segments);
        }

        /// <summary>Appends an existing mesh (with its vertex colours) at the current transform.</summary>
        public void AddMesh(Mesh mesh)
        {
            if (mesh == null) return;
            Vector3[] v = mesh.vertices;
            Vector3[] n = mesh.normals;
            Vector2[] uv = mesh.uv;
            Color32[] c = mesh.colors32;
            int[] t = mesh.triangles;
            int start = vertices.Count;

            for (int i = 0; i < v.Length; i++)
            {
                vertices.Add(matrix.MultiplyPoint3x4(v[i]));
                Vector3 nn = i < n.Length ? normalMatrix.MultiplyVector(n[i]) : Vector3.up;
                normals.Add(nn.sqrMagnitude > 1e-12f ? nn.normalized : Vector3.up);
                uvs.Add(i < uv.Length ? uv[i] : Vector2.zero);
                colors.Add(i < c.Length ? c[i] : new Color32(255, 255, 255, 255));
            }

            bool mirrored = matrix.determinant < 0f;
            for (int i = 0; i + 2 < t.Length; i += 3)
            {
                triangles.Add(start + t[i]);
                triangles.Add(start + (mirrored ? t[i + 2] : t[i + 1]));
                triangles.Add(start + (mirrored ? t[i + 1] : t[i + 2]));
            }
        }

        // ------------------------------------------------------------------ Low level

        private int AddVertex(Vector3 localPosition, Vector3 localNormal, Vector2 uv, Color color, float glow)
        {
            vertices.Add(matrix.MultiplyPoint3x4(localPosition));
            Vector3 n = normalMatrix.MultiplyVector(localNormal);
            normals.Add(n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up);
            uvs.Add(uv);
            Color c = color;
            c.a = 1f - Mathf.Clamp01(glow);
            colors.Add(c);
            return vertices.Count - 1;
        }

        // Adds a triangle, flipping it if needed so its face points along the expected (local) normal.
        private void AddTriangle(int a, int b, int c, Vector3 expectedLocalNormal)
        {
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];
            Vector3 faceNormal = Vector3.Cross(pb - pa, pc - pa);
            Vector3 expected = normalMatrix.MultiplyVector(expectedLocalNormal);
            if (Vector3.Dot(faceNormal, expected) < 0f)
            {
                int t = b;
                b = c;
                c = t;
            }

            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            uvs.Clear();
            colors.Clear();
            triangles.Clear();
            stack.Clear();
            SetMatrix(Matrix4x4.identity);
        }
    }
}

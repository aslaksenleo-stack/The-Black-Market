// Minimal UnityEngine stand-in so art code (MeshBuilder & recipes) can run outside Unity for previews.
using System;
using System.Collections.Generic;
namespace UnityEngine.Rendering { public enum IndexFormat { UInt16, UInt32 } }
namespace UnityEngine
{
    public static class Mathf
    {
        public const float PI = (float)Math.PI, Deg2Rad = PI / 180f, Rad2Deg = 180f / PI;
        public static float Sin(float a) => (float)Math.Sin(a); public static float Cos(float a) => (float)Math.Cos(a);
        public static float Abs(float a) => Math.Abs(a); public static float Max(float a, float b) => Math.Max(a, b); public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b); public static float Clamp01(float v) => v < 0 ? 0 : v > 1 ? 1 : v;
        public static float Clamp(float v, float a, float b) => v < a ? a : v > b ? b : v; public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Sqrt(float v) => (float)Math.Sqrt(v); public static float Atan2(float y, float x) => (float)Math.Atan2(y, x); public static float Asin(float v) => (float)Math.Asin(v);
    }
    public struct Vector2
    {
        public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 up => new Vector2(0, 1); public static Vector2 zero => new Vector2(0, 0);
        public float sqrMagnitude => x * x + y * y; public float magnitude => Mathf.Sqrt(sqrMagnitude);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-9f ? new Vector2(x / m, y / m) : new Vector2(0, 0); } }
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
    }
    public struct Vector3
    {
        public float x, y, z; public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new Vector3(0, 0, 0); public static Vector3 one => new Vector3(1, 1, 1);
        public static Vector3 up => new Vector3(0, 1, 0); public static Vector3 down => new Vector3(0, -1, 0);
        public static Vector3 right => new Vector3(1, 0, 0); public static Vector3 left => new Vector3(-1, 0, 0);
        public static Vector3 forward => new Vector3(0, 0, 1); public static Vector3 back => new Vector3(0, 0, -1);
        public float sqrMagnitude => x * x + y * y + z * z; public float magnitude => Mathf.Sqrt(sqrMagnitude);
        public Vector3 normalized { get { float m = magnitude; return m > 1e-9f ? this / m : zero; } }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);
        public static Vector3 operator *(Vector3 a, float s) => new Vector3(a.x * s, a.y * s, a.z * s);
        public static Vector3 operator *(float s, Vector3 a) => a * s;
        public static Vector3 operator /(Vector3 a, float s) => new Vector3(a.x / s, a.y / s, a.z / s);
        public static Vector3 Cross(Vector3 a, Vector3 b) => new Vector3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        public static float Dot(Vector3 a, Vector3 b) => a.x * b.x + a.y * b.y + a.z * b.z;
        public static Vector3 Scale(Vector3 a, Vector3 b) => new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
        public static float Distance(Vector3 a, Vector3 b) => (a - b).magnitude;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * Mathf.Clamp01(t);
    }
    public struct Quaternion
    {
        public float x, y, z, w; public Quaternion(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
        public static Quaternion identity => new Quaternion(0, 0, 0, 1);
        public static Quaternion AngleAxis(float deg, Vector3 axis) { axis = axis.normalized; float h = deg * Mathf.Deg2Rad * 0.5f, s = Mathf.Sin(h); return new Quaternion(axis.x * s, axis.y * s, axis.z * s, Mathf.Cos(h)); }
        public static Quaternion operator *(Quaternion a, Quaternion b) => new Quaternion(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y, a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x, a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);
        public static Vector3 operator *(Quaternion q, Vector3 v) { var m = Matrix4x4.Rotate(q); return m.MultiplyVector(v); }
        public static Quaternion Euler(Vector3 e) => AngleAxis(e.y, Vector3.up) * AngleAxis(e.x, Vector3.right) * AngleAxis(e.z, Vector3.forward);
        public static Quaternion Euler(float x, float y, float z) => Euler(new Vector3(x, y, z));
        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            from = from.normalized; to = to.normalized; float d = Vector3.Dot(from, to);
            if (d > 0.99999f) return identity;
            if (d < -0.99999f) { Vector3 ax = Vector3.Cross(Vector3.right, from); if (ax.sqrMagnitude < 1e-6f) ax = Vector3.Cross(Vector3.up, from); return AngleAxis(180f, ax); }
            Vector3 c = Vector3.Cross(from, to); return AngleAxis((float)Math.Acos(d) * Mathf.Rad2Deg, c);
        }
        public Vector3 eulerAngles { get {
            var m = Matrix4x4.Rotate(this); float sx = -m.m12; sx = Mathf.Clamp(sx, -1, 1); float ex = Mathf.Asin(sx), ey, ez;
            if (Math.Cos(ex) > 1e-4) { ey = Mathf.Atan2(m.m02, m.m22); ez = Mathf.Atan2(m.m10, m.m11); } else { ey = Mathf.Atan2(-m.m20, m.m00); ez = 0; }
            return new Vector3(ex * Mathf.Rad2Deg, ey * Mathf.Rad2Deg, ez * Mathf.Rad2Deg); } }
    }
    public struct Matrix4x4
    {
        public float m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33;
        float this[int r, int c] { get { switch (r * 4 + c) { case 0: return m00; case 1: return m01; case 2: return m02; case 3: return m03; case 4: return m10; case 5: return m11; case 6: return m12; case 7: return m13; case 8: return m20; case 9: return m21; case 10: return m22; case 11: return m23; case 12: return m30; case 13: return m31; case 14: return m32; default: return m33; } }
            set { switch (r * 4 + c) { case 0: m00 = value; break; case 1: m01 = value; break; case 2: m02 = value; break; case 3: m03 = value; break; case 4: m10 = value; break; case 5: m11 = value; break; case 6: m12 = value; break; case 7: m13 = value; break; case 8: m20 = value; break; case 9: m21 = value; break; case 10: m22 = value; break; case 11: m23 = value; break; case 12: m30 = value; break; case 13: m31 = value; break; case 14: m32 = value; break; default: m33 = value; break; } } }
        public static Matrix4x4 identity { get { var m = new Matrix4x4(); m.m00 = m.m11 = m.m22 = m.m33 = 1; return m; } }
        public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) { var r = new Matrix4x4(); for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) { float s = 0; for (int k = 0; k < 4; k++) s += a[i, k] * b[k, j]; r[i, j] = s; } return r; }
        public static Matrix4x4 Rotate(Quaternion q)
        {
            float x = q.x, y = q.y, z = q.z, w = q.w; var m = identity;
            m.m00 = 1 - 2 * (y * y + z * z); m.m01 = 2 * (x * y - z * w); m.m02 = 2 * (x * z + y * w);
            m.m10 = 2 * (x * y + z * w); m.m11 = 1 - 2 * (x * x + z * z); m.m12 = 2 * (y * z - x * w);
            m.m20 = 2 * (x * z - y * w); m.m21 = 2 * (y * z + x * w); m.m22 = 1 - 2 * (x * x + y * y); return m;
        }
        public static Matrix4x4 TRS(Vector3 p, Quaternion q, Vector3 s)
        {
            var r = Rotate(q); var sc = identity; sc.m00 = s.x; sc.m11 = s.y; sc.m22 = s.z; var t = identity; t.m03 = p.x; t.m13 = p.y; t.m23 = p.z; return t * r * sc;
        }
        public Matrix4x4 transpose { get { var r = new Matrix4x4(); for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) r[i, j] = this[j, i]; return r; } }
        public Matrix4x4 inverse { get {
            var n = new System.Numerics.Matrix4x4(m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33);
            System.Numerics.Matrix4x4.Invert(n, out var i);
            var r = new Matrix4x4 { m00 = i.M11, m01 = i.M12, m02 = i.M13, m03 = i.M14, m10 = i.M21, m11 = i.M22, m12 = i.M23, m13 = i.M24, m20 = i.M31, m21 = i.M32, m22 = i.M33, m23 = i.M34, m30 = i.M41, m31 = i.M42, m32 = i.M43, m33 = i.M44 }; return r; } }
        public float determinant { get { var n = new System.Numerics.Matrix4x4(m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33); return n.GetDeterminant(); } }
        public Vector3 MultiplyPoint3x4(Vector3 v) => new Vector3(m00 * v.x + m01 * v.y + m02 * v.z + m03, m10 * v.x + m11 * v.y + m12 * v.z + m13, m20 * v.x + m21 * v.y + m22 * v.z + m23);
        public Vector3 MultiplyVector(Vector3 v) => new Vector3(m00 * v.x + m01 * v.y + m02 * v.z, m10 * v.x + m11 * v.y + m12 * v.z, m20 * v.x + m21 * v.y + m22 * v.z);
    }
    public struct Color
    {
        public float r, g, b, a; public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1, 1, 1, 1);
        public static Color Lerp(Color x, Color y, float t) { t = Mathf.Clamp01(t); return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t); }
        public static bool operator ==(Color x, Color y) => x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
        public static bool operator !=(Color x, Color y) => !(x == y);
        public override bool Equals(object o) => o is Color c && c == this; public override int GetHashCode() => 0;
        public static Color operator *(Color c, float s) => new Color(c.r * s, c.g * s, c.b * s, c.a * s);
    }
    public struct Color32
    {
        public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        static byte B(float v) => (byte)Math.Round(Mathf.Clamp01(v) * 255);
        public static implicit operator Color32(Color c) => new Color32(B(c.r), B(c.g), B(c.b), B(c.a));
    }
    public class Object { public string name; }
    public class Mesh : Object
    {
        public Rendering.IndexFormat indexFormat; public List<Vector3> V, N; public List<Vector2> UV; public List<Color32> C; public List<int> T;
        public void SetVertices(List<Vector3> v) => V = new List<Vector3>(v); public void SetNormals(List<Vector3> n) => N = new List<Vector3>(n);
        public void SetUVs(int ch, List<Vector2> u) => UV = new List<Vector2>(u); public void SetColors(List<Color32> c) => C = new List<Color32>(c);
        public void SetTriangles(List<int> t, int sub) => T = new List<int>(t);
        public Vector3[] vertices => V.ToArray(); public Vector3[] normals => N.ToArray(); public Vector2[] uv => UV.ToArray(); public Color32[] colors32 => C.ToArray(); public int[] triangles => T.ToArray(); public void RecalculateBounds() { }
    }
}

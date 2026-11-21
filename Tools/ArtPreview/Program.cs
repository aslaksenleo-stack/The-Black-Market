using System; using System.Globalization; using System.IO; using System.Text; using System.Collections.Generic;
using UnityEngine; using BlackMarket.Art; using BlackMarket.Items;
static class P {
  static string F(float f) => f.ToString("0.####", CultureInfo.InvariantCulture);
  static string V(Vector3 v) => "["+F(v.x)+","+F(v.y)+","+F(v.z)+"]";
  static void Part(StringBuilder sb, Mesh m, Vector3 off, string mat, Vector3 rot = default) {
    if (m == null) return;
    sb.Append("{\"mat\":\"").Append(mat).Append("\",\"off\":").Append(V(off)).Append(",\"rot\":").Append(V(rot)).Append(",\"p\":[");
    for (int i=0;i<m.V.Count;i++){ if(i>0)sb.Append(','); sb.Append(F(m.V[i].x)+","+F(m.V[i].y)+","+F(m.V[i].z)); }
    sb.Append("],\"n\":["); for (int i=0;i<m.N.Count;i++){ if(i>0)sb.Append(','); sb.Append(F(m.N[i].x)+","+F(m.N[i].y)+","+F(m.N[i].z)); }
    sb.Append("],\"uv\":["); for (int i=0;i<m.UV.Count;i++){ if(i>0)sb.Append(','); sb.Append(F(m.UV[i].x)+","+F(m.UV[i].y)); }
    sb.Append("],\"c\":["); for (int i=0;i<m.C.Count;i++){ if(i>0)sb.Append(','); sb.Append(m.C[i].r+","+m.C[i].g+","+m.C[i].b+","+m.C[i].a); }
    sb.Append("],\"i\":[").Append(string.Join(",", m.T)).Append("]},");
  }
  static void Character(StringBuilder sb, string look, int seed, Vector3 at, float yaw) {
    var c = CharacterBuilder.Build(look, seed, 0f);
    var q = Quaternion.Euler(0, yaw, 0);
    Vector3 W(Vector3 local) => at + q * local;
    var r = new Vector3(0, yaw, 0);
    Part(sb, c.Body, W(Vector3.zero), "Character", r); Part(sb, c.Head, W(c.HeadPivot), "Character", r);
    Part(sb, c.ArmLeft, W(c.ArmLeftPivot), "Character", r + new Vector3(-55,0,0)); Part(sb, c.ArmRight, W(c.ArmRightPivot), "Character", r + new Vector3(-55,0,0));
    Part(sb, c.LegLeft, W(c.LegLeftPivot), "Character", r); Part(sb, c.LegRight, W(c.LegRightPivot), "Character", r);
  }
  static void Main(string[] a) {
    var mode = a.Length > 1 ? a[1] : "models";
    var sb = new StringBuilder("{");
    if (mode == "shop") {
      var layout = ShopEnvironment.Build();
      sb.Append("\"pieces\":[");
      foreach (var p in layout.Pieces) Part(sb, p.Mesh, p.Position, p.Material.ToString(), p.Euler);
      Character(sb, a.Length > 2 ? a[2] : "explorer", 3, layout.Anchors["CustomerCounter"], 180f);
      var item = ItemModelBuilder.Build("crystal"); Part(sb, item.Solid, layout.Anchors["ItemSpot"], "Props", new Vector3(0,200,0)); Part(sb, item.Glass, layout.Anchors["ItemSpot"], "Glass");
      sb.Length--; sb.Append("],\"lights\":[");
      foreach (var l in layout.Lights) sb.Append("{\"dir\":"+(l.Directional?"true":"false")+",\"pos\":"+V(l.Position)+",\"euler\":"+V(l.Euler)+",\"color\":["+F(l.Color.r)+","+F(l.Color.g)+","+F(l.Color.b)+"],\"intensity\":"+F(l.Intensity)+",\"range\":"+F(l.Range)+"},");
      sb.Length--; sb.Append("],\"textures\":{");
      foreach (var id in TexturePainter.Ids) sb.Append("\""+id+"\":\""+Convert.ToBase64String(TexturePainter.Paint(id, 256))+"\",");
      sb.Length--; sb.Append("}}");
    } else {
      int seed = 3;
      foreach (var id in CharacterBuilder.LookIds) { var c = CharacterBuilder.Build(id, seed++, id=="noble"?12f:0f);
        sb.Append("\"char_"+id+"\":["); Part(sb, c.Body, Vector3.zero, "Props"); Part(sb, c.Head, c.HeadPivot, "Props");
        Part(sb, c.ArmLeft, c.ArmLeftPivot, "Props", new Vector3(0,0,-8)); Part(sb, c.ArmRight, c.ArmRightPivot, "Props", new Vector3(0,0,8));
        Part(sb, c.LegLeft, c.LegLeftPivot, "Props"); Part(sb, c.LegRight, c.LegRightPivot, "Props"); sb.Length--; sb.Append("],"); }
      foreach (var d in ItemCatalog.All) { var m = ItemModelBuilder.Build(d.ModelId, d.Id=="azure_crystal");
        sb.Append("\"item_"+d.ModelId+"\":["); Part(sb, m.Solid, Vector3.zero, "Props"); Part(sb, m.Glass, Vector3.zero, "Glass"); sb.Length--; sb.Append("],"); }
      sb.Length--; sb.Append("}");
    }
    File.WriteAllText(a[0], sb.ToString()); Console.WriteLine("wrote " + sb.Length/1024 + " KB");
  }
}

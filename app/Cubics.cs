namespace Oriels;

public class Cubic {
  public bool active;
  public Vec3 p0, p1, p2, p3;
  public Color color;

  public Cubic() {
    color = Color.White;
    active = false;
  }
}

public class CubicCon {
  public CubicCon() { }

  public void Step() {
    Cubic[] cubics = Mono.inst.cubics;
    Rig rig = Mono.inst.rig;
    Con rCon = rig.rCon;
    Con lCon = rig.lCon;
    Vec3 rPos = Mono.inst.rGlove.virtualGlove.position;
    Vec3 lPos = Mono.inst.lGlove.virtualGlove.position;

    bool place = rCon.device.IsStickJustClicked || lCon.device.IsStickJustClicked;
    if (place) {
      for (int i = 0; i < cubics.Length; i++) {
        if (!cubics[i].active) {
          cubics[i].active = true;
          cubics[i].p0 = rPos;
          cubics[i].p1 = rig.rCon.pos;
          cubics[i].p2 = rig.lCon.pos;
          cubics[i].p3 = lPos;
          cubics[i].color = Color.White;
          break;
        }
      }
      Cubic cubic = cubics[PullRequest.RandomRange(0, cubics.Length)];
      cubic.p0 = rPos;
      cubic.p1 = rig.rCon.pos;
      cubic.p2 = rig.lCon.pos;
      cubic.p3 = lPos;
      cubic.color = Color.White;
    }
  }
}

public static class Bezier {
  static int detail = 64;
  public static void Draw(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, Color color) {
    LinePoint[] bezier = new LinePoint[detail];
    for (int i = 0; i < bezier.Length; i++) {
      float t = i / ((float)bezier.Length - 1);
      Vec3 a = Vec3.Lerp(p0, p1, t);
      Vec3 b = Vec3.Lerp(p1, p2, t);
      Vec3 c = Vec3.Lerp(p2, p3, t);
      Vec3 pos = Vec3.Lerp(Vec3.Lerp(a, b, t), Vec3.Lerp(b, c, t), t);
      bezier[i] = new LinePoint(pos, color, 0.01f);
    }
    Lines.Add(bezier);
  }

  public static Vec3 Sample(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, float t) {
    Vec3 a = Vec3.Lerp(p0, p1, t);
    Vec3 b = Vec3.Lerp(p1, p2, t);
    Vec3 c = Vec3.Lerp(p2, p3, t);
    Vec3 pos = Vec3.Lerp(Vec3.Lerp(a, b, t), Vec3.Lerp(b, c, t), t);
    return pos;
  }
  public static Vec3 Sample(Vec3[] points, float t) {
    return Sample(points[0], points[1], points[2], points[3], t);
  }
}
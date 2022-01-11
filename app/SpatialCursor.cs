using System;
using StereoKit;

public abstract class SpatialCursor {
  public Vec3 p0, p1, p2, p3;
  public float min, str, max;

  public Model model = Model.FromFile("cursor.glb", Shader.Default);

  public abstract void Step(Pose[] poses, float scalar);
  public abstract void Calibrate();
}

public class StretchCursor : SpatialCursor {
  public StretchCursor() {
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
  }
  public override void Step(Pose[] poses, float scalar) {
    Pose dom = poses[0];
    Pose sub = poses[1];
    float stretch = (sub.position - dom.position).Magnitude;
    stretch = Math.Max(stretch - 0.1f, 0);
    p0 = dom.position + dom.Forward * stretch * 3;

    model.Draw(Matrix.TS(p0, 0.06f));
  }
  public override void Calibrate() {}
}

public class ReachCursor : SpatialCursor {
  Monolith mono;
  bool chirality;
  public ReachCursor(Monolith mono, bool chirality) {
    this.mono = mono;
    this.chirality = chirality;
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
    this.deadzone = 0;
  }
  public Vec3 origin;
  public float deadzone;
  public override void Step(Pose[] poses, float scalar) {
    Vec3 pos = poses[0].position;
    Vec3 wrist = mono.Wrist(chirality).position;
    Pose shoulder = mono.Shoulder(chirality);

    Vec3 from = (shoulder.orientation * origin) + shoulder.position;

    str = min + (scalar * max);

    float stretch = Vec3.Distance(from, wrist);
    stretch = Math.Max(stretch - deadzone, 0);

    Vec3 dir = (pos - from).Normalized;
    p0 = pos + dir * stretch * str;

    Lines.Add(from, wrist, new Color(1, 0, 1), 0.005f);
    Lines.Add(pos, p0, new Color(0, 1, 1), 0.005f);
  }
  public override void Calibrate() {
    Vec3 wrist = mono.Wrist(chirality).position;
    Pose shoulder = mono.Shoulder(chirality);
    origin = shoulder.orientation.Inverse * (wrist - shoulder.position);
  }
}

public class TwistCursor : SpatialCursor {
  Monolith mono;
  bool chirality;
  public TwistCursor(Monolith mono, bool chirality) {
    this.mono = mono;
    this.chirality = chirality;
    this.min = 1f;
    this.str = 6f;
    this.max = 10f;
  }
  Vec3 twistFrom = -Vec3.Right;
  public override void Step(Pose[] poses, float scalar) {
    
    Vec3 pos = poses[0].position;
    Quat quat = mono.Con(chirality).ori;
    Quat from = Quat.LookAt(Vec3.Zero, quat * Vec3.Forward, twistFrom);
    float twist = (float)(Math.Acos(Vec3.Dot(from * Vec3.Up, quat * Vec3.Up)) / Math.PI);
    outty = Vec3.Dot(from * Vec3.Up, quat * Vec3.Right * (chirality ? 1 : -1)) > 0;
    
    p0 = pos + quat * Vec3.Forward * twist * str;

    // Render
    // model.Draw(Matrix.TS(p0, 0.02f));
    Lines.Add(pos, pos + quat * Vec3.Up * 0.1f, new Color(1, 0, 1), 0.005f);
    Lines.Add(pos, p0, new Color(0, 1, 1), 0.005f);
    // draw the twist (angle)
    Vec3 lastPos = pos;
    for (int i = 0; i < 32; i++) {
      float tw = twist * (i / 31f);
      tw *= outty ? -1 : 1;
      Vec3 nextPos = pos + from * new Vec3((float)Math.Sin(tw * (float)Math.PI), (float)Math.Cos(tw * (float)Math.PI), 0) * 0.1f;

      Lines.Add(lastPos, nextPos, new Color(1, 1, 0), 0.0025f); // convert to LinePoints ?
      lastPos = nextPos;
    }
  }
  public override void Calibrate() {
    Quat quat = mono.Con(chirality).ori;
    twistFrom = quat * Vec3.Up;
  }

  public bool outty;
}

public class CubicFlow : SpatialCursor {
  Monolith mono;
  TwistCursor domTwist;
  TwistCursor subTwist;
  public CubicFlow(Monolith mono) {
    this.mono = mono;
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
    this.domTwist = new TwistCursor(mono, true);
    this.subTwist = new TwistCursor(mono, false);
  }
  bool domTwisting = false; bool domUp = false;
  bool subTwisting = false; bool subUp = false;
  public override void Step(Pose[] poses, float scalar) {
    Pose dom = poses[0];
    Pose sub = poses[1];

    if (mono.rCon.device.stick.y < 0.1f) {
      domTwist.Calibrate();
      domTwisting = false;
    } else {
      if (!domTwisting) {
        domUp = mono.rCon.device.stick.x > 0;
        domTwisting = true;
      }
    }
    domTwist.Step(new Pose[] { dom }, scalar);

    if (mono.lCon.device.stick.y < 0.1f) {
      subTwist.Calibrate();
      subTwisting = false;
    } else {
      if (!subTwisting) {
        subUp = mono.lCon.device.stick.x < 0;
        subTwisting = true;
      }
    }
    subTwist.Step(new Pose[] { sub }, scalar);

    p0 = dom.position;
    p1 = domTwist.p0;
    p2 = subTwist.p0;
    p3 = sub.position;


    if (domTwist.outty) { // domUp
      p0 = domTwist.p0;
      p1 = dom.position;
    }

    if (subTwist.outty) {
      p2 = sub.position;
      p3 = subTwist.p0;
    }
    // Vec3 np0 = Vec3.Lerp(p0, p1, (1 + domCon.stick.x) / 2);
    // Vec3 np1 = Vec3.Lerp(p1, p0, (1 + domCon.stick.x) / 2);
    // Vec3 np2 = Vec3.Lerp(p2, p3, (1 + -subCon.stick.x) / 2);
    // Vec3 np3 = Vec3.Lerp(p3, p2, (1 + -subCon.stick.x) / 2);

    // p0 = np0;
    // p1 = np1;
    // p2 = np2;
    // p3 = np3;


    // if toggle
  }

  public override void Calibrate() {}
}

// a more symmetrical one would be cool
public class SupineCursor : SpatialCursor {
  public SupineCursor() {
    // this.min = 1f;
    // this.str = 3f;
    // this.max = 10f;
  }
  float calibStr;
  Quat calibQuat;
  Pose dom, sub;
  public override void Step(Pose[] poses, float scalar) {
    dom = poses[0];
    sub = poses[1];

    Quat rel = Quat.LookAt(Vec3.Zero, sub.orientation * Vec3.Forward);
    float twist = (Vec3.Dot(rel * -Vec3.Right, sub.orientation * Vec3.Up) + 1) / 2;
    p0 = dom.position + dom.orientation * calibQuat * Vec3.Forward * calibStr * twist;

    model.Draw(Matrix.TS(p0, 0.06f));
  }
  public override void Calibrate() {
    Vec3 target = Input.Head.position + Input.Head.Forward;
    calibStr = Vec3.Distance(dom.position, target) * 2;

    Quat calibAlign = Quat.LookAt(dom.position, target);
    calibQuat = dom.orientation.Inverse * calibAlign;
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

// for fun
// public class ClawCursor : SpatialCursor {

//   Quat calibOff, calibMain;
//   public void Step(Vec3 chest, Pose offPose, Pose mainPose, bool calibrate) {
//     float wingspan = 0.5f;
//     Quat offQuat = calibOff * offPose.orientation;
//     Quat mainQuat = calibMain * mainPose.orientation;
//     Vec3 elbow = chest + mainQuat * mainQuat * Vec3.Forward * wingspan;
//     p0 = elbow + offQuat * offQuat * Vec3.Forward * wingspan;

//     if (calibrate) {
//       calibOff = offPose.orientation.Inverse;
//       calibMain = mainPose.orientation.Inverse;
//     }

//     Lines.Add(chest, elbow, Color.White, 0.01f);
//     Lines.Add(elbow, p0, Color.White, 0.01f);
//     model.Draw(Matrix.TS(p0, 0.06f));
//   }
// }

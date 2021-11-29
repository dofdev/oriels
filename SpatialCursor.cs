using System;
using StereoKit;

// build this out tentatively
public abstract class SpatialCursor {
  public Vec3 p0, p1, p2, p3;
  public float min, str, max;

  public Model model = Model.FromFile("cursor.glb", Shader.Default);

  public abstract void Step(Pose[] poses);
  public abstract void Calibrate();
}

public class Cursors {
  SpatialCursor[] oneHanded = new SpatialCursor[] { new ReachCursor(), new TwistCursor() }; int oneIndex = 0;
  SpatialCursor[] twoHanded = new SpatialCursor[] { new StretchCursor(), new CubicFlow(), new SupineCursor() }; int twoIndex = 0;

  public SpatialCursor Step(Pose lHand, Pose rHand) {
    Pose domHand = rHand; Pose subHand = lHand;

    SpatialCursor cursor =  oneHanded[oneIndex];
    cursor.Step(new Pose[] { domHand, subHand });
    return cursor;
  }
}

public class StretchCursor : SpatialCursor {
  public StretchCursor() {
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
  }
  public override void Step(Pose[] poses) {
    Pose dom = poses[0];
    Pose sub = poses[1];
    float stretch = (sub.position - dom.position).Magnitude;
    stretch = Math.Max(stretch - 0.1f, 0);
    p0 = dom.position + dom.Forward * stretch * 3;

    model.Draw(Matrix.TS(p0, 0.06f));
  }
  public override void Calibrate() {}
}

// this is just a stretch cursor derivative
public class ReachCursor : SpatialCursor {
  public ReachCursor() {
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
  }
  Vec3 pos;
  Vec3 origin;
  public override void Step(Pose[] poses) {
    pos = poses[0].position;
    float stretch = (origin - pos).Length;
    Vec3 dir = (pos - origin).Normalized;
    p0 = pos + dir * stretch * 3;

    model.Draw(Matrix.TS(pos, 0.06f));
    Lines.Add(origin, pos, Color.White, 0.01f);
    model.Draw(Matrix.TS(origin, 0.04f));
  }
  public override void Calibrate() {
    origin = pos;
  }
}

public class TwistCursor : SpatialCursor {
  public TwistCursor() {
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
  }
  public override void Step(Pose[] poses) {
    Vec3 pos = poses[0].position;
    Quat quat = poses[0].orientation;
    Quat rel = Quat.LookAt(Vec3.Zero, quat * Vec3.Forward);
    float twist = (Vec3.Dot(rel * -Vec3.Right, quat * Vec3.Up) + 1) / 2;
    p0 = pos + quat * Vec3.Forward * twist * str;

    model.Draw(Matrix.TS(p0, 0.02f));
  }
  public override void Calibrate() { }
}

public class CubicFlow : SpatialCursor {
  public CubicFlow() {
    this.min = 1f;
    this.str = 3f;
    this.max = 10f;
  }
  TwistCursor domTwist = new TwistCursor();
  TwistCursor subTwist = new TwistCursor();
  public override void Step(Pose[] poses) {
    Pose dom = poses[0];
    Pose sub = poses[1];
    domTwist.Step(new Pose[] { dom });
    subTwist.Step(new Pose[] { sub });

    p0 = dom.position;
    p1 = domTwist.p0;
    p2 = subTwist.p0;
    p3 = sub.position;

    // if toggle

    Draw(this.p0, this.p1, this.p2, this.p3);
  }

  LinePoint[] bezier = new LinePoint[64];
  public void Draw(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3) {
    for (int i = 0; i < bezier.Length; i++) {
      float t = i / ((float)bezier.Length - 1);
      Vec3 a = Vec3.Lerp(p0, p1, t);
      Vec3 b = Vec3.Lerp(p1, p2, t);
      Vec3 c = Vec3.Lerp(p2, p3, t);
      Vec3 pos = Vec3.Lerp(Vec3.Lerp(a, b, t), Vec3.Lerp(b, c, t), t);
      bezier[i] = new LinePoint(pos, Color.White, 0.01f);
    }
    Lines.Add(bezier);
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
  public override void Step(Pose[] poses) {
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

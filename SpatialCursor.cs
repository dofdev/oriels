using System;
using StereoKit;

// build this out tentatively
public abstract class SpatialCursor {
  public Vec3 pos;
  public Quat rot;

  public static Model model = Model.FromFile("cursor.glb", Shader.Default);
}

public class StretchCursor : SpatialCursor {
  public void Step(Pose offPose, Pose mainPose) {
    float stretch = (offPose.position - mainPose.position).Magnitude;
    stretch = Math.Max(stretch - 0.1f, 0);
    pos = mainPose.position + mainPose.Forward * stretch * 3;

    model.Draw(Matrix.TS(pos, 0.06f));
  }
}

// this is just a stretch cursor derivative
public class ReachCursor : SpatialCursor {
  static Vec3 origin;
  public void Step(Vec3 mainPos, bool calibrate) {
    float stretch = (origin - mainPos).Length;
    Vec3 dir = (mainPos - origin).Normalized;
    Vec3 pos = mainPos + dir * stretch * 3;
    model.Draw(Matrix.TS(pos, 0.06f));
    Lines.Add(origin, pos, Color.White, 0.01f);
    model.Draw(Matrix.TS(origin, 0.04f));

    if (calibrate) {
      origin = mainPos;
    }
  }
}

public class TwistCursor : SpatialCursor {
  public void Step(Vec3 mainPos, Quat mainQuat) {
    Quat rel = Quat.LookAt(Vec3.Zero, mainQuat * Vec3.Forward);
    float twist = (Vec3.Dot(rel * -Vec3.Right, mainQuat * Vec3.Up) + 1) / 2;
    pos = mainPos + mainQuat * Vec3.Forward * twist;

    model.Draw(Matrix.TS(pos, 0.06f));
  }
}

// a more symmetrical one would be cool
public class SupineCursor : SpatialCursor {
  float calibStr;
  Quat calibQuat;
  public void Step(Pose offPose, Pose mainPose, bool calibrate) {
    Quat rel = Quat.LookAt(Vec3.Zero, offPose.orientation * Vec3.Forward);
    float twist = (Vec3.Dot(rel * -Vec3.Right, offPose.orientation * Vec3.Up) + 1) / 2;
    
    pos = mainPose.position + mainPose.orientation * calibQuat * Vec3.Forward * calibStr * twist;

    if (calibrate) {   
      Vec3 target = Input.Head.position + Input.Head.Forward;
      calibStr = Vec3.Distance(mainPose.position, target) * 2;

      Quat calibAlign = Quat.LookAt(mainPose.position, target);
      calibQuat = mainPose.orientation.Inverse * calibAlign;
    }

    model.Draw(Matrix.TS(pos, 0.06f));
  }
}

// for fun
public class ClawCursor : SpatialCursor {
  Quat calibOff, calibMain;
  public void Step(Vec3 chest, Pose offPose, Pose mainPose, bool calibrate) {
    float wingspan = 0.5f;
    Quat offQuat = calibOff * offPose.orientation;
    Quat mainQuat = calibMain * mainPose.orientation;
    Vec3 elbow = chest + mainQuat * mainQuat * Vec3.Forward * wingspan;
    pos = elbow + offQuat * offQuat * Vec3.Forward * wingspan;

    if (calibrate) {
      calibOff = offPose.orientation.Inverse;
      calibMain = mainPose.orientation.Inverse;
    }

    Lines.Add(chest, elbow, Color.White, 0.01f);
    Lines.Add(elbow, pos, Color.White, 0.01f);
    model.Draw(Matrix.TS(pos, 0.06f));
  }
}

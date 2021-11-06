using System;
using StereoKit;

// build this out tentatively
public abstract class SpatialCursor {
  public Vec3 pos;
  public Quat rot;

  public static Model model = Model.FromFile("cursor.glb", Shader.Default);
}

// : SpatialCursor
public class ReachCursor {
  static Material unlitMat = Default.MaterialUnlit.Copy();
  static Model modelCursor = Model.FromFile("cursor.glb", Shader.Default);
  static Model modelSphere = new Model(Default.MeshSphere, unlitMat);

  static Vec3[] pullPoints = new Vec3[2];

  public void Step() {
    for (int h = 0; h < (int)Handed.Max; h++) {
      // Get the pose for the index fingertip
      Hand hand = Input.Hand((Handed)h);
      Vec3 indexTip = hand[FingerId.Index, JointId.Tip].Pose.position;
      Vec3 thumbTip = hand[FingerId.Thumb, JointId.Tip].Pose.position;
      Vec3 pinchPos = Vec3.Lerp(indexTip, thumbTip, 0.5f);
      if (hand.IsPinched) {
        pullPoints[h] = pinchPos;
      }

      float stretch = (pullPoints[h] - pinchPos).Length;
      Vec3 dir = (pinchPos - pullPoints[h]).Normalized;
      Vec3 pos = pinchPos + dir * stretch * 3;
      modelCursor.Draw(Matrix.TS(pos, 0.06f));
      Lines.Add(pullPoints[h], pos, Color.White, 0.01f);
      modelSphere.Draw(Matrix.TS(pullPoints[h], 0.04f));
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

public class SupineCursor : SpatialCursor {
  float calibStr;
  Quat calibQuat;
  public void Step(Quat offQuat, Vec3 mainPos, Quat mainQuat, bool calibrate = false) {
    Quat rel = Quat.LookAt(Vec3.Zero, offQuat * Vec3.Forward);
    float twist = (Vec3.Dot(rel * -Vec3.Right, offQuat * Vec3.Up) + 1) / 2;
    
    pos = mainPos + mainQuat * calibQuat * Vec3.Forward * calibStr * twist;

    if (calibrate) {   
      Vec3 target = Input.Head.position + Input.Head.Forward;
      calibStr = Vec3.Distance(mainPos, target) * 2;

      Quat calibAlign = Quat.LookAt(mainPos, target);
      calibQuat = mainQuat.Inverse * calibAlign;
    }

    model.Draw(Matrix.TS(pos, 0.06f));
  }
}

public class BallsCursor : SpatialCursor {
  Vec3 offPivot, mainPivot;
  Quat offBall, mainBall;
  Quat oldOffQuat, oldMainQuat;
  float offRadius, mainRadius;
  bool offPress, mainPress;
  public void Step(
    Vec3 offPos, Quat offQuat, 
    Vec3 mainPos, Quat mainQuat, 
    bool offGrip, bool mainGrip, bool calibrate = false
    ) {

    Function(offPos, ref offPivot, ref offBall, offQuat, ref oldOffQuat, ref offRadius, offGrip, ref offPress);
    Function(mainPos, ref mainPivot, ref mainBall, mainQuat, ref oldMainQuat, ref mainRadius, mainGrip, ref mainPress);

    void Function(Vec3 handPos, ref Vec3 pivot, ref Quat ball, Quat quat, ref Quat oldQuat, ref float radius, bool grip, ref bool press) {
      if (grip) {
        if (!press) {
          pivot = pos + ball * Vec3.Forward * radius;
          oldQuat = quat;
          press = true;
        }
        ball = Quat.Difference(oldQuat, quat) * ball;
        pos = pivot + (ball * -Vec3.Forward * radius);

        oldQuat = quat;
      }
      else {
        press = false;
      }

      // Sphere sphere = new Sphere(pos, mainDiameter);
      Default.MeshSphere.Draw(
        clearMat, 
        Matrix.TS(pos + ball * Vec3.Forward * radius, radius * 2)
      );
    }

    if (calibrate)
    {
      pos = Vec3.Lerp(offPos, mainPos, 0.5f);
      offRadius = mainRadius = Vec3.Distance(offPos, mainPos) / 2;
      offBall = Quat.LookAt(mainPos, offPos);
      mainBall = Quat.LookAt(offPos, mainPos);
    }

    model.Draw(Matrix.TS(pos, 0.06f));
    clearMat.SetColor("color", new Color(1, 1, 1, 0.1f));
  }
  Material clearMat = Default.MaterialHand;
}

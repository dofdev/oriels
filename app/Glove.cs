using System;
using StereoKit;

public enum Pull {
  Stretch, Backhanded
}

public class Glove {
  bool chirality;
  public Glove(bool chirality) {
    this.chirality = chirality;
  }

  public Pose virtualGlove;
  Quat projection = Quat.Identity;
  Vec3 direction {
    get { return projection * new Vec3(0, 0, -1); }
    set { projection = Quat.LookDir(value); }
  }

  public Pull? pulling = null;
  float stretch;
  float stretchDeadzone = 0;
  Vec3 pullPoint;

  float twist;
  bool twistOut;
  Quat twistOffset;
  Quat oldOri;

  int firstFace;

  public void Step() {
    Rig rig = Mono.inst.rig;
    Pose shoulder = rig.Shoulder(chirality);
    Pose wrist = rig.Wrist(chirality);
    Con con = rig.Con(chirality), otherCon = rig.Con(!chirality);
    bool pull = otherCon.gripBtn.frameDown;

    if (firstFace == 0) { 
      if (con.device.IsX1JustPressed) { firstFace = 1; }
      if (con.device.IsX2JustPressed) { firstFace = 2; }
    }
    if (!con.device.IsX1Pressed && !con.device.IsX2Pressed) { firstFace = 0; }

    bool twisting = firstFace == 1;
    bool reaching = firstFace == 2;

    bool lift = false;
    if (firstFace == 1 && con.device.IsX2Pressed) { lift = true; }
    if (firstFace == 2 && con.device.IsX1Pressed) { lift = true; }

    // exclusive states?

    if (reaching) {
      // shoulder stuff
      // pullPoint = (shoulder.orientation * origin) + shoulder.position;
      // shoulder.orientation.Inverse * (con.pose.position - shoulder.position)
      if (lift) {
        pullPoint = con.pos + -direction * stretch;
      } else {
        direction = PullRequest.Direction(con.pos, pullPoint);
      }
    } else {
      pullPoint = con.pos;
    }

    switch (pulling) {
      default:
        if (pull) {
          // need the rotation of the wrist rather than the hand for this to be reliable
          Vec3 localPos = con.ori.Inverse * (otherCon.pos - con.pos);
          pulling = (chirality ? localPos.x < 0 : localPos.x > 0) ? Pull.Stretch : Pull.Backhanded;
        }
        stretchDeadzone = pull ? Vec3.Distance(con.pos, otherCon.pos) : 0;
        virtualGlove.orientation = con.ori;
        break;

      case Pull.Stretch:
        pullPoint = otherCon.pos;
        projection = con.ori;
        virtualGlove.orientation = otherCon.ori;
        break;

      case Pull.Backhanded:
        pullPoint = otherCon.pos;
        direction = PullRequest.Direction(con.pos, otherCon.pos);
        virtualGlove.orientation = con.ori;
        break;
    }

    if (!otherCon.gripBtn.held) {
      pulling = null;
    }

    if (!twisting) { 
      stretch = Math.Max(Vec3.Distance(pullPoint, con.pos) - stretchDeadzone, 0);
      twist = 0;
      
      twistOffset = Quat.Identity;
    }

    float twistDelta = MathF.Acos(Vec3.Dot(con.ori * projection * Vec3.Up, oldOri * projection * Vec3.Up)) / SKMath.Pi;
    twistOut = Vec3.Dot(con.ori * projection * Vec3.Up, oldOri * projection * Vec3.Right * (chirality ? 1 : -1)) > 0;
    twistDelta *= twistOut ? -1 : 1;
    if (!float.IsFinite(twistDelta)) { twistDelta = 0; }
    if (twisting) {
      stretch = 0;

      if (lift) {
        twistOffset = con.ori.Inverse * projection;
      } else {
        twist = twist + twistDelta;

        projection = con.ori * twistOffset;
      }

      virtualGlove.orientation = con.ori;
    } 
    oldOri = con.ori;

    virtualGlove.position = con.pos + direction * (stretch + Math.Abs(twist)) * 3;

    Render(con.Pose(), virtualGlove, wrist, stretch, twist, chirality);
  }

  // decouple the rendering
  // the render-relevent DATA that gets streamed over the network
  // that way we can render the same way for all peers
  Mesh mesh = Default.MeshCube;
  Material mat = Default.Material;
  Model model = Model.FromFile("skinned_test.glb", Shader.Default);
  public void Render(Pose glove, Pose virtualGlove, Pose wrist, float stretch, float twist, bool chirality) {
    Lines.Add(pullPoint, glove.position, new Color(1, 0, 1, 0.1f), 0.005f);
    Lines.Add(glove.position, virtualGlove.position, new Color(0, 1, 1, 0.1f), 0.005f);

    // Twist
    float twistAbs = Math.Abs(twist);
    Vec3 twistStuff = glove.position + projection * glove.orientation.Inverse * (wrist.position - glove.position);
    int segments = twistAbs == 0 ? -1 : 6 + (int)(twistAbs * 10);
    LinePoint[] linePoints = new LinePoint[segments + 2];
    linePoints[0] = new LinePoint(twistStuff, new Color(1, 1, 0), 0.005f);

    for (int i = 0; i <= segments; i++) {
      float tw = twistAbs * Math.Min(i / (float)(segments - 1), 1);
      tw *= chirality ? 1 : -1;
      tw *= twist > 0 ? 1 : -1;
      float tighten = Math.Max(1 - (twistAbs / 9), 0);
      float radius = i == segments ? 0.06f : 0.05f * (1 - (1 - i / (float)segments) * (1 - tighten));
      Vec3 nextPos = twistStuff + projection * new Vec3(SKMath.Sin(tw * SKMath.Pi), SKMath.Cos(tw * SKMath.Pi), 0) * radius;

      // Lines.Add(lastPos, nextPos, new Color(1, 1, 0), 0.005f);
      linePoints[i + 1] = new LinePoint(nextPos, new Color(1, 1, 0), 0.005f);
    }
    Lines.Add(linePoints);

    // mesh.Draw(mat, glove.ToMatrix(new Vec3(0.02f, 0.08f, 0.08f) / 1));
    mesh.Draw(mat, virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3));


    // ModelNode top = model.FindNode("Top");
    // top.LocalTransform = Matrix.R(Quat.FromAngles(Vec3.Right * 45));
    // Console.WriteLine(top.Name);
    // model.Draw(glove.ToMatrix(Vec3.One / 10));
  }
}

using System;
using StereoKit;

public enum Pull {
  Stretch, Backhanded
}

public class Glove {
  Monolith mono;
  bool chirality;
  public Glove(Monolith mono, bool chirality) {
    this.mono = mono;
    this.chirality = chirality;
  }

  public Pose virtualGlove;
  Vec3 direction;

  public Pull? pulling = null;
  float stretch;
  float stretchDeadzone = 0;
  Vec3 pullPoint;
  Vec3 twistPoint;
  bool twistOut;

  public void Step() {
    Pose shoulder = mono.Shoulder(chirality);
    Pose wrist = mono.Wrist(chirality);
    Con con = mono.Con(chirality), otherCon = mono.Con(!chirality);
    bool reach = con.device.IsX2Pressed;
    bool pull = otherCon.gripBtn.frameDown;
    bool lift = con.device.IsX1Pressed;
    lift = false;
    bool twist = con.device.IsX1Pressed;

    // exclusive states?

    if (reach) {
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
        direction = con.ori * Vec3.Forward;
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

    stretch = Math.Max(Vec3.Distance(pullPoint, con.pos) - stretchDeadzone, 0);

    if (!twist) { twistPoint = con.ori * Vec3.Up; }
    Quat twistFrom = Quat.LookAt(Vec3.Zero, con.ori * Vec3.Forward, twistPoint);
    if (twist) {
      stretch = (float)(Math.Acos(Vec3.Dot(twistFrom * Vec3.Up, con.ori * Vec3.Up)) / Math.PI);
      twistOut = Vec3.Dot(twistFrom * Vec3.Up, con.ori * Vec3.Right * (chirality ? 1 : -1)) > 0;

      direction = con.ori * Vec3.Forward;
      virtualGlove.orientation = twistFrom;
    } 

    virtualGlove.position = con.pos + direction * stretch * 3;

    Render(con.Pose(), virtualGlove, wrist, twist, stretch, twistOut, twistFrom);
  }

  // decouple the rendering
  // the render-relevent DATA that gets streamed over the network
  // that way we can render the same way for all peers
  static Mesh mesh = Default.MeshCube;
  static Material mat = Default.Material;
  public void Render(Pose glove, Pose virtualGlove, Pose wrist, bool twist, float stretch, bool twistOut, Quat twistFrom) {
    Lines.Add(pullPoint, glove.position, new Color(1, 0, 1), 0.005f);
    Lines.Add(glove.position, virtualGlove.position, new Color(0, 1, 1), 0.005f);

    // Twist
    float twistValue = twist ? stretch : 0;
    Lines.Add(
      wrist.position + glove.orientation * Vec3.Up * 0.04f, 
      wrist.position + glove.orientation * Vec3.Up * 0.05f, 
      new Color(1, 1, 0), 0.005f
    );
    Vec3 lastPos = wrist.position;
    for (int i = 0; i < 32; i++) {
      float tw = twistValue * (i / 31f);
      tw *= twistOut ? -1 : 1;
      Vec3 nextPos = wrist.position + twistFrom * new Vec3(SKMath.Sin(tw * SKMath.Pi), SKMath.Cos(tw * SKMath.Pi), 0) * 0.05f;

      Lines.Add(lastPos, nextPos, new Color(1, 1, 0), 0.005f); // convert to LinePoints ?
      lastPos = nextPos;
    }

    mesh.Draw(mat, glove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3));
    mesh.Draw(mat, virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f)));
  }
}

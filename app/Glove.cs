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

  public Pose cursor;
  Vec3 cursorDir;

  public Pull? pulling = null;
  float stretchDeadzone = 0;
  Vec3 pullPoint;

  // bool rightPlanted = false;

  public void Step() {
    Pose shoulder = mono.Shoulder(chirality);
    Con con = mono.Con(chirality), otherCon = mono.Con(!chirality);

    pullPoint = con.pos;
    // cursorDir = (con.pos - pullPoint).Normalized; // WRONG PLACE?

    switch (pulling) {
      case Pull.Stretch:
        pullPoint = otherCon.pos;
        cursorDir = con.ori * Vec3.Forward;
        break;

      case Pull.Backhanded:

        break;

      default:
        if (otherCon.gripBtn.frameDown) {
          // comparison evaluation
          pulling = Pull.Stretch;
          stretchDeadzone = Vec3.Distance(con.pos, otherCon.pos);
        }
        break;
    }

    if (!otherCon.gripBtn.held) {
      // null
      pulling = null;
    }

    // Vec3 from = (shoulder.orientation * origin) + shoulder.position;
    float stretch = Vec3.Distance(pullPoint, con.pos);
    stretch = Math.Max(stretch - stretchDeadzone, 0);

    cursor.position = con.pos + cursorDir * stretch * 3;

    Lines.Add(pullPoint, con.pos, new Color(1, 0, 1), 0.005f);
    Lines.Add(con.pos, cursor.position, new Color(0, 1, 1), 0.005f);



    // if (con.stick.Magnitude > 0.1f) {
    //   if (con.stick.y < 0f) {
    //     rightPlanted = true;
    //   }
    // } else {
    //   rightPlanted = false;
    // }

    // if (!rightPlanted) {
    //   reachCursor.p0 = con.pose.position;
    //   reachCursor.Calibrate();
    // }

    // if (con.grip > 0.5f) {
    //   Vec3 toPos = shoulder.orientation.Inverse * (con.pose.position - shoulder.position);
    //   if (!rightGripDown) {
    //     float deadzone = Vec3.Distance(leftReachCursor.origin, toPos);
    //     if (deadzone < 0.1f) {
    //       leftReachCursor.deadzone = deadzone;
    //       rightGripDown = true;
    //     }
    //   }

    //   if (rightGripDown) {
    //     leftReachCursor.origin = toPos;
    //   }
    // } else {
    //   leftReachCursor.deadzone = 0;
    //   rightGripDown = false;
    // }

    Render(con.Pose(), cursor);
  }

  // decouple the rendering
  // render-relevent DATA that gets streamed over the network
  // that way we can render the same way for all peers
  static Mesh mesh = Default.MeshCube;
  static Material mat = Default.Material;
  public void Render(Pose pose, Pose cursor) {
    mesh.Draw(mat, pose.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f)));
    mesh.Draw(mat, cursor.ToMatrix(Vec3.One * 0.035f));
  }
}

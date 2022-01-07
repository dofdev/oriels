using System;
using StereoKit;

public enum Grab {
  Stretch, Backhanded
}

public class Glove {
  Monolith mono;
  bool chirality;
  public ReachCursor reachCursor;
  public Glove(Monolith mono, bool chirality) {
    this.mono = mono;
    this.chirality = chirality;
    this.reachCursor = new ReachCursor(mono, chirality);
  }

  public Vec3 cursor;

  public Grab? grabbed = null;
  float stretchDeadzone = 0;
  Vec3 pullPoint;

  // bool rightPlanted = false;

  public void Step() {
    Pose shoulder = mono.Shoulder(chirality);
    Con con = mono.Con(chirality); // !chirality

    pullPoint = con.pos;

    switch (grabbed) {
      case Grab.Stretch:
        break;

      case Grab.Backhanded:
        break;

      default:
        if (con.gripBtn.frameDown) {
          // comparison evaluation 
          grabbed = Grab.Stretch;
        }
        break;
    }

    if (!con.gripBtn.held) {
      // null
      grabbed = null;
    }

    // Vec3 from = (shoulder.orientation * origin) + shoulder.position;
    float stretch = Vec3.Distance(pullPoint, con.pos);
    stretch = Math.Max(stretch - stretchDeadzone, 0);

    cursor = con.pos + (con.pos - pullPoint).Normalized * stretch * 3;

    Lines.Add(pullPoint, con.pos, new Color(1, 0, 1), 0.005f);
    Lines.Add(con.pos, cursor, new Color(0, 1, 1), 0.005f);




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

  }
}
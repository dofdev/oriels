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
  float stretchDeadzone = 0;
  Vec3 pullPoint;

  bool planted = false;

  public void Step() {
    Pose shoulder = mono.Shoulder(chirality);
    Con con = mono.Con(chirality), otherCon = mono.Con(!chirality);
    bool pull = otherCon.gripBtn.frameDown;

    if (!planted) {
      pullPoint = con.pos;
    } else {
      // reach cursor
      // shoulder stuff
      // pullPoint = (shoulder.orientation * origin) + shoulder.position;
      // shoulder.orientation.Inverse * (con.pose.position - shoulder.position)
      direction = PullRequest.Direction(con.pos, pullPoint);
    }

    switch (pulling) {
      default:
        if (con.device.stick.Magnitude > 0.1f) {
          if (con.device.stick.y < 0f) {
            planted = true;
          }
        } else {
          planted = false;
        }

        if (pull) {
          // need the rotation of the wrist rather than the hand for this to be reliable
          Vec3 localPos = con.ori.Inverse * (otherCon.pos - con.pos);
          if (chirality ? localPos.x < 0 : localPos.x > 0) {
            pulling = Pull.Stretch;
          } else {
            pulling = Pull.Backhanded;
          }

          stretchDeadzone = Vec3.Distance(con.pos, otherCon.pos);
        } else {
          stretchDeadzone = 0;
        }
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

    float stretch = Vec3.Distance(pullPoint, con.pos);
    stretch = Math.Max(stretch - stretchDeadzone, 0);
    virtualGlove.position = con.pos + direction * stretch * 3;

    Render(con.Pose(), virtualGlove);
  }

  // decouple the rendering
  // the render-relevent DATA that gets streamed over the network
  // that way we can render the same way for all peers
  static Mesh mesh = Default.MeshCube;
  static Material mat = Default.Material;
  public void Render(Pose glove, Pose virtualGlove) {
    Lines.Add(pullPoint, glove.position, new Color(1, 0, 1), 0.005f);
    Lines.Add(glove.position, virtualGlove.position, new Color(0, 1, 1), 0.005f);

    mesh.Draw(mat, glove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f) / 3));
    mesh.Draw(mat, virtualGlove.ToMatrix(new Vec3(0.025f, 0.1f, 0.1f)));
  }
}

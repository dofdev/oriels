using System;
using StereoKit;

class Cubic {
  public bool active;
  public Vec3 p0, p1, p2, p3;
  public Color color;

  public Cubic() {
    color = Color.White;
    active = false;
  }

  public void Enable(Vec3 p0, Vec3 p1, Vec3 p2, Vec3 p3, Color c) {
    this.p0 = p0;
    this.p1 = p1;
    this.p2 = p2;
    this.p3 = p3;
    color = c;
    active = true;
  }

  public void Disable() {
    active = false;
  }

  public void Draw() {
    if (active) {
      Bezier.Draw(p0, p1, p2, p3, color);
    }
  }
}

class CubicCon {
  public void Step(Con domCon, Con subCon, Peer peer, ref Cubic[] cubics) {
    bool place = domCon.device.IsStickJustClicked || subCon.device.IsStickJustClicked;
    if (place) {
      for (int i = 0; i < cubics.Length; i++) {
        if (!cubics[i].active) {
          cubics[i].Enable(peer.cursor0, peer.cursor1, peer.cursor2, peer.cursor3, peer.color);
          break;
        }
      }
      cubics[PullRequest.RandomRange(0, cubics.Length)].Enable(peer.cursor0, peer.cursor1, peer.cursor2, peer.cursor3, peer.color);
    }
  }
}
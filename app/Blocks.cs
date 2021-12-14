using System;
using StereoKit;

class Block {
  public static Mesh mesh = Default.MeshCube;
  public static Material mat = Default.Material;

  public bool active = false;
  public Solid solid;

  public Color color;
  public float size = 0.5f;

  // if you grab someone else's it becomes your own
  // how to communicate to the other peer that you have grabbed it?
  // public int request; // request ownership
  // public int owner; // then if owner continue as usual
  // public bool busy; // marked as held so no fighting
  public Block(SolidType type, Color color) {
    this.solid = new Solid(Vec3.Zero, Quat.Identity, type);
    this.size = 0.5f;
    this.solid.AddBox(Vec3.One * size, 3);
    this.color = color;
    Disable();
  }

  // public Block(Vec3 pos, Quat rot, SolidType type, Color color) {
  //   this.solid = new Solid(pos, rot, type);
  //   this.solid.AddBox(Vec3.One, 1);
  //   this.color = color;
  // }

  public void Enable(Vec3 pos, Quat rot) {
    solid.SetAngularVelocity(Vec3.Zero);
    solid.SetVelocity(Vec3.Zero);
    solid.Teleport(pos, rot);
    solid.Enabled = active = true;
  }

  public void Disable() {
    solid.Enabled = active = false;
  }

  public void Draw() {
    if (active) {
      mesh.Draw(mat, solid.GetPose().ToMatrix(Vec3.One * size), color);
    }
  }
}

class BlockCon {
  public int index = -1;
  public Vec3 offset = Vec3.Zero;
  public Quat heldRot = Quat.Identity, spinRot = Quat.Identity, spinDelta = Quat.Identity;
  public Quat oldConRot = Quat.Identity, oldHeldRot = Quat.Identity;
  public Vec3 delta = Vec3.Zero, momentum = Vec3.Zero, angularMomentum = Vec3.Zero;

  float lastPressed = 0;
  bool pressed = false;

  public void Step(Controller con, Controller otherCon, Vec3 cursor, ref BlockCon otherBlockCon, ref Block[] blocks) {
    bool doublePressed = false;
    if (con.trigger > 0.5f) {
      if (!pressed) {
        if (lastPressed > Time.Totalf - 0.5f) {
          doublePressed = true;
        }
        lastPressed = Time.Totalf;
        pressed = true;
      }
    } else {
      pressed = false;
    }

    if (doublePressed) {
      if (index < 0) {
        bool bFound = false;
        for (int i = 0; i < blocks.Length; i++) {
          if (!blocks[i].active) {
            blocks[i].Enable(cursor, Quat.Identity);
            bFound = true;
            break;
          }
        }
        if (!bFound) {
          blocks[PullRequest.RandomRange(0, blocks.Length)].Enable(cursor, Quat.Identity);
        }
      } else {
        blocks[index].Disable();
        index = -1;
      }
    }

    Quat conRotDelta = (con.aim.orientation * oldConRot.Inverse).Normalized;

    if (con.trigger > 0.1f) {
      if (index < 0) {
        // BLOCK EXCHANGE
        // loop over peer blocks as well
        // disable theirs ? (id of the peer, index of block)
        // wait for their block to be disabled
        // recycle one of yours to replace it

        for (int i = 0; i < blocks.Length; i++) {
          Pose blockPose = blocks[i].solid.GetPose();
          Bounds bounds = new Bounds(Vec3.Zero, Vec3.One * blocks[i].size);
          if (blocks[i].active && bounds.Contains(blockPose.orientation.Inverse * (cursor - blockPose.position))) {
            index = i;
            if (otherBlockCon.index == i) {
              otherBlockCon.index = -1;
            }
            // block.color = colorCube.color;
            // clear
            spinRot = spinDelta = Quat.Identity;
            blocks[i].solid.SetAngularVelocity(Vec3.Zero);
            blocks[i].solid.SetVelocity(Vec3.Zero);
            // set
            heldRot = (con.aim.orientation.Inverse * blockPose.orientation).Normalized;
            offset = blockPose.orientation.Inverse * (blockPose.position - cursor);

            // 
            break;
          }
        }
      }

      if (index >= 0) {
        Quat newRot = (con.aim.orientation * heldRot * spinRot).Normalized;
        // trackballer
        if (con.trigger > 0.99f) {
          spinDelta = Quat.Slerp(
            spinDelta.Normalized,
            (newRot.Inverse * conRotDelta * newRot).Normalized,
            Time.Elapsedf / 0.1f
          );
        }
        spinRot *= spinDelta * spinDelta;
        Quat toRot = (con.aim.orientation * heldRot * spinRot).Normalized;
        Vec3 toPos = cursor + (con.aim.orientation * heldRot * spinRot).Normalized * offset;
        // cursor - offset;
        if (con.stick.y > 0.1f) {
          toRot = blocks[index].solid.GetPose().orientation;
        }
        blocks[index].solid.Move(toPos, toRot);

        Quat newHeldRot = blocks[index].solid.GetPose().orientation;
        angularMomentum = Vec3.Lerp(angularMomentum, PullRequest.AngularDisplacement((newHeldRot * oldHeldRot.Inverse).Normalized), Time.Elapsedf / 0.1f);
        oldHeldRot = newHeldRot;

        delta = (cursor + (con.aim.orientation * heldRot * spinRot).Normalized * offset) - blocks[index].solid.GetPose().position;
        momentum = Vec3.Lerp(momentum, delta, Time.Elapsedf / 0.1f);
      }
    } else {
      if (index >= 0) {
        blocks[index].solid.SetAngularVelocity(angularMomentum / Time.Elapsedf);
        blocks[index].solid.SetVelocity(momentum / Time.Elapsedf);
      }
      index = -1;
    }

    oldConRot = con.aim.orientation;
  }
}

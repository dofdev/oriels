using System;
using StereoKit;

SKSettings settings = new SKSettings {
  appName = "oriels",
  assetsFolder = "add",
  depthMode = DepthMode.D32,
  disableUnfocusedSleep = true,
};
if (!SK.Initialize(settings))
  Environment.Exit(1);

Input.HandSolid(Handed.Max, false);
// Input.HandVisible(Handed.Max, false);
// TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);

Monolith mono = new Monolith();
mono.Run();

public class Con {
  public Controller device;
  public Vec3 pos;
  public Quat ori;
  public Btn gripBtn;
  public Btn triggerBtn;

  public void Step(bool chirality) {
    device = Input.Controller(chirality ? Handed.Right : Handed.Left);
    pos = device.pose.position;
    ori = device.aim.orientation;
    gripBtn.Step(device.grip > 0.5f);
    triggerBtn.Step(device.trigger > 0.5f);
  }

  public Pose Pose() {
    return new Pose(pos, ori);
  }
}

public struct Btn {
  public bool frameDown, held, frameUp;

  public void Step(bool down) {
    frameDown = down && !held;
    frameUp = !down && held;
    held = down;
  }
}

public class Monolith {
  public MonoNet net;
  public Scene scene;
  public Mic mic;

  public Con rCon = new Con(), lCon = new Con();
  public Con Con(bool chirality) {
    return chirality ? rCon : lCon;
  }
  public Pose rShoulder, lShoulder;
  public Pose Shoulder(bool chirality) {
    return chirality ? rShoulder : lShoulder;
  }
  public Pose rWrist, lWrist;
  public Pose Wrist(bool chirality) {
    return chirality ? rWrist : lWrist;
  }
  public Glove lGlove, rGlove;
  public Glove Glove(bool chirality) {
    return chirality ? rGlove : lGlove;
  }
  public ColorCube colorCube = new ColorCube();
  public Block[] blocks;
  public BlockCon rBlock, lBlock;
  public BlockCon BlockCon(bool chirality) {
    return chirality ? rBlock : lBlock;
  }
  public Cubic[] cubics;
  public CubicCon cubicCon;

  public Vec3 rDragStart, lDragStart;
  public float railT;

  Mesh ball = Default.MeshSphere;
  Material mat = Default.Material;
  Mesh cube = Default.MeshCube;

  public void Run() {
    Renderer.SetClip(0.02f, 1000f);

    scene = new Scene(this);
    net = new MonoNet(this);
    net.Start();
    // mic = new Mic();
    rGlove = new Glove(this, true);
    lGlove = new Glove(this, false);
    blocks = new Block[] {
      new Block(), new Block(), new Block(),
      new Block(), new Block(), new Block()
    };
    rBlock = new BlockCon(this, true);
    lBlock = new BlockCon(this, false);
    cubics = new Cubic[] {
      new Cubic(), new Cubic(), new Cubic(),
      new Cubic(), new Cubic(), new Cubic()
    };
    cubicCon = new CubicCon(this);


    Vec3 pos = new Vec3(0, 0, 0);
    Vec3 vel = new Vec3(0, 0, 0);

    Oriel oriel = new Oriel();
    oriel.Start(3);
    // Oriel otherOriel = new Oriel();
    // otherOriel.Start(4);

    Vec3 oldLPos = Vec3.Zero;

    while (SK.Step(() => {
      Renderer.CameraRoot = Matrix.T(pos);
      
      rCon.Step(true);
      lCon.Step(false);

      // Shoulders
      Vec3 headPos = Input.Head.position + Input.Head.Forward * -0.15f;
      Vec3 shoulderDir = (
        (lCon.pos.X0Z - headPos.X0Z).Normalized + 
        (rCon.pos.X0Z - headPos.X0Z).Normalized
      ).Normalized;

      if (Vec3.Dot(shoulderDir, Input.Head.Forward) < 0) { shoulderDir = -shoulderDir; }
      rShoulder = new Pose(headPos + Quat.LookDir(shoulderDir) * new Vec3(0.2f, -0.2f, 0), Quat.LookDir(shoulderDir));
      lShoulder = new Pose(headPos + Quat.LookDir(shoulderDir) * new Vec3(-0.2f, -0.2f, 0), Quat.LookDir(shoulderDir));

      // Wrists
      rWrist = new Pose(rCon.pos + rCon.ori * new Vec3(0, 0, 0.052f), rCon.ori);
      lWrist = new Pose(lCon.pos + lCon.ori * new Vec3(0, 0, 0.052f), lCon.ori);

      // Gloves
      rGlove.Step();
      lGlove.Step();

      // Blocks
      rBlock.Step();
      lBlock.Step();

      // Cubic
      cubicCon.Step();

      // boolean over network to determine if a peers cubic flow should be drawn


      // throw yourself (delta -> vel -> momentum)
      // bring rails back

      // FULLSTICK
      // Quat rot = Quat.FromAngles(subCon.stick.y * -90, 0, subCon.stick.x * 90);
      // Vec3 dir = Vec3.Up * (subCon.IsStickClicked ? -1 : 1);
      // Vec3 fullstick = subCon.aim.orientation * rot * dir;
      // pos += fullstick * subCon.trigger * Time.Elapsedf;




      // DRAG DRIFT
      // Vec3 rPos = net.me.cursor0;
      // Vec3 lPos = net.me.cursor3;

      // use grip grab reach cursor origin it then becoming a backhanded stretch cursor
      // if (rCon.grip > 0.5f) {
      //   if (!rightGripping) {
      //     gripPos = rPos;
      //     gripLeft = false;
      //     rightGripping = true;
      //   }
      // } else {
      //   rightGripping = false;
      // }

      // if (lCon.grip > 0.5f) {
      //   if (!leftGripping) {
      //     gripPos = lPos;
      //     gripLeft = true;
      //     leftGripping = true;
      //   }
      // } else {
      //   leftGripping = false;
      // }

      // if (rightGripping || leftGripping) {
      //   Vec3 gripTo = gripLeft ? lPos : rPos;
      //   pos = -(gripTo - Input.Head.position) + gripPos - (Input.Head.position - pos);
      //   vel = Vec3.Zero;
      // }
      // delete: gripPos, gripLeft, rightGripping, leftGripping
      // gripPos = r/l OldPos
      // rightGripping/leftGripping -> state machine (world grip, stretch, backhanded, grinding?)





      // CUBIC BEZIER RAIL

      // float grindDir = 1f;
      // bool grinding = false;
      // bool grinded = false;
      // Vec3 grindVel = Vec3.Forward;
      // Vec3[] grindRail = new Vec3[4];

      // if (rCon.device.grip > 0.5f) {
      //   if (!grinded) {
      //     if (!grinding) {
      //       int closest = 0;
      //       float closestDist = float.MaxValue;
      //       Vec3 closestPoint = Vec3.Zero;
      //       int closestRail = 0;
      //       for (int i = 0; i < net.me.cubics.Length; i++) {
      //         if (net.me.cubics[i].active) {
      //           Vec3[] rail = new Vec3[] {
      //             net.me.cubics[i].p0,
      //             net.me.cubics[i].p1,
      //             net.me.cubics[i].p2,
      //             net.me.cubics[i].p3,
      //           };
      //           for (int j = 0; j < rail.Length; j++) {
      //             Vec3 point = Bezier.Sample(rail, (float)j / (rail.Length - 1f));
      //             float dist = Vec3.Distance(point, rCon.pos + vel.Normalized * 0.25f);
      //             if (dist < closestDist && dist < 0.5f) {
      //               closest = j;
      //               closestRail = i;
      //               closestDist = dist;
      //               closestPoint = point;
      //               railT = (float)j / (rail.Length - 1f);
      //               grinding = true;
      //             }
      //           }
      //         }
      //       }
      //       if (grinding) {
      //         grindRail = new Vec3[] {
      //           net.me.cubics[closestRail].p0,
      //           net.me.cubics[closestRail].p1,
      //           net.me.cubics[closestRail].p2,
      //           net.me.cubics[closestRail].p3,
      //         };
      //         // pos = closestPoint - (subCon.pose.position - pos);
      //         grindVel = vel;
      //         Vec3 fromPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
      //         Vec3 toPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f);
      //         grindDir = Vec3.Dot((fromPos - toPos).Normalized, grindVel) < 0f ? 1 : -1;
      //       }
      //     }

      //     if (grinding) {
      //       Vec3 grindPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
      //       Vec3 nextPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f * grindDir);

      //       // vel += (toPos - fromPos);

      //       pos = -(rCon.pos - Input.Head.position) + grindPos - (Input.Head.position - pos);
      //       vel = Vec3.Zero;

      //       railT += Time.Elapsedf * grindVel.Magnitude * grindDir; // scale based on length of rail * calculate and cache on place
      //       // bool clamped = false;
      //       // float railTpreClamp = railT;
      //       // if
      //       railT = Math.Clamp(railT, 0, 1);

      //       grindVel = (nextPos - grindPos).Normalized * grindVel.Magnitude;

      //       if (railT == 1 || railT == 0) {
      //         vel = grindVel;
      //         grinding = false;
      //         grinded = true;
      //         railT = 0f;
      //       }


      //       cube.Draw(mat, Matrix.TS(grindPos, new Vec3(0.1f, 0.1f, 0.1f)));
      //       // cube.Draw(mat, Matrix.TS(toPos, new Vec3(0.1f, 0.1f, 0.1f) * 0.333f));
      //       // pos = Vec3.Lerp(pos, Bezier.Sample(net.me.cubics[0].p0, net.me.cubics[0].p1, net.me.cubics[0].p2, net.me.cubics[0].p3, railT) - (subCon.aim.position - pos), Time.Elapsedf * 6f);
      //       // how to reliably determine and control which direction to go? (velocity)
      //     }
      //   }
      // } else {
      //   grinded = false;
      //   if (grinding) {
      //     vel = grindVel;
      //     grinding = false;
      //   }
      // }

      // Console.WriteLine(World.RefreshInterval.ToString());

      

      // if (domCon.IsX1JustUnPressed && Time.Totalf - movePress < 0.2f) {
      //   pos = p00 - (Input.Head.position - pos);
      // }

      // just push off of the air lol better than teleporting
      // not cursor dependent

      // pos.x = (float)Math.Sin(Time.Total * 0.1f) * 0.5f;
      
      pos += vel * Time.Elapsedf;

      float preX = pos.x; pos.x = Math.Clamp(pos.x, -scene.scale / 2, scene.scale / 2); if (pos.x != preX) { vel.x = 0; }
      float preY = pos.y; pos.y = Math.Clamp(pos.y, 0f, scene.scale / 2); if (pos.y != preY) { vel.y = 0; }
      float preZ = pos.z; pos.z = Math.Clamp(pos.z, -scene.scale / 2, scene.scale / 2); if (pos.z != preZ) { vel.z = 0; }

      vel *= 1 - Time.Elapsedf * 0.2f;




      // COLOR CUBE
      // reveal when palm up
      float reveal = lCon.device.pose.Right.y * 1.666f;
      float look = 1 - Math.Clamp((1 - Math.Clamp(Vec3.Dot((lCon.device.pose.position - Input.Head.position).Normalized, Input.Head.Forward), 0f, 1f)) * 5f, 0f, 1f);
      reveal *= look;
      colorCube.size = colorCube.ogSize * Math.Clamp(reveal, 0, 1);
      colorCube.center = lCon.device.pose.position + lCon.device.pose.Right * 0.0666f;
      // move with grip
      if (reveal > colorCube.thicc) { // !leftPlanted
        if (reveal > 1f && lCon.device.trigger > 0.5f) {
          colorCube.cursor -= (lCon.device.pose.position - oldLPos) / colorCube.ogSize * 2;
        } else {
          // clamp 0 - 1
          colorCube.cursor.x = Math.Clamp(colorCube.cursor.x, -1, 1);
          colorCube.cursor.y = Math.Clamp(colorCube.cursor.y, -1, 1);
          colorCube.cursor.z = Math.Clamp(colorCube.cursor.z, -1, 1);
        }
        colorCube.Step();
      }
      oldLPos = lCon.device.pose.position;


      oriel.Step(rGlove.virtualGlove.position, lGlove.virtualGlove.position);
      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      scene.Step();

      net.me.Step(this);
      net.send = true;

    }));
    SK.Shutdown();
  }
}

public class Lerper {
  public float t = 0;
  public float spring = 1;
  public float dampen = 1;
  float vel;

  public void Step(float to = 1, bool bounce = false) {
    float dir = to - t;
    vel += dir * spring * Time.Elapsedf;

    if (Math.Sign(vel) != Math.Sign(dir)) {
      vel *= 1 - (dampen * Time.Elapsedf);
    } else {
      vel *= 1 - (dampen * 0.33f * Time.Elapsedf);
    }

    float newt = t + vel * Time.Elapsedf;
    if (bounce && (newt < 0 || newt > 1)) {
      vel *= -0.5f;
      newt = Math.Clamp(newt, 0, 1);
    }

    t = newt;
  }

  public void Reset() {
    t = vel = 0;
  }
}

public static class PullRequest {
  public static void BoundsDraw(Bounds b, float thickness, Color color) {
    Vec3 c = Vec3.One / 2;
    Vec3 ds = b.dimensions;
    for (int i = 0; i < 4; i++) {
      Quat q = Quat.FromAngles(i * 90, 0, 0);
      Lines.Add(q * (new Vec3(0, 0, 0) - c) * ds, q * (new Vec3(0, 1, 0) - c) * ds, color, color, thickness);
      Lines.Add(q * (new Vec3(0, 1, 0) - c) * ds, q * (new Vec3(1, 1, 0) - c) * ds, color, color, thickness);
      Lines.Add(q * (new Vec3(1, 1, 0) - c) * ds, q * (new Vec3(1, 0, 0) - c) * ds, color, color, thickness);

      // convert to linepoints
    }
  }

  // amplify quaternions (q * q * lerp(q.i, q, %))

  public static Vec3 AngularDisplacement(Quat q) {
    float angle; Vec3 axis;
    ToAngleAxis(q, out angle, out axis);
    return axis * angle;
    // * (float)(Math.PI / 180); // radians -> degrees
    // / Time.Elapsedf; // delta -> velocity
  }

  public static void ToAngleAxis(Quat q, out float angle, out Vec3 axis) {
    q = q.Normalized;
    angle = 2 * (float)Math.Acos(q.w);
    float s = (float)Math.Sqrt(1 - q.w * q.w);
    axis = Vec3.Right;
    // avoid divide by zero
    // + if s is close to zero then direction of axis not important
    if (s > 0.001) {
      axis.x = q.x / s;
      axis.y = q.y / s;
      axis.z = q.z / s;
    }
  }

  static Random r = new Random();
  public static int RandomRange(int min, int max) {
    return r.Next(min, max);
  }

  public static Vec3 Direction(Vec3 to, Vec3 from) {
    return (to - from).Normalized;
  }


  static Mesh meshCube = Default.MeshCube;
  static Material matCube = Default.Material;
  public static void BlockOut(Matrix m, Color color, Material mat = null) {
    if (mat == null) {
      mat = matCube;
      mat.FaceCull = Cull.None;
    }
    meshCube.Draw(mat, m, color);
  }
}

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
Input.HandVisible(Handed.Max, false);
// TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);

Monolith mono = new Monolith();
mono.Run();

public class Monolith {
  public Mic mic;
  // public Controller rCon, lCon;

  public Vec3 rDragStart, lDragStart;
  public float railT;

  Mesh ball = Default.MeshSphere;
  Material mat = Default.Material;
  Mesh cube = Default.MeshCube;

  public void Run() {
    Renderer.SetClip(0.0f, 1000f);
    // Renderer.
    // mic = new Mic();
    Vec3 pos = new Vec3(0, 0, 0);
    Vec3 vel = new Vec3(0, 0, 0);

    Solid floor = new Solid(Vec3.Up * -1.5f, Quat.Identity, SolidType.Immovable);
    float scale = 64f;
    Vec3 floorScale = new Vec3(scale, 0.1f, scale);
    floor.AddBox(floorScale);
    // box on each side
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, -scale / 2));
    floor.AddBox(new Vec3(scale, scale / 2, 0.1f), 1, new Vec3(0, scale / 4, scale / 2));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(-scale / 2, scale / 4, 0));
    floor.AddBox(new Vec3(0.1f, scale / 2, scale), 1, new Vec3(scale / 2, scale / 4, 0));
    // and ceiling
    floor.AddBox(new Vec3(scale, 0.1f, scale), 1, new Vec3(0, scale / 2, 0));
    Material matFloor = new Material(Shader.Default);
    matFloor.SetTexture("diffuse", Tex.FromFile("floor.png"));
    matFloor.SetFloat("tex_scale", 32);


    Cursors cursors = new Cursors(this);

    Oriel oriel = new Oriel();
    oriel.Start(3);
    // Oriel otherOriel = new Oriel();
    // otherOriel.Start(4);

    MonoNet net = new MonoNet(this);
    net.Start();

    ColorCube colorCube = new ColorCube();
    Vec3 oldLPos = Vec3.Zero;

    SpatialCursor rightCursor = new ReachCursor();
    SpatialCursor leftCursor = new ReachCursor();
    bool rightPlanted = false;
    bool leftPlanted = false;

    SpatialCursor cubicFlow = new CubicFlow();

    Tex camTex = new Tex(TexType.Rendertarget);
    camTex.SetSize(600, 400);
    Material camMat = new Material(Shader.Unlit);
    camMat.SetTexture("diffuse", camTex);
    Mesh quad = Default.MeshQuad;

    Vec3 gripPos = Vec3.Zero;
    bool rightGripping = false, leftGripping = false;
    bool gripLeft = false;


    float grindDir = 1f;
    bool grinding = false;
    bool grinded = false;
    Vec3 grindVel = Vec3.Forward;
    Vec3[] grindRail = new Vec3[4];

    while (SK.Step(() => {
      Renderer.CameraRoot = Matrix.T(pos);
      Controller rCon = Input.Controller(Handed.Right); 
      Controller lCon = Input.Controller(Handed.Left); 

      cube.Draw(matFloor, floor.GetPose().ToMatrix(floorScale), Color.White * 0.666f);


      // Shoulders
      Vec3 headPos = Input.Head.position + Input.Head.Forward * -0.15f;
      Vec3 toSub = (lCon.pose.position.X0Z - headPos.X0Z).Normalized;
      Vec3 toDom = (rCon.pose.position.X0Z - headPos.X0Z).Normalized;
      Vec3 middl = (toSub + toDom).Normalized;

      if (Vec3.Dot(middl, Input.Head.Forward) < 0) {
        middl = -middl;
      }

      // Lines.Add(headPos.X0Z, headPos.X0Z + toSub.X0Z, Color.White, 0.005f);
      // Lines.Add(headPos.X0Z, headPos.X0Z + toDom.X0Z, Color.White, 0.005f);
      // Lines.Add(headPos.X0Z, headPos.X0Z + middl.X0Z, Color.White, 0.005f);

      // cube.Draw(mat, Matrix.TRS(headPos, Input.Head.orientation, new Vec3(0.3f, 0.3f, 0.3f)));

      Vec3 rShoulder = headPos + Quat.LookDir(middl) * new Vec3(0.2f, -0.2f, 0);
      Vec3 lShoulder = headPos + Quat.LookDir(middl) * new Vec3(-0.2f, -0.2f, 0);
      // cube.Draw(mat, Matrix.TRS(headPos, Input.Head.orientation, new Vec3(0.25f, 0.3f, 0.3f)), new Color(1,0,0));
      // Lines.Add(headPos + Vec3.Up * -0.2f, rShoulder, new Color(1, 0, 0), 0.01f);
      // Lines.Add(headPos + Vec3.Up * -0.2f, lShoulder, new Color(1, 0, 0), 0.01f);

      // if (domCon.IsX1JustPressed) {
      //   domPlanted = !domPlanted;
      // }
      // if (subCon.IsX1JustPressed) {
      //   subPlanted = !subPlanted;
      // }






      // there is a lot of stuff in this class that needs to move out
      // as we need to make way for an oriel game, for god's sake
      // start with the largest and work our way down
      // is there someway to package these systems in a sensible way
      // there is something about a player here... though that may be to general...


      // don't crash if server isn't up
      // its not tho...








      if (rCon.stick.Magnitude > 0.1f) {
        if (rCon.stick.y < 0f) {
          rightPlanted = true;
        }
      } else {
        rightPlanted = false;
      }

      if (lCon.stick.Magnitude > 0.1f) {
        if (lCon.stick.y < 0f) {
          leftPlanted = true;
        }
      } else {
        leftPlanted = false;
      }

      rightCursor.Step(new Pose[] { rCon.pose, new Pose(rShoulder, Quat.LookDir(middl)) }, 1);
      if (!rightPlanted) {
        rightCursor.Calibrate();
        rightCursor.p0 = rCon.pose.position;
      }
      leftCursor.Step(new Pose[] { lCon.pose, new Pose(lShoulder, Quat.LookDir(middl)) }, 1); // ((Input.Controller(Handed.Left).stick.y + 1) / 2)
      if (!leftPlanted) {
        leftCursor.Calibrate();
        leftCursor.p0 = lCon.pose.position;
      } 
      // cursor.p1 = subCursor.p0; // override *later change all one handed cursors to be dual wielded by default*

      cubicFlow.Step(new Pose[] { new Pose(rightCursor.p0, rCon.aim.orientation), new Pose(leftCursor.p0, lCon.aim.orientation) }, 1);
      if (rCon.stick.y > 0.1f || lCon.stick.y > 0.1f) {
        Bezier.Draw(cubicFlow.p0, cubicFlow.p1, cubicFlow.p2, cubicFlow.p3, Color.White);
        net.me.cursor0 = cubicFlow.p0; net.me.cursor1 = cubicFlow.p1; net.me.cursor2 = cubicFlow.p2; net.me.cursor3 = cubicFlow.p3;
      } else {
        net.me.cursor0 = rightCursor.p0; net.me.cursor1 = rightCursor.p0; net.me.cursor2 = leftCursor.p0; net.me.cursor3 = leftCursor.p0;
      }

      // throw yourself (delta -> vel -> momentum)
      // bring rails back
      // boolean over network to determine if a peers cubic flow should be drawn


      for (int i = 0; i < net.me.blocks.Length; i++) {
        Pose blockPose = net.me.blocks[i].solid.GetPose();
        Bounds bounds = new Bounds(Vec3.Zero, Vec3.One * net.me.blocks[i].size);
        if (net.me.blocks[i].active && (
          bounds.Contains(blockPose.orientation.Inverse * (net.me.cursor0 - blockPose.position)) || 
          bounds.Contains(blockPose.orientation.Inverse * (net.me.cursor3 - blockPose.position))
        )) {
          net.me.blocks[i].color = new Color(0.8f, 1, 1);
        } else {
          net.me.blocks[i].color = new Color(1, 1, 1);
        }
      }


      // FULLSTICK
      // Quat rot = Quat.FromAngles(subCon.stick.y * -90, 0, subCon.stick.x * 90);
      // Vec3 dir = Vec3.Up * (subCon.IsStickClicked ? -1 : 1);
      // Vec3 fullstick = subCon.aim.orientation * rot * dir;
      // pos += fullstick * subCon.trigger * Time.Elapsedf;

      // DRAG DRIFT
      Vec3 rPos = net.me.cursor0;
      // if (domCon.grip) {
      //   // movePress = Time.Totalf;
      //   domDragStart = domPos;
      // }
      // vel += -(domPos - domDragStart) * 24 * domCon.grip;
      // domDragStart = domPos;

      Vec3 lPos = net.me.cursor3;
      // if (subCon.grip) {
      //   // movePress = Time.Totalf;
      //   subDragStart = subPos;
      // }
      // if (subCon.IsX1Pressed) {
      // }
      // vel += -(subPos - subDragStart) * 24 * subCon.grip;
      // subDragStart = subPos;
      if (rCon.grip > 0.5f) {
        if (!rightGripping) {
          gripPos = rPos;
          gripLeft = false;
          rightGripping = true;
        }
      } else {
        rightGripping = false;
      }

      if (lCon.grip > 0.5f) {
        if (!leftGripping) {
          gripPos = lPos;
          gripLeft = true;
          leftGripping = true;
        }
      } else {
        leftGripping = false;
      }

      if (rightGripping || leftGripping) {
        Vec3 gripTo = gripLeft ? lPos : rPos;
        pos = -(gripTo - Input.Head.position) + gripPos - (Input.Head.position - pos);
        vel = Vec3.Zero;
      }

      // CUBIC BEZIER RAIL
      // Vec3[] rail = new Vec3[] {
      //   new Vec3(0, 0, -1),
      //   new Vec3(0, 0, -2),
      //   new Vec3(1, 2, -3),
      //   new Vec3(0, 1, -4),
      // };
      // Bezier.Draw(rail);

      if (rCon.grip > 0.5f) {
        if (!grinded) {
          if (!grinding) {
            int closest = 0;
            float closestDist = float.MaxValue;
            Vec3 closestPoint = Vec3.Zero;
            int closestRail = 0;
            for (int i = 0; i < net.me.cubics.Length; i++) {
              if (net.me.cubics[i].active) {
                Vec3[] rail = new Vec3[] {
                  net.me.cubics[i].p0,
                  net.me.cubics[i].p1,
                  net.me.cubics[i].p2,
                  net.me.cubics[i].p3,
                };
                for (int j = 0; j < rail.Length; j++) {
                  Vec3 point = Bezier.Sample(rail, (float)j / (rail.Length - 1f));
                  float dist = Vec3.Distance(point, rCon.pose.position + vel.Normalized * 0.25f);
                  if (dist < closestDist && dist < 0.5f) {
                    closest = j;
                    closestRail = i;
                    closestDist = dist;
                    closestPoint = point;
                    railT = (float)j / (rail.Length - 1f);
                    grinding = true;
                  }
                }
              }
            }
            if (grinding) {
              grindRail = new Vec3[] {
                net.me.cubics[closestRail].p0,
                net.me.cubics[closestRail].p1,
                net.me.cubics[closestRail].p2,
                net.me.cubics[closestRail].p3,
              };
              // pos = closestPoint - (subCon.pose.position - pos);
              grindVel = vel;
              Vec3 fromPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
              Vec3 toPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f);
              grindDir = Vec3.Dot((fromPos - toPos).Normalized, grindVel) < 0f ? 1 : -1;
            }
          }

          if (grinding) {
            Vec3 grindPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
            Vec3 nextPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f * grindDir);

            // vel += (toPos - fromPos);

            pos = -(rCon.pose.position - Input.Head.position) + grindPos - (Input.Head.position - pos);
            vel = Vec3.Zero;

            railT += Time.Elapsedf * grindVel.Magnitude * grindDir; // scale based on length of rail * calculate and cache on place
            // bool clamped = false;
            // float railTpreClamp = railT;
            // if
            railT = Math.Clamp(railT, 0, 1);

            grindVel = (nextPos - grindPos).Normalized * grindVel.Magnitude;

            if (railT == 1 || railT == 0) {
              vel = grindVel;
              grinding = false;
              grinded = true;
              railT = 0f;
            }


            cube.Draw(mat, Matrix.TS(grindPos, new Vec3(0.1f, 0.1f, 0.1f)));
            // cube.Draw(mat, Matrix.TS(toPos, new Vec3(0.1f, 0.1f, 0.1f) * 0.333f));
            // pos = Vec3.Lerp(pos, Bezier.Sample(net.me.cubics[0].p0, net.me.cubics[0].p1, net.me.cubics[0].p2, net.me.cubics[0].p3, railT) - (subCon.aim.position - pos), Time.Elapsedf * 6f);
            // how to reliably determine and control which direction to go? (velocity)
          }
        }
      } else {
        grinded = false;
        if (grinding) {
          vel = grindVel;
          grinding = false;
        }
      }

      // Console.WriteLine(World.RefreshInterval.ToString());

      

      // if (domCon.IsX1JustUnPressed && Time.Totalf - movePress < 0.2f) {
      //   pos = p00 - (Input.Head.position - pos);
      // }

      // just push off of the air lol better than teleporting
      // not cursor dependent

      // pos.x = (float)Math.Sin(Time.Total * 0.1f) * 0.5f;
      if (!grinding) {
        pos += vel * Time.Elapsedf;
      }

      float preX = pos.x; pos.x = Math.Clamp(pos.x, -scale / 2, scale / 2); if (pos.x != preX) { vel.x = 0; }
      float preY = pos.y; pos.y = Math.Clamp(pos.y, 0f, scale / 2); if (pos.y != preY) { vel.y = 0; }
      float preZ = pos.z; pos.z = Math.Clamp(pos.z, -scale / 2, scale / 2); if (pos.z != preZ) { vel.z = 0; }

      vel *= 1 - Time.Elapsedf * 0.2f;

      // COLOR CUBE
      // reveal when palm up
      float reveal = lCon.pose.Right.y * 1.666f;
      float look = 1 - Math.Clamp((1 - Math.Clamp(Vec3.Dot((lCon.pose.position - Input.Head.position).Normalized, Input.Head.Forward), 0f, 1f)) * 5f, 0f, 1f);
      reveal *= look;
      colorCube.size = colorCube.ogSize * Math.Clamp(reveal, 0, 1);
      colorCube.center = lCon.pose.position + lCon.pose.Right * 0.0666f;
      // move with grip
      if (reveal > colorCube.thicc && !leftPlanted) {
        if (reveal > 1f && lCon.trigger > 0.5f) {
          colorCube.p0 -= (lCon.pose.position - oldLPos) / colorCube.ogSize * 2;
        } else {
          // clamp 0 - 1
          colorCube.p0.x = Math.Clamp(colorCube.p0.x, -1, 1);
          colorCube.p0.y = Math.Clamp(colorCube.p0.y, -1, 1);
          colorCube.p0.z = Math.Clamp(colorCube.p0.z, -1, 1);
        }
        colorCube.Step();
      }
      oldLPos = lCon.pose.position;


      // net.me.cursorA = Vec3.Up * (float)Math.Sin(Time.Total);
      net.me.color = colorCube.color;
      // net.me.cursor0 = cubicFlow.p0; net.me.cursor1 = cubicFlow.p1;
      // net.me.cursor2 = cubicFlow.p2; net.me.cursor3 = cubicFlow.p3;
      
      net.me.headset = Input.Head;
      net.me.mainHand = rCon.aim; net.me.offHand = lCon.aim; 
      for (int i = 0; i < net.peers.Length; i++) {
        Peer peer = net.peers[i];
        if (peer != null) {
          peer.Draw(true);
        }
      }

      net.me.Step(rCon, lCon);




      oriel.Step(net.me.cursor0, net.me.cursor3);

      // otherOriel.bounds.center = Vec3.Forward * -2;
      // otherOriel.Step();

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // cursor.Draw(Matrix.S(0.1f));

      
      // Renderer.RenderTo(camTex, Matrix.TR(Input.Head.position + Vec3.Up * 10, Quat.FromAngles(-90f, 0, 0)), Matrix.Orthographic(2f, 2f, 0.1f, 100f), RenderLayer.All, RenderClear.All);
      // quad.Draw(camMat, Matrix.TR(Input.Head.Forward, Quat.FromAngles(0, 180, 0)));
    })) ;
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

public class Bitting {
  public class DrawKey {
    public int x, y;
    public Key key;
    public DrawKey(int x, int y, Key key) {
      this.x = x;
      this.y = y;
      this.key = key;
    }
  }
  Tex tex = new Tex(TexType.Image, TexFormat.Rgba32);
  Material material = Default.Material;
  Mesh quad = Default.MeshQuad;
  int[,] bitchar = new int[,] {
    {0, 0, 0, 0, 0, 0, 0},
    {0, 0, 0, 0, 0, 0, 0},
    {0, 0, 0, 0, 0, 0, 0},
  };
  DrawKey[] drawKeys = new DrawKey[] {
    new DrawKey(0, 0, Key.F), new DrawKey(0, 2, Key.D), new DrawKey(0, 4, Key.S), new DrawKey(0, 6, Key.A),
    new DrawKey(2, 0, Key.J), new DrawKey(2, 2, Key.K), new DrawKey(2, 4, Key.L), new DrawKey(2, 6, Key.Semicolon),
  }; DrawKey lastKey = null;

  public void Start() {
    tex.SetSize(128, 128);
    tex.SampleMode = TexSample.Point;
    material.SetTexture("diffuse", tex);
  }

  public void Step() {
    // clear
    if (Input.Key(Key.Space).IsJustActive()) {
      for (int i = 0; i < bitchar.GetLength(0); i++) {
        for (int j = 0; j < bitchar.GetLength(1); j++) {
          bitchar[i, j] = 0;
        }
      }
      lastKey = null;
    }

    for (int i = 0; i < drawKeys.Length; i++) {
      DrawKey drawKey = drawKeys[i];
      if (Input.Key(drawKey.key).IsJustActive()) {
        bitchar[drawKey.x, drawKey.y] = 1;
        if (lastKey != null) {
          // draw line between last and current
          int x1 = lastKey.x;
          int y1 = lastKey.y;
          int x2 = drawKey.x;
          int y2 = drawKey.y;
          int dx = Math.Abs(x2 - x1);
          int dy = Math.Abs(y2 - y1);
          int sx = x1 < x2 ? 1 : -1;
          int sy = y1 < y2 ? 1 : -1;
          int err = dx - dy;
          while (true) {
            bitchar[x1, y1] = 1;
            if (x1 == x2 && y1 == y2) break;
            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x1 += sx; }
            if (e2 < dx) { err += dx; y1 += sy; }
          }
        }
        lastKey = drawKey;
        break;
      }
    }

    Color32[] pixels = new Color32[tex.Width * tex.Height];
    tex.GetColors(ref pixels);
    for (int i = 0; i < pixels.Length; i++) {
      pixels[i] = new Color32(0, 0, 0, 0);
      int x = i % tex.Width;
      int y = i / tex.Width;
      if (x < 3 && y < 7 && bitchar[x, y] == 1) {
        pixels[i] = new Color32(0, 255, 255, 0);
      }
    }
    tex.SetColors(tex.Width, tex.Height, pixels);

    quad.Draw(material, Matrix.TR(Vec3.Zero, Quat.FromAngles(0, 180, 0)));
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
}

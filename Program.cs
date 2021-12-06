using StereoKit;
using System;
using System.Runtime.InteropServices;

class Program {
  static void Main(string[] args) {
    SKSettings settings = new SKSettings {
      appName = "oriels",
      assetsFolder = "Assets",
    };
    if (!SK.Initialize(settings))
      Environment.Exit(1);

    // TextStyle style = Text.MakeStyle(Font.FromFile("DMMono-Regular.ttf"), 0.1f, Color.White);
    Mono mono = new Mono();
    mono.Run();
  }
}

public class Mono {
  public Mic mic;
  public Controller domCon, subCon; public bool lefty;

  public Vec3 domDragStart, subDragStart;
  public float railT;

  Mesh ball = Default.MeshSphere;
  Material mat = Default.Material;
  Mesh cube = Default.MeshCube;

  public void Run() {
    Renderer.SetClip(0f, 200f);
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
    

    Cursors cursors = new Cursors(this);

    Oriel oriel = new Oriel();
    oriel.Start(3);
    

  // Oriel otherOriel = new Oriel();
  // otherOriel.Start(4);

  MonoNet net = new MonoNet(this);
    net.Start();

    ColorCube colorCube = new ColorCube();
    Vec3 oldSubPos = Vec3.Zero;

    SpatialCursor cursor = new ReachCursor();
    SpatialCursor subCursor = new ReachCursor();
    bool domPlanted = false;
    bool subPlanted = false;

    SpatialCursor cubicFlow = new CubicFlow();

    Tex camTex = new Tex(TexType.Rendertarget);
    camTex.SetSize(600, 400);
    Material camMat = new Material(Shader.Unlit);
    camMat.SetTexture("diffuse", camTex);
    Mesh quad = Default.MeshQuad;

    bool draggingOriel = false;
    Vec3 orielOffset = Vec3.Zero;


    Vec3 gripPos = Vec3.Zero;
    bool domGripping = false, subGripping = false;
    bool gripLeft = false;


    float grindDir = 1f;
    bool grinding = false;
    bool grinded = false;
    Vec3 grindVel = Vec3.Forward;
    Vec3[] grindRail = new Vec3[4];


    Input.HandSolid(Handed.Right, false);
    Input.HandSolid(Handed.Left, false);
    Input.HandVisible(Handed.Right, false);
    Input.HandVisible(Handed.Left, false);

    while (SK.Step(() => {
      Renderer.CameraRoot = Matrix.T(pos);

      cube.Draw(mat, floor.GetPose().ToMatrix(floorScale), Color.White * 0.666f);

      if (lefty) { domCon = Input.Controller(Handed.Left); subCon = Input.Controller(Handed.Right); }
      else { domCon = Input.Controller(Handed.Right); subCon = Input.Controller(Handed.Left); }
      // if (subCon.IsX2JustPressed) { lefty = !lefty; }

      // ball.Draw(ballMat, Matrix.TS(pos, 0.1f));

      // SpatialCursor cursor = cursors.Step(domCon.aim, subCon.aim);

      // Shoulders
      Vec3 headPos = Input.Head.position + Input.Head.Forward * -0.15f;
      Vec3 toSub = (subCon.pose.position.X0Z - headPos.X0Z).Normalized;
      Vec3 toDom = (domCon.pose.position.X0Z - headPos.X0Z).Normalized;
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

      if (domCon.IsX1JustPressed) {
        domPlanted = !domPlanted;
      }
      if (subCon.IsX1JustPressed) {
        subPlanted = !subPlanted;
      }


      cursor.Step(new Pose[] { domCon.pose, new Pose(rShoulder, Quat.LookDir(middl)) }, 1);
      if (!domPlanted) {
        cursor.Calibrate();
        cursor.p0 = domCon.pose.position;
      }
      subCursor.Step(new Pose[] { subCon.pose, new Pose(lShoulder, Quat.LookDir(middl)) }, 1); // ((Input.Controller(Handed.Left).stick.y + 1) / 2)
      if (!subPlanted) {
        subCursor.Calibrate();
        subCursor.p0 = subCon.pose.position;
      } 
      // cursor.p1 = subCursor.p0; // override *later change all one handed cursors to be dual wielded by default*

      cubicFlow.Step(new Pose[] { new Pose(cursor.p0, domCon.aim.orientation), new Pose(subCursor.p0, subCon.aim.orientation) }, 1);
      if (domCon.stick.Magnitude > 0.1f || subCon.stick.Magnitude > 0.1f) {
        Bezier.Draw(cubicFlow.p0, cubicFlow.p1, cubicFlow.p2, cubicFlow.p3, Color.White);
        net.me.cursor0 = cubicFlow.p0; net.me.cursor1 = cubicFlow.p1; net.me.cursor2 = cubicFlow.p2; net.me.cursor3 = cubicFlow.p3;
      } else {
        net.me.cursor0 = cursor.p0; net.me.cursor1 = cursor.p0; net.me.cursor2 = subCursor.p0; net.me.cursor3 = subCursor.p0;
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
      Vec3 domPos = net.me.cursor0;
      // if (domCon.grip) {
      //   // movePress = Time.Totalf;
      //   domDragStart = domPos;
      // }
      // vel += -(domPos - domDragStart) * 24 * domCon.grip;
      // domDragStart = domPos;

      Vec3 subPos = net.me.cursor3;
      // if (subCon.grip) {
      //   // movePress = Time.Totalf;
      //   subDragStart = subPos;
      // }
      // if (subCon.IsX1Pressed) {
      // }
      // vel += -(subPos - subDragStart) * 24 * subCon.grip;
      // subDragStart = subPos;
      if (domCon.grip > 0.5f) {
        if (!domGripping) {
          gripPos = domPos;
          gripLeft = false;
          domGripping = true;
        }
      } else {
        domGripping = false;
      }

      if (subCon.grip > 0.5f) {
        if (!subGripping) {
          gripPos = subPos;
          gripLeft = true;
          subGripping = true;
        }
      } else {
        subGripping = false;
      }

      if (domGripping || subGripping) {
        Vec3 gripTo = gripLeft ? subPos : domPos;
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

      if (domCon.grip > 0.5f) {
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
                  float dist = Vec3.Distance(point, domCon.pose.position + vel.Normalized * 0.25f);
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

            pos = -(domCon.pose.position - Input.Head.position) + grindPos - (Input.Head.position - pos);
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
      float reveal = subCon.pose.Right.y * 1.666f;
      float look = 1 - Math.Clamp((1 - Math.Clamp(Vec3.Dot((subCon.pose.position - Input.Head.position).Normalized, Input.Head.Forward), 0f, 1f)) * 6f, 0f, 1f);
      reveal *= look;
      colorCube.size = colorCube.ogSize * Math.Clamp(reveal, 0, 1);
      colorCube.center = subCon.pose.position + subCon.pose.Right * 0.0666f;
      // move with grip
      if (reveal > colorCube.thicc && !subPlanted) {
        if (reveal > 1f && subCon.trigger > 0.5f) {
          colorCube.p0 -= (subCon.pose.position - oldSubPos) / colorCube.ogSize * 2;
        } else {
          // clamp 0 - 1
          colorCube.p0.x = Math.Clamp(colorCube.p0.x, -1, 1);
          colorCube.p0.y = Math.Clamp(colorCube.p0.y, -1, 1);
          colorCube.p0.z = Math.Clamp(colorCube.p0.z, -1, 1);
        }
        colorCube.Step();
      }
      oldSubPos = subCon.pose.position;


      // net.me.cursorA = Vec3.Up * (float)Math.Sin(Time.Total);
      net.me.color = colorCube.color;
      // net.me.cursor0 = cubicFlow.p0; net.me.cursor1 = cubicFlow.p1;
      // net.me.cursor2 = cubicFlow.p2; net.me.cursor3 = cubicFlow.p3;
      
      net.me.headset = Input.Head;
      net.me.mainHand = domCon.aim; net.me.offHand = subCon.aim; 
      for (int i = 0; i < net.peers.Length; i++) {
        MonoNet.Peer peer = net.peers[i];
        if (peer != null) {
          peer.Draw(true);
        }
      }

      net.me.Step(domCon, subCon);




      if (domCon.trigger > 0.5f && subCon.trigger > 0.5f) {
        if (!draggingOriel) {
          if (oriel.bounds.Contains(net.me.cursor0) || oriel.bounds.Contains(net.me.cursor3)) {
            draggingOriel = true;
          }
        } else {

          oriel.bounds.center = Vec3.Lerp(net.me.cursor0, net.me.cursor3, 0.5f);
          //
          float distX = Math.Abs(net.me.cursor0.x - net.me.cursor3.x);
          float distY = Math.Abs(net.me.cursor0.y - net.me.cursor3.y);
          float distZ = Math.Abs(net.me.cursor0.z - net.me.cursor3.z);
          oriel.bounds.dimensions = new Vec3(distX, distY, distZ);
        }
      } else {
        draggingOriel = false;
      }

      oriel.Step();

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

public class Mic {
  public float[] bufferRaw = new float[0];
  public int bufferRawSize = 0;

  public int comp = 8;
  public float[] buffer = new float[0];
  public int bufferSize = 0;

  FilterButterworth filter;
  public void Step() {
    if (Microphone.IsRecording) {
      // Ensure our buffer of samples is large enough to contain all the
      // data the mic has ready for us this frame
      if (Microphone.Sound.UnreadSamples > bufferRaw.Length) {
        bufferRaw = new float[Microphone.Sound.UnreadSamples];
        buffer = new float[Microphone.Sound.UnreadSamples / comp];
      }

      // Read data from the microphone stream into our buffer, and track 
      // how much was actually read. Since the mic data collection runs in
      // a separate thread, this will often be a little inconsistent. Some
      // frames will have nothing ready, and others may have a lot!
      bufferRawSize = Microphone.Sound.ReadSamples(ref bufferRaw);
      bufferSize = bufferRawSize / comp;

      if (bufferSize > 0) {
        // LowPassFilter lowpass = new LowPassFilter(48000 / comp / 2, 2, 48000);
        for (int i = 0; i < bufferRawSize; i++) {
          // bufferRaw[i] = (float)lowpass.compute(bufferRaw[i]);
          filter.Update(bufferRaw[i]);
          bufferRaw[i] = filter.Value;
        }
        // voice.WriteSamples(bufferRaw);
      
        buffer[0] = bufferRaw[0];
        for (int i = 1; i < bufferSize; i++) {
          buffer[i] = bufferRaw[i * comp - 1];
        }

        // upsample
        float[] upsampled = new float[bufferSize * comp];
        for (int i = 0; i < bufferSize - 1; i++) {
          upsampled[Math.Max(i * comp - 1, 0)] = buffer[i];
          for (int j = 1; j < comp; j++) {
            upsampled[i * comp - 1 + j] = SKMath.Lerp(buffer[i], buffer[i + 1], (float)j / (float)comp);
          }
        }
        voice.WriteSamples(upsampled);
      }
    } else {
      Microphone.Start();
      voice = Sound.CreateStream(0.5f);
      voiceInst = voice.Play(Vec3.Zero, 0.5f);
      filter = new FilterButterworth(48000 / comp / 2, 48000, FilterButterworth.PassType.Lowpass, (float)Math.Sqrt(2));
    }
  }
  public Sound voice;
  public SoundInst voiceInst; // update position

  public class FilterButterworth {
    /// <summary>
    /// rez amount, from sqrt(2) to ~ 0.1
    /// </summary>
    private readonly float resonance;

    private readonly float frequency;
    private readonly int sampleRate;
    private readonly PassType passType;

    private readonly float c, a1, a2, a3, b1, b2;

    /// <summary>
    /// Array of input values, latest are in front
    /// </summary>
    private float[] inputHistory = new float[2];

    /// <summary>
    /// Array of output values, latest are in front
    /// </summary>
    private float[] outputHistory = new float[3];

    public FilterButterworth(float frequency, int sampleRate, PassType passType, float resonance) {
      this.resonance = resonance;
      this.frequency = frequency;
      this.sampleRate = sampleRate;
      this.passType = passType;

      switch (passType) {
        case PassType.Lowpass:
          c = 1.0f / (float)Math.Tan(Math.PI * frequency / sampleRate);
          a1 = 1.0f / (1.0f + resonance * c + c * c);
          a2 = 2f * a1;
          a3 = a1;
          b1 = 2.0f * (1.0f - c * c) * a1;
          b2 = (1.0f - resonance * c + c * c) * a1;
          break;
        case PassType.Highpass:
          c = (float)Math.Tan(Math.PI * frequency / sampleRate);
          a1 = 1.0f / (1.0f + resonance * c + c * c);
          a2 = -2f * a1;
          a3 = a1;
          b1 = 2.0f * (c * c - 1.0f) * a1;
          b2 = (1.0f - resonance * c + c * c) * a1;
          break;
      }
    }

    public enum PassType {
      Highpass,
      Lowpass,
    }

    public void Update(float newInput) {
      float newOutput = a1 * newInput + a2 * this.inputHistory[0] + a3 * this.inputHistory[1] - b1 * this.outputHistory[0] - b2 * this.outputHistory[1];

      this.inputHistory[1] = this.inputHistory[0];
      this.inputHistory[0] = newInput;

      this.outputHistory[2] = this.outputHistory[1];
      this.outputHistory[1] = this.outputHistory[0];
      this.outputHistory[0] = newOutput;
    }

    public float Value {
      get { return this.outputHistory[0]; }
    }
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

[StructLayout(LayoutKind.Sequential)]
struct BufferData {
  public Vec3 windDirection;
  // public Vec3[] tri;
  public float windStrength;
}
public class Oriel {
  public Bounds bounds;
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Material crown = new Material(Shader.FromFile("crown.hlsl"));
  Mesh mesh = Default.MeshCube;
  Mesh quad = Default.MeshQuad;
  Vec3 _dimensions;

  MaterialBuffer<BufferData> buffer;

  public void Start(int bufferIndex) {
    bounds = new Bounds(Vec3.Forward * 2, new Vec3(1f, 0.5f, 0.5f));
    _dimensions = bounds.dimensions;
    buffer = new MaterialBuffer<BufferData>(bufferIndex);
  }

  BufferData data = new BufferData();
  public void Step() {
    data.windDirection = new Vec3(
      (float)Math.Sin(Time.Total * 1) / 6,
      (float)Math.Cos(Time.Total * 2) / 6,
      (float)Math.Sin(Time.Total * 3) / 6
    );
    // data.a = new Color(1.0f, 0.5f, 0.5f);
    // data.b = new Color(0.5f, 1.0f, 0.5f);
    // data.c = new Color(0.5f, 0.5f, 1.0f);
    // data.tri = new Vec3[] {
    //   new Vec3(0, 0, 0),
    //   new Vec3(0, 0, 1),
    //   new Vec3(1, 0, 0),
    // };
    
    data.windStrength = (1 + (float)Math.Sin(Time.Total)) / 2;
    buffer.Set(data);
    

    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));

    
    mat.FaceCull = Cull.None;
    mat.SetVector("_dimensions", bounds.dimensions);
    mat.SetVector("_center", bounds.center);
    // mat.Wireframe = true;

    Matrix m = Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions);
    Pose head = Input.Head;
    // Vec3 quadPos = head.position + head.Forward * 0.0021f;
    // if (bounds.Contains(head.position, head.position, 0.036f)) {
    //   mat.FaceCull = Cull.Front;
    //   m = Matrix.TRS(head.position, head.orientation, new Vec3(1.0f, 0.5f, 0.0088f * 2));
    //   Renderer.
    // }
    mesh.Draw(mat, m);

    // if (bounds.Contains(head.position, quadPos)) {
    //   quad.Draw(mat, Matrix.TRS(quadPos, Quat.LookAt(quadPos, head.position), Vec3.One * 0.5f));
    // }

    // instead of a quad, just slap the same mesh to the head


    // crown.SetVector("_center", bounds.center);
    crown.SetFloat("_height", bounds.dimensions.y);
    crown.SetFloat("_ypos", bounds.center.y);
    crown.FaceCull = Cull.Front;
    crown.Transparency = Transparency.Add;
    crown.DepthTest = DepthTest.Always;
    // crown.QueueOffset = 0;
    // crown.DepthWrite = false;
    mesh.Draw(crown, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
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

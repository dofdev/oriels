using StereoKit;
using System;

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

  public void Run() {
    // mic = new Mic();
    Vec3 pos = new Vec3(0, 0, 0);

    Cursors cursors = new Cursors(this);

    MonoNet net = new MonoNet(this);
    net.Start();

    Oriel oriel = new Oriel();
    oriel.Start();

    // ColorCube cube = new ColorCube();
    // OrbitalView.strength = 4;
    // OrbitalView.distance = 0.4f;
    // cube.thickness = 0.01f;

    while (SK.Step(() => {
      if (lefty) { domCon = Input.Controller(Handed.Left); subCon = Input.Controller(Handed.Right); } 
      else { domCon = Input.Controller(Handed.Right); subCon = Input.Controller(Handed.Left); }
      if (subCon.IsX2JustPressed) { lefty = !lefty; }

      SpatialCursor cursor = cursors.Step(domCon.aim, subCon.aim);

      if (domCon.IsX1JustPressed) {
        pos = cursor.p0;
      }
      // pos.x = (float)Math.Sin(Time.Total * 0.1f) * 0.5f;
      Renderer.CameraRoot = Matrix.T(pos);

      // cursor.Step(lHand.aim, rHand.aim); cursor.DrawSelf();
      // net.me.cursorA = Vec3.Up * (float)Math.Sin(Time.Total);
      net.me.cursorA = cursor.p0; net.me.cursorB = cursor.p1;
      net.me.cursorC = cursor.p2; net.me.cursorD = cursor.p3;
      net.me.headset = Input.Head;
      net.me.mainHand = domCon.aim; net.me.offHand = subCon.aim; 
      for (int i = 0; i < net.peers.Length; i++) {
        MonoNet.Peer peer = net.peers[i];
        if (peer != null) {
          net.Cubee(Matrix.TRS(peer.cursorA, Quat.Identity, Vec3.One * 0.05f));
          net.Cubee(peer.headset.ToMatrix(Vec3.One * 0.3f));
          net.Cubee(peer.offHand.ToMatrix(Vec3.One * 0.1f));
          net.Cubee(peer.mainHand.ToMatrix(Vec3.One * 0.1f));
          // cubicFlow.Draw(peer.cursorA, peer.cursorB, peer.cursorC, peer.cursorD);
        }
      }

      oriel.Step();

      // Matrix orbitMatrix = OrbitalView.transform;
      // cube.Step(Matrix.S(Vec3.One * 0.2f) * orbitMatrix);
      // Default.MaterialHand["color"] = cube.color;

      // cursor.Draw(Matrix.S(0.1f));
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

public class Oriel {
  public Bounds bounds;
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Mesh mesh = Default.MeshCube;
  Mesh quad = Default.MeshQuad;
  Vec3 _dimensions;
  public void Start() {
    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    _dimensions = bounds.dimensions;
  }

  public void Step() {
    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));

    mat.Transparency = Transparency.Blend;
    mat.SetFloat("_height", bounds.dimensions.y);
    mat.SetFloat("_ypos", bounds.center.y);
    // mat.FaceCull = Cull.None;
    mesh.Draw(mat, Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions));
    Pose head = Input.Head;
    Vec3 quadPos = head.position + head.Forward * 0.04f;
    if (bounds.Contains(head.position, quadPos)) {
      quad.Draw(mat, Matrix.TRS(quadPos, Quat.LookAt(quadPos, head.position), Vec3.One * 0.5f));
    }
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
}

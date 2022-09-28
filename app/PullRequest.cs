namespace Oriels;
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

  public static Vec3 Slerp(Vec3 a, Vec3 b, float t) {
    float dot = Vec3.Dot(a, b);
    dot = Clamp(dot, -1f, 1f);

    float theta = MathF.Acos(dot) * t;
    Vec3 relativeVec = b - a * dot;
    relativeVec.Normalize();

    return (a * MathF.Cos(theta)) + (relativeVec * MathF.Sin(theta));
  }

  // amplify quaternions (q * q * lerp(q.i, q, %))

  public static Vec3 AngularDisplacement(Quat q) {
    float angle; Vec3 axis;
    ToAxisAngle(q, out axis, out angle);
    return axis * angle;
    // * (float)(Math.PI / 180); // radians -> degrees
    // / Time.Elapsedf; // delta -> velocity
  }

  public static void ToAxisAngle(this Quat q, out Vec3 axis, out float angle) {
    q = q.Normalized; // q.Normalize(); ?
    angle = 2 * MathF.Acos(q.w);
    float s = MathF.Sqrt(1 - q.w * q.w);
    // float s = 2 * MathF.Asin(angle * 0.5f);
    axis = Vec3.Right;
    // avoid divide by zero
    // + if s is close to zero then direction of axis not important
    if (s > 0.001) {
      axis.x = q.x / s;
      axis.y = q.y / s;
      axis.z = q.z / s;
    }
  }

  public static void LookDirection(this ref Quat q, Vec3 dir) {
    Vec3 up = Vec3.Up;
    
    // using AxisAngle
    Vec3 axis = Vec3.Cross(up, dir);
    float angle = MathF.Atan2(Vec3.Dot(up, dir), axis.Length);
    q = FromAxisAngle(axis.Normalized, angle);
  }

  public static Quat FromAxisAngle(Vec3 axis, float angle) {
    float halfAngle = angle * 0.5f;
    float sin = (float)Math.Sin(halfAngle);
    float cos = (float)Math.Cos(halfAngle);
    return new Quat(axis.x * sin, axis.y * sin, axis.z * sin, cos).Normalized;
  }

  static Random r = new Random();
  public static int RandomRange(int min, int max) {
    return r.Next(min, max);
  }

  public static Vec3 Direction(Vec3 to, Vec3 from) {
    return (to - from).Normalized;
  }

  // swizzle
  public static Vec3 JustX(this Vec3 v) {
    return new Vec3(v.x, 0, 0);
  }
  public static Vec3 JustY(this Vec3 v) {
    return new Vec3(0, v.y, 0);
  }
  public static Vec3 JustZ(this Vec3 v) {
    return new Vec3(0, 0, v.z);
  }

  // public static Vec3 Down {
  //   get { return new Vec3(0, -1, 0); }
  // }

  public static Vec3 Abs(this Vec3 v) {
    return new Vec3(
      MathF.Abs(v.x),
      MathF.Abs(v.y),
      MathF.Abs(v.z)
    );
  }

  public static Vec3 Sign(this Vec3 v) {
    return new Vec3(
      MathF.Sign(v.x),
      MathF.Sign(v.y),
      MathF.Sign(v.z)
    );
  }

  /// <summary>
  /// a(1,1,1) b(2,2,2) t(0,1,0.5) return(1,2,1.5)
  /// </summary>
  public static Vec3 Splice(this Vec3 a, Vec3 b, Vec3 t, bool nor = false) {
    return new Vec3(
      Lerp(a.x, b.x, nor ? MathF.Sign(t.x) : t.x),
      Lerp(a.y, b.y, nor ? MathF.Sign(t.y) : t.y),
      Lerp(a.z, b.z, nor ? MathF.Sign(t.z) : t.z)
    );
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

  public static Mesh GetMesh(this Model model, string name) {
    for (int i = 0; i < model.Nodes.Count; i++) {
      if (model.Nodes[i].Name == name) {
        return model.Nodes[i].Mesh;
      }
    }
    Console.WriteLine("Mesh not found: " + name);
    return Mesh.Quad;
  }

  public static void SetMat(this Material mat, int offset, Cull cull, bool depthWrite) {
    mat.QueueOffset = offset;
    mat.FaceCull = cull;
    mat.DepthWrite = depthWrite;
  }

  public static Vec3 RandomInCube(Vec3 center, float size) {
    Random r = new Random();
    return center + new Vec3(
      (r.NextSingle() - 0.5f) * size,
      (r.NextSingle() - 0.5f) * size,
      (r.NextSingle() - 0.5f) * size
    );
  }

  public static float Lerp(float a, float b, float t) {
    return a + (b - a) * t;
  }

  // public static Vec3 Slerp(Vec3 a, Vec3 b, float t) {
  //   // spherical linear interpolation
  //   float dot = Vec3.Dot(a, b);
  //   if (dot > 0.9995f) {
  //     return Vec3.Lerp(a, b, t);
  //   }
  //   float theta = (float)Math.Acos(dot);
  //   float sinTheta = (float)Math.Sin(theta);
  //   return Vec3.Lerp(a * (float)Math.Sin(theta - theta * t) / sinTheta, b * (float)Math.Sin(theta * t) / sinTheta, t);
  // }

  [Serializable]
  public class Noise {
    const uint CAP = 4294967295;
    const uint BIT_NOISE1 = 0xB5297A4D;
    const uint BIT_NOISE2 = 0x68E31DA4;
    const uint BIT_NOISE3 = 0x1B56C4E9;

    public uint seed;

    public Noise(uint seed) {
      this.seed = seed;
    }

    int position;
    public float uvalue {
      get {
        float v = RNG(position, seed) / (float)CAP;
        position++;
        return v;
      }
    }

    public float value { // not ideal *loss of precision*
      get {
        return uvalue * 2 - 1;
      }
    }

    public float D1(int pos) {
      return RNG(pos, seed) / (float)CAP;
    }

    public float D2(int x, int y) {
      // large prime number with non-boring bits
      const int PRIME = 198491317;
      return RNG(x + (PRIME * y), seed) / (float)CAP;
    }

    public float D3(int x, int y, int z) {
      // large prime number with non-boring bits
      const int PRIME1 = 198491317;
      const int PRIME2 = 6542989;
      return RNG(x + (PRIME1 * y) + (PRIME2 * z), seed) / (float)CAP;
    }

    public uint RNG(int pos, uint seed) {
      uint mangled = (uint)pos;
      mangled *= BIT_NOISE1;
      mangled += seed;
      mangled ^= mangled >> 8;
      mangled += BIT_NOISE2;
      mangled ^= mangled << 8;
      mangled *= BIT_NOISE3;
      mangled ^= mangled >> 8;
      return mangled;
    }
  }

  public static float Clamp01(float v) {
    return MathF.Max(0, MathF.Min(1, v));
  }

  public static float Clamp(float v, float min, float max) {
    return MathF.Max(min, MathF.Min(max, v));
  }

  [Serializable]
  public class PID {
    public float p, i;
    float integral = 0f;
    float value = 0f;
    // float scalar = 1f;

    public PID(float p = 1, float i = 0.1f) {
      this.p = p;
      this.i = i;
    }

    public float Update(float target) {
      float error = value - target;
      integral += error;
      float delta = ((p * error) + (i * integral));
      return value -= delta * Time.Elapsedf;
    }
  }

  [Serializable]
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
}

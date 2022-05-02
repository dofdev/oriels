using System;
using StereoKit;

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

  static Pose _pose = new Pose();
  public static Pose WorldPose(this Pose pose, float scale = 1) {
    return pose;
  }

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
    public float value {
      get {
        float v = RNG(position, seed) / (float)CAP;
        position++;
        return v;
      }
    }

    public float D1(int position) {
      return RNG(position, seed) / (float)CAP;
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

    public uint RNG(int position, uint seed) {
      uint mangled = (uint)position;
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

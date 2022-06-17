using System;
using System.Runtime.InteropServices;
using StereoKit;

// [StructLayout(LayoutKind.Sequential)]
// struct BufferData {
//   public Vec3 position;
//   public float time;
// }

public class Oriel {
  Monolith mono;

  public Bounds bounds;
  Material mat = new Material(Shader.FromFile("oriel.hlsl"));
  Mesh mesh = Default.MeshCube;
  public float crown = 0.0666f;

  bool scalingOriel = false;
  bool draggingOriel = false;
  Vec3 orielOffset = Vec3.Zero;

  // MaterialBuffer<BufferData> buffer;

  public void Start(Monolith mono, int bufferIndex) {
    this.mono = mono;

    bounds = new Bounds(Vec3.Zero, new Vec3(1f, 0.5f, 0.5f));
    bounds.center = new Vec3(0.5f, -0.25f, -1f);

    // buffer = new MaterialBuffer<BufferData>(bufferIndex);
  }

  // BufferData data = new BufferData();
  public void Step() {
    Vec3 rGlovePos = mono.rGlove.virtualGlove.position;
    Vec3 lGlovePos = mono.lGlove.virtualGlove.position;
    if (mono.rCon.triggerBtn.held && mono.lCon.triggerBtn.held) {
      if (!scalingOriel) {
        if (bounds.Contains(rGlovePos) || bounds.Contains(lGlovePos)) {
          scalingOriel = true;
        }
      } else {
        bounds.center = Vec3.Lerp(rGlovePos, lGlovePos, 0.5f);
        float distX = Math.Abs(rGlovePos.x - lGlovePos.x);
        float distY = Math.Abs(rGlovePos.y - lGlovePos.y);
        float distZ = Math.Abs(rGlovePos.z - lGlovePos.z);
        bounds.dimensions = new Vec3(distX, distY, distZ);
      }
    } else {
      scalingOriel = false;
    }

    if (!scalingOriel && mono.rCon.triggerBtn.held) {
      if (!draggingOriel) {
        bool inCrown = rGlovePos.y > (bounds.center.y + bounds.dimensions.y / 2.0) - crown;
        if (bounds.Contains(rGlovePos) && inCrown) {
          orielOffset = rGlovePos - bounds.center;
          draggingOriel = true;
        }
      } else {
        bounds.center = rGlovePos - orielOffset;
      }
    } else {
      draggingOriel = false;
    }


    // data.position = rGlovePos;
    // data.time = (float)Time.Total;
    // buffer.Set(data);



    Matrix matrix = Matrix.TR(
      bounds.center,
      Quat.FromAngles(0, Time.Totalf * 12, 0).Normalized * Quat.FromAngles(90, 0, 0) * Quat.FromAngles(0, 90, 0)
    ).Inverse;
    mat["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrix);

    // circle around center
    // bounds.center = Quat.FromAngles(0, 0, Time.Totalf * 60) * Vec3.Up * 0.3f;
    // bounds.dimensions = _dimensions * (1f + (MathF.Sin(Time.Totalf * 3) * 0.3f));


    mat.FaceCull = Cull.Front;
    mat.SetFloat("_distmax", 1000);
    mat.SetVector("_dimensions", bounds.dimensions);
    mat.SetVector("_center", bounds.center);
    mat.SetFloat("_crown", crown);

    // Pose pose = mono.rGlove.virtualGlove;
    // pose.position -= bounds.center;
    // mat.SetMatrix("_matrix", pose.ToMatrix());
    mat["_rGlove"] = (Matrix)System.Numerics.Matrix4x4.Transpose(mono.rGlove.virtualGlove.ToMatrix().Inverse);
    mat["_lGlove"] = (Matrix)System.Numerics.Matrix4x4.Transpose(mono.lGlove.virtualGlove.ToMatrix().Inverse);

    Matrix m = Matrix.TRS(bounds.center, Quat.Identity, bounds.dimensions);
    Pose head = Input.Head;
    
    mesh.Draw(mat, m);
  }
}
namespace Oriels;

public class Oriel {
  Model model = Model.FromFile("oriel.glb");
  Mesh meshCube, meshSphere;

  Material matClear = new Material(Shader.Default);
  
  public Material matOriel = new Material(Shader.FromFile("shaders/oriel.hlsl"));
  Material matFrame = new Material(Shader.FromFile("shaders/frame.hlsl"));
  Material matPanes = new Material(Shader.FromFile("shaders/panes.hlsl"));

  public Matrix matrix, matrixInv;
  public Bounds bounds;
  public Quat ori = Quat.Identity;

  // inner matrix
  public bool scaleHeight = true;
  public float scale = 0.1f;
  public float multiplier = 1f;

  public Oriel() {
    // meshCube = model.GetMesh("oriel");
    meshCube = Mesh.Cube;
    meshSphere = Mesh.Sphere;
    matClear.Transparency = Transparency.Add;

    matFrame.SetMat(102, Cull.Back, true);
    matFrame.Transparency = Transparency.Blend;
    matFrame.SetTexture("dither", Tex.FromFile("dither.png"));
    matPanes.SetMat(100, Cull.Front, false);
    matOriel.SetMat(101, Cull.None, true);

    bounds = new Bounds(
      new Vec3(-1.0f, -0.5f, 0.5f),
      new Vec3(0.8f, 0.5f, 0.5f)
    );
    matrix = Matrix.TR(bounds.center, ori);
    matrixInv = matrix.Inverse;


    cursor = bounds.center;
    cornerRadius = cursorRadius / 2;
  }

  public Vec3 cursor = Vec3.Zero;
  public float cursorRadius = 0.1f;
  public float cornerRadius;
  
  Vec3 detect = Vec3.Zero;
  int detectCount = 0;
  public Vec3 LocalAnchor { get { return detect * bounds.dimensions / 2; } }
  public Vec3 Anchor { get { return matrix.Transform(LocalAnchor); } }

  Quat qOffset = Quat.Identity;
  Vec3 vOffset = Vec3.Zero;
  Vec3 lOffset = Vec3.Zero;
  Matrix mOffset = Matrix.Identity;
  public void Frame() {
    // input
    Rig rig = Mono.inst.rig;
    Glove rGlove = Mono.inst.rGlove;
    // cursor = rGlove.virtualGlove.position;
    Quat rGloveRot = rGlove.virtualGlove.orientation;
    // Vec3 lGlovePos = rig.lGlove.virtualGlove.position;
    bool grab = !Input.Key(Key.Space).IsActive(); // !rig.rCon.triggerBtn.held

    if (!Input.Key(Key.Shift).IsActive()) {
      Vec3 input = new Vec3(
        (Input.Key(Key.S).IsActive() ? 1 : 0) - (Input.Key(Key.A).IsActive() ? 1 : 0),
        (Input.Key(Key.F).IsActive() ? 1 : 0) - (Input.Key(Key.Q).IsActive() ? 1 : 0),
        (Input.Key(Key.R).IsActive() ? 1 : 0) - (Input.Key(Key.W).IsActive() ? 1 : 0)
      );
      if (input.Length > 0) {
        cursor += input.Normalized * Time.Elapsedf * 0.4f;
      }
    }

    Vec3 localCursor = matrixInv.Transform(cursor);

    // ori = Quat.FromAngles(0, MathF.Sin(Time.Totalf) * 90f, 0);


    if (grab) {
      // generate all the potential anchors
      // pick the closest one
      Vec3 v = Vec3.Zero;
      float minDist = float.MaxValue;
      for (int i = 0; i < anchors.Length; i++) {
        Vec3 a = matrix.Transform(anchors[i] * bounds.dimensions / 2);
        float dist = (a - cursor).Length;
        if (dist < minDist) {
          minDist = dist;
          v = anchors[i];
        }
      }

      detect = v; // rename this
      detectCount = (int)(MathF.Abs(v.x) + MathF.Abs(v.y) + MathF.Abs(v.z));

      vOffset = cursor - bounds.center;
      lOffset = ori.Inverse * vOffset;
      qOffset = (ori.Inverse * rGloveRot).Normalized;
      mOffset = matrix;
    } 
    else {
      if (detectCount == 1) { // Move (face -> crown *face)
        ori = (rGloveRot * qOffset.Inverse).Normalized;
        bounds.center = cursor - ori * lOffset;
      } 
      else if (detectCount == 2) { // Rotate (edge -> edge)
        // localPos = mOffset.Inverse.Transform(cursor);
        // Vec3 dir = new Vec3(
        //   detect.x == 0 ? 0 : localPos.x,
        //   detect.y == 0 ? 0 : localPos.y,
        //   detect.z == 0 ? 0 : localPos.z
        // );
        // Vec3 up = new Vec3(
        //   detect.x == 0 ? 1 : 0,
        //   detect.y == 0 ? 1 : 0,
        //   detect.z == 0 ? 1 : 0
        // );

        // Quat q = Quat.LookAt(Vec3.Zero, dir, up);

        // if (!adjusting) {
        //   qOffset = (q.Inverse * ori).Normalized;
        //   adjusting = true;
        // } else {
        //   ori = (q * qOffset).Normalized;
        // }
      } 
      else if (detectCount == 3) { // Scale (corner -> corner)
        Vec3 zAnchor = Anchor - ori * detect.JustZ() * cornerRadius;
        float stab = MathF.Max(cursorRadius - Vec3.Distance(zAnchor, cursor), 0);

        if (stab > 0) {
          float distZ = Math.Abs(-LocalAnchor.z - localCursor.z);
          bounds.center.z = PullRequest.Lerp(matrix.Transform(-LocalAnchor).z, cursor.z, 0.5f);
          bounds.dimensions.z = distZ;
        }
      }
    }

    matrix = Matrix.TR(bounds.center, ori);
    matrixInv = matrix.Inverse;
  }

  // design vars
  public float crown = 0.16f;

  public void Render() {
    // // matFrame.Wireframe = true;
    // matFrame.DepthTest = DepthTest.Always;
    // matFrame.SetVector("_cursor", cursor);
    // matFrame.SetFloat("_time", Time.Totalf);
    // meshCube.Draw(matFrame,
    //   Matrix.TRS(bounds.center, ori, bounds.dimensions),
    //   new Color(0.1f, 0.1f, 0.1f)
    // );

    // matPanes.DepthTest = DepthTest.Greater;
    matPanes["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrixInv);
    meshCube.Draw(matPanes,
      Matrix.TRS(bounds.center, ori, bounds.dimensions),
      new Color(0f, 0f, 0f)
      // new Color(78 / 256f, 142 / 256f, 191 / 256f)
    );

    matOriel.SetVector("_center", bounds.center);
    matOriel.SetVector("_dimensions", bounds.dimensions);
    matOriel.SetVector("_light", ori * new Vec3(0.6f, -0.9f, 0.3f));
    matOriel.SetFloat("_lit", 1);
    matOriel["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrixInv);



    // cursor
    if (detectCount > 0) {
      meshCube.Draw(Material.Default,
        Matrix.TS(LocalAnchor, Vec3.One * 0.01f) * matrix
      );

      // draw the corner with 0.1f length lines
      if (detectCount == 3) {
        Color col = new Color(1f, 1f, 1f);
        float thk = 0.005f;
        Lines.Add(Anchor, Anchor - ori * detect.JustX() * cornerRadius, col, thk);
        Lines.Add(Anchor, Anchor - ori * detect.JustY() * cornerRadius, col, thk);
        Lines.Add(Anchor, Anchor - ori * detect.JustZ() * cornerRadius, col, thk);

        // draw cube(s) on intersecting corner ends

        // Lines.Add(
        //   zAnchor, zAnchor + ori * detect.JustZ() * stab * 1.5f,
        //   new Color(0, 0, 1), 0.01f
        // );
      }
    }

    meshCube.Draw(Material.Default,
      Matrix.TRS(cursor, Quat.Identity, new Vec3(0.01f, 0.01f, 0.01f)),
      new Color(1f, 1f, 1f)
    );

    meshSphere.Draw(matClear,
      Matrix.TRS(cursor, Quat.Identity, new Vec3(1f, 1f, 1f) * cursorRadius * 2),
      new Color(0.5f, 0.5f, 0.5f)
    );
  }

  // faces, edges, corners
  Vec3[] anchors = new Vec3[] {
    // faces
    new Vec3(1, 0, 0),
    new Vec3(-1, 0, 0),
    new Vec3(0, 1, 0),
    new Vec3(0, -1, 0),
    new Vec3(0, 0, 1),
    new Vec3(0, 0, -1),
    // edges
    new Vec3(1, 1, 0),
    new Vec3(-1, 1, 0),
    new Vec3(1, -1, 0),
    new Vec3(-1, -1, 0),
    new Vec3(1, 0, 1),
    new Vec3(-1, 0, 1),
    new Vec3(1, 0, -1),
    new Vec3(-1, 0, -1),
    new Vec3(0, 1, 1),
    new Vec3(0, -1, 1),
    new Vec3(0, 1, -1),
    new Vec3(0, -1, -1),
    // corners
    new Vec3(1, 1, 1),
    new Vec3(-1, 1, 1),
    new Vec3(1, -1, 1),
    new Vec3(-1, -1, 1),
    new Vec3(1, 1, -1),
    new Vec3(-1, 1, -1),
    new Vec3(1, -1, -1),
    new Vec3(-1, -1, -1)
  };
}
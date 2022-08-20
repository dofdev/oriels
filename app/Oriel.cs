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
  public bool scaleWithHeight = false;
  public float scale = 0.5f;
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
      // Vec3.Zero,
      new Vec3(0.8f, 0.5f, 0.5f)
    );
    ori = Quat.FromAngles(0, 90, 0);
    matrix = Matrix.TR(bounds.center, ori);
    matrixInv = matrix.Inverse;


    cursor = bounds.center;
    cornerRadius = cursorRadius / 2;
  }

  public bool interacting;
  public bool scaling;

  public Vec3 cursor = Vec3.Zero;
  public Quat cursorOri = Quat.Identity;
  public Color cursorColor = Color.White;
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

  Vec3 cornerDetect = Vec3.Zero;
  public Vec3 XAnchor { get { return LocalAnchor - detect.JustX() * cornerRadius; } }
  public Vec3 YAnchor { get { return LocalAnchor - detect.JustY() * cornerRadius; } }
  public Vec3 ZAnchor { get { return LocalAnchor - detect.JustZ() * cornerRadius; } }
  Vec3 anchorOffset = Vec3.Zero;
  
  public void Frame() {
    // input
    Rig rig = Mono.inst.rig;
    Glove rGlove = Mono.inst.rGlove;
    // Vec3 lGlovePos = rig.lGlove.virtualGlove.position;

    bool onpress = rig.rCon.triggerBtn.frameDown;
    bool held = rig.rCon.triggerBtn.held;
    bool onlift = rig.rCon.triggerBtn.frameUp;
    cursor = rGlove.virtualGlove.position;
    cursorOri = rGlove.virtualGlove.orientation;

    // bool onpress = Input.Key(Key.Space).IsJustActive();
    // bool held = Input.Key(Key.Space).IsActive();
    // bool onlift = Input.Key(Key.Space).IsJustInactive();
    // if (!Input.Key(Key.Shift).IsActive()) {
    //   Vec3 input = new Vec3(
    //     (Input.Key(Key.S).IsActive() ? 1 : 0) - (Input.Key(Key.A).IsActive() ? 1 : 0),
    //     (Input.Key(Key.F).IsActive() ? 1 : 0) - (Input.Key(Key.Q).IsActive() ? 1 : 0),
    //     (Input.Key(Key.R).IsActive() ? 1 : 0) - (Input.Key(Key.W).IsActive() ? 1 : 0)
    //   );
    //   if (input.Length > 0) {
    //     cursor += input.Normalized * Time.Elapsedf * 0.4f;
    //   }
    // }
    // cursorOri = Quat.FromAngles(MathF.Sin(Time.Totalf) * 15, 0, 0);



    Vec3 localCursor = matrixInv.Transform(cursor);

    if (!interacting) {
      // generate all the potential anchors
      // pick the closest one
      Vec3 v = Vec3.Zero;
      float minDist = float.MaxValue;
      for (int i = 0; i < anchors.Length; i++) {
        Vec3 a = matrix.Transform(anchors[i] * bounds.dimensions / 2);
        float dist = (a - cursor).Length;
        // Vec3.Dot((bounds.center - rig.Head.position).Normalized, (matrix.Transform(anchors[i]) - bounds.center).Normalized) > 0 &&
        if (dist < minDist) {
          minDist = dist;
          v = anchors[i];
        }
      }

      detect = v; // rename this
      detectCount = (int)(MathF.Abs(v.x) + MathF.Abs(v.y) + MathF.Abs(v.z));

      vOffset = cursor - bounds.center;
      lOffset = ori.Inverse * vOffset;
      qOffset = (ori.Inverse * cursorOri).Normalized;
      mOffset = matrix;

      interacting = onpress;
      scaling = false;
      cornerDetect = Vec3.Zero;
    } 

    if (interacting) {
      if (detectCount == 1) { // Move (face -> crown *face)
        ori = (cursorOri * qOffset.Inverse).Normalized;
        // gravity snapping (within 6 degrees) *horizontal
        // always? *here **tilt = nosnap
        if (Vec3.Dot(-Vec3.Up, ori * -Vec3.Up) > 0.9998f) {
          Vec3 fwd = ori * Vec3.Forward;
          ori = Quat.LookDir(fwd.X0Z.Normalized);
        }
        bounds.center = cursor - ori * lOffset;

        interacting = held;
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

        interacting = held;
      } 
      else if (detectCount == 3) { // Scale (corner -> corner)
        if (!scaling) {
          cornerDetect = new Vec3(
            MathF.Max(cursorRadius - Vec3.Distance(XAnchor, localCursor), 0),
            MathF.Max(cursorRadius - Vec3.Distance(YAnchor, localCursor), 0),
            MathF.Max(cursorRadius - Vec3.Distance(ZAnchor, localCursor), 0)
          );

          anchorOffset = localCursor - LocalAnchor;

          scaling = onlift;
        }

        if (scaling) {
          Vec3 oldAnchor = Anchor;
          Vec3 delta = ((localCursor - anchorOffset) + LocalAnchor).Abs();
          bounds.dimensions = bounds.dimensions.Splice(delta, cornerDetect, true);
          bounds.center += Anchor - oldAnchor;

          scaling = !held;
          interacting = !held;
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
    Color col = new Color(0.15f, 0.15f, 0.15f);
    float thk = 0.005f;
    if (detectCount == 1 || detectCount == 2) {
      Vec3 edge = Vec3.One - detect.Abs();
      meshCube.Draw(matClear,
        Matrix.TS(
          LocalAnchor,
          (Vec3.One * thk) + (edge * bounds.dimensions / 3f)
        ) * matrix, col
      );
    }
    if (detectCount == 3) {
      // Lines.Add(Anchor, Anchor - ori * detect.JustX() * cornerRadius, col, thk);
      // Lines.Add(Anchor, Anchor - ori * detect.JustY() * cornerRadius, col, thk);
      // Lines.Add(Anchor, Anchor - ori * detect.JustZ() * cornerRadius, col, thk);
      Vec3 x = detect.JustX() * cornerRadius;
      meshCube.Draw(matClear,
        Matrix.TS(
          LocalAnchor - (x / 2f),
          (Vec3.One * thk) + x
        ) * matrix, col
      );
      Vec3 y = detect.JustY() * cornerRadius;
      meshCube.Draw(matClear,
        Matrix.TS(
          LocalAnchor - (y / 2f),
          (Vec3.One * thk) + y
        ) * matrix, col
      );
      Vec3 z = detect.JustZ() * cornerRadius;
      meshCube.Draw(matClear,
        Matrix.TS(
          LocalAnchor - (z / 2f),
          (Vec3.One * thk) + z
        ) * matrix, col
      );

      // draw cube(s) on intersecting corner ends


      if (cornerDetect.x > 0) {
        meshCube.Draw(matClear, 
          Matrix.TS(XAnchor, Vec3.One * thk * 2) * matrix,
          new Color(1, 0, 0)
        );
      }
      if (cornerDetect.y > 0) {
        meshCube.Draw(matClear, 
          Matrix.TS(YAnchor, Vec3.One * thk * 2) * matrix,
          new Color(0, 1, 0)
        );
      }
      if (cornerDetect.z > 0) {
        meshCube.Draw(matClear, 
          Matrix.TS(ZAnchor, Vec3.One * thk * 2) * matrix,
          new Color(0, 0, 1)
        );
      }

      // Lines.Add(
      //   zAnchor, zAnchor + ori * detect.JustZ() * stab * 1.5f,
      //   new Color(0, 0, 1), 0.01f
      // );
    }

    meshCube.Draw(Material.Default,
      Matrix.TRS(cursor, cursorOri, new Vec3(0.04f, 0.01f, 0.04f)),
      cursorColor
    );

    meshSphere.Draw(matClear,
      Matrix.TS(cursor, new Vec3(1f, 1f, 1f) * cursorRadius * 2),
      new Color(0.1f, 0.1f, 0.1f)
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
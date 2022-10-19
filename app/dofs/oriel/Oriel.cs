namespace Oriels;

public class Oriel {
  Material matClear = new Material(Shader.Default);
  
  public Material matOriel = new Material(Shader.FromFile("shaders/oriel.hlsl"));
  Material matFrame = new Material(Shader.FromFile("shaders/frame.hlsl"));
  Material matPanes = new Material(Shader.FromFile("shaders/panes.hlsl"));

  public Matrix matrix, matrixInv;
  public Bounds bounds;
  public Quat ori = Quat.Identity;
	public Color color = new Color(0.5f, 0.5f, 0.5f);

  public Oriel(Vec3 pos, Quat ori, Vec3 dimensions) {
    matClear.Transparency = Transparency.Add;

    matFrame.SetMat(102, Cull.Back, true);
    // matFrame.Transparency = Transparency.Add;
    matFrame.SetTexture("dither", Tex.FromFile("dither.png"));
    
		matPanes.SetMat(100, Cull.Front, false); // true?
    // matPanes.Transparency = Transparency.Add;
    // matPanes.DepthTest = DepthTest.Always;

    matOriel.SetMat(101, Cull.None, true);
    // matOriel.Transparency = Transparency.Add;
    // matOriel.DepthTest = DepthTest.Always;

    bounds = new Bounds(pos, dimensions);
    this.ori = ori;
    matrix = Matrix.TR(bounds.center, this.ori);
    matrixInv = matrix.Inverse;


    cursor = bounds.center;
    cornerRadius = cursorRadius / 2;
  }

  public bool interacting;
  public bool scaling;

  public Vec3 cursor = Vec3.Zero;
  public Vec3 localCursor = Vec3.Zero;
  public Quat cursorOri = Quat.Identity;
  public Color cursorColor = new Color(0.2f, 0.2f, 0.2f);
  public float cursorRadius = 0.04f;
  public float cornerRadius;
  
  Vec3 detect = Vec3.Zero;
  int detectCount = 0;
  public Vec3 LocalAnchor { get { return detect * bounds.dimensions / 2; } }
  public Vec3 Anchor { get { return matrix.Transform(LocalAnchor); } }
  Quat qOffset = Quat.Identity;
  Vec3 vOffset = Vec3.Zero;
  Vec3 lOffset = Vec3.Zero;
  Vec3 dirOrigin = Vec3.Zero;
  Matrix mOffset = Matrix.Identity;

  Vec3 cornerDetect = Vec3.Zero;
  public Vec3 XAnchor { get {
    float x = PullRequest.Clamp(localCursor.x, 
      LocalAnchor.x - cornerRadius, 
      LocalAnchor.x + cornerRadius
    );
    return new Vec3(x, LocalAnchor.y, LocalAnchor.z);
  } }
  public Vec3 YAnchor { get {
    float y = PullRequest.Clamp(localCursor.y, 
      LocalAnchor.y - cornerRadius, 
      LocalAnchor.y + cornerRadius
    );
    return new Vec3(LocalAnchor.x, y, LocalAnchor.z);
  } }
  public Vec3 ZAnchor { get {
    float z = PullRequest.Clamp(localCursor.z, 
      LocalAnchor.z - cornerRadius, 
      LocalAnchor.z + cornerRadius
    );
    return new Vec3(LocalAnchor.x, LocalAnchor.y, z);
  } }
  Vec3 anchorOffset = Vec3.Zero;
  
  public void Frame() {
    // input
    Rig rig = Mono.inst.rig;
    Glove rGlove = Mono.inst.rGlove;
    // Vec3 lGlovePos = rig.lGlove.virtualGlove.position;


		// glove
		Hand rHand = Input.Hand(Handed.Right);
    // why do I have to do this?
    // rHand.IsJustPinched ||
    bool frameDown = Input.Key(Key.MouseLeft).IsJustActive()   || rig.rCon.triggerBtn.frameDown;
    bool held      = Input.Key(Key.MouseLeft).IsActive()       || rig.rCon.triggerBtn.held;
    bool frameUp   = Input.Key(Key.MouseLeft).IsJustInactive() || rig.rCon.triggerBtn.frameUp;
    cursor    = rGlove.virtualGlove.position;
    cursorOri = rGlove.virtualGlove.orientation;

		// hand
    // Trackballer tb = (Trackballer)Mono.inst.dofs[3];
		// bool frameDown = tb.btnOut.frameDown;
		// bool held = tb.btnOut.held;
		// bool frameUp = tb.btnOut.frameUp;
		
		// WaveCursor wc = (WaveCursor)Mono.inst.dofs[1];
		// cursor = wc.cursor.position;
		// cursorOri = Quat.Identity; // wc.cursor.orientation;


    // debug
    // bool frameDown = Input.Key(Key.Space).IsJustActive();
    // bool held = Input.Key(Key.Space).IsActive();
    // bool frameUp = Input.Key(Key.Space).IsJustInactive();
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



    localCursor = matrixInv.Transform(cursor);

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

      dirOrigin = (localCursor * detect.Abs()).Normalized;

      qOffset = (ori.Inverse * cursorOri).Normalized;
      mOffset = matrix;

      interacting = frameDown && minDist < cursorRadius;
      scaling = false;
      cornerDetect = Vec3.Zero;
    } 

    if (interacting) {
      if (detectCount == 1) { // Grab (face -> crown *face)
        Quat newOri = (cursorOri * qOffset.Inverse).Normalized;
        ori = Quat.Slerp(ori, newOri, Time.Elapsedf * 9f);
        // gravity snapping (within 6 degrees) *horizontal
        // always? *here **tilt = nosnap
        if (Vec3.Dot(-Vec3.Up, ori * -Vec3.Up) > 0.9998f) {
          Vec3 fwd = ori * Vec3.Forward;
          ori = Quat.LookDir(fwd.X0Z.Normalized);
        }
        bounds.center = cursor - ori * lOffset;

        interacting = held;
      } 
      else if (detectCount == 2) { // Tilt (edge -> edge)
        // local quaternion delta
        Vec3 dir = (localCursor * detect.Abs()).Normalized;
        Quat delta = (Quat.LookDir(dir) * Quat.LookDir(dirOrigin).Inverse).Normalized;
        
        ori = (ori * delta).Normalized;

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

          scaling = frameUp;
        }

        if (scaling) {
          Vec3 oldAnchor = Anchor;
          Vec3 delta = ((localCursor - anchorOffset) + LocalAnchor);
          delta = delta * LocalAnchor.Sign(); // instead of delta.Abs();
          bounds.dimensions = bounds.dimensions.Splice(delta, cornerDetect, true);
          bounds.dimensions = new Vec3(
            MathF.Max(bounds.dimensions.x, 0.02f),
            MathF.Max(bounds.dimensions.y, 0.02f),
            MathF.Max(bounds.dimensions.z, 0.02f)
          );
          bounds.center += Anchor - oldAnchor;

          scaling = interacting = !held;
          if (cornerDetect.MagnitudeSq == 0) {
            scaling = interacting = false;
          }
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
    // Mesh.Cube.Draw(matFrame,
    //   Matrix.TRS(bounds.center, ori, bounds.dimensions),
    //   new Color(0.1f, 0.1f, 0.1f)
    // );

    // matPanes.DepthTest = DepthTest.Greater;
    matPanes["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrixInv);
    Mesh.Cube.Draw(matPanes,
      Matrix.TRS(bounds.center, ori, bounds.dimensions),
      color
    );

    matOriel.SetVector("_center", bounds.center);
    matOriel.SetVector("_dimensions", bounds.dimensions);
    matOriel.SetVector("_light", ori * new Vec3(0.6f, -0.9f, 0.3f));
    matOriel.SetFloat("_lit", 1);
    matOriel["_matrix"] = (Matrix)System.Numerics.Matrix4x4.Transpose(matrixInv);



    // cursor
    Color col = new Color(0.15f, 0.15f, 0.15f);
    float thk = 0.002f;
    float prx = PullRequest.Clamp(
      cursorRadius - (localCursor - LocalAnchor).Magnitude / 3, 0, cursorRadius
    ) / cursorRadius;
    if (detectCount == 1 || detectCount == 2) {
      Vec3 edge = Vec3.One - detect.Abs();
      Mesh.Cube.Draw(matClear,
        Matrix.TS(
          LocalAnchor,
          (Vec3.One * thk) + (edge * bounds.dimensions / 3f * prx)
        ) * matrix, col
      );
    }
    if (detectCount == 3) {
      Mesh.Cube.Draw(matClear,
        Matrix.TS(
          Vec3.Lerp(XAnchor, LocalAnchor, 0.5f),
          new Vec3(MathF.Abs(XAnchor.x - LocalAnchor.x), thk, thk)
        ) * matrix, col
      );
      Mesh.Cube.Draw(matClear,
        Matrix.TS(
          Vec3.Lerp(YAnchor, LocalAnchor, 0.5f),
          new Vec3(thk, MathF.Abs(YAnchor.y - LocalAnchor.y), thk)
        ) * matrix, col
      );
      Mesh.Cube.Draw(matClear,
        Matrix.TS(
          Vec3.Lerp(ZAnchor, LocalAnchor, 0.5f),
          new Vec3(thk, thk, MathF.Abs(ZAnchor.z - LocalAnchor.z))
        ) * matrix, col
      );

      // draw cube(s) on intersecting corner ends
      if (cornerDetect.x > 0) {
        Mesh.Cube.Draw(matClear, 
          Matrix.TS(XAnchor, Vec3.One * thk * 2) * matrix,
          new Color(1, 0, 0)
        );
      }
      if (cornerDetect.y > 0) {
        Mesh.Cube.Draw(matClear, 
          Matrix.TS(YAnchor, Vec3.One * thk * 2) * matrix,
          new Color(0, 1, 0)
        );
      }
      if (cornerDetect.z > 0) {
        Mesh.Cube.Draw(matClear, 
          Matrix.TS(ZAnchor, Vec3.One * thk * 2) * matrix,
          new Color(0, 0, 1)
        );
      }
    }

    Mesh.Sphere.Draw(Mono.inst.matHolo,
      Matrix.TRS(cursor, cursorOri, new Vec3(0.02f, 0.02f, 0.02f)),
      cursorColor
    );

    Mesh.Sphere.Draw(Mono.inst.matHolo,
      Matrix.TS(cursor, new Vec3(1f, 1f, 1f) * cursorRadius * 2),
      new Color(0.1f, 0.1f, 0.1f)
    );
  }

  // faces, edges, corners
  Vec3[] anchors = new Vec3[] {
    // faces
    new Vec3(1,  0,  0), new Vec3(-1,  0,  0),
    new Vec3(0,  1,  0), new Vec3( 0, -1,  0),
    new Vec3(0,  0,  1), new Vec3( 0,  0, -1),
    // edges
    new Vec3(1,  1,  0), new Vec3(-1,  1,  0),
    new Vec3(1, -1,  0), new Vec3(-1, -1,  0),
    new Vec3(1,  0,  1), new Vec3(-1,  0,  1),
    new Vec3(1,  0, -1), new Vec3(-1,  0, -1),
    new Vec3(0,  1,  1), new Vec3( 0, -1,  1),
    new Vec3(0,  1, -1), new Vec3( 0, -1, -1),
    // corners
    new Vec3(1,  1,  1), new Vec3(-1,  1,  1),
    new Vec3(1, -1,  1), new Vec3(-1, -1,  1),
    new Vec3(1,  1, -1), new Vec3(-1,  1, -1),
    new Vec3(1, -1, -1), new Vec3(-1, -1, -1)
  };
}


/* 
	COMMENTS

	try rendering as additive for an AR effect!

	compositor
		multi-oriel requires a compositor approach
		even if you just start with input management

*/
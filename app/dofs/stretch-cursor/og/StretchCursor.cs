namespace Oriels;

class StretchCursor : dof {
  Pose p0, p1;
  public StretchCursor() {
    
  }

  public Pose cursor;
  public void Init() {
    
  }

  bool isTracking = false;
  public void Frame() {
    Hand hand = Input.Hand(handId);

    if (hand.tracked.IsActive() && !hand.tracked.IsJustActive()) {
      p0.position = hand.Get(FingerId.Index, JointId.KnuckleMajor).position;
      p1.position = hand.Get(FingerId.Index, JointId.Tip).position;

      isTracking = true;
    }


    // Vec3   vec      = p0.position - p1.position;
    // float  len      = vec.Length;
    // float  stretch  = Math.Max(len - deadzone, 0f);
    // Vec3   dir      = backhand ? vec / len : p0.orientation * Vec3.Forward;



    float fI = Flexion(hand, FingerId.Index);
    float fM = Flexion(hand, FingerId.Middle);
    float fR = Flexion(hand, FingerId.Ring);
    float fL = Flexion(hand, FingerId.Little);

    float stretch = (fI + fI + fM + fM + fM + fR + fR + fL) / 8f; // based on finger length

    Vec3 dir = PullRequest.Direction(
      hand.Get(FingerId.Index, JointId.Tip).position,
      hand.Get(FingerId.Index, JointId.KnuckleMajor).position
    );

    cursor.position = hand.Get(FingerId.Index, JointId.Tip).position + dir * stretch * strength * Mono.inst.stretchStr;

    Mesh.Cube.Draw(Material.Default, Matrix.TS(cursor.position, 0.01f));

    if (isTracking) { Demo(); }
  }

  public bool  backhand = true;
  public float deadzone = 0.1f;
  public float strength = 3f;
  public int   handId   = 0;


  float Flexion(Hand hand, FingerId finger) {
    float flexion = (Vec3.Dot(
      PullRequest.Direction(
        hand.Get(finger, JointId.Tip).position,
        hand.Get(finger, JointId.KnuckleMinor).position
      ),
      PullRequest.Direction(
        hand.Get(finger, JointId.KnuckleMid).position,
        hand.Get(finger, JointId.KnuckleMajor).position
      )
    ) + 1f) / 2;

    return Math.Max(flexion - deadzone, 0f) / (1 - deadzone);
  }











  Vec3 smoothPos;
  Vec3[] points = new Vec3[64];
  void Demo() {

    smoothPos = Vec3.Lerp(smoothPos, cursor.position, Time.Elapsedf * 6f);

    // while (Vec3.Distance(points[0], smoothPos) > 0.03f) {
    //   for (int i = points.Length - 1; i > 0; i--) {
    //     points[i] = points[i - 1];
    //   }
    //   points[0] += PullRequest.Direction(smoothPos, points[0]) * 0.02f;
    // }


    points[0] = smoothPos;

    int len = (int)(points.Length * Mono.inst.trailLen);
    for (int i = 0; i < len; i++) {
      if (i > 0) {
        Vec3 dir = Vec3.Forward;
        if (points[i].v != points[i - 1].v) {
          dir = PullRequest.Direction(points[i], points[i - 1]);
        }
        points[i] = points[i - 1] + dir * 0.02f * Mono.inst.trailScl;
      }

      Vec3 from = i > 0 ? points[i - 1] : smoothPos;

      Mesh.Cube.Draw(
        Material.Default,
        Matrix.TRS(points[i], Quat.LookDir(PullRequest.Direction(points[i], from)), 0.01f * Mono.inst.trailScl), 
        Color.HSV(i / (float)len, 1, 1)
      );
    }
  }
}

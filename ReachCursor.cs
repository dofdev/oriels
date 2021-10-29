using System;
using StereoKit;

public class ReachCursor {
  static Material unlitMat = Default.MaterialUnlit.Copy();

  static Model modelCursor = Model.FromFile("cursor.glb", Shader.Default);
  static Model modelSphere = new Model(Default.MeshSphere, unlitMat);
  
  static Vec3[] pullPoints = new Vec3[2];

  public void Step() {
    Matrix matrix = new Matrix();
    matrix.Translation = new Vec3(0, 0, -1);

    for (int h = 0; h < (int)Handed.Max; h++)
    {
      // Get the pose for the index fingertip
      Hand hand = Input.Hand((Handed)h);
      Vec3 indexTip = hand[FingerId.Index, JointId.Tip].Pose.position;
      Vec3 thumbTip = hand[FingerId.Thumb, JointId.Tip].Pose.position;
      Vec3 pinchPos = Vec3.Lerp(indexTip, thumbTip, 0.5f);
      if (hand.IsPinched)
      {
        pullPoints[h] = pinchPos;
      }

      float stretch = (pullPoints[h] - pinchPos).Length;
      Vec3 dir = (pinchPos - pullPoints[h]).Normalized;
      Vec3 pos = pinchPos + dir * stretch * 3;
      modelCursor.Draw(Matrix.TS(pos, 0.06f));
      Lines.Add(pullPoints[h], pos, Color.White, 0.01f);
      modelSphere.Draw(Matrix.TS(pullPoints[h], 0.04f));
    }
  }
}
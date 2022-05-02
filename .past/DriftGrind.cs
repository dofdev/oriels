public class DriftGrind {

  public Vec3 rDragStart, lDragStart;
  public float railT;

  Vec3 pos = new Vec3(0, 0, 0);
  Vec3 vel = new Vec3(0, 0, 0);

  public void Step() {
    // DRAG DRIFT
    // Vec3 rPos = net.me.cursor0;
    // Vec3 lPos = net.me.cursor3;

    // use grip grab reach cursor origin it then becoming a backhanded stretch cursor
    // if (rCon.grip > 0.5f) {
    //   if (!rightGripping) {
    //     gripPos = rPos;
    //     gripLeft = false;
    //     rightGripping = true;
    //   }
    // } else {
    //   rightGripping = false;
    // }

    // if (lCon.grip > 0.5f) {
    //   if (!leftGripping) {
    //     gripPos = lPos;
    //     gripLeft = true;
    //     leftGripping = true;
    //   }
    // } else {
    //   leftGripping = false;
    // }

    // if (rightGripping || leftGripping) {
    //   Vec3 gripTo = gripLeft ? lPos : rPos;
    //   pos = -(gripTo - Input.Head.position) + gripPos - (Input.Head.position - pos);
    //   vel = Vec3.Zero;
    // }
    // delete: gripPos, gripLeft, rightGripping, leftGripping
    // gripPos = r/l OldPos
    // rightGripping/leftGripping -> state machine (world grip, stretch, backhanded, grinding?)





    // CUBIC BEZIER RAIL

    // float grindDir = 1f;
    // bool grinding = false;
    // bool grinded = false;
    // Vec3 grindVel = Vec3.Forward;
    // Vec3[] grindRail = new Vec3[4];

    // if (rCon.device.grip > 0.5f) {
    //   if (!grinded) {
    //     if (!grinding) {
    //       int closest = 0;
    //       float closestDist = float.MaxValue;
    //       Vec3 closestPoint = Vec3.Zero;
    //       int closestRail = 0;
    //       for (int i = 0; i < net.me.cubics.Length; i++) {
    //         if (net.me.cubics[i].active) {
    //           Vec3[] rail = new Vec3[] {
    //             net.me.cubics[i].p0,
    //             net.me.cubics[i].p1,
    //             net.me.cubics[i].p2,
    //             net.me.cubics[i].p3,
    //           };
    //           for (int j = 0; j < rail.Length; j++) {
    //             Vec3 point = Bezier.Sample(rail, (float)j / (rail.Length - 1f));
    //             float dist = Vec3.Distance(point, rCon.pos + vel.Normalized * 0.25f);
    //             if (dist < closestDist && dist < 0.5f) {
    //               closest = j;
    //               closestRail = i;
    //               closestDist = dist;
    //               closestPoint = point;
    //               railT = (float)j / (rail.Length - 1f);
    //               grinding = true;
    //             }
    //           }
    //         }
    //       }
    //       if (grinding) {
    //         grindRail = new Vec3[] {
    //           net.me.cubics[closestRail].p0,
    //           net.me.cubics[closestRail].p1,
    //           net.me.cubics[closestRail].p2,
    //           net.me.cubics[closestRail].p3,
    //         };
    //         // pos = closestPoint - (subCon.pose.position - pos);
    //         grindVel = vel;
    //         Vec3 fromPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
    //         Vec3 toPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f);
    //         grindDir = Vec3.Dot((fromPos - toPos).Normalized, grindVel) < 0f ? 1 : -1;
    //       }
    //     }

    //     if (grinding) {
    //       Vec3 grindPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT);
    //       Vec3 nextPos = Bezier.Sample(grindRail[0], grindRail[1], grindRail[2], grindRail[3], railT + 0.1f * grindDir);

    //       // vel += (toPos - fromPos);

    //       pos = -(rCon.pos - Input.Head.position) + grindPos - (Input.Head.position - pos);
    //       vel = Vec3.Zero;

    //       railT += Time.Elapsedf * grindVel.Magnitude * grindDir; // scale based on length of rail * calculate and cache on place
    //       // bool clamped = false;
    //       // float railTpreClamp = railT;
    //       // if
    //       railT = Math.Clamp(railT, 0, 1);

    //       grindVel = (nextPos - grindPos).Normalized * grindVel.Magnitude;

    //       if (railT == 1 || railT == 0) {
    //         vel = grindVel;
    //         grinding = false;
    //         grinded = true;
    //         railT = 0f;
    //       }


    //       cube.Draw(mat, Matrix.TS(grindPos, new Vec3(0.1f, 0.1f, 0.1f)));
    //       // cube.Draw(mat, Matrix.TS(toPos, new Vec3(0.1f, 0.1f, 0.1f) * 0.333f));
    //       // pos = Vec3.Lerp(pos, Bezier.Sample(net.me.cubics[0].p0, net.me.cubics[0].p1, net.me.cubics[0].p2, net.me.cubics[0].p3, railT) - (subCon.aim.position - pos), Time.Elapsedf * 6f);
    //       // how to reliably determine and control which direction to go? (velocity)
    //     }
    //   }
    // } else {
    //   grinded = false;
    //   if (grinding) {
    //     vel = grindVel;
    //     grinding = false;
    //   }
    // }

    // Console.WriteLine(World.RefreshInterval.ToString());



    // if (domCon.IsX1JustUnPressed && Time.Totalf - movePress < 0.2f) {
    //   pos = p00 - (Input.Head.position - pos);
    // }

    // just push off of the air lol better than teleporting
    // not cursor dependent

    // pos.x = (float)Math.Sin(Time.Total * 0.1f) * 0.5f;

    // pos += vel * Time.Elapsedf;

    // float preX = pos.x; pos.x = Math.Clamp(pos.x, -scene.scale / 2, scene.scale / 2); if (pos.x != preX) { vel.x = 0; }
    // float preY = pos.y; pos.y = Math.Clamp(pos.y, 0f, scene.scale / 2); if (pos.y != preY) { vel.y = 0; }
    // float preZ = pos.z; pos.z = Math.Clamp(pos.z, -scene.scale / 2, scene.scale / 2); if (pos.z != preZ) { vel.z = 0; }

    // vel *= 1 - Time.Elapsedf * 0.2f;
  }
}
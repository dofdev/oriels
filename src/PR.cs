namespace Oriels;
public static class PR {
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
		// / Time.Stepf; // delta -> velocity
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

	// // construct the quaternion that rotates one vector to another
	// // uses the usual trick to get the half angle
	// public static Quat Delta(Vec3 to, Vec3 from) {
	// 	Vec3 vec = Vec3.Cross(from, to);
	// 	return new Quat(
	// 		vec.x,
	// 		vec.y,
	// 		vec.z,
	// 		1 + Vec3.Dot(to, from )
	// 	).Normalized;
	// }

  // Quat q;
  // public static void Relative(Quat to) => q = to * q * to.Inverse;
	public static Quat Relative(Quat to, Quat delta) {
		return (to * delta * to.Inverse).Normalized;
	}

	// ?
	public static Vec3 Relative(Quat to, Vec3 delta) {
		return to * delta * to.Inverse;
	}

	// public static void LookDirection(this ref Quat q, Vec3 dir) {
	//   Vec3 up = Vec3.Up;
		
	//   // using AxisAngle
	//   Vec3 axis = Vec3.Cross(up, dir);
	//   float angle = MathF.Atan2(Vec3.Dot(up, dir), axis.Length);
	//   q = FromAxisAngle(axis.Normalized, angle);
	// }

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

	public static Vec3 SnapToLine(this Vec3 v, Vec3 a, Vec3 b, bool clamp, out float t, float tMin = 0, float tMax = 1) {
    Quat q = Quat.LookDir(Vec3.Direction(b, a));
    Vec3 lv = q.Inverse * (v - a);
    lv.x = lv.y = 0;
		Vec3 r = q * lv + a;

		float d = (b - a).Length;
		t = (r - a).Length / d;
		if (clamp) {
			t = t < tMin ? tMin : (t > tMax ? tMax : t);
			r = a + (b - a) * t;
		}
    return r;
  }

  /*
	// turn this into a function
		Vec3 vA = new Vec3(-1, 0, 0);
		Vec3 vB = new Vec3(1, 1, 1);

		Vec3 vC = Input.Hand(Handed.Right).palm.position;

		Quat q = Quat.LookDir((vB - vA).Normalized);

		// snap vC to line vA-vB
		Vec3 local = q.Inverse * (vC - vA);
		local.x = 0;
		local.y = 0;
		vC = q * local + vA;

		Lines.Add(vA, vB, new Color(1, 1, 1), 0.002f);
		Mesh.Cube.Draw(matDev, Matrix.TRS(vC, q, 0.04f));

	*/

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

	// public static float ToFloat(
	// 		ref string s, 
	// 		float min = float.NegativeInfinity, 
	// 		float max = float.PositiveInfinity
	// 	) {
	// 	try {
	// 		float value = Clamp(float.Parse(s), min, max);
	// 		// if clamped, update string
	// 		if (value != float.Parse(s)) {
	// 			s = value.ToString();
	// 		}
	// 		return value;
	// 	} catch {
	// 		return 0;
	// 	}
	// }

	public static float Diff(float a, float b) {
		return MathF.Abs(a - b);
	}

	public class Delta {
		public float delta { get; private set; }

		float last;
		public float Step(float current) {
			delta = current - last;
			last = current;
			return delta;
		}
	}

	public class DeltaV {
		public Vec3 delta { get; private set; }

		Vec3 last;
		public Vec3 Step(Vec3 current) {
			delta = current - last;
			last = current;
			return delta;
		}
	}



	public class PID {
		public float p, i;
		public float value;
		float integral;
		// float scalar = 1f;

		public PID(float p = 1, float i = 0.1f) {
			this.p = p;
			this.i = i;
		}

		public float Update(float target) {
			float error = value - target;
			integral += error;
			float delta = ((p * error) + (i * integral));
			return value -= delta * Time.Stepf;
		}
	}

	public class Vec3PID {
		public Vec3 value, integral;
		// float scalar = 1f;

		public Vec3 Update(Vec3 target, float p = 1, float i = 0.1f) {
			Vec3 error = value - target;
			integral += error;
			Vec3 delta = ((p * error) + (i * integral));
			return value -= delta * Time.Stepf;
		}
	}

	public class Lerper {
		public float t = 0;
		public float spring = 1;
		public float dampen = 1;
		float vel;

		public void Step(float to = 1, bool bounce = false) {
			float dir = to - t;
			vel += dir * spring * Time.Stepf;

			if (Math.Sign(vel) != Math.Sign(dir)) {
				vel *= 1 - (dampen * Time.Stepf);
			} else {
				vel *= 1 - (dampen * 0.33f * Time.Stepf);
			}

			float newt = t + vel * Time.Stepf;
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

	public class OneEuroFilter {
		public OneEuroFilter(double minCutoff, double beta) {
			firstTime = true;
			this.minCutoff = minCutoff;
			this.beta = beta;

			xFilt = new LowpassFilter();
			dxFilt = new LowpassFilter();
			dcutoff = 1;
		}

		protected bool firstTime;
		protected double minCutoff;
		protected double beta;
		protected LowpassFilter xFilt;
		protected LowpassFilter dxFilt;
		protected double dcutoff;

		public double MinCutoff {
			get { return minCutoff; }
			set { minCutoff = value; }
		}

		public double Beta {
			get { return beta; }
			set { beta = value; }
		}

		public double Filter(double x, double rate) {
			double dx = firstTime ? 0 : (x - xFilt.Last()) * rate;
			if (firstTime) {
				firstTime = false;
			}

			var edx = dxFilt.Filter(dx, Alpha(rate, dcutoff));
			var cutoff = minCutoff + beta * Math.Abs(edx);

			return xFilt.Filter(x, Alpha(rate, cutoff));
		}

		protected double Alpha(double rate, double cutoff) {
			var tau = 1.0 / (2 * Math.PI * cutoff);
			var te = 1.0 / rate;
			return 1.0 / (1.0 + tau / te);
		}
	}

	public class LowpassFilter {
		public LowpassFilter() {
			firstTime = true;
		}

		protected bool firstTime;
		protected double hatXPrev;

		public double Last() {
			return hatXPrev;
		}

		public double Filter(double x, double alpha) {
			double hatX = 0;
			if (firstTime) {
				firstTime = false;
				hatX = x;
			} else
				hatX = alpha * x + (1 - alpha) * hatXPrev;

			hatXPrev = hatX;

			return hatX;
		}
	}
}
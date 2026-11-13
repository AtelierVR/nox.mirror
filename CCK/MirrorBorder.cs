using System.Collections.Generic;
using UnityEngine;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// Black frame drawn around a mirror panel, of a constant <b>world</b> thickness.
	/// <para>
	/// A panel is one mesh scaled on two axes at once (a 10 x 10 plane scaled to 0.9 x 1.4 m, for instance),
	/// so a child quad would be squashed differently along each edge. This component builds the whole frame -
	/// the face on the glass, the four sides and the back - in the units of the panel, and rebuilds it whenever
	/// the panel is resized, which keeps the frame square whatever size the mirror takes.
	/// </para>
	/// <para>
	/// The frame straddles the silhouette of the panel (see <see cref="outside"/>) and reaches back to the
	/// <c>Back</c> child of the panel, so the panel reads as a framed object from the side as well.
	/// </para>
	/// <para>
	/// It runs in the editor as well (<see cref="ExecuteAlways"/>), so resizing the panel in a scene shows the
	/// frame straight away. Nothing watches the size every frame: the component that resizes the panel asks
	/// for this refresh (see <see cref="PersonalMirror.RefreshPanel"/>). The object it sits on has to stay
	/// unscaled and unrotated: the frame is written in the space of the panel, and it places itself.
	/// </para>
	/// </summary>
	[ExecuteAlways]
	[AddComponentMenu("Nox/Mirror/Mirror Border")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	[DefaultExecutionOrder(1200)]
	public class MirrorBorder : MonoBehaviour {
		[SerializeField, Min(0f), Tooltip("Thickness of the frame, in world metres, whatever the size of the panel.")]
		private float thickness = 0.03f;

		[SerializeField, Min(0.0001f), Tooltip("Width of the panel mesh in its own units: a Unity plane is 10 units wide, which is what makes the thickness a size in metres.")]
		private float meshSize = 10f;

		[SerializeField, Min(0f), Tooltip("How far the face of the frame is pushed in front of the panel, in world metres, so the two do not fight for the same pixels.")]
		private float offset = 0.002f;

		[SerializeField, Range(0f, 1f), Tooltip("Part of the thickness that hangs past the edge of the panel, the rest covering it: the frame sits on the outline, so the edge of the glass is never visible. 0 is a frame fully inside, 1 fully outside.")]
		private float outside = 0.5f;

		[SerializeField, Tooltip("Back of the panel, which gives the depth of the frame. Looked up by name (\"Back\") when empty.")]
		private Transform back;

		/// <summary>Size the frame was built for, so it is only rebuilt when the panel changes.</summary>
		private Vector4 _built;

		/// <summary>Depth the frame was built for, checked apart from the size.</summary>
		private float _builtBack;

		/// <summary>Mesh of the frame, kept between the updates and recreated if Unity drops it.</summary>
		private Mesh _mesh;

		private MeshFilter _filter;

		/// <summary>Renderer of the frame, hidden while the settings turn the frames off.</summary>
		private MeshRenderer _renderer;

		private void OnEnable()
			=> Apply();

		/// <summary>
		/// Rebuilds the frame, as Unity asks when the component or its values change in the editor. Called by
		/// the mirror when it resizes its panel (see <see cref="PersonalMirror.RefreshPanel"/>).
		/// </summary>
		public void OnValidate()
			=> Apply();

		private void OnDestroy() {
			if (!_mesh)
				return;

			if (Application.isPlaying)
				Object.Destroy(_mesh);
			else
				Object.DestroyImmediate(_mesh);

			_mesh = null;
		}

		/// <summary>
		/// Rebuilds the frame when the panel was resized. Called on demand, it only does the work when the
		/// size, the thickness or the depth of the panel changed.
		/// </summary>
		public void Apply() {
			if (!_renderer)
				_renderer = GetComponent<MeshRenderer>();

			// The settings of the session can hide the frame: it is kept, so turning them back on is instant.
			if (_renderer)
				_renderer.enabled = MirrorSettings.Borders;

			var panel = transform.parent;
			if (!panel)
				return;

			// The frame is written in the space of the panel, which is only true when this object adds nothing.
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
			transform.localScale    = Vector3.one;

			var scale = panel.lossyScale;

			// World size of the panel: its mesh spans `meshSize` units, the scale of the parent turns those into metres.
			var width  = Mathf.Abs(scale.x) * meshSize;
			var height = Mathf.Abs(scale.z) * meshSize;

			if (width <= 1e-5f || height <= 1e-5f)
				return;

			// The back of the panel is where the frame stops: without one, it goes back by its own thickness.
			if (!back)
				back = panel.Find("Back");

			var backY = back ? back.localPosition.y : -thickness;

			var size = new Vector4(width, height, thickness, outside);

			if ((_built - size).sqrMagnitude < 1e-10f
			    && Mathf.Abs(_builtBack - backY) < 1e-7f
			    && _mesh
			    && _filter
			    && _filter.sharedMesh == _mesh)
				return;

			_built     = size;
			_builtBack = backY;

			// Metres per unit of the panel, on each of the three axes it is scaled on.
			var unitX = Mathf.Max(width / meshSize, 1e-5f);
			var unitY = Mathf.Max(Mathf.Abs(scale.y), 1e-5f);
			var unitZ = Mathf.Max(height / meshSize, 1e-5f);

			var halfX = meshSize * 0.5f;
			var halfZ = meshSize * 0.5f;

			// A frame of `thickness` metres is not the same number of units on both axes: that is what keeps it square.
			var barX = Mathf.Min(thickness / unitX, halfX);
			var barZ = Mathf.Min(thickness / unitZ, halfZ);

			// Face of the frame, in front of the glass; back of the frame, on the back of the panel.
			var front = offset / unitY;

			if (backY >= front)
				backY = front - 1e-3f;

			// Outline of the glass and outline of the frame: the frame hangs past the edge by `outside` of its thickness.
			var outerX = halfX + barX * outside;
			var outerZ = halfZ + barZ * outside;
			var innerX = outerX - barX;
			var innerZ = outerZ - barZ;

			var vertices  = new List<Vector3>(64);
			var normals   = new List<Vector3>(64);
			var uvs       = new List<Vector2>(64);
			var triangles = new List<int>(96);

			// Face on the glass.
			Face(vertices, normals, uvs, triangles, Vector3.up,
				V(-outerX, front, innerZ), V(outerX, front, innerZ), V(outerX, front, outerZ), V(-outerX, front, outerZ));
			Face(vertices, normals, uvs, triangles, Vector3.up,
				V(-outerX, front, -outerZ), V(outerX, front, -outerZ), V(outerX, front, -innerZ), V(-outerX, front, -innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.up,
				V(-outerX, front, -innerZ), V(-innerX, front, -innerZ), V(-innerX, front, innerZ), V(-outerX, front, innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.up,
				V(innerX, front, -innerZ), V(outerX, front, -innerZ), V(outerX, front, innerZ), V(innerX, front, innerZ));

			// Face on the back of the panel, seen from behind.
			Face(vertices, normals, uvs, triangles, Vector3.down,
				V(-outerX, backY, innerZ), V(outerX, backY, innerZ), V(outerX, backY, outerZ), V(-outerX, backY, outerZ));
			Face(vertices, normals, uvs, triangles, Vector3.down,
				V(-outerX, backY, -outerZ), V(outerX, backY, -outerZ), V(outerX, backY, -innerZ), V(-outerX, backY, -innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.down,
				V(-outerX, backY, -innerZ), V(-innerX, backY, -innerZ), V(-innerX, backY, innerZ), V(-outerX, backY, innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.down,
				V(innerX, backY, -innerZ), V(outerX, backY, -innerZ), V(outerX, backY, innerZ), V(innerX, backY, innerZ));

			// Four sides of the frame, from the face to the back.
			Face(vertices, normals, uvs, triangles, Vector3.forward,
				V(-outerX, backY, outerZ), V(outerX, backY, outerZ), V(outerX, front, outerZ), V(-outerX, front, outerZ));
			Face(vertices, normals, uvs, triangles, Vector3.back,
				V(-outerX, backY, -outerZ), V(outerX, backY, -outerZ), V(outerX, front, -outerZ), V(-outerX, front, -outerZ));
			Face(vertices, normals, uvs, triangles, Vector3.left,
				V(-outerX, backY, -outerZ), V(-outerX, backY, outerZ), V(-outerX, front, outerZ), V(-outerX, front, -outerZ));
			Face(vertices, normals, uvs, triangles, Vector3.right,
				V(outerX, backY, -outerZ), V(outerX, backY, outerZ), V(outerX, front, outerZ), V(outerX, front, -outerZ));

			// Inner sides, against the glass: what the frame would show from a grazing angle.
			Face(vertices, normals, uvs, triangles, Vector3.back,
				V(-innerX, backY, innerZ), V(innerX, backY, innerZ), V(innerX, front, innerZ), V(-innerX, front, innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.forward,
				V(-innerX, backY, -innerZ), V(innerX, backY, -innerZ), V(innerX, front, -innerZ), V(-innerX, front, -innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.right,
				V(-innerX, backY, -innerZ), V(-innerX, backY, innerZ), V(-innerX, front, innerZ), V(-innerX, front, -innerZ));
			Face(vertices, normals, uvs, triangles, Vector3.left,
				V(innerX, backY, -innerZ), V(innerX, backY, innerZ), V(innerX, front, innerZ), V(innerX, front, -innerZ));

			var mesh = EnsureMesh();

			mesh.Clear();
			mesh.SetVertices(vertices);
			mesh.SetNormals(normals);
			mesh.SetUVs(0, uvs);
			mesh.SetTriangles(triangles, 0);
			mesh.RecalculateBounds();
		}

		/// <summary>Shorthand for a point of the frame, in the units of the panel.</summary>
		private static Vector3 V(float x, float y, float z)
			=> new(x, y, z);

		/// <summary>
		/// Writes one flat face of the frame, as two triangles, with its own normal. The corners may be given
		/// in any order: the winding is fixed so the face shows on the side of <paramref name="normal"/>.
		/// </summary>
		/// <param name="vertices">Vertices of the whole frame.</param>
		/// <param name="normals">Normals of the whole frame.</param>
		/// <param name="uvs">Uvs of the whole frame.</param>
		/// <param name="triangles">Triangles of the whole frame.</param>
		/// <param name="normal">Direction the face looks at.</param>
		/// <param name="a">First corner.</param>
		/// <param name="b">Second corner.</param>
		/// <param name="c">Third corner.</param>
		/// <param name="d">Fourth corner.</param>
		private static void Face(
			List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles,
			Vector3       normal,   Vector3       a,       Vector3       b,   Vector3       c, Vector3 d
		) {
			// The quad is built as (a, b, c, d) going around: swap b and d when that turns the wrong way.
			if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
				(b, d) = (d, b);

			var index = vertices.Count;

			vertices.Add(a);
			vertices.Add(b);
			vertices.Add(c);
			vertices.Add(d);

			for (var i = 0; i < 4; i++)
				normals.Add(normal);

			uvs.Add(new Vector2(0f, 0f));
			uvs.Add(new Vector2(1f, 0f));
			uvs.Add(new Vector2(1f, 1f));
			uvs.Add(new Vector2(0f, 1f));

			triangles.Add(index + 0);
			triangles.Add(index + 1);
			triangles.Add(index + 2);
			triangles.Add(index + 0);
			triangles.Add(index + 2);
			triangles.Add(index + 3);
		}

		/// <summary>Mesh of the frame, created on demand and hidden from the project.</summary>
		private Mesh EnsureMesh() {
			if (!_filter)
				_filter = GetComponent<MeshFilter>();

			if (!_mesh) {
				_mesh = new Mesh {
					name      = "Mirror Border",
					hideFlags = HideFlags.HideAndDontSave
				};
			}

			if (_filter.sharedMesh != _mesh)
				_filter.sharedMesh = _mesh;

			return _mesh;
		}
	}
}

using UnityEngine;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// Keeps a flat decal - the logo painted on the back of a <see cref="PersonalMirror"/> - at a fixed
	/// <b>world</b> size and aspect, whatever the scale of the panel it is parented to.
	/// <para>
	/// A mirror panel is built by scaling one mesh on two axes at once (a 10 x 10 plane scaled to
	/// 0.9 x 1.4 m, for instance), so a child quad inherits a different factor on each axis and the logo
	/// comes out stretched. This component reads those factors from the parent and rewrites its own local
	/// scale so the decal keeps its proportions when the panel is made wider or taller.
	/// </para>
	/// <para>
	/// It runs in the editor as well (<see cref="ExecuteAlways"/>), so resizing the panel updates the
	/// decal straight away instead of only in play mode. Nothing watches the size every frame: the component
	/// that resizes the panel asks for this refresh (see <see cref="PersonalMirror.RefreshPanel"/>).
	/// </para>
	/// </summary>
	[ExecuteAlways]
	[AddComponentMenu("Nox/Mirror/Aspect Decal")]
	[RequireComponent(typeof(MeshFilter))]
	public class AspectDecal : MonoBehaviour {
		[SerializeField, Min(0.001f), Tooltip("Width of the decal in world metres. The height follows from the aspect.")]
		private float width = 0.54f;

		[SerializeField, Min(0.001f), Tooltip("Width divided by height, kept whatever the scale of the parent. 1 is a square.")]
		private float aspect = 1f;

		[SerializeField, Min(0f), Tooltip("Free space kept between the decal and the edge of the panel, in world metres.")]
		private float padding = 0.05f;

		[SerializeField, Min(0.0001f), Tooltip("Width of the panel mesh in its own units: a Unity plane is 10 units wide, which is what makes the padding a size in metres.")]
		private float meshSize = 10f;

		/// <summary>Mesh the decal is drawn with, cached to know which axes it spans.</summary>
		private MeshFilter _filter;

		/// <summary>Width of the decal in world metres.</summary>
		public float Width {
			get => width;
			set => width = value;
		}

		/// <summary>Ratio (width / height) the decal keeps.</summary>
		public float Aspect {
			get => aspect;
			set => aspect = value;
		}

		private void OnEnable()
			=> Apply();

		/// <summary>
		/// Resizes the decal, as Unity asks when the component or its values change in the editor. Called by
		/// the mirror when it resizes its panel (see <see cref="PersonalMirror.RefreshPanel"/>).
		/// </summary>
		public void OnValidate()
			=> Apply();

		/// <summary>Resizes the decal so it measures <see cref="width"/> metres across, or less when the panel is too small.</summary>
		public void Apply() {
			if (!_filter)
				_filter = GetComponent<MeshFilter>();

			var mesh = _filter ? _filter.sharedMesh : null;
			if (!mesh)
				return;

			var local = transform.localScale;
			var lossy = transform.lossyScale;

			// World length produced by one unit of localScale, on each axis of the object. The parent
			// scale is the only part that changes when the panel is resized: the ratio stays constant.
			var unitX = Unit(lossy.x, local.x);
			var unitY = Unit(lossy.y, local.y);
			var unitZ = Unit(lossy.z, local.z);

			var bounds = mesh.bounds.size;

			// A quad and a plane both lie flat, but on different axes (XY for a quad, XZ for a plane):
			// the axis the mesh is flat on is the one to keep, the tallest of the two others is the height.
			var flatZ = bounds.z >= bounds.y;

			// The decal is shrunk when the panel is too small to hold it: it never spills over the edge.
			var size = Fit(width, width / Mathf.Max(aspect, 0.0001f));

			var next = local;

			// The mesh may already be 10 units wide: its own bounds are part of the world size.
			next.x = size.x / (unitX * Mathf.Max(bounds.x, 0.0001f));

			if (flatZ)
				next.z = size.y / (unitZ * Mathf.Max(bounds.z, 0.0001f));
			else
				next.y = size.y / (unitY * Mathf.Max(bounds.y, 0.0001f));

			if ((next - local).sqrMagnitude > 1e-10f)
				transform.localScale = next;
		}

		/// <summary>
		/// Size of the decal once it is made to fit on its panel, keeping the ratio of the size it was asked for.
		/// </summary>
		/// <param name="wantedWidth">Width asked for, in world metres.</param>
		/// <param name="wantedHeight">Height asked for, in world metres.</param>
		private Vector2 Fit(float wantedWidth, float wantedHeight) {
			var wanted = new Vector2(wantedWidth, wantedHeight);

			var panel = transform.parent;
			if (!panel)
				return wanted;

			var scale = panel.lossyScale;

			// Space left on the panel once the padding is taken on both sides.
			var freeX = Mathf.Abs(scale.x) * meshSize - 2f * padding;
			var freeZ = Mathf.Abs(scale.z) * meshSize - 2f * padding;

			if (freeX <= 1e-4f || freeZ <= 1e-4f || wanted.x <= 1e-4f || wanted.y <= 1e-4f)
				return wanted;

			var factor = Mathf.Min(1f, Mathf.Min(freeX / wanted.x, freeZ / wanted.y));

			return wanted * factor;
		}

		/// <summary>World length of one unit of <paramref name="localScale"/> on an axis.</summary>
		private static float Unit(float lossy, float local)
			=> Mathf.Abs(local) > 0.00001f
				? Mathf.Abs(lossy) / Mathf.Abs(local)
				: 1f;
	}
}

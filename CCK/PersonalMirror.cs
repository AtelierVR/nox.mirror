using Nox.CCK.Interactions;
using UnityEngine;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// The personal mirror of the player: the panel they take in front of them. The <see cref="Mirror"/> of the
	/// same object draws the reflection, this component owns the panel, its settings and its grab.
	/// <para>
	/// The grab is the one of <see cref="GrabbableBase"/>: the panel follows the grab point of the grabber that
	/// took it (see <c>PersonalMirror.Grab.cs</c>). The reflection normal is the up axis of the object, see
	/// <see cref="Mirror"/>.
	/// </para>
	/// <para>
	/// It always follows the mirror settings of the session, whatever the values saved on the panel: the size,
	/// the frame, the resolution and the reflection layers are pushed on the <see cref="Mirror"/> and on the
	/// components drawn around the glass.
	/// </para>
	/// </summary>
	[AddComponentMenu("Nox/Mirror/Personal Mirror")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Mirror))]
	public partial class PersonalMirror : GrabbableBase {
		[Header("Panel")]
		[SerializeField, Min(0.0001f), Tooltip("Width of the mesh, in its own units: a Unity plane is 10 units wide, which is what lets the size setting be a size in metres.")]
		private float meshSize = 10f;

		[Header("Release")]
		[SerializeField, Tooltip("Keep the mirror where it is once released: its body stays frozen in place instead of falling.")]
		private bool freezeOnRelease = true;

		/// <summary>Mirror of the panel, which renders the reflection.</summary>
		private Mirror _mirror;

		protected override void Awake() {
			base.Awake();

			_mirror = GetComponent<Mirror>();
			_mirror?.UseSettings();

			OnReleasedEvent.AddListener(OnReleased);

			ApplySettings();

			if (freezeOnRelease)
				FreezeBody();
		}

		private void OnDestroy()
			=> OnReleasedEvent.RemoveListener(OnReleased);

		private void OnEnable()
			=> ApplySettings();

		/// <summary>
		/// Writes the size and the frame of the settings on the panel, then lays out what is drawn on it. The
		/// resolution and the reflection layers are read by the <see cref="Mirror"/> itself.
		/// <para>
		/// The panel follows its own settings, not the values saved on the prefab: the spawner calls this again
		/// once the panel is instantiated, because instantiating writes the scale of the prefab back on it.
		/// </para>
		/// </summary>
		public void ApplySettings() {
			var size = MirrorSettings.Size;

			// The mesh spans the panel in its own units, and the parent may already be scaled: the local scale is
			// what makes a size setting a real size in metres, whatever the prefab was saved with.
			var parent = transform.parent;
			var scale  = parent ? parent.lossyScale : Vector3.one;
			var local  = transform.localScale;

			local.x = size.x / (meshSize * Mathf.Max(Mathf.Abs(scale.x), 0.0001f));
			local.z = size.y / (meshSize * Mathf.Max(Mathf.Abs(scale.z), 0.0001f));

			transform.localScale = local;

			// The frame and the decal are laid out for a size: they are rebuilt now instead of watching the panel.
			RefreshPanel();
		}

		/// <summary>Re-lays out everything drawn on the panel - the frame, the logo - for the size it has now.</summary>
		public void RefreshPanel() {
			foreach (var border in GetComponentsInChildren<MirrorBorder>(true))
				border.OnValidate();

			foreach (var decal in GetComponentsInChildren<AspectDecal>(true))
				decal.OnValidate();
		}
	}
}

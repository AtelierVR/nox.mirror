using Nox.CCK.Mirror;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Events;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Mirror {
	/// <summary>
	/// Takes the personal mirror of the player in front of their view, and keeps it for the whole session:
	/// the panel is instantiated without a parent, which <see cref="InstantiateHelper"/> turns into a
	/// <see cref="Object.DontDestroyOnLoad"/> object, so leaving the world does not take it away.
	/// <para>
	/// Nothing is kept between the calls: the panel is found where it lives (the
	/// <see cref="SceneExtensions.DontDestroyOnLoad"/> scene), and the session comes from
	/// <see cref="MirrorModule"/>. There is only ever one panel - asking for it again puts the spawned one
	/// back in front of the player instead of taking a second one.
	/// </para>
	/// </summary>
	public static class PersonalMirrorSpawner {
		/// <summary>Prefab of the panel, in the assets of this mod.</summary>
		private const string PrefabPath = "prefabs/personal_mirror.prefab";

		/// <summary>Distance between the view and the panel, in metres.</summary>
		private const float Distance = 1.1f;

		/// <summary>Raised when the session allows or refuses the personal mirror.</summary>
		public static readonly UnityEvent<bool> OnAllowedChanged = new();

		/// <summary>Raised when a panel is taken out (<c>true</c>) or removed (<c>false</c>).</summary>
		public static readonly UnityEvent<bool> OnSpawnChanged = new();

		/// <summary>Whether the session allows the personal mirror (allowed when it has no opinion).</summary>
		public static bool Allowed
			=> MirrorModule.Personal(MirrorModule.CurrentSession);

		/// <summary>Panel currently spawned, or <c>null</c>.</summary>
		public static PersonalMirror Current
			=> Find();

		/// <summary>Whether a personal mirror is spawned.</summary>
		public static bool IsSpawned
			=> Current != null;

		/// <summary>
		/// Applies the answer of the settings and of the session: the panel follows the Enable setting - taken in
		/// front of the player when it is on, removed when it is off - and a session that refuses it removes the
		/// panel whatever the setting says.
		/// </summary>
		public static void Apply() {
			var allowed = Allowed;

			if (!allowed || !MirrorSettings.Enabled)
				Despawn();
			else
				Spawn();

			OnAllowedChanged.Invoke(allowed);
		}

		/// <summary>
		/// Applies the settings again on the panel that is spawned, if any. Called by the settings: the panel
		/// does not watch them, so a size or a frame changed in the settings has to be pushed on it.
		/// </summary>
		public static void Refresh() {
			var panel = Current;
			if (panel)
				panel.ApplySettings();
		}

		/// <summary>
		/// Takes the personal mirror in front of the player, or brings the one already spawned back in front
		/// of them. Does nothing while the session refuses it.
		/// </summary>
		/// <returns>The panel, or <c>null</c> when it could not be taken.</returns>
		public static PersonalMirror Spawn() {
			if (!Application.isPlaying || !Allowed)
				return null;

			// No view yet (the client starts before the rig): a panel placed now would sit at the origin, the
			// next Apply takes it in front of the player instead.
			if (!View)
				return null;

			if (Current is { } spawned) {
				Place(spawned.transform);
				OnSpawnChanged.Invoke(true);
				return spawned;
			}

			var asset = Main.CoreAPI?.AssetAPI?.GetAsset<GameObject>(PrefabPath);
			if (!asset) {
				Logger.LogError($"Personal mirror prefab not found: {PrefabPath}");
				return null;
			}

			var panel = asset.Instantiate<PersonalMirror>();
			panel.name = $"[PersonalMirror_{panel.gameObject.GetId()}]";
			panel.ApplySettings();
			Place(panel.transform);
			OnSpawnChanged.Invoke(true);
			return panel;
		}

		/// <summary>
		/// Takes the personal mirror in front of the player, or removes the one already spawned: what a click on
		/// the widget of the player menu does. The widget is the Enable setting, so the click flips it and the
		/// panel follows.
		/// </summary>
		/// <returns>The panel when one was taken, <c>null</c> when it was removed or refused.</returns>
		public static PersonalMirror Toggle() {
			Settings.MirrorEnabled.Value = !IsSpawned;
			return Current;
		}

		/// <summary>Removes the spawned panels.</summary>
		public static void Despawn() {
			if (!Application.isPlaying)
				return;

			var scene = SceneExtensions.DontDestroyOnLoad;
			if (!scene.IsValid())
				return;

			foreach (var root in scene.GetRootGameObjects()) {
				if (!root)
					continue;

				foreach (var panel in root.GetComponentsInChildren<PersonalMirror>(true))
					panel.gameObject.Destroy();
			}
			OnSpawnChanged.Invoke(false);
		}

		/// <summary>Panel spawned in the <see cref="SceneExtensions.DontDestroyOnLoad"/> scene, or <c>null</c>.</summary>
		private static PersonalMirror Find() {
			if (!Application.isPlaying)
				return null;

			var scene = SceneExtensions.DontDestroyOnLoad;
			if (!scene.IsValid())
				return null;

			foreach (var root in scene.GetRootGameObjects()) {
				if (!root)
					continue;

				var panel = root.GetComponentInChildren<PersonalMirror>(true);
				if (panel)
					return panel;
			}

			return null;
		}

		/// <summary>Puts a panel in front of the view of the player, stood up and facing them.</summary>
		/// <param name="panel">Panel to move.</param>
		private static void Place(Transform panel) {
			var view = View;
			if (!view)
				return;

			var forward = view.forward;
			forward.y = 0f;

			// Looking straight up or down: there is no level forward, so the panel goes in front of the body.
			if (forward.sqrMagnitude < 1e-6f)
				forward = Vector3.Cross(view.right, Vector3.up);

			forward.Normalize();

			// The panel is vertical and its normal - the up axis of the object, see Mirror - points back at
			// the player, which is the pose a grab gives it (see GrabPose.Facing).
			panel.SetPositionAndRotation(
				view.position + forward * Distance,
				Quaternion.LookRotation(Vector3.up, -forward)
			);
		}

		/// <summary>View the panel is placed against: the camera of the current controller, or the main one.</summary>
		private static Transform View {
			get {
				var watched = CameraChop.WatchedCamera;
				if (watched is { Length: > 0 } && watched[0])
					return watched[0].transform;

				return Camera.main ? Camera.main.transform : null;
			}
		}
	}
}

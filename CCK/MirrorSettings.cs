using Nox.CCK.Interaction;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// Settings of the personal mirrors, shared by every <see cref="PersonalMirror"/> of the session.
	/// <para>
	/// The values, the persistence and the UI entries live in the runtime of the mirror mod
	/// (<c>Nox.Mirror.Settings</c>); this holder is what the mirror components read, so the CCK part stays
	/// independent from the settings API. A component re-reads them every frame, so a change applies to
	/// every mirror of the session without a reload.
	/// </para>
	/// </summary>
	public static class MirrorSettings {
		public const float MinimalResolution = 0.1f;
		public const float MaximalResolution = 2.0f;

		/// <summary>Longest side of a reflection texture in stereo, whatever the mirror's own max resolution is.</summary>
		public const int StereoMaxResolution = 1024;

		/// <summary>Smallest panel side, in metres.</summary>
		public const float MinimalSize = 0.2f;

		/// <summary>Largest panel side, in metres.</summary>
		public const float MaximalSize = 5f;

		public const float DefaultWidth = 0.9f;
		public const float DefaultHeight = 1.4f;

		/// <summary>
		/// Layer the mirror panel, the menus, the nameplates and the hover glow sit on: never reflected, a mirror
		/// would otherwise draw its own panel frame after frame.
		/// </summary>
		public const string UiLayerName = Layers.Ui;

		/// <summary>Layer the hover glow is drawn on (see <c>GrabbableHighlight</c>): the UI layer, so mirrors skip it too.</summary>
		public const string GlowLayerName = UiLayerName;

		/// <summary>
		/// Layers a reflection never renders, whatever the player picks in the settings: the UI, which carries the
		/// mirror panels, the menus, the nameplates and the hover glow.
		/// </summary>
		public static readonly string[] UnreflectedLayers = { UiLayerName };

		/// <summary>Scale applied to the screen resolution of the reflection (0.1 - 2).</summary>
		public static float Resolution = 1f;

		/// <summary>Whether the personal mirror is available at all: off, no panel can be taken.</summary>
		public static bool Enabled = true;

		/// <summary>Whether a grab puts the mirror on the grab point, turned towards the grabber.</summary>
		public static bool CentreOnGrab = true;

		/// <summary>Whether the personal mirror is drawn with its black frame around the glass.</summary>
		public static bool Borders = true;

		/// <summary>Whether the personal mirror is held while the input is pressed, or taken and released by two presses.</summary>
		public static GrabInputMode GrabMode = GrabInputMode.Hold;

		private static float _width = DefaultWidth;
		private static float _height = DefaultHeight;

		/// <summary>Width of the panel of every personal mirror, in metres.</summary>
		public static float Width {
			get => _width;
			set => _width = Clamp(value);
		}

		/// <summary>Height of the panel of every personal mirror, in metres.</summary>
		public static float Height {
			get => _height;
			set => _height = Clamp(value);
		}

		/// <summary>Size of the panel of every personal mirror, in metres.</summary>
		public static Vector2 Size {
			get => new Vector2(_width, _height);
			set {
				_width = Clamp(value.x);
				_height = Clamp(value.y);
			}
		}

		/// <summary>Layers every personal mirror reflects.</summary>
		public static LayerMask ReflectionLayers = -1;

		/// <summary>Clamps a panel side into the range the settings allow.</summary>
		/// <param name="size">Side, in metres.</param>
		public static float Clamp(float size)
			=> Mathf.Clamp(size, MinimalSize, MaximalSize);
	}
}

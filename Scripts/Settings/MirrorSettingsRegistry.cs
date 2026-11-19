using System;
using Nox.CCK.Mirror;
using Nox.Settings;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// The settings entries of the mirror mod, the ones shown as the "Mirror" page of the settings menu.
	/// <para>
	/// They are built once and registered in the settings mod with the main initializer, then removed when the
	/// mod is disposed: nothing else has to know which entries exist. The entries own the values; registering
	/// pushes them into <see cref="MirrorSettings"/>, which is what every mirror of the session reads.
	/// </para>
	/// </summary>
	public static class MirrorSettingsRegistry {
		/// <summary>Entries registered in the settings mod, in display order.</summary>
		private static IHandler[] _handlers = Array.Empty<IHandler>();

		/// <summary>Entries currently registered, in display order.</summary>
		public static IHandler[] Handlers
			=> _handlers;

		/// <summary>Builds the entries, adds them to the settings mod, and pushes the stored values.</summary>
		/// <param name="api">Settings registry of the session.</param>
		public static void Register(ISettingAPI api) {
			if (api == null || _handlers.Length > 0)
				return;

			// Built in display order: the "General" section first, then the "Personal" one.
			_handlers = new IHandler[] {
				new MirrorEnabled(),
				new Resolution(),
				new MirrorWidth(),
				new MirrorHeight(),
				new MirrorReflectionsSetting(),
				new MirrorBorders(),
				new MirrorCentreOnGrab(),
				new MirrorGrabMode()
			};

			foreach (var handler in _handlers)
				api.Add(handler);

			Push();
		}

		/// <summary>
		/// Pushes the stored values where the mirrors of the session read them. The reflections push their own
		/// mask when they are built (and on every change), they only need their rows here.
		/// </summary>
		public static void Push() {
			MirrorSettings.Enabled      = MirrorEnabled.Value;
			MirrorSettings.Resolution   = Resolution.Value;
			MirrorSettings.Width        = MirrorWidth.Value;
			MirrorSettings.Height       = MirrorHeight.Value;
			MirrorSettings.CentreOnGrab = MirrorCentreOnGrab.Value;
			MirrorSettings.GrabMode     = MirrorGrabMode.Value;
			MirrorSettings.Borders      = MirrorBorders.Value;
		}

		/// <summary>Removes the entries from the settings mod.</summary>
		/// <param name="api">Settings registry of the session.</param>
		public static void Unregister(ISettingAPI api) {
			if (api != null) {
				foreach (var handler in _handlers)
					api.Remove(handler.Path);
			}

			_handlers = Array.Empty<IHandler>();
		}
	}
}

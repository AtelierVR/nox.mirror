using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Whether a grab moves the personal mirror so the grab point - the cursor of the grab - lands on the
	/// centre of the panel, and turns it so the mirror looks back at its grab origin.
	/// <para>
	/// Off, the mirror simply rides the grab point like any prop, keeping the pose it had in the grip.
	/// </para>
	/// </summary>
	public sealed class MirrorCentreOnGrab : ToggleHandler {
		public override string[] Path
			=> new[] { "mirror", "personal", "centre_on_grab" };

		public override int Order
			=> 45010;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "centre_on_grab" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/toggle.prefab");

		public MirrorCentreOnGrab() {
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValue(Value, notify: false);
		}

		public static new bool Value {
			get => Config.Load().Get(GetConfigPath(), MirrorSettings.CentreOnGrab);
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				MirrorSettings.CentreOnGrab = value;
				config.Save();
			}
		}

		protected override void OnValueChanged(bool value)
			=> Value = value;
	}
}

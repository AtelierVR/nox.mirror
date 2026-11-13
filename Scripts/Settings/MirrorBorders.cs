using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Whether the personal mirror is drawn with its black frame around the glass.
	/// <para>
	/// Off, the frame is kept but hidden, so the settings keep their state when it is turned back on.
	/// </para>
	/// </summary>
	public sealed class MirrorBorders : ToggleHandler {
		public override string[] Path
			=> new[] { "mirror", "personal", "borders" };

		public override int Order
			=> 45004;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "borders" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/toggle.prefab");

		public MirrorBorders() {
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValue(Value, notify: false);
		}

		public static new bool Value {
			get => Config.Load().Get(GetConfigPath(), MirrorSettings.Borders);
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				MirrorSettings.Borders = value;
				PersonalMirrorSpawner.Refresh();
				config.Save();
			}
		}

		protected override void OnValueChanged(bool value)
			=> Value = value;
	}
}

using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Width of the panel of every personal mirror, in metres.
	/// <para>
	/// The value is stored in the config and pushed to <see cref="MirrorSettings"/>, then the spawned panel is
	/// asked to apply the settings again: the slider resizes the mirror of the session at once.
	/// </para>
	/// </summary>
	public sealed class MirrorWidth : RangeHandler {
		public override string[] Path
			=> new[] { "mirror", "personal", "width" };

		public override int Order
			=> 45001;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "width" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public MirrorWidth() {
			SetRange(MirrorSettings.MinimalSize, MirrorSettings.MaximalSize);
			SetStep(0.01f);
			SetValue(Value, notify: false);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValueKey("settings.range.value.float_meters");
		}

		public static new float Value {
			get => MirrorSettings.Clamp(Config.Load().Get(GetConfigPath(), MirrorSettings.DefaultWidth));
			set {
				var size = MirrorSettings.Clamp(value);
				if (Mathf.Approximately(size, Value))
					return;

				Config.Load().Set(GetConfigPath(), size);
				MirrorSettings.Width = size;
				PersonalMirrorSpawner.Refresh();

				ConfigDebounce.Save();
			}
		}

		protected override void OnValueChanged(float value)
			=> Value = value;
	}
}

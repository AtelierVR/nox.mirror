using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Mirror.Settings {
	public sealed class Resolution : RangeHandler {
		public override string[] GetPath()
			=> new[] { "graphic", "mirror", "resolution" };

		private static string[] GetConfigPath()
			=> new[] { "settings", "graphic", "mirror", "resolution" };

		public override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public Resolution() {
			SetRange(MirrorSettings.MinimalResolution, MirrorSettings.MaximalResolution);
			SetStep(0.001f);
			SetValue(Value);
			SetLabelKey($"settings.entry.{string.Join(".", GetPath())}.label");
			SetValueKey("settings.range.value.percent");
		}

		public static float Value {
			get => Config.Load().Get(GetConfigPath(), MirrorSettings.Resolution);
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				MirrorSettings.Resolution = value;
				config.Save();
			}
		}

		public override void OnValueChanged(float value) {
			Value = value;
		}
	}
}
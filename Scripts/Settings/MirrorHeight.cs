using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Height of the panel of every personal mirror, in metres.
	/// <para>
	/// The value is stored in the config and pushed to <see cref="MirrorSettings"/>, then the spawned panel is
	/// asked to apply the settings again: the slider resizes the mirror of the session at once.
	/// </para>
	/// </summary>
	public sealed class MirrorHeight : RangeHandler {
		public override string[] Path
			=> new[] { "mirror", "personal", "height" };

		public override int Order
			=> 45002;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "height" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/range.prefab");

		public MirrorHeight() {
			SetRange(MirrorSettings.MinimalSize, MirrorSettings.MaximalSize);
			SetStep(0.01f);
			SetValue(Value, notify: false);
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValueKey("settings.range.value.float_meters");
		}

		public static new float Value {
			get => MirrorSettings.Clamp(Config.Load().Get(GetConfigPath(), MirrorSettings.DefaultHeight));
			set {
				var size = MirrorSettings.Clamp(value);
				if (Mathf.Approximately(size, Value))
					return;

				Config.Load().Set(GetConfigPath(), size);
				MirrorSettings.Height = size;
				PersonalMirrorSpawner.Refresh();

				ConfigDebounce.Save();
			}
		}

		protected override void OnValueChanged(float value)
			=> Value = value;
	}
}

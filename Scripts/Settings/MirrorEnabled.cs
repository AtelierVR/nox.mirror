using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Whether the personal mirror is on: turning it on takes a panel in front of the player, turning it off
	/// removes it. The widget of the player menu is the same switch.
	/// <para>
	/// A map that refuses the personal mirrors leaves the setting visible but not editable, so it cannot be
	/// turned on there.
	/// </para>
	/// </summary>
	public sealed class MirrorEnabled : ToggleHandler {
		public override string[] Path
			=> new[] { "mirror", "general", "enabled" };

		public override int Order
			=> 44000;

		public override bool IsEditable
			=> PersonalMirrorSpawner.Allowed;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "general", "enabled" };

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/toggle.prefab");

		public MirrorEnabled() {
			_current = this;
			SetLabelKey($"settings.entry.{string.Join(".", Path)}.label");
			SetValue(Value, notify: false);
		}

		/// <summary>Row registered in the settings page, so a change made elsewhere (the widget) is shown on it.</summary>
		private static MirrorEnabled _current;

		public static new bool Value {
			get => Config.Load().Get(GetConfigPath(), MirrorSettings.Enabled);
			set {
				if (!PersonalMirrorSpawner.Allowed)
					return;

				var config = Config.Load();
				config.Set(GetConfigPath(), value);
				MirrorSettings.Enabled = value;
				PersonalMirrorSpawner.Apply();
				_current?.SetValue(value, notify: false);
				config.Save();
			}
		}

		public override GameObject GetContent(RectTransform transform, IMenu menu) {
			var content = base.GetContent(transform, menu);

			PersonalMirrorSpawner.OnAllowedChanged.AddListener(OnAllowedChanged);

			return content;
		}

		protected override void OnDestroy() {
			PersonalMirrorSpawner.OnAllowedChanged.RemoveListener(OnAllowedChanged);

			if (_current == this)
				_current = null;

			base.OnDestroy();
		}

		/// <summary>The session changed its answer: the toggle is greyed out where the mirrors are refused.</summary>
		private void OnAllowedChanged(bool allowed)
			=> RefreshEditable();

		protected override void OnValueChanged(bool value)
			=> Value = value;
	}
}

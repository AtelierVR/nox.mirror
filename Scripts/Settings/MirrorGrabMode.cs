using System.Collections.Generic;
using Nox.CCK.Interaction;
using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// How the personal mirror is grabbed: <see cref="GrabInputMode.Hold"/> carries it while the input is
	/// pressed, <see cref="GrabInputMode.Toggle"/> takes it on a press and releases it on the next one.
	/// <para>
	/// The grabbers read it from the mirror itself (see <see cref="IGrabInputMode"/>), so the setting works
	/// the same with the desktop cursor, a near/far ray or a hand.
	/// </para>
	/// </summary>
	public sealed class MirrorGrabMode : DropdownHandler {
		public override string[] Path
			=> new[] { "mirror", "personal", "grab_mode" };

		public override int Order
			=> 45011;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "grab_mode" };

		public MirrorGrabMode() {
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");
			SetOptions(GetOptions());
			SetValue(Key(Value), notify: false);
		}

		private static Dictionary<string, string[]> GetOptions()
			=> new() {
				[Key(GrabInputMode.Hold)]   = new[] { "settings.entry.mirror.personal.grab_mode.option.hold" },
				[Key(GrabInputMode.Toggle)] = new[] { "settings.entry.mirror.personal.grab_mode.option.toggle" }
			};

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/dropdown.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Main.CoreAPI?.ModAPI?.GetMod("ui")?.GetInstance<IUiAPI>()?.MakeModal(menu);

		protected override void OnValueChanged(string value)
			=> Value = Parse(value);

		/// <summary>Config key of a mode.</summary>
		/// <param name="value">Grab mode.</param>
		public static string Key(GrabInputMode value)
			=> value == GrabInputMode.Toggle ? "toggle" : "hold";

		/// <summary>Mode stored under a config key: an unknown key falls back on hold.</summary>
		/// <param name="key">Stored key.</param>
		public static GrabInputMode Parse(string key)
			=> key == "toggle" ? GrabInputMode.Toggle : GrabInputMode.Hold;

		public static new GrabInputMode Value {
			get => Parse(Config.Load().Get(GetConfigPath(), Key(MirrorSettings.GrabMode)));
			set {
				var config = Config.Load();
				config.Set(GetConfigPath(), Key(value));
				MirrorSettings.GrabMode = value;
				config.Save();
			}
		}
	}
}

using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Mirror;
using Nox.CCK.Settings;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.modals;
using UnityEngine;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Layers the personal mirrors reflect: a switch for everything at once, then one switch per layer.
	/// <para>
	/// The rows are the layers themselves, read from the project (see <see cref="Layers"/>): what is switched
	/// on is reflected, what is off is hidden from the mirror. The mask is stored as the list of the chosen
	/// rows and pushed to <see cref="MirrorSettings"/>, which every mirror of the session reads.
	/// </para>
	/// </summary>
	public sealed class MirrorReflectionsSetting : MultiChoiceHandler {
		/// <summary>Row that reflects every layer the list offers, instead of one of them.</summary>
		private const string AllKey = "all";

		public override string[] Path
			=> new[] { "mirror", "personal", "reflections" };

		public override int Order
			=> 45003;

		private static string[] GetConfigPath()
			=> new[] { "settings", "mirror", "personal", "reflections" };

		public MirrorReflectionsSetting() {
			SetLabel($"settings.entry.{string.Join(".", Path)}.label");

			// First row reflects everything at once, then one switch per layer, in layer index order:
			// the key is the index, the label the name.
			AddChoice(AllKey, "settings.multi.all");

			foreach (var layer in ListedLayers())
				AddChoice(layer.Value.ToString(), "settings.mirror.multi.option", layer.Key);

			Apply(Config.Load().Get(GetConfigPath(), AllKey));
		}

		protected override GameObject GetPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/button.prefab");

		protected override GameObject GetRowPrefab()
			=> Main.CoreAPI.AssetAPI.GetAsset<GameObject>("settings:prefabs/toggle.prefab");

		protected override IModalBuilder GetModalBuilder(IMenu menu)
			=> Main.CoreAPI?.ModAPI?.GetMod("ui")?.GetInstance<IUiAPI>()?.MakeModal(menu);

		/// <summary>
		/// Layers the list offers: the ones of the project (see <see cref="Layers.GetVisibleLayers"/>) without the
		/// ones a reflection must never show - the mirror panel, the nameplates and the menus (UI) and the hover
		/// glow, which exist only for the camera of the player (see <see cref="MirrorSettings.UnreflectedLayers"/>).
		/// </summary>
		private static IEnumerable<KeyValuePair<string, int>> ListedLayers()
			=> Layers.GetVisibleLayers()
				.Where(pair => !MirrorSettings.UnreflectedLayers.Contains(pair.Key));

		/// <summary>Writes the mask of the chosen rows where the mirrors read it, and stores the choice.</summary>
		protected override void OnSelectionChanged() {
			var mask = Selected.Aggregate(0, (current, key) => current | MaskOf(key));

			var config = Config.Load();
			config.Set(GetConfigPath(), Value);
			MirrorSettings.ReflectionLayers = mask;
			config.Save();
		}

		/// <summary>Layer mask of a row: every layer of the list for the "everything" row, one bit otherwise.</summary>
		/// <param name="key">Key of the row.</param>
		private static int MaskOf(string key) {
			if (key == AllKey)
				return ListedLayers()
					.Aggregate(0, (mask, layer) => mask | 1 << layer.Value);
			return int.TryParse(key, out var index)
				? 1 << index
				: 0;
		}
	}
}

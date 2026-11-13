using Cysharp.Threading.Tasks;
using Nox.CCK.Theme;
using Nox.CCK.Utils;
using Nox.UI;
using Nox.UI.Widgets;
using UnityEngine;
using UnityEngine.UI;

namespace Nox.Mirror {
	/// <summary>
	/// Widget of the player menu that takes the personal mirror in front of the player (see
	/// <see cref="PersonalMirrorSpawner"/>).
	/// <para>
	/// The panel lives in <see cref="Object.DontDestroyOnLoad"/> and is unique: a click takes it in front of the
	/// player, the next one removes it. The button is greyed out when the session refuses the personal mirror
	/// (the <c>mirror.personal</c> flag of its data) and while a spawned panel listens to that flag. While a
	/// panel is out, the tile keeps the accent colour of a selected item.
	/// </para>
	/// </summary>
	public class MirrorWidget : MonoBehaviour, IWidget {
		/// <summary>Opacity of the tile while the panel is out: the strength of a selected item.</summary>
		private const float ACTIVE_ALPHA = 0.149f;

		public static string GetDefaultKey()
			=> "personalMirror";

		private Button _button;
		private GameObject _content;

		/// <summary>Colours of the button as the prefab gives them, put back when the panel is removed.</summary>
		private ColorBlock _colors;

		public string GetKey()
			=> GetDefaultKey();

		public Vector2Int GetSize()
			=> Vector2Int.one;

		public int GetPriority()
			=> 95;

		private void OnClick()
			=> PersonalMirrorSpawner.Toggle();

		private void OnEnable() {
			PersonalMirrorSpawner.OnAllowedChanged.AddListener(OnAllowedChanged);
			PersonalMirrorSpawner.OnSpawnChanged.AddListener(OnSpawnChanged);
			ThemeManager.OnThemeChanged.AddListener(OnThemeChanged);
			RefreshState();
		}

		private void OnDisable() {
			PersonalMirrorSpawner.OnAllowedChanged.RemoveListener(OnAllowedChanged);
			PersonalMirrorSpawner.OnSpawnChanged.RemoveListener(OnSpawnChanged);
			ThemeManager.OnThemeChanged.RemoveListener(OnThemeChanged);
		}

		/// <summary>The session changed its answer: the button follows it.</summary>
		/// <param name="allowed">Whether the personal mirror is allowed.</param>
		private void OnAllowedChanged(bool allowed) {
			if (_button)
				_button.interactable = allowed;
		}

		/// <summary>The panel was taken out or removed: the tile follows it.</summary>
		/// <param name="spawned">Whether a panel is out.</param>
		private void OnSpawnChanged(bool spawned) {
			if (!_button)
				return;

			var colors = Themed();

			if (!spawned) {
				_button.colors = colors;
				Fade(colors.normalColor);
				return;
			}

			var active = colors.selectedColor != colors.normalColor
				? colors.selectedColor
				: Accent();

			colors.normalColor = active;
			colors.selectedColor = active;
			colors.highlightedColor = WithAlpha(active, active.a * 1.6f);
			colors.pressedColor = WithAlpha(active, active.a * 2.2f);
			_button.colors = colors;
			Fade(active);
		}

		/// <summary>The theme changed: the tile is painted from the colours of the theme, so it is repainted.</summary>
		/// <param name="_">Id of the new theme.</param>
		private void OnThemeChanged(string _)
			=> OnSpawnChanged(PersonalMirrorSpawner.IsSpawned);

		/// <summary>Colours of the prefab with the theme of the session applied.</summary>
		private ColorBlock Themed() {
			var theme = ThemeManager.Current;
			var colors = _colors;

			colors.normalColor = theme.Map(colors.normalColor);
			colors.highlightedColor = theme.Map(colors.highlightedColor);
			colors.pressedColor = theme.Map(colors.pressedColor);
			colors.selectedColor = theme.Map(colors.selectedColor);
			colors.disabledColor = theme.Map(colors.disabledColor);

			return colors;
		}

		/// <summary>Accent of the theme, at the opacity of a selected item.</summary>
		private static Color Accent() {
			var accent = ThemeManager.Current.Map(ThemeAsset.Accent);
			accent.a = ACTIVE_ALPHA;
			return accent;
		}

		/// <summary>Same colour, another opacity, clamped to the unit range.</summary>
		private static Color WithAlpha(Color color, float alpha)
			=> new(color.r, color.g, color.b, Mathf.Clamp01(alpha));

		/// <summary>Paints the target of the button now, without waiting for a state change.</summary>
		/// <param name="color">Colour of that state.</param>
		private void Fade(Color color)
			=> _button.targetGraphic?.CrossFadeColor(color * _button.colors.colorMultiplier, 0f, true, true);


		/// <summary>Aligns the button on the current state (no polling, called on enable).</summary>
		private void RefreshState() {
			if (_button)
				_button.interactable = PersonalMirrorSpawner.Allowed;
			OnSpawnChanged(PersonalMirrorSpawner.IsSpawned);
		}

		public static bool TryMake(IMenu menu, RectTransform parent, out (GameObject, IWidget) values) {
			var prefab = Main.CoreAPI?.AssetAPI?.GetAsset<GameObject>("ui:prefabs/grid_item.prefab");
			var instance = prefab ? Instantiate(prefab, parent) : null;
			if (!instance) {
				values = (null, null);
				return false;
			}

			var component = instance.AddComponent<MirrorWidget>();
			component._button = Reference.GetComponent<Button>("button", instance);

			if (component._button) {
				component._colors = component._button.colors;
				component._button.onClick.AddListener(component.OnClick);
				component._button.interactable = PersonalMirrorSpawner.Allowed;
			}

			instance.name = $"[{component.GetKey()}_{instance.GetId()}]";
			values = (instance, component);

			prefab = Main.CoreAPI?.AssetAPI?.GetAsset<GameObject>("ui:prefabs/widget.prefab");
			if (prefab)
				component._content = Instantiate(prefab, Reference.GetComponent<RectTransform>("content", instance));

			component.RefreshState();
			component.UpdateIcon().Forget();
			return true;
		}

		private async UniTask UpdateIcon() {
			if (!_content)
				return;

			var icon = await Main.CoreAPI.AssetAPI.GetAssetAsync<Sprite>("ui:icons/mirror.png");
			var labelIcon = Reference.GetComponent<Image>("icon", _content);
			if (labelIcon)
				labelIcon.sprite = icon;
		}
	}
}

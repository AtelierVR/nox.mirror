using System;
using Nox.CCK.Language;
using Nox.CCK.Mirror;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Mirror.Settings;
using Nox.Settings;

namespace Nox.Mirror {
	public class Main : IMainModInitializer {
		static internal IHandler[] Settings = Array.Empty<IHandler>();
		public static IMainModCoreAPI CoreAPI;
		private LanguagePack _lang;

		public static ISettingAPI SettingAPI
			=> CoreAPI.ModAPI
				.GetMod("settings")
				.GetInstance<ISettingAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_lang   = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);

			Settings = new IHandler[] {
				new Resolution()
			};

			foreach (var setting in Settings)
				SettingAPI.Add(setting);

			MirrorSettings.Resolution = Resolution.Value;
		}

		public void OnDisposeMain() {
			foreach (var setting in Settings)
				SettingAPI.Remove(setting.GetPath());
			Settings = Array.Empty<IHandler>();
			LanguageManager.RemovePack(_lang);
			CoreAPI = null;
		}
	}
}
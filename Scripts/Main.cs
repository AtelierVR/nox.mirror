using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.Mirror.Settings;
using Nox.Scripting;
using Nox.Settings;

namespace Nox.Mirror {
	public class Main : IMainModInitializer {
		public static IMainModCoreAPI CoreAPI;
		private LanguagePack _lang;

		public static ISettingAPI SettingAPI
			=> CoreAPI.ModAPI
				.GetMod("settings")
				.GetInstance<ISettingAPI>();

		/// <summary>Registry the <c>mirror</c> scripting module is declared in.</summary>
		public static IScriptingAPI ScriptingAPI
			=> CoreAPI?.ModAPI
				?.GetMod("scripting")
				?.GetInstance<IScriptingAPI>();

		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI = api;
			_lang   = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_lang);
			MirrorSettingsRegistry.Register(SettingAPI);
			ScriptingAPI?.RegisterModule(MirrorModule.Module);
		}

		public void OnDisposeMain() {
			ScriptingAPI?.UnregisterModule(MirrorModule.Module);
			ConfigDebounce.Flush();
			MirrorSettingsRegistry.Unregister(SettingAPI);
			LanguageManager.RemovePack(_lang);
			CoreAPI = null;
		}
	}
}

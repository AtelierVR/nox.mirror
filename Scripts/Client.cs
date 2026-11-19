using System;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.Sessions;
using Nox.UI;
using UnityEngine;

namespace Nox.Mirror {
	/// <summary>
	/// Client initializer of the mirror mod: adds the personal mirror to the widgets of the player menu, and
	/// watches the sessions so a session that refuses the personal mirror has its panel removed.
	/// </summary>
	public class Client : IClientModInitializer {
		/// <summary>Core API of this mod, needed to fetch the assets of the menu.</summary>
		static internal IClientModCoreAPI API { get; private set; }

		private EventSubscription[] _events = Array.Empty<EventSubscription>();

		public void OnInitializeClient(IClientModCoreAPI api) {
			API = api;
			_events = new[] {
				API.EventAPI.Subscribe("widget_request", OnWidgetRequest),
				API.EventAPI.Subscribe("session_current_changed", OnSessionCurrentChanged)
			};

			// Nothing is emitted for the session that is already current when the mod loads.
			Watch(MirrorModule.CurrentSession, null);
		}

		public void OnDisposeClient() {
			foreach (var subscription in _events)
				API?.EventAPI?.Unsubscribe(subscription);

			MirrorModule.CurrentSession?.Data.OnChanged.RemoveListener(OnDataChanged);

			_events = Array.Empty<EventSubscription>();
			API     = null;
		}

		/// <summary>The session of the player changed: the panel follows the rules of the new one.</summary>
		/// <param name="context">Event data: the previous session, then the new one.</param>
		private static void OnSessionCurrentChanged(EventData context) {
			context.TryGet(0, out ISession previous);
			context.TryGet(1, out ISession current);

			Watch(current, previous);
		}

		/// <summary>
		/// Moves the data listener to the session that is now current, and applies its answer to the spawned panel.
		/// </summary>
		/// <param name="current">Session now current, <c>null</c> outside a session.</param>
		/// <param name="previous">Session that was current, <c>null</c> on the first call.</param>
		private static void Watch(ISession current, ISession previous) {
			if (previous != null)
				previous.Data.OnChanged.RemoveListener(OnDataChanged);

			if (current != null)
				current.Data.OnChanged.AddListener(OnDataChanged);

			PersonalMirrorSpawner.Apply();
		}

		/// <summary>The current session changed one of its values: only the personal mirror flag matters.</summary>
		/// <param name="key">Path of the value.</param>
		/// <param name="value">New value.</param>
		/// <param name="previous">Previous value.</param>
		private static void OnDataChanged(string[] key, object value, object previous) {
			if (MirrorModule.IsPersonal(key) && value is bool)
				PersonalMirrorSpawner.Apply();
		}

		/// <summary>Answers the widget request of the player menu with the personal mirror button.</summary>
		/// <param name="context">Request: menu id and the container the widgets go in.</param>
		private static void OnWidgetRequest(EventData context) {
			if (!context.TryGet(0, out int menuId))
				return;
			if (!context.TryGet(1, out RectTransform parent))
				return;

			var menu = API?.ModAPI
				?.GetMod("ui")
				?.GetInstance<IUiAPI>()
				?.Get<IMenu>(menuId);
			if (MirrorWidget.TryMake(menu, parent, out var widget))
				context.Callback(widget.Item2, widget.Item1);
		}
	}
}

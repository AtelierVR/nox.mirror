using System;
using Nox.CCK.Scripting;
using Nox.Scripting;
using Nox.Sessions;

namespace Nox.Mirror {
	/// <summary>
	/// Scripting module <c>"mirror"</c> - whether the players of the current session may take a personal
	/// mirror.
	/// <code>
	/// import mirror from 'mirror';
	///
	/// mirror.personal = false;   // no personal mirror in this session
	/// mirror.personal;           // true by default
	/// </code>
	/// <para>
	/// The value lives in the session <see cref="ISession.Data"/>: a world script that refuses the mirrors
	/// makes the spawned panel disappear and greys the widget of the player menu out.
	/// </para>
	/// </summary>
	public static class MirrorModule {
		/// <summary>Key of the flag in the session data.</summary>
		internal static readonly string[] PersonalPath = { "mirror", "personal" };

		/// <summary>
		/// Whether the session allows the personal mirror, allowed when it says nothing or when there is no
		/// session yet.
		/// </summary>
		/// <param name="session">Session to read, may be <c>null</c>.</param>
		internal static bool Personal(ISession session)
			=> session == null || session.Data.Get(PersonalPath, true);

		/// <summary>Tells the session whether its players may take a personal mirror.</summary>
		/// <param name="session">Session to write, ignored when <c>null</c>.</param>
		/// <param name="value">Whether the personal mirror is allowed.</param>
		internal static void SetPersonal(ISession session, bool value)
			=> session?.Data.Set(PersonalPath, value);

		/// <summary>Whether a changed value of the session data is the personal mirror flag.</summary>
		/// <param name="key">Path of the changed value.</param>
		internal static bool IsPersonal(string[] key) {
			if (key == null || key.Length != PersonalPath.Length)
				return false;

			for (var i = 0; i < PersonalPath.Length; i++)
				if (key[i] != PersonalPath[i])
					return false;

			return true;
		}

		/// <summary>Registry of the sessions of the client.</summary>
		internal static ISessionAPI SessionAPI
			=> Main.CoreAPI?.ModAPI
				?.GetMod("sessions")
				?.GetInstance<ISessionAPI>();

		/// <summary>Session the player is in, or <c>null</c> outside a session.</summary>
		internal static ISession CurrentSession {
			get {
				var api = SessionAPI;
				if (api == null || string.IsNullOrEmpty(api.Current))
					return null;

				return api.TryGet(api.Current, out var session) ? session : null;
			}
		}

		public static readonly IScriptingModuleDefinition Module =
			ScriptingModuleBuilder.Create("mirror")
				.WithTags("session")
				.AddVariable(
					"personal",
					ctx => Personal(ctx.Session),
					(ctx, value) => {
						if (value is not bool allowed)
							throw new Exception($"mirror.personal must be a boolean, not {value?.GetType().Name ?? "null"}");
						SetPersonal(ctx.Session, allowed);
					}
				).Build();
	}
}

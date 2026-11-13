using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Mirror.Settings {
	/// <summary>
	/// Writes the config once the value stops moving. A slid value changes on every frame of the drag, and
	/// rewriting the file each time costs more than the setting itself (0.5 ms of disk write per tick).
	/// </summary>
	internal static class ConfigDebounce {
		/// <summary>Quiet time before the file is written, in milliseconds.</summary>
		private const int DELAY = 300;

		private static CancellationTokenSource _token;

		/// <summary>Writes the config once nothing changed for <see cref="DELAY"/> ms.</summary>
		public static void Save() {
			_token?.Cancel();
			_token?.Dispose();
			_token = new CancellationTokenSource();
			SaveAsync(_token.Token).Forget();
		}

		/// <summary>Writes the config now, cancelling a save that was waiting.</summary>
		public static void Flush() {
			_token?.Cancel();
			_token?.Dispose();
			_token = null;
			Config.Load().Save();
		}

		private static async UniTaskVoid SaveAsync(CancellationToken token) {
			try {
				await UniTask.Delay(DELAY, DelayType.Realtime, cancellationToken: token);
			} catch (OperationCanceledException) {
				return;
			}

			Config.Load().Save();
		}
	}
}

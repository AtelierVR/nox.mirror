using Nox.CCK.Interactions;
using Nox.Interactions;
using UnityEngine;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// Grab side of the <see cref="PersonalMirror"/>: where the panel goes while it is held, and how it stops
	/// when it is let go.
	/// </summary>
	public partial class PersonalMirror {
		/// <summary>
		/// The panel places itself on its grab point (see <see cref="FollowPoint"/>), so its body is written directly instead
		/// of being driven by the hold servo, which would only chase the grip.
		/// </summary>
		protected override bool KeepBodyDynamic
			=> false;

		/// <summary>
		/// With <see cref="MirrorSettings.CentreOnGrab"/> the mirror is put on the grab point - the cursor of the
		/// ray - and turned so its normal (the up axis, see <see cref="Mirror"/>) points back at the grabber:
		/// the centre of the mirror is under the cursor and the mirror looks at its grab origin.
		/// <para>
		/// Only a ray does that: a hand keeps the grip it caught it with, so the panel stays in the palm where it
		/// was taken and rides it like any prop (see <see cref="GrabbableBase"/>).
		/// </para>
		/// </summary>
		protected override void FollowPoint(Transform point, float deltaTime) {
			if (!MirrorSettings.CentreOnGrab || !FromRay) {
				base.FollowPoint(point, deltaTime);
				return;
			}

			if (!point)
				return;

			transform.SetPositionAndRotation(point.position, Facing(point, transform.rotation));
		}

		/// <summary>Whether the panel is carried at a distance by a ray, the grab that has a cursor to centre on.</summary>
		private bool FromRay
			=> Driver is { Kind: GrabKind.Indirect };

		/// <summary>Stops the body where it is, so the mirror stays in place instead of falling.</summary>
		private void FreezeBody() {
			var body = Body;
			if (!body)
				return;

			// A kinematic body rejects velocity writes: zero them before freezing it.
			if (!body.isKinematic) {
				body.linearVelocity  = Vector3.zero;
				body.angularVelocity = Vector3.zero;
			}

			body.useGravity  = false;
			body.isKinematic = true;
		}

		/// <summary>A mirror floats where it is: the body must not fall once the last grabber lets go.</summary>
		private void OnReleased(GrabbableBase mirror, GrabberBase grabber) {
			if (freezeOnRelease)
				FreezeBody();
		}

		/// <summary>
		/// Rotation of a panel whose up axis points back at the grab origin: the grab point looks away from the
		/// grabber, the mirror has to face it while keeping a vertical it can be read with.
		/// </summary>
		private static Quaternion Facing(Transform point, Quaternion fallback) {
			if (!point)
				return fallback;

			var axis = -point.forward;

			if (!TryVertical(axis, Vector3.up, out var vertical)
			    && !TryVertical(axis, point.up, out vertical)
			    && !TryVertical(axis, point.right, out vertical))
				return fallback;

			return Quaternion.LookRotation(vertical, axis);
		}

		/// <summary>Vertical of a panel whose up axis is <paramref name="axis"/>, taken from a reference direction.</summary>
		private static bool TryVertical(Vector3 axis, Vector3 reference, out Vector3 vertical) {
			vertical = Vector3.ProjectOnPlane(reference, axis);

			if (vertical.sqrMagnitude < 0.0001f) {
				vertical = Vector3.zero;
				return false;
			}

			vertical.Normalize();
			return true;
		}
	}
}

using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace Nox.CCK.Mirror {
	/// <summary>
	/// A high-quality planar mirror for URP with XR (VR/AR) support.
	/// The mirror uses transform.up as the reflection normal.
	/// For a vertical mirror, rotate the object so Y (green arrow) points toward the viewer.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(Renderer))]
	public class Mirror : MonoBehaviour {
		[Header("Mirror Settings")]
		[SerializeField, Tooltip("Take the resolution from the mirror settings of the session. Turn it off to keep the resolution of this mirror only (a world creator's choice) - a Personal Mirror ignores the switch and always follows the settings.")]
		private bool _useResolutionSettings = true;

		[Range(MirrorSettings.MinimalResolution, MirrorSettings.MaximalResolution)]
		[SerializeField] private float _resolutionScale = MirrorSettings.Resolution;

		[SerializeField, Tooltip("Take the reflection layers from the mirror settings of the session. Turn it off to keep the layers of this mirror only (a world creator's choice) - a Personal Mirror ignores the switch and always follows the settings.")]
		private bool _useReflectionLayersSettings = true;

		[SerializeField, Tooltip("Longest side of the reflection textures, in pixels.")]
		private int _maxResolution = 2048;

		[SerializeField, Tooltip("Layers the reflection camera renders when the mirror does not take them from the settings.")]
		private LayerMask _reflectionLayers = -1;

		/// <summary>
		/// Scale applied to the screen resolution of the reflection: the one of <see cref="MirrorSettings"/> when
		/// <c>_useResolutionSettings</c> is on, the local value otherwise.
		/// </summary>
		public virtual float ResolutionScale
			=> _useResolutionSettings
				? MirrorSettings.Resolution
				: _resolutionScale;

		/// <summary>
		/// Layers the reflection camera renders: the ones of <see cref="MirrorSettings"/> when
		/// <c>_useReflectionLayersSettings</c> is on, the local mask otherwise.
		/// </summary>
		public virtual LayerMask ReflectionLayers
			=> _useReflectionLayersSettings
				? MirrorSettings.ReflectionLayers
				: _reflectionLayers;

		[Header("Performance")]
		[SerializeField] private bool _disablePixelLights = true;

		[Header("Rendering")]
		[SerializeField] private float _clipPlaneOffset = 0.07f;

		// Private fields
		private Camera _mirrorCamera;
		private RenderTexture _reflectionTextureLeft;
		private RenderTexture _reflectionTextureRight;
		private Renderer _renderer;
		private Material _mirrorMaterial;
		private MaterialPropertyBlock _propertyBlock;

		private bool _texturesAssigned;
		private int _lastTextureWidth;
		private int _lastTextureHeight;

		private static readonly int LeftEyeTextureId = Shader.PropertyToID("_LeftEyeTexture");
		private static readonly int RightEyeTextureId = Shader.PropertyToID("_RightEyeTexture");

		private static bool s_IsRendering;
		private const string MirrorShaderName = "Nox/MirrorShader";

		/// <summary>Frustum planes of the camera being rendered, reused (CalculateFrustumPlanes allocates).</summary>
		private readonly Plane[] _frustum = new Plane[6];

		/// <summary>
		/// Forces the settings of the session on this mirror, whatever its own switches say: what the personal
		/// mirror needs, since the sliders of the settings are the only thing that drives it.
		/// </summary>
		/// <param name="resolution">Whether the resolution follows the settings.</param>
		/// <param name="layers">Whether the reflection layers follow the settings.</param>
		public void UseSettings(bool resolution = true, bool layers = true) {
			_useResolutionSettings = resolution;
			_useReflectionLayersSettings = layers;
		}

		protected virtual void OnEnable() {
			_renderer = GetComponent<Renderer>();
			_propertyBlock = new MaterialPropertyBlock();

			EnsureMirrorMaterial();

			RenderOrder.Subscribe(OnBeginCameraRendering, RenderPhase.Capture);
		}

		private void OnDisable() {
			RenderOrder.Unsubscribe(OnBeginCameraRendering);
			Cleanup();
		}

		private void EnsureMirrorMaterial() {
			if (_renderer == null)
				return;

			var currentMat = _renderer.sharedMaterial;
			if (currentMat != null && currentMat.shader != null &&
				currentMat.shader.name == MirrorShaderName)
				return;

			var shader = Shader.Find(MirrorShaderName);
			if (shader == null) {
				Debug.LogError($"[Mirror] Shader '{MirrorShaderName}' not found!");
				return;
			}

			_mirrorMaterial = new Material(shader) {
				name = "Mirror Material",
				hideFlags = HideFlags.DontSave
			};

			_renderer.sharedMaterial = _mirrorMaterial;
		}

		private void EnsureMirrorCamera() {
			if (_mirrorCamera != null)
				return;

			var go = new GameObject("Mirror Camera", typeof(Camera));
			go.hideFlags = HideFlags.HideAndDontSave;

			_mirrorCamera = go.GetComponent<Camera>();
			_mirrorCamera.enabled = false;
			_mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;

			// Add URP camera data
			var urpData = go.AddComponent<UniversalAdditionalCameraData>();
			urpData.renderShadows = false;
			urpData.requiresColorOption = CameraOverrideOption.Off;
			urpData.requiresDepthOption = CameraOverrideOption.Off;
			urpData.renderPostProcessing = false;
		}

		private void EnsureRenderTextures(Camera cam) {
			int sourceWidth, sourceHeight;
			if (cam.stereoEnabled) {
				var eyeDesc = XRSettings.eyeTextureDesc;
				sourceWidth = eyeDesc.width > 0 ? eyeDesc.width : cam.pixelWidth;
				sourceHeight = eyeDesc.height > 0 ? eyeDesc.height : cam.pixelHeight;
			} else {
				sourceWidth = cam.pixelWidth;
				sourceHeight = cam.pixelHeight;
			}

			var maxSize = cam.stereoEnabled
				? Mathf.Min(_maxResolution, MirrorSettings.StereoMaxResolution)
				: _maxResolution;

			var w = Mathf.Clamp((int)(sourceWidth * ResolutionScale), 64, maxSize);
			var h = Mathf.Clamp((int)(sourceHeight * ResolutionScale), 64, maxSize);

			// Check if we need to update textures
			var sizeChanged = _lastTextureWidth != w || _lastTextureHeight != h;

			if (!_reflectionTextureLeft || sizeChanged) {
				if (_reflectionTextureLeft) {
					_reflectionTextureLeft.Release();
					DestroyImmediate(_reflectionTextureLeft);
				}

				_reflectionTextureLeft = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) {
					name = "Mirror Left",
					hideFlags = HideFlags.DontSave
				};

				_texturesAssigned = false;
				_lastTextureWidth = w;
				_lastTextureHeight = h;
			}

			if (cam.stereoEnabled) {
				if (!_reflectionTextureRight || sizeChanged) {
					if (_reflectionTextureRight) {
						_reflectionTextureRight.Release();
						DestroyImmediate(_reflectionTextureRight);
					}

					_reflectionTextureRight = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) {
						name = "Mirror Right",
						hideFlags = HideFlags.DontSave
					};

					_texturesAssigned = false;
				}
			}
		}

		private void OnBeginCameraRendering(Camera cam) {
			// Only render for Game/SceneView cameras
			if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)
				return;

			// Skip our own camera
			if (_mirrorCamera && cam == _mirrorCamera)
				return;

			// Prevent recursion
			if (s_IsRendering)
				return;

			// Check if enabled and visible
			if (!enabled || !_renderer || !_renderer.enabled)
				return;

			// Check visibility (is the mirror on screen?)
			if (!_renderer.isVisible)
				return;

			GeometryUtility.CalculateFrustumPlanes(cam, _frustum);
			if (!GeometryUtility.TestPlanesAABB(_frustum, _renderer.bounds))
				return;

			s_IsRendering = true;

			try {
				RenderMirror(cam);
			} finally {
				s_IsRendering = false;
			}
		}

		private void RenderMirror(Camera cam) {
			EnsureMirrorCamera();
			EnsureRenderTextures(cam);

			_mirrorCamera.CopyFrom(cam);
			_mirrorCamera.enabled = false;
			_mirrorCamera.stereoTargetEye = StereoTargetEyeMask.None;

			var pixelLightCount = QualitySettings.pixelLightCount;
			if (_disablePixelLights)
				QualitySettings.pixelLightCount = 0;

			try {
				if (cam.stereoEnabled) {
					// VR mode - render both eyes
					RenderEye(cam, Camera.StereoscopicEye.Left, _reflectionTextureLeft);
					RenderEye(cam, Camera.StereoscopicEye.Right, _reflectionTextureRight);

					if (!_texturesAssigned) {
						_propertyBlock.SetTexture(LeftEyeTextureId, _reflectionTextureLeft);
						_propertyBlock.SetTexture(RightEyeTextureId, _reflectionTextureRight);
						_renderer.SetPropertyBlock(_propertyBlock);
						_texturesAssigned = true;
					}
				} else {
					// Non-VR mode
					RenderEye(cam, Camera.StereoscopicEye.Left, _reflectionTextureLeft);

					if (!_texturesAssigned) {
						_propertyBlock.SetTexture(LeftEyeTextureId, _reflectionTextureLeft);
						_propertyBlock.SetTexture(RightEyeTextureId, _reflectionTextureLeft);
						_renderer.SetPropertyBlock(_propertyBlock);
						_texturesAssigned = true;
					}
				}
			} finally {
				if (_disablePixelLights)
					QualitySettings.pixelLightCount = pixelLightCount;
			}
		}

		private void RenderEye(Camera sourceCam, Camera.StereoscopicEye eye, RenderTexture target) {
			var stereo = sourceCam.stereoEnabled;

			var eyeView = stereo
				? sourceCam.GetStereoViewMatrix(eye)
				: sourceCam.worldToCameraMatrix;

			if (stereo && RenderOrder.TryGetFrameViewMatrix(sourceCam, out var frameView))
				eyeView = sourceCam.worldToCameraMatrix * frameView.inverse * eyeView;

			var projMatrix = stereo
				? sourceCam.GetStereoProjectionMatrix(eye)
				: sourceCam.projectionMatrix;

			var mirrorPos = transform.position;
			var mirrorNormal = transform.up;

			// Calculate reflection plane
			var d = -Vector3.Dot(mirrorNormal, mirrorPos) - _clipPlaneOffset;
			var reflectionPlane = new Vector4(mirrorNormal.x, mirrorNormal.y, mirrorNormal.z, d);

			// Calculate reflection matrix
			var reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);

			// Reflect the eye's world-space position (extracted from the eye view matrix)
			// for a consistent camera position (used by shading's cameraPos).
			var eyePos = eyeView.inverse.MultiplyPoint(Vector3.zero);
			var reflectedPos = reflectionMatrix.MultiplyPoint(eyePos);

			// Setup mirror camera: reflect the world, then view it through the eye's
			// own matrices so per-eye IPD is preserved without any double/triple counting.
			_mirrorCamera.transform.position = reflectedPos;
			_mirrorCamera.transform.rotation = sourceCam.transform.rotation;
			_mirrorCamera.projectionMatrix = projMatrix;
			_mirrorCamera.worldToCameraMatrix = eyeView * reflectionMatrix;

			// Oblique projection for near clipping at mirror plane
			var clipPlane = CameraHelper.CameraSpacePlane(_mirrorCamera, mirrorPos, mirrorNormal, _clipPlaneOffset, 1.0f);
			_mirrorCamera.projectionMatrix = _mirrorCamera.CalculateObliqueMatrix(clipPlane);

			// Render settings: the layers are the ones the player picked, nothing is forced out here.
			_mirrorCamera.cullingMask = ReflectionLayers.value;
			_mirrorCamera.targetTexture = target;

			// Restore any CameraChop-hidden bones so the mirror sees the full avatar,
			// then re-hide them afterwards for the primary first-person camera.
			CameraChop.RestoreAllScales();

			// Render with inverted culling
			GL.invertCulling = true;
			RenderPipeline.SubmitRenderRequest(_mirrorCamera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
			GL.invertCulling = false;

			// Re-apply first-person bone hiding so the source camera render is unaffected.
			CameraChop.HideAllForCamera(sourceCam);
		}

		private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane) {
			var m = Matrix4x4.identity;

			m.m00 = 1f - 2f * plane[0] * plane[0];
			m.m01 = -2f * plane[0] * plane[1];
			m.m02 = -2f * plane[0] * plane[2];
			m.m03 = -2f * plane[3] * plane[0];

			m.m10 = -2f * plane[1] * plane[0];
			m.m11 = 1f - 2f * plane[1] * plane[1];
			m.m12 = -2f * plane[1] * plane[2];
			m.m13 = -2f * plane[3] * plane[1];

			m.m20 = -2f * plane[2] * plane[0];
			m.m21 = -2f * plane[2] * plane[1];
			m.m22 = 1f - 2f * plane[2] * plane[2];
			m.m23 = -2f * plane[3] * plane[2];

			m.m30 = 0f;
			m.m31 = 0f;
			m.m32 = 0f;
			m.m33 = 1f;

			return m;
		}

		private void Cleanup() {
			if (_mirrorCamera) {
				DestroyImmediate(_mirrorCamera.gameObject);
				_mirrorCamera = null;
			}

			if (_reflectionTextureLeft) {
				_reflectionTextureLeft.Release();
				DestroyImmediate(_reflectionTextureLeft);
				_reflectionTextureLeft = null;
			}

			if (_reflectionTextureRight) {
				_reflectionTextureRight.Release();
				DestroyImmediate(_reflectionTextureRight);
				_reflectionTextureRight = null;
			}
		}

#if UNITY_EDITOR
		private void Reset() {
			_renderer = GetComponent<Renderer>();
			EnsureMirrorMaterial();
		}

		private void OnValidate() {
			_renderer ??= GetComponent<Renderer>();
		}

		private void OnDrawGizmosSelected() {
			// Draw mirror plane
			Gizmos.color = Color.cyan;
			Gizmos.matrix = transform.localToWorldMatrix;
			Gizmos.DrawWireCube(Vector3.zero, new Vector3(1, 0.01f, 1));

			// Draw UP direction (mirror normal)
			Gizmos.color = Color.green;
			Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.5f);
			Gizmos.DrawSphere(Vector3.up * 0.5f, 0.02f);
		}
#endif
	}
}

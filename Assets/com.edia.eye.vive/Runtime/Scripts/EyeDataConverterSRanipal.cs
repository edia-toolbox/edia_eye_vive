using System;
using System.Runtime.InteropServices;
using Edia;
using Edia.Eye;
using UnityEngine;
using ViveSR.anipal.Eye;

namespace Edia.Eye.Vive {

/// Biggest part by "Corvus" (Vive Community Forum)
/// https://forum.vive.com/topic/9341-vive-eye-tracking-at-120hz/
///
/// Adapted to allow sending out the ET data with 120Hz to the eDIA `EyeDataHandler` in a threadsafe fashion. 
/// <summary>
/// Example usage for eye tracking callback
/// Note: Callback runs on a separate thread to report at ~120hz.
/// Unity is not threadsafe and cannot call any UnityEngine api from within callback thread.
/// 
/// Collects eyetracking data from the SRanipal SDK and converts into our eDIA EyDataPackage format and supplies this to the EyeDataHandler
/// </summary>
	public class EyeDataConverterSRanipal : MonoBehaviour {
		private static EyeData_v2 eyeData = new EyeData_v2();
		private static bool eye_callback_registered = false;

		[SerializeField]
		public static ILslTimeAccessible LslTimer;
		public bool UseLslTiming = false;

		private void Start() {

			if (UseLslTiming) {
				// check if the LslTiming component is available
				if (GetComponent<ILslTimeAccessible>() == null) {
					Debug.LogError("To use LSL timing, the EyeDataConverterSRanipal requires a component on the same GameObject " +
								   "which implements the ILslTimeAccessible interface (e.g., Edia.Lsl.LslTiming or " +
								   "Edia.Lsl.EyeOutlet).");
				}
				else {
					LslTimer = GetComponent<ILslTimeAccessible>();
				}
			}
		}

		void Update() {
			if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING) return;

			if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false) {
				SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
					Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
				eye_callback_registered = true;
			}
			else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true) {
				SRanipal_Eye.WrapperUnRegisterEyeDataCallback(
					Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
				eye_callback_registered = false;
			}
		}

		private void OnDisable() {
			Release();
		}

		void OnApplicationQuit() {
			Release();
		}

		/// <summary>
		/// Release callback thread when disabled or quit
		/// </summary>
		private static void Release() {
			if (eye_callback_registered == true) {
				SRanipal_Eye.WrapperUnRegisterEyeDataCallback(
					Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
				eye_callback_registered = false;
			}
		}

		/// <summary>
		/// Required class for IL2CPP scripting backend support
		/// </summary>
		internal class MonoPInvokeCallbackAttribute : System.Attribute {
			public MonoPInvokeCallbackAttribute() {
			}
		}

		/// <summary>
		/// Eye tracking data callback thread.
		/// Reports data at ~120hz
		/// MonoPInvokeCallback attribute required for IL2CPP scripting backend
		/// </summary>
		/// <param name="eye_data">Reference to latest eye_data</param>
		[MonoPInvokeCallback]
		private static void EyeCallback(ref EyeData_v2 eye_data) {
			
			eyeData = eye_data;

			foreach (Constants.EyeId eye in Enum.GetValues(typeof(Constants.EyeId))) {

				EyeDataPackage ed = new();
				SingleEyeData tmpData = new();

                switch (eye) {
					case Constants.EyeId.LEFT:
						tmpData = eyeData.verbose_data.left;
						break;
					case Constants.EyeId.RIGHT:
						tmpData = eyeData.verbose_data.right;
						break;
					case Constants.EyeId.CENTER:
						tmpData = eyeData.verbose_data.combined.eye_data;
						break;
				}
				
				ed.isValid = tmpData.GetValidity(0);
                double timestampLsl = LslTimer != null ? LslTimer.GetLslTime() : 0; // Get current LSL time when callback fires.

				ed.eye = eye.ToString().ToLower();
                ed.timestamp_et = eyeData.timestamp;  // in ms
                ed.timestamp_lsl = timestampLsl; // in s

                if (ed.isValid) {

                    ed.direction_x_local = tmpData.gaze_direction_normalized.x * -1f; //SRanipal uses right-handed coord system
                    ed.direction_y_local = tmpData.gaze_direction_normalized.y;
                    ed.direction_z_local = tmpData.gaze_direction_normalized.z;
                    
                    ed.position_x_local = tmpData.gaze_origin_mm.x * -1f * 0.001f; //SRanipal uses right-handed coord system
                    ed.position_y_local = tmpData.gaze_origin_mm.y * 0.001f;
                    ed.position_z_local = tmpData.gaze_origin_mm.z * 0.001f;

					switch (eye) {
                        case Constants.EyeId.LEFT:
                            ed.diameter = eyeData.verbose_data.left.pupil_diameter_mm;
                            ed.openness = eyeData.verbose_data.left.eye_openness;
                            break;
                        case Constants.EyeId.RIGHT:
                            ed.diameter = eyeData.verbose_data.right.pupil_diameter_mm;
                            ed.openness = eyeData.verbose_data.right.eye_openness;
                            break;
                        case Constants.EyeId.CENTER:
                            ed.diameter = (eyeData.verbose_data.left.pupil_diameter_mm + eyeData.verbose_data.right.pupil_diameter_mm) / 2f;
                            ed.openness = (eyeData.verbose_data.left.eye_openness + eyeData.verbose_data.right.eye_openness) / 2f;
                            break;
					}

					Quaternion rot = Quaternion.identity;
					if (new Vector3(ed.direction_x_local, ed.direction_y_local, ed.direction_z_local) != Vector3.zero) {
						rot = Quaternion.LookRotation(new Vector3(ed.direction_x_local, ed.direction_y_local, ed.direction_z_local));
					}

					ed.rotation_x_local = rot.eulerAngles.x;
					ed.rotation_y_local = rot.eulerAngles.y;
					ed.rotation_z_local = rot.eulerAngles.z;
				}

				// TODO: check if this is losing samples
				// "Lock" locks the access to the data queue on the EyeDataHandler (making it thread safe - maybe). 
				lock (EyeDataHandler.Instance.Lock) {
					EyeDataHandler.Instance.AddEyeDataPackage(ed);
				}
			}
		}
	}
}
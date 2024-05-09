using System.Runtime.InteropServices;
using Edia;
using Edia.Eye;
using UnityEngine;
using ViveSR.anipal.Eye;

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
public class EyeDataConverterSRanipal : MonoBehaviour
{
    private static EyeData_v2 eyeData = new EyeData_v2();
    private static bool eye_callback_registered = false;

    public static ILslTimer LslTimer;
    public bool UseLslTiming = true;

    private void Start() {

        if (UseLslTiming) {
            // check if the LslTiming component is available
            if (GetComponent<ILslTimer>() == null) {
                Debug.LogError("EyeDataConverterSRanipal requires a LslTiming component (edia_lsl) on the same GameObject.");
            } else {
                LslTimer = GetComponent<ILslTimer>();
            }
        }
    }

    void Update()
    {
        if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING) return;

        if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
        {
            SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = true;
        }
        else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
        {
            SRanipal_Eye.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }

    private void OnDisable()
    {
        Release();
    }

    void OnApplicationQuit()
    {
        Release();
    }

    /// <summary>
    /// Release callback thread when disabled or quit
    /// </summary>
    private static void Release()
    {
        if (eye_callback_registered == true)
        {
            SRanipal_Eye.WrapperUnRegisterEyeDataCallback(
                Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
            eye_callback_registered = false;
        }
    }

    /// <summary>
    /// Required class for IL2CPP scripting backend support
    /// </summary>
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute()
        {
        }
    }

    /// <summary>
    /// Eye tracking data callback thread.
    /// Reports data at ~120hz
    /// MonoPInvokeCallback attribute required for IL2CPP scripting backend
    /// </summary>
    /// <param name="eye_data">Reference to latest eye_data</param>
    [MonoPInvokeCallback]
    private static void EyeCallback(ref EyeData_v2 eye_data)
    {
        eyeData = eye_data;

        foreach (string eye in new string[] { "left", "right", "center" }) {

            SingleEyeData tmpData;
            switch (eye) {
                case "left":
                    tmpData = eyeData.verbose_data.left;
                    break;
                case "right":
                    tmpData = eyeData.verbose_data.right;
                    break;
                case "center":
                    tmpData = eyeData.verbose_data.combined.eye_data;
                    break;
                default:
                    tmpData = eyeData.verbose_data.combined.eye_data;
                    break;
            }

            double timestampLsl;

            if (LslTimer != null) {
                timestampLsl = LslTimer.GetTime();
            } else {
                timestampLsl = 0;
            };

            EyeDataPackage ed;

            if (tmpData.GetValidity(0)) {

                ed = new() {
                    eye = eye,
                    timestamp_et = eyeData.timestamp,
                    timestamp_lsl = timestampLsl,
                    //SRanipal uses right-handed coord system:
                    direction_x_local = tmpData.gaze_direction_normalized.x * -1f,
                    direction_y_local = tmpData.gaze_direction_normalized.y,
                    direction_z_local = tmpData.gaze_direction_normalized.z,
                    //SRanipal uses right-handed coord system
                    position_x_local = tmpData.gaze_origin_mm.x * -1f * 0.001f,
                    position_y_local = tmpData.gaze_origin_mm.y * 0.001f,
                    position_z_local = tmpData.gaze_origin_mm.z * 0.001f
                };

                float diameterRight = eyeData.verbose_data.right.pupil_diameter_mm;
                float diameterLeft = eyeData.verbose_data.left.pupil_diameter_mm;

                switch (eye) {
                    case "left":
                        ed.diameter = diameterLeft;
                        break;
                    case "right":
                        ed.diameter = diameterRight;
                        break;
                    case "center":
                        ed.diameter = (diameterRight + diameterLeft) / 2f;
                        break;
                }
                Quaternion rot = Quaternion.identity;
                if (new Vector3(ed.direction_x_local, ed.direction_y_local, ed.direction_z_local) != Vector3.zero) {
                    rot = Quaternion.LookRotation(new Vector3(ed.direction_x_local, ed.direction_y_local, ed.direction_z_local));
                } 
                
                ed.rotation_x_local = rot.eulerAngles.x;
                ed.rotation_y_local = rot.eulerAngles.y;
                ed.rotation_z_local = rot.eulerAngles.z;

                ed.openness = tmpData.eye_openness;
            } 
            
            else {
                ed = new() {
                    eye = eye,
                    direction_x_local = 0f,
                    direction_y_local = 0f,
                    direction_z_local = 0f,
                    position_x_local = 0f,
                    position_y_local = 0f,
                    position_z_local = 0f,
                    diameter = 0f,
                    rotation_x_local = 0f,
                    rotation_y_local = 0f,
                    rotation_z_local = 0f,
                    openness = 0f,
                    timestamp_et = eyeData.timestamp,
                    timestamp_lsl = timestampLsl
                };
            }

            // TODO: check if this is losing samples
            // "Lock" locks the access to the data queue on the EyeDataHandler (making it thread safe - maybe). 
            lock (EyeDataHandler.Instance.Lock) {
                EyeDataHandler.Instance.AddEyeDataPackage(ed);
            }
        }
    }
}

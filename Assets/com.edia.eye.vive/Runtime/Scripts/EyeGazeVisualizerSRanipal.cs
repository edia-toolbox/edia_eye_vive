//========= Copyright 2018, HTC Corporation. All rights reserved. ===========
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace ViveSR {
	namespace anipal 	{
		namespace Eye {
			public class EyeGazeVisualizerSRanipal : MonoBehaviour {
				
				public int LengthOfRay = 25;
				[SerializeField] 
				private LineRenderer GazeRayRenderer;
				[SerializeField] 
				private LineRenderer GazeRayRendererRight;
				[SerializeField] 
				private LineRenderer GazeRayRendererLeft;
				
				public bool showLeftRay = false;
				public bool showRightRay = false;
				public bool showCombinedRay = false;

				private static EyeData_v2 eyeData = new EyeData_v2();
				private bool eye_callback_registered = false;

				// eDIA
				public Transform mainCamReference = null; // container for maincam object as calling `Camera.main` every frame is like the NUMBER 1 mistake ! 

				private void Start() 				{
					if (!SRanipal_Eye_Framework.Instance.EnableEye)
					{
						enabled = false;
						return;
					}

					Assert.IsNotNull(GazeRayRenderer);

					GazeRayRenderer.enabled 	= showCombinedRay;
					GazeRayRendererLeft.enabled 	= showLeftRay;
					GazeRayRendererRight.enabled 	= showRightRay;

					mainCamReference = Edia.XRManager.Instance.XRCam; // get a reference instead of let unity do a search for 'camera.main' in the objectlist each frame

				}

				private void Update() {

					if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
					    SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

					//! This whole callback thing seems pretty cumbersome

					if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false) {
						SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
						eye_callback_registered = true;
					}
					else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)	{
						SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
						eye_callback_registered = false;
					}

					Vector3 GazeOriginCombinedLocal, GazeDirectionCombinedLocal;

					if (eye_callback_registered)	{
						// Debug.Log("eye_callback_registered");
						if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
						else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
						else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal, eyeData)) { }
						else return;
					}
					else	{
						if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.COMBINE, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
						else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.LEFT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
						else if (SRanipal_Eye_v2.GetGazeRay(GazeIndex.RIGHT, out GazeOriginCombinedLocal, out GazeDirectionCombinedLocal)) { }
						else return;
					}

					Vector3 GazeDirectionCombined = mainCamReference.TransformDirection(GazeDirectionCombinedLocal);
					GazeRayRenderer.SetPosition(0, mainCamReference.transform.position - mainCamReference.transform.up * 0.05f);
					GazeRayRenderer.SetPosition(1, mainCamReference.transform.position + GazeDirectionCombined * LengthOfRay);
				}


				private void Release()	{
					if (eye_callback_registered == true) {
						SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
						eye_callback_registered = false;
					}
				}

				private static void EyeCallback(ref EyeData_v2 eye_data) {
					eyeData = eye_data;
				}

			}
		}
	}
}

using UnityEngine;
using Edia;

namespace Edia.Eye.Vive {

	public class EyeCalibrationTriggerViveSRanipal : EyeCalibrationTriggerBase {

		public override void OnEvEyeCalibrationRequested(eParam e) {
			Debug.Log(name + " OnEvEyeCalibrationRequested");

			// VIVE
			ViveSR.anipal.Eye.SRanipal_Eye.LaunchEyeCalibration();
		}
	}
}
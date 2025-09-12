# Instructions

Place the `ViveSR` folder next to this file (i.e., in the `SDK` folder).  
The `ViveSR` folder is part of the HTC `VIVE Eye and Facial Tracking SDK` which you can (maybe) download from the [Vive Developers website](https://developer.vive.com/eu/support/sdk/category_howto/where-to-download-eye-tracking-runtime-and-sdk.html). 
Note that HTC is now recommending the OpenXR based SDK and the "old" `VIVE Eye and Facial Tracking SDK` (which is needed for the current implementation of `EDIA`) may not be available there.

If you need help, please get in touch with the `EDIA` team, try to find `ViveSR` somewhere else on the internet (e.g., [here](https://gitlab.rlp.net/oberfeld_lab/AV-Room/-/tree/develop/Assets/ViveSR) but I don't know if this is a compatible version), 
or help us to transition to `OpenXR`.

**We recommend that you get in touch with us.**

Next: Create a file called `ViveSR.asmdef` within the `ViveSR` folder. (If the `ViveSR` folder already contains this file, you can ignore this step.)
Use a notepad to open the file and paste the following content into the file: 

```json
{
  "name": "ViveSR",
  "references": [],
  "optionalUnityReferences": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}

```


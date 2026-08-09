using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace NOCameraImprovements;

public static class MyPluginInfo
{
	public const string PLUGIN_GUID = "com.minec.NOCameraImprovements";
	public const string PLUGIN_NAME = "NOCameraImprovements";
	public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static new ManualLogSource Logger;
	internal static Plugin Instance;
	internal static bool CameraClip = false;
	
	private  ConfigEntry<bool> clipDefault;

	private void Awake()
	{
		Instance = this;
		Logger = base.Logger;
		
		clipDefault = Config.Bind<bool>("Camera", "Clipping Default", false, "If the camera is allowed to clip by default");
		
		CameraClip = clipDefault.Value;
		
		Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
		harmony.PatchAll();

		Logger.LogInfo($"{MyPluginInfo.PLUGIN_NAME} v{MyPluginInfo.PLUGIN_VERSION} loaded!");
	}
}
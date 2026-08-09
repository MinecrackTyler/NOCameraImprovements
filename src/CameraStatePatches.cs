using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NuclearOption.SavedMission;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NOCameraImprovements;

[HarmonyPatch]
public class CameraStatePatches
{
	[HarmonyTargetMethods]
	private static IEnumerable<MethodBase> TargetMethods()
	{
		yield return AccessTools.Method(typeof(CameraControlledState), nameof(CameraControlledState.UpdateState));
		yield return AccessTools.Method(typeof(CameraFreeState), nameof(CameraFreeState.UpdateState));
	}

	[HarmonyTranspiler]
	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		var matcher = new CodeMatcher(instructions);

		var linecastMethod = AccessTools.Method(typeof(Physics),
			nameof(Physics.Linecast),
			new[] { typeof(Vector3), typeof(Vector3), typeof(RaycastHit).MakeByRefType(), typeof(int) }
		);
		
		var replacementMethod = AccessTools.Method(typeof(CameraStatePatches), nameof(LinecastOverride));
		
		matcher.MatchForward(false, new CodeMatch(OpCodes.Call, linecastMethod)).SetOperandAndAdvance(replacementMethod);
		
		return matcher.InstructionEnumeration();
	}

	private static bool LinecastOverride(Vector3 start, Vector3 end, out RaycastHit hitInfo, int layerMask)
	{
		hitInfo = default;
		if (Plugin.CameraClip) return false;
		
		return Physics.Linecast(start, end, out hitInfo, layerMask);
	}
}

[HarmonyPatch]
public static class CameraControlUIPatches
{
    [HarmonyPatch(typeof(CameraControlUI), nameof(CameraControlUI.Start))]
    [HarmonyPostfix]
    private static void Start_Postfix(CameraControlUI __instance)
    {
       try
       {
          var improvementsWindow = Object.Instantiate(__instance.container.transform.Find("Inputs").gameObject, __instance.container.transform, false);
          improvementsWindow.name = "CameraImprovements";

          improvementsWindow.transform.Find("Header").Find("label").GetComponent<Text>().text = "IMPROVEMENTS";
          var toggle = improvementsWindow.transform.Find("Header").Find("InputsToggle");
          
          improvementsWindow.transform.Find("RotSpeed").Destroy();
          improvementsWindow.transform.Find("TransSpeed").name = "Clip";
          improvementsWindow.transform.Find("Clip").Find("Value").Destroy();
          improvementsWindow.transform.Find("Clip").Find("TransSpeedSlider").Destroy();
          improvementsWindow.transform.Find("Clip").Find("Label").GetComponent<Text>().text = "CLIP";
          var newToggle = Object.Instantiate(toggle, improvementsWindow.transform.Find("Clip"), false);
          newToggle.name = "ClipToggle";

          var toggleComponent = newToggle.GetComponent<Toggle>();
          toggleComponent.onValueChanged.RemoveAllListeners();
          for (int i = 0; i < toggleComponent.onValueChanged.GetPersistentEventCount(); i++)
          {
             toggleComponent.onValueChanged.SetPersistentListenerState(i, UnityEventCallState.Off);
          }

          toggleComponent.isOn = Plugin.CameraClip;
          toggleComponent.onValueChanged.AddListener(isOn => Plugin.CameraClip = isOn);
          toggle.Destroy();
          improvementsWindow.GetComponent<RectTransform>().sizeDelta = new Vector2(improvementsWindow.GetComponent<RectTransform>().sizeDelta.x, 60f);
          
          SetupScrollRect(__instance.container);
       }
       catch (Exception ex)
       {
          Plugin.Logger.LogError($"Error setting up UI panel: {ex}");
       }
    }

    private static void SetupScrollRect(GameObject container)
    {
        if (container.GetComponent<ScrollRect>() != null) return;

        RectTransform containerRect = container.GetComponent<RectTransform>();

        container.GetComponent<VerticalLayoutGroup>().Destroy();
        
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(container.transform, false);
        
        Image vpImage = viewport.GetComponent<Image>();
        vpImage.color = new Color(0, 0, 0, 0.01f);
        
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 5f;
        
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        int childCount = container.transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = container.transform.GetChild(i);
            if (child != viewport.transform)
            {
                child.SetParent(content.transform, false);
                child.SetAsFirstSibling();
            }
        }
        
        ScrollRect scrollRect = container.AddComponent<ScrollRect>();
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 20f;
    }

    private static void Destroy(this GameObject go)
    {
       if (go == null) return;
       Object.Destroy(go);
    }
    private static void Destroy(this Transform transform)
    {
       if (transform == null) return;
       Object.Destroy(transform.gameObject);
    }

    private static void Destroy(this Component component)
    {
	    Object.Destroy(component);
    }
}
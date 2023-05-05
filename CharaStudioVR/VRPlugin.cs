using System;
using System.Collections;
using System.Runtime.InteropServices;
using BepInEx;
using BepInEx.Logging;
using KKAPI;
using KKS_VR.Controls;
using KKS_VR.Features;
using KKS_VR.Fixes;
using KKS_VR.Interpreters;
using KKS_VR.Settings;
using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using VRGIN.Core;

namespace KKS_VR
{
    /// <summary>
    /// Studio code was forked from KKS_CharaStudioVR at https://vr-erogamer.com/archives/1065
    /// </summary>
    [BepInPlugin(GUID, Name, Version)]
    [BepInProcess(KoikatuAPI.StudioProcessName)]
    [BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
    public class VRPlugin : BaseUnityPlugin
    {
        public const string GUID = "KKS_CharaStudioVR";
        public const string Name = "KKS Chara Studio VR";
        public const string Version = Constants.Version;

        internal static new ManualLogSource Logger;

        public void Awake()
        {
            Logger = base.Logger;

            var vrActivated = Environment.CommandLine.Contains("--vr") || Environment.CommandLine.Contains("--studiovr");
            if (vrActivated)
            {
                OpenVRHelperTempfixHook.Patch();
                StartCoroutine(LoadDevice());
            }
        }

        private IEnumerator LoadDevice()
        {
            // For some reason using Scene.LoadSceneName instead of SceneManager will break the background color, probably some timing issue
            yield return new WaitUntil(() => Manager.Scene.initialized && SceneManager.GetActiveScene().name == "Studio");

            VRLog.Level = VRLog.LogMode.Info;
            base.Logger.LogInfo("Loading OpenVR...");

            var ovrsettings = OpenVRSettings.GetSettings(true);
            ovrsettings.StereoRenderingMode = OpenVRSettings.StereoRenderingModes.MultiPass;
            ovrsettings.InitializationType = OpenVRSettings.InitializationTypes.Scene;
            ovrsettings.EditorAppKey = "kss.charastudio.exe";
            var instance = SteamVR_Settings.instance;
            instance.autoEnableVR = true;
            instance.editorAppKey = "kss.charastudio.exe";
            var openVRLoader = ScriptableObject.CreateInstance<OpenVRLoader>();
            if (!openVRLoader.Initialize())
            {
                base.Logger.LogInfo("Failed to Initialize OpenVR.");
                yield break;
            }

            if (!openVRLoader.Start())
            {
                base.Logger.LogInfo("Failed to Start OpenVR.");
                yield break;
            }

            base.Logger.LogInfo("Initializing SteamVR...");

            try
            {
                SteamVR_Behaviour.Initialize(false);
            }
            catch (Exception data)
            {
                base.Logger.LogError(data);
            }

            while (true)
            {
                var initializedState = SteamVR.initializedState;
                switch (initializedState)
                {
                    case SteamVR.InitializedStates.Initializing:
                        yield return new WaitForSeconds(0.1f);
                        continue;
                    case SteamVR.InitializedStates.InitializeSuccess:
                        break;
                    case SteamVR.InitializedStates.InitializeFailure:
                        base.Logger.LogInfo("Failed to initialize SteamVR.");
                        yield break;
                    default:
                        base.Logger.LogInfo($"Unknown SteamVR initializeState {initializedState}.");
                        yield break;
                }

                break;
            }

            base.Logger.LogInfo("Initializing the plugin...");

            SaveLoadSceneHook.InstallHook();
            LoadFixHook.InstallHook();

            TopmostToolIcons.Patch();

            VRManager.Create<KKSCharaStudioInterpreter>(CharaStudioContext.GetContext());

            VR.Manager.SetMode<StudioStandingMode>();

            var obj = new GameObject("KKSCharaStudioVR");
            DontDestroyOnLoad(obj);
            IKTool.Create(obj);
            VRCameraMoveHelper.Install(obj);
            VRItemObjMoveHelper.Install(obj);

            PrivacyScreen.Initialize();

            NativeMethods.DisableProcessWindowsGhosting();

            DontDestroyOnLoad(VRCamera.Instance.gameObject);

            base.Logger.LogInfo("Finished loading into VR mode!");
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern void DisableProcessWindowsGhosting();
        }
    }
}

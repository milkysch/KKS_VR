using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KKS_VR.Settings;
using KKS_VR.Util;
using Studio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Handlers;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;

namespace KKS_VR.Controls
{
    internal class GripMoveStudioNEOV2Tool : Tool
    {
        private GUIQuad internalGui;

        private float pressDownTime;

        private Vector2 touchDownPosition;

        private float menuDownTime;

        private float touchpadDownTime;

        private double _DeltaX;

        private double _DeltaY;

        private EVRButtonId moveSelfButton = EVRButtonId.k_EButton_Grip;

        private EVRButtonId grabScreenButton = EVRButtonId.k_EButton_Axis1;

        private string moveSelfButtonName = "rgrip";

        private CharaStudioSettings _settings;

        private float triggerDownTime;

        private float gripDownTime;

        private GameObject mirror1;

        private GameObject grabHandle;

        private GameObject pointer;

        private bool screenGrabbed;

        private GameObject lastGrabbedObject;

        private GameObject grabbingObject;

        private MenuHandler menuHandlder;

        private GripMenuHandler gripMenuHandler;

        private IKTool ikTool;

        private float nearestGrabable = float.MaxValue;

        private string[] FINGER_KEYS = new string[5] { "cf_J_Hand_Thumb", "cf_J_Hand_Index", "cf_J_Hand_Middle", "cf_J_Hand_Ring", "cf_J_Hand_Little" };

        private static FieldInfo f_dicGuideObject = typeof(GuideObjectManager).GetField("dicGuideObject", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private GameObject marker;

        public GameObject target;

        private bool lockRotXZ = true;

        public override Texture2D Image => UnityHelper.LoadImage("icon_gripmove.png");

        public GUIQuad Gui { get; private set; }

        private DeviceLegacyAdapter controller => Controller;

        protected override void OnAwake()
        {
            base.OnAwake();
            SceneManager.sceneLoaded += OnSceneWasLoaded;
            Setup();
        }

        private void resetGUIPosition()
        {
            var head = VR.Camera.Head;
            internalGui.transform.parent = transform;
            internalGui.transform.localScale = Vector3.one * 0.4f;
            if (head != null)
            {
                internalGui.transform.position = head.TransformPoint(new Vector3(0f, 0f, 0.3f));
                internalGui.transform.rotation = Quaternion.LookRotation(head.TransformVector(new Vector3(0f, 0f, 1f)));
            }
            else
            {
                internalGui.transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
                internalGui.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            internalGui.transform.parent = transform.parent;
            internalGui.UpdateAspect();
        }

        private void CreatePointer()
        {
            if (pointer == null)
            {
                pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointer.name = "pointer";
                pointer.GetComponent<SphereCollider>();
                pointer.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f) * VR.Context.Settings.IPDScale;
                pointer.transform.parent = transform;
                pointer.transform.localPosition = new Vector3(0f, -0.03f, 0.03f);
                var component = pointer.GetComponent<Renderer>();
                component.enabled = true;
                component.shadowCastingMode = ShadowCastingMode.Off;
                component.receiveShadows = false;
                component.material = new Material(MaterialHelper.GetColorZOrderShader());
            }
        }

        protected override void OnDestroy()
        {
            if (marker != null) Destroy(marker);
            if (mirror1 != null) Destroy(mirror1);
            if (grabHandle != null) Destroy(grabHandle);
            if (internalGui != null) DestroyImmediate(internalGui.gameObject);
        }

        private void Setup()
        {
            try
            {
                VRLog.Info("Loading GripMoveTool");
                _settings = VR.Manager.Context.Settings as CharaStudioSettings;
                internalGui = GUIQuad.Create(null);
                internalGui.gameObject.AddComponent<MoveableGUIObject>();
                internalGui.gameObject.AddComponent<BoxCollider>();
                internalGui.IsOwned = true;
                DontDestroyOnLoad(internalGui.gameObject);
                CreatePointer();
                gripMenuHandler = gameObject.AddComponent<GripMenuHandler>();
                gripMenuHandler.enabled = false;
            }
            catch (Exception obj)
            {
                VRLog.Info(obj);
            }

            if (marker == null)
            {
                marker = new GameObject("__GripMoveMarker__");
                marker.transform.parent = transform.parent;
                marker.transform.position = transform.position;
                marker.transform.rotation = transform.rotation;
            }

            if (_settings != null)
            {
                moveSelfButton = EVRButtonId.k_EButton_Grip;
                moveSelfButtonName = "rgrip";
                grabScreenButton = EVRButtonId.k_EButton_Axis1;
            }

            menuHandlder = GetComponent<MenuHandler>();
            ikTool = IKTool.instance;
        }

        protected override void OnStart()
        {
            base.OnStart();
            StartCoroutine(ResetGUIPositionCo());
        }

        private IEnumerator ResetGUIPositionCo()
        {
            yield return new WaitForSeconds(0.1f);
            resetGUIPosition();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (gripMenuHandler != null) gripMenuHandler.enabled = false;
            if (menuHandlder != null) menuHandlder.enabled = true;
            if ((bool)internalGui) internalGui.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (gripMenuHandler != null) gripMenuHandler.enabled = true;
            if (menuHandlder != null) menuHandlder.enabled = false;
            if ((bool)internalGui) internalGui.gameObject.SetActive(true);
        }

        protected void OnSceneWasLoaded(Scene scene, LoadSceneMode sceneMode)
        {
            if (sceneMode == LoadSceneMode.Single) StopAllCoroutines();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (controller == null) return;
            if (controller.GetPressDown(EVRButtonId.k_EButton_Axis1)) triggerDownTime = Time.time;
            if (controller.GetPressDown(EVRButtonId.k_EButton_Grip)) gripDownTime = Time.time;
            if (controller.GetPressDown(EVRButtonId.k_EButton_ApplicationMenu)) menuDownTime = Time.time;
            if (controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A)) touchpadDownTime = Time.time;
            if (controller.GetPress(EVRButtonId.k_EButton_Axis1) && controller.GetPress(EVRButtonId.k_EButton_Grip) && controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) &&
                Time.time - menuDownTime > 0.5f)
            {
                lockRotXZ = !lockRotXZ;
                if (lockRotXZ) ResetRotation();
            }

            if (controller.GetPress(EVRButtonId.k_EButton_ApplicationMenu) && Time.time - menuDownTime > 1.5f)
            {
                resetGUIPosition();
                menuDownTime = Time.time;
            }

            if (controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A)) controller.GetPress(EVRButtonId.k_EButton_Grip);
            var pressDown = controller.GetPressDown(grabScreenButton);
            var press = controller.GetPress(grabScreenButton);
            var pressUp = controller.GetPressUp(grabScreenButton);
            if (grabHandle == null)
            {
                grabHandle = new GameObject("__GripMoveGrabHandle__");
                grabHandle.transform.parent = transform;
                grabHandle.transform.position = transform.position;
                grabHandle.transform.rotation = transform.rotation;
            }

            if (pressDown && screenGrabbed && lastGrabbedObject != null && grabHandle != null)
            {
                grabbingObject = lastGrabbedObject;
                grabHandle.transform.position = lastGrabbedObject.transform.position;
                grabHandle.transform.rotation = lastGrabbedObject.transform.rotation;
                if (lastGrabbedObject.GetComponent<MoveableGUIObject>() != null)
                {
                    _ = lastGrabbedObject.transform.parent;
                    var component = lastGrabbedObject.GetComponent<MoveableGUIObject>();
                    if (component.guideObject != null)
                    {
                        ApplyFingerFKIfNeeded(component.guideObject);
                        grabHandle.transform.rotation = component.guideObject.transformTarget.rotation;
                        grabbingObject.transform.rotation = component.guideObject.transformTarget.rotation;
                        component.OnMoveStart();
                    }
                }
            }

            var flag = false;
            if ((controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A)) && lastGrabbedObject != null &&
                lastGrabbedObject.GetComponent<MoveableGUIObject>() != null)
            {
                var guideObject = lastGrabbedObject.GetComponent<MoveableGUIObject>().guideObject;
                if (guideObject != null)
                {
                    if (guideObject.guideSelect != null && guideObject.guideSelect.treeNodeObject != null)
                        guideObject.guideSelect.treeNodeObject.OnClickSelect();
                    else
                        Singleton<GuideObjectManager>.Instance.selectObject = guideObject;
                    flag = true;
                }
            }

            if ((controller.GetPressDown(EVRButtonId.k_EButton_Axis0) || controller.GetPressDown(EVRButtonId.k_EButton_A) && !flag) && (bool)gripMenuHandler &&
                gripMenuHandler.LaserVisible) VRItemObjMoveHelper.Instance.VRToggleObjectSelectOnCursor();
            if (press && grabbingObject != null)
            {
                grabbingObject.transform.position = grabHandle.transform.position;
                grabbingObject.transform.rotation = grabHandle.transform.rotation;
                if (grabbingObject.GetComponent<MoveableGUIObject>() != null) grabbingObject.GetComponent<MoveableGUIObject>().OnMoved();
            }

            if (screenGrabbed && grabbingObject != null && pressUp)
            {
                if (grabbingObject.GetComponent<MoveableGUIObject>() != null) grabbingObject.GetComponent<MoveableGUIObject>().OnReleased();
                grabbingObject = null;
            }

            if (controller.GetPress(moveSelfButton) && grabbingObject == null)
            {
                target = VR.Camera.SteamCam.origin.gameObject;
                if (target != null)
                {
                    if (mirror1 == null)
                    {
                        mirror1 = new GameObject("__GripMoveMirror1__");
                        mirror1.transform.position = transform.position;
                        mirror1.transform.rotation = transform.rotation;
                    }

                    var vector = marker.transform.position - transform.position;
                    var q = marker.transform.rotation * Quaternion.Inverse(transform.rotation);
                    var quaternion = RemoveLockedAxisRot(q);
                    var parent = target.transform.parent;
                    mirror1.transform.position = transform.position;
                    mirror1.transform.rotation = transform.rotation;
                    target.transform.parent = mirror1.transform;
                    mirror1.transform.rotation = quaternion * mirror1.transform.rotation;
                    mirror1.transform.position = mirror1.transform.position + vector;
                    target.transform.parent = parent;
                }
            }

            lastGrabbedObject = null;
            nearestGrabable = float.MaxValue;
            marker.transform.position = transform.position;
            marker.transform.rotation = transform.rotation;
        }

        private void ApplyFingerFKIfNeeded(GuideObject guideObject)
        {
            new List<Transform>();
            var list = new List<GuideObject>();
            if (IsFinger(guideObject.transformTarget)) list.Add(guideObject);
            foreach (var item in list) item.transformTarget.localEulerAngles = item.changeAmount.rot;
        }

        private bool IsFinger(Transform t)
        {
            var fINGER_KEYS = FINGER_KEYS;
            foreach (var value in fINGER_KEYS)
                if (t.name.Contains(value))
                    return true;
            return false;
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[3]
            {
                HelpText.Create("Swipe as wheel.", FindAttachPosition("touchpad"), new Vector3(0.06f, 0.04f, 0f)),
                HelpText.Create("Grip and move controller to move yourself", FindAttachPosition("rgrip"), new Vector3(0.06f, 0.04f, 0f)),
                HelpText.Create("Trigger to grab objects / IK markers and move them along with controller.", FindAttachPosition("trigger"), new Vector3(-0.06f, -0.04f, 0f))
            });
        }

        private void ResetRotation()
        {
            if (target != null)
            {
                var eulerAngles = target.transform.rotation.eulerAngles;
                eulerAngles.x = 0f;
                eulerAngles.z = 0f;
                target.transform.rotation = Quaternion.Euler(eulerAngles);
            }
        }

        private IEnumerator UpdateMarkerPos()
        {
            yield return new WaitForEndOfFrame();
            marker.transform.position = transform.position;
            marker.transform.rotation = transform.rotation;
        }

        private Quaternion RemoveLockedAxisRot(Quaternion q)
        {
            if (lockRotXZ) return RemoveXZRot(q);
            return q;
        }

        public static Quaternion RemoveXZRot(Quaternion q)
        {
            var eulerAngles = q.eulerAngles;
            eulerAngles.x = 0f;
            eulerAngles.z = 0f;
            return Quaternion.Euler(eulerAngles);
        }

        private void OnTriggerStay(Collider collider)
        {
            if (collider.GetComponent<GUIQuad>() != null)
            {
                screenGrabbed = true;
                lastGrabbedObject = collider.gameObject;
            }
            else if (collider.GetComponent<MoveableGUIObject>() != null)
            {
                screenGrabbed = true;
                if (lastGrabbedObject != null)
                {
                    var sqrMagnitude = (collider.gameObject.transform.position - pointer.transform.position).sqrMagnitude;
                    if (sqrMagnitude < nearestGrabable)
                    {
                        lastGrabbedObject = collider.gameObject;
                        nearestGrabable = sqrMagnitude;
                    }
                }
                else
                {
                    lastGrabbedObject = collider.gameObject;
                }
            }

            if (screenGrabbed && lastGrabbedObject != null && pointer != null) pointer.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        private void OnTriggerEnter(Collider collider)
        {
        }

        private void OnTriggerExit(Collider collider)
        {
            var gameObject = collider.gameObject;
            if (screenGrabbed && collider.GetComponent<MoveableGUIObject>() != null && gameObject == lastGrabbedObject)
            {
                pointer.GetComponent<MeshRenderer>().material.color = Color.white;
                screenGrabbed = false;
                lastGrabbedObject = null;
            }
        }
    }
}

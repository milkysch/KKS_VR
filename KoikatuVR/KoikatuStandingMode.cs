﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Modes;
using UnityEngine;

namespace KoikatuVR
{
    class KoikatuStandingMode : StandingMode
    {
        public override IEnumerable<Type> Tools
        {
            get
            {
                return new[] { typeof(Tools.KoikatuMenuTool), typeof(Tools.KoikatuWarpTool), typeof(Tools.SchoolTool)};
            }
        }

        protected override IEnumerable<IShortcut> CreateShortcuts()
        {
            // Disable all VRGIN shortcuts. We'll define necessary shortcuts
            // (if any) by ourselves.
            return Enumerable.Empty<IShortcut>();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Caress.VRMouth.Init();
        }

        protected override Controller CreateLeftController()
        {
            return AddComponents(base.CreateLeftController());
        }

        protected override Controller CreateRightController()
        {
            return AddComponents(base.CreateRightController());
        }

        private static Controller AddComponents(Controller controller)
        {
            controller.gameObject.AddComponent<Caress.CaressController>();
            controller.gameObject.AddComponent<LocationPicker>();
            controller.gameObject.AddComponent<TalkSceneHandler>().enabled = false;
            return controller;
        }

        protected override void SyncCameras()
        {
            // Do nothing. CameraControlControl and friends take care of this.
        }

        protected override void InitializeScreenCapture()
        {
            // Don't enable CapturePanorama because it looks broken (throws an
            // exception).
        }
    }
}

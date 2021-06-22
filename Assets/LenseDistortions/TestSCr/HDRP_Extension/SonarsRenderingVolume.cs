
    using System.Collections.Generic;
    using UnityEngine.Rendering;
    using System.Linq;
    using System;
using UnityEngine;

[Serializable]
    public class PassContainer
    {
        public GKO_Renderer customPass;
    }
    public class SonarsRenderingVolume : MonoBehaviour
    {
        [SerializeField]
        public PassContainer c;
        public List<SonarRenderer> customPasses = new List<SonarRenderer>();
        public List<float> a;
        public static SonarsRenderingVolume VolumeInstance;
        // Start is called before the first frame update
        void Awake()
        {
            VolumeInstance = this;
        }

        // Update is called once per frame
        void Update()
        {

        }

        internal bool Execute(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            bool executed = false;


            foreach (var pass in customPasses)
            {
                if (pass != null)
                {
                    pass.ExecuteInternal(renderContext, cmd);
                    executed = true;
                }
            }

            return executed;
        }

        internal void CleanupPasses()
        {
            foreach (var pass in customPasses)
                pass.CleanupPassInternal();
        }

        public static SonarsRenderingVolume GetActiveSonars()
        {
            return VolumeInstance;
        }
    }

using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System;
using UnityEngine.Serialization;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;
    [System.Serializable]
    public class SonarRenderer
    {
        [System.NonSerialized]
        bool isSetup = false;

        bool isExecuting = false;

        public string m_Name = "Sonar Renderer";
        /// <summary>
        /// Called when your pass needs to be executed by a camera
        /// </summary>
        /// <param name="renderContext"></param>
        /// <param name="cmd"></param>
        /// <param name="hdCamera"></param>
        /// <param name="cullingResult"></param>
        /// 
        internal void ExecuteInternal(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!isSetup)
            {
                Setup(renderContext, cmd);
                isSetup = true;
            }
            Debug.Log(m_Name);
            isExecuting = true;
            Execute(renderContext, cmd);
            isExecuting = false;
        }

        protected virtual void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        protected virtual void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd) { }

        internal void CleanupPassInternal()
        {
            if (isSetup)
            {
                Cleanup();
                isSetup = false;
            }
        }

        protected virtual void Cleanup() { }

    }
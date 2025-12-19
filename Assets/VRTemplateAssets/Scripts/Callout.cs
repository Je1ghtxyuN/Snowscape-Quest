using System.Collections;
using UnityEngine;

namespace Unity.VRTemplate
{
    /// <summary>
    /// Callout used to display information like world and controller tooltips.
    /// </summary>
    public class Callout : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The tooltip Transform associated with this Callout.")]
        Transform m_LazyTooltip;

        [SerializeField]
        [Tooltip("The line curve GameObject associated with this Callout.")]
        GameObject m_Curve;

        [SerializeField]
        [Tooltip("The required time to dwell on this callout before the tooltip and curve are enabled.")]
        float m_DwellTime = 1f;

        [SerializeField]
        [Tooltip("Whether the associated tooltip will be unparented on Start.")]
        bool m_Unparent = true;

        [SerializeField]
        [Tooltip("Whether the associated tooltip and curve will be disabled on Start.")]
        bool m_TurnOffAtStart = true;

        bool m_Gazing = false;

        Coroutine m_StartCo;
        Coroutine m_EndCo;

        void Start()
        {
            if (m_Unparent)
            {
                if (m_LazyTooltip != null)
                    m_LazyTooltip.SetParent(null);
            }

            if (m_TurnOffAtStart)
            {
                if (m_LazyTooltip != null)
                    m_LazyTooltip.gameObject.SetActive(false);
                if (m_Curve != null)
                    m_Curve.SetActive(false);
            }
        }

        public void GazeHoverStart()
        {
            // 检查游戏对象是否激活
            if (!isActiveAndEnabled) return;

            m_Gazing = true;
            if (m_StartCo != null)
                StopCoroutine(m_StartCo);
            if (m_EndCo != null)
                StopCoroutine(m_EndCo);
            m_StartCo = StartCoroutine(StartDelay());
        }

        public void GazeHoverEnd()
        {
            // 检查游戏对象是否激活
            if (!isActiveAndEnabled) return;

            m_Gazing = false;
            m_EndCo = StartCoroutine(EndDelay());
        }

        IEnumerator StartDelay()
        {
            yield return new WaitForSeconds(m_DwellTime);

            // 再次检查，防止在等待期间对象被禁用
            if (m_Gazing && isActiveAndEnabled)
                TurnOnStuff();
        }

        IEnumerator EndDelay()
        {
            // 等待一帧，确保状态稳定
            yield return null;

            // 检查对象是否仍然激活
            if (!m_Gazing && isActiveAndEnabled)
                TurnOffStuff();
        }

        void TurnOnStuff()
        {
            if (m_LazyTooltip != null && m_LazyTooltip.gameObject != null)
                m_LazyTooltip.gameObject.SetActive(true);
            if (m_Curve != null)
                m_Curve.SetActive(true);
        }

        void TurnOffStuff()
        {
            if (m_LazyTooltip != null && m_LazyTooltip.gameObject != null)
                m_LazyTooltip.gameObject.SetActive(false);
            if (m_Curve != null)
                m_Curve.SetActive(false);
        }

        // 添加OnDisable方法，确保对象禁用时清理状态
        private void OnDisable()
        {
            // 停止所有协程
            if (m_StartCo != null)
                StopCoroutine(m_StartCo);
            if (m_EndCo != null)
                StopCoroutine(m_EndCo);

            m_StartCo = null;
            m_EndCo = null;

            // 如果对象被禁用，确保关闭相关UI
            if (!m_Gazing)
                TurnOffStuff();
        }
    }
}
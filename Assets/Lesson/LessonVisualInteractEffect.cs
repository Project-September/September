using System;
using InGame.Interact;
using UnityEngine;

namespace September.Lesson
{
    /// <summary>
    /// 授業用: 見た目が変化する展示物を開始するインタラクト効果。
    /// </summary>
    [Serializable]
    public sealed class LessonVisualInteractEffect : CharacterInteractEffectBase
    {
        // どの展示物の見た目を変えるか。PrefabのInspectorで設定する。
        [SerializeField, InspectorName("見た目を変える展示物"), Tooltip("この展示物についているLessonVisualExhibitを指定します。")]
        private LessonVisualExhibit _visualExhibit;

        /// <summary>
        /// インタラクト完了時に、見た目の変化を開始する。
        /// </summary>
        public override void OnInteractStart(IInteractableContext context, InteractableBase target)
        {
            if (_visualExhibit)
            {
                _visualExhibit.Activate();
            }
            else
            {
                // 参照が未設定ならInspectorで接続し直せるように通知する。
                Debug.LogWarning("LessonVisualExhibitを設定してください。", target);
            }
        }

        /// <summary>
        /// 展示物システムが効果を複製するときに呼ぶ。変化させる展示物の参照を引き継ぐ。
        /// </summary>
        public override CharacterInteractEffectBase Clone()
        {
            return new LessonVisualInteractEffect
            {
                _visualExhibit = _visualExhibit,
            };
        }
    }
}

using Fusion;
using UnityEngine;

namespace September.Lesson
{
    /// <summary>
    /// 授業用: 色・大きさ・回転のいずれかを分かりやすく変化させる展示物。
    /// </summary>
    public sealed class LessonVisualExhibit : NetworkBehaviour
    {
        // 色を変える対象。Rendererは物体の見た目を画面に描く部品。
        [SerializeField, InspectorName("色を変える見た目"), Tooltip("色を変えるRendererです。色の授業では必ず指定してください。")]
        private Renderer _targetRenderer;
        // 大きさや向きを変える対象。Transformは位置・回転・大きさを持つ。
        [SerializeField, InspectorName("大きさ・回転の対象"), Tooltip("大きさや回転を変えるTransformです。通常は展示物の見た目の子オブジェクトを指定します。")]
        private Transform _visualTransform;
        // インタラクト後、見た目が変わる秒数。
        [SerializeField, Min(0.1f), InspectorName("見た目が変わる時間（秒）"), Tooltip("インタラクト後、選んだ変化を何秒間見せるか指定します。")]
        private float _duration = 5f;

        // [Networked]により、効果の終了時刻を他のプレイヤーとも共有する。
        [Networked] private TickTimer EffectTimer { get; set; }

        // 効果が終わったとき元へ戻せるよう、開始前の見た目を保存する。
        private Color _defaultColor;
        private Vector3 _defaultScale;

        /// <summary>
        /// オブジェクトが作られたとき、効果終了後に戻すための色と大きさを記録する。
        /// </summary>
        private void Awake()
        {
            // 対象が未設定なら、このスクリプトが付いている物体を使う。
            _visualTransform ??= transform;
            if (_targetRenderer) _defaultColor = _targetRenderer.material.color;
            _defaultScale = _visualTransform.localScale;
        }

        /// <summary>
        /// インタラクトを受けて、指定秒数のタイマーを開始する。
        /// </summary>
        public void Activate()
        {
            // ネットワーク対戦では、状態を決める側だけがタイマーを書き換える。
            if (!HasStateAuthority) return;
            EffectTimer = TickTimer.CreateFromSeconds(Runner, _duration);
        }

        /// <summary>
        /// 画面を描くたびに呼ばれる。タイマー中は効果を見せ、終了後は元へ戻す。
        /// </summary>
        public override void Render()
        {
            if (EffectTimer.ExpiredOrNotRunning(Runner))
            {
                RestoreVisual();
                return;
            }

            // 時間から変化量を作る。数字の4を大きくすると色や大きさの変化が速くなる。
            float phase = Runner.SimulationTime * 4f;
            ApplyLessonVisual(phase);
        }

        /// <summary>
        /// 授業で編集する場所。使いたい見た目の処理を一つ選んで呼び出す。
        /// </summary>
        private void ApplyLessonVisual(float phase)
        {
            // 行頭の // は「この行を実行しない」という意味。使う行の // だけ消す。
            // 複数選ぶと、色・大きさ・回転を同時に変えることもできる。
            ApplyColorPulse(phase);
            // ApplyScalePulse(phase);
            // ApplySpin(phase);
        }

        /// <summary>
        /// 水色とピンク色を時間に合わせて往復させる。
        /// </summary>
        private void ApplyColorPulse(float phase)
        {
            if (!_targetRenderer) return;
            _targetRenderer.material.color = Color.Lerp(Color.cyan, Color.magenta, Mathf.PingPong(phase, 1f));
        }

        /// <summary>
        /// 元の大きさを基準に、少し大きくしたり小さくしたりする。
        /// </summary>
        private void ApplyScalePulse(float phase)
        {
            float scale = 1f + Mathf.Sin(phase) * 0.35f;
            _visualTransform.localScale = _defaultScale * scale;
        }

        /// <summary>
        /// 展示物を上向きの軸を中心に回転させる。
        /// </summary>
        private void ApplySpin(float phase)
        {
            _visualTransform.Rotate(Vector3.up, 180f * Runner.DeltaTime, Space.Self);
        }

        /// <summary>
        /// タイマーが終わったら、変えた色と大きさを元の値に戻す。
        /// </summary>
        private void RestoreVisual()
        {
            if (_targetRenderer) _targetRenderer.material.color = _defaultColor;
            if (_visualTransform) _visualTransform.localScale = _defaultScale;
        }
    }
}

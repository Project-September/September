using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Splines;

namespace InGame.Player.Okubo
{
    /// <summary>
    /// 生成したメッシュを実行時にもアセットとして保存してしまう問題を抑制するためのコンポーネント
    /// </summary>
    [RequireComponent(typeof(SplineExtrude))]
    [DefaultExecutionOrder(-1)] // SplineExtrudeより前に実行する
    public class RuntimeSplineExtrude : MonoBehaviour
    {
        [SerializeField] private SplineContainer _splineContainer;
        [SerializeField, ReadOnly] private SplineExtrude _splineExtrude;
        [SerializeField, ReadOnly] private MeshFilter _meshFilter;

        private Mesh _mesh;
        private bool _isInitialized;

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                // SplineExtrudeのOnValidateが呼ばれる前に無効化する必要があるのでこのタイミングで行う
                Init();
                Validate();
                return;
            }

            _splineExtrude = GetComponent<SplineExtrude>();
            _meshFilter = GetComponent<MeshFilter>();

            // Containerがアサインされている状態だと、Meshがnullの場合に警告ログが出続けるのでnullにする
            _splineExtrude.Container = null;
            _splineExtrude.enabled = false;
        }

        private void Awake()
        {
            Init();
        }

        private void Update()
        {
            Validate();
        }

        private void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            _splineExtrude.Container = _splineContainer;

            // 実行時メッシュをアサインする。SplineExtrude側がこれに書き込む。アセットではないため保存処理が走らなくなる。
            _mesh = new Mesh();
            _meshFilter.sharedMesh = _mesh;
        }

        // 不正なMeshが生成される可能性がある場合に無効化する
        private void Validate()
        {
            Spline spline = _splineExtrude.Spline;

            float span = Mathf.Abs(_splineExtrude.Range.y - _splineExtrude.Range.x);
            int segmentCount = Mathf.Max((int)Mathf.Ceil(spline.GetLength() * span * _splineExtrude.SegmentsPerUnit), 1);

            bool isValid = segmentCount > 1; // segmentCountが1だと頂点が存在せずMesh生成時に例外になるっぽい

            _splineExtrude.enabled = isValid;
            if (!isValid) _mesh?.Clear();
        }

        private void OnDestroy()
        {
            // 安全のため明示的に破棄
            if (_mesh)
            {
                Destroy(_mesh);
                _mesh = null;
            }
        }
    }
}

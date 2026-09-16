using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UniRx;
using UniRx.Triggers;

namespace September.Lobby
{
    public class BuildView : BuildViewBase
    {
        [Header("表示に必要なUI群")]
        [SerializeField] BuildSelectObject[] _buildObjects;
        [SerializeField] Button _decisionButton;
        [SerializeField] TextMeshProUGUI _buildName;
        [SerializeField] TextMeshProUGUI _buildInfo;
        [SerializeField] GridLayoutGroup _iconLayoutGroup;
        int _buildCount;
        int _currentSelectIndex;
        bool _confirming;
        bool _initialized;
        bool _wasInteractive;
        Transform _decisionArrow;

        public override void Init(BuildDataBase[] builds)
        {
            if (_initialized) return;
            _buildCount = Mathf.Min(_buildObjects.Length, builds.Length);
            if (!_iconLayoutGroup && _buildCount > 0)
                _iconLayoutGroup = _buildObjects[0].GetComponentInParent<GridLayoutGroup>(true);
            // 仮UI処理
            if (_buildObjects != null && builds != null)
            {
                // UIへの範囲外アクセスを防止したfor文
                for (int i = 0; i < _buildObjects.Length; i++)
                {
                    var obj = _buildObjects[i];
                    if (obj == null) continue;
                    bool hasBuild = i < _buildCount;
                    obj.gameObject.SetActive(hasBuild);
                    if (!hasBuild) continue;
                    obj.Init();
                    obj.SetIconImage(builds[i].BuildSprite);
                    // ボタンでインデックスを直接選択できるように
                    var index = i;
                    obj.Button.OnSelectAsObservable().Subscribe(_ =>
                    {
                        if (!_confirming) MoveIndexForButton(index);
                    }).AddTo(this);
                    obj.Button.OnCancelAsObservable().Subscribe(e =>
                    {
                        SetPhase(false);
                        FocusCurrentBuild();
                        e.Use();
                    }).AddTo(this);
                    obj.RegisterAction(() =>
                    {
                        if (_confirming) return;
                        MoveIndexForButton(index);
                        SetPhase(true);
                        _decisionButton.Select();
                    });
                    // 最初のUIは選択時の処理を、それ以外には未選択時の処理を施す
                    if (i == 0)
                        obj.Select();
                    else
                        obj.Unselect();
                }
            }

            if (_buildName && _buildInfo)
            {
                _buildName.text = builds[0].BuildName;
                _buildInfo.text = builds[0].BuildInfo;
            }
            _decisionArrow = _decisionButton.transform.Find("Arrow");
            _decisionButton.navigation = new Navigation { mode = Navigation.Mode.None };
            // Inspectorで登録済みの場合は、確定処理を二重登録しない。
            bool registered = false;
            for (int i = 0; i < _decisionButton.onClick.GetPersistentEventCount(); i++)
                registered |= _decisionButton.onClick.GetPersistentTarget(i) == this &&
                    _decisionButton.onClick.GetPersistentMethodName(i) == nameof(SelectBuild);
            if (!registered) _decisionButton.onClick.AddListener(SelectBuild);
            _decisionButton.OnCancelAsObservable().Subscribe(e =>
            {
                if (!_confirming) return;
                SetPhase(false);
                FocusCurrentBuild();
                e.Use();
            }).AddTo(this);
            _initialized = true;
            SetPhase(false);
        }

        void SetPhase(bool confirming)
        {
            _confirming = confirming;
            _decisionButton.interactable = confirming;
            if (_decisionArrow) _decisionArrow.gameObject.SetActive(confirming);
            for (int i = 0; i < _buildCount; i++)
            {
                var obj = _buildObjects[i];
                if (obj) obj.Button.interactable = !confirming;
            }
        }

        void LateUpdate()
        {
            if (!_initialized || _buildObjects.Length == 0) return;
            var button = _confirming ? _decisionButton : _buildObjects[_currentSelectIndex].Button;
            bool interactive = button.isActiveAndEnabled && button.IsInteractable();
            if (interactive && !_wasInteractive)
            {
                SetPhase(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(
                    (RectTransform)(_iconLayoutGroup ? _iconLayoutGroup.transform : transform));
                MoveIndexForButton(0);
                FocusCurrentBuild();
            }
            else if (interactive && !HasCurrentPhaseFocus())
            {
                SetPhase(false);
                FocusCurrentBuild();
            }
            _wasInteractive = interactive;
            if (interactive && !_confirming) RefreshNavigation();
        }

        Vector2 IconPosition(int index)
        {
            Transform layout = _iconLayoutGroup ? _iconLayoutGroup.transform : transform;
            Transform cell = _buildObjects[index].transform;
            // 拡大するボタンではなくGrid直下のセルを基準にする。
            if (_iconLayoutGroup)
                while (cell.parent && cell.parent != layout) cell = cell.parent;
            var rect = (RectTransform)cell;
            return layout.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        }

        void RefreshNavigation()
        {
            // 列数・開始方向・開始角を含め、現在の配置から四方向の隣を決める。
            for (int i = 0; i < _buildCount; i++)
            {
                if (!_buildObjects[i]) continue;
                _buildObjects[i].Button.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = FindNeighbor(i, Vector2.up),
                    selectOnDown = FindNeighbor(i, Vector2.down),
                    selectOnLeft = FindNeighbor(i, Vector2.left),
                    selectOnRight = FindNeighbor(i, Vector2.right)
                };
            }
        }

        Button FindNeighbor(int index, Vector2 direction)
        {
            Button nearest = null;
            float distance = float.PositiveInfinity;
            Vector2 origin = IconPosition(index);
            for (int i = 0; i < _buildCount; i++)
            {
                if (i == index || !_buildObjects[i]) continue;
                var button = _buildObjects[i].Button;
                if (!button.isActiveAndEnabled || !button.IsInteractable()) continue;
                Vector2 delta = IconPosition(i) - origin;
                float forward = Vector2.Dot(delta, direction);
                float sideways = Mathf.Abs(Vector2.Dot(delta, new Vector2(-direction.y, direction.x)));
                // 同じ行・列だけを対象にし、端から斜めや決定ボタンへ飛ばさない。
                if (forward <= 0.01f || sideways > 0.1f || forward >= distance) continue;
                nearest = button;
                distance = forward;
            }
            return nearest;
        }

        public override void SelectBuild()
        {
            if (_confirming) base.SelectBuild();
        }

        public override void VisualizeBuildInfo(int index, BuildDataBase build)
        {
            if (_confirming) return;
            // 直前に選択していた要素に対して未選択時の処理
            _buildObjects[_currentSelectIndex].Unselect();

            // 新たな要素に対して選択時の処理
            _buildObjects[index].Select();

            // 選択中のビルドルート詳細を表示
            if (_buildName && _buildInfo)
            {
                _buildName.text = build.BuildName;
                _buildInfo.text = build.BuildInfo;
            }

            // 直前のインデックスを保存
            _currentSelectIndex = index;

#if UNITY_EDITOR
            Debug.Log($"選択中 => {build.BuildName}");
#endif
        }

        public override void VisualizeSelection(int index)
        {
            // 確定しても、最後に選んだルートの拡大とフレームは維持する。
            _buildObjects[_currentSelectIndex]?.Select();
            SetPhase(false);
        }

        void FocusCurrentBuild()
        {
            if (_buildCount == 0) return;
            int index = Mathf.Clamp(_currentSelectIndex, 0, _buildCount - 1);
            var button = _buildObjects[index].Button;
            if (button.isActiveAndEnabled && button.IsInteractable()) button.Select();
        }

        bool HasCurrentPhaseFocus()
        {
            if (!EventSystem.current) return false;
            var expected = _confirming
                ? _decisionButton.gameObject
                : _buildObjects[_currentSelectIndex].Button.gameObject;
            return EventSystem.current.currentSelectedGameObject == expected;
        }
    }
}

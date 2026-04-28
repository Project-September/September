using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlsUIGenerator : MonoBehaviour
{
    // 操作説明を生成した際にそのオブジェクトの親になるオブジェクト
    // VerticalLayoutGroup,ContentSizeFitterなどのレイアウトコンポーネントがアタッチされていることを想定
    [SerializeField] private GameObject _descriptionObject;
    [SerializeField] private GameObject _iconPrefab;

    [SerializeField] 
    private SerializableDictionary<ControlDescriptionType, ControlDescription> _descriptionDictionary;

    /// <summary>
    /// ControlDescriptionの内容をもとに操作説明UIを生成する
    /// </summary>
    public void GenerateDescription(ControlDescriptionType descriptionType)
    {
        if (_descriptionObject == null)
        {
            Debug.LogError("DescriptionObject is not assigned");
            return;
        }

        ClearChildren();

        ControlDescription description = _descriptionDictionary.Dictionary[descriptionType];

        foreach (var action in description.Actions)
        {
            GameObject icon = Instantiate(_iconPrefab, _descriptionObject.transform);
            icon.name = $"{action.ActionName}Icon";
            icon.GetComponent<Image>().sprite = action.Icon;
            icon.GetComponentInChildren<TextMeshProUGUI>().text = action.Description;
        }
    }

    /// <summary>
    /// 生成前に子オブジェクトを全て削除する
    /// </summary>
    private void ClearChildren()
    {
        for (int i = _descriptionObject.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(_descriptionObject.transform.GetChild(i).gameObject);
        }
    }
}

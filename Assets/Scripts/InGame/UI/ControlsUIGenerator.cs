using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlsUIGenerator : MonoBehaviour
{
    // 操作説明を生成した際にそのオブジェクトの親になるオブジェクト
    // VerticalLayoutGroup,ContentSizeFitterなどのレイアウトコンポーネントがアタッチされていることを想定
    [SerializeField] private GameObject _descriptionObject;
    [SerializeField] private ControlDescription _descriptionData;
    [SerializeField] private GameObject _iconPrefab;

    private void Start()
    {
        GenerateDescription(_descriptionData);
    }

    /// <summary>
    /// ControlDescriptionの内容をもとに操作説明UIを生成する
    /// </summary>
    public void GenerateDescription(ControlDescription description)
    {   
        foreach (var action in description.Actions)
        {
            GameObject icon = Instantiate(_iconPrefab, _descriptionObject.transform);
            icon.name = $"{action.ActionName}Icon";
            icon.GetComponent<Image>().sprite = action.Icon;
            icon.GetComponentInChildren<TextMeshProUGUI>().text = action.Description;
        }
    }
}

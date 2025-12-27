using System;
using UnityEngine;
using UnityEngine.UI;

namespace September.NewResult
{
    [Serializable]
    public class PageController
    {
        [SerializeField] private Transform _page;
        [SerializeField] private Selectable _selectWhenShowPage;
        [SerializeField] private Selectable _selectWhenHidePage;
        [SerializeField] private Button _showButton;
        [SerializeField] private Button _hideButton;
        [SerializeField] private Transform _background;

        public void Init()
        {
            _page.gameObject.SetActive(false);
            _background.gameObject.SetActive(false);
            _showButton.onClick.AddListener(ShowPage);
            _hideButton.onClick.AddListener(HidePage);
        }

        public void ShowPage()
        {
            _page.gameObject.SetActive(true);
            _background.gameObject.SetActive(true);
            _selectWhenShowPage.Select();
        }

        public void HidePage()
        {
            _page.gameObject.SetActive(false);
            _background.gameObject.SetActive(false);
            _selectWhenHidePage.Select();
        }
    }
}
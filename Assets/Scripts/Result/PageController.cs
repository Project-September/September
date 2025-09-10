using UnityEngine;

namespace Result
{
    public class PageController : MonoBehaviour
    {
        [SerializeField] private RectTransform[] _pages;
        private int _currentPage;

        public void ShowPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].gameObject.SetActive(i == index);
            }
            
            _currentPage = index;
        }

        public void NextPage()
        {
            int next = (_currentPage + 1) % _pages.Length;
            ShowPage(next);
        }

        public void PrevPage()
        {
            int prev = (_currentPage - 1 +  _pages.Length) % _pages.Length;
            ShowPage(prev);
        }
    }
}
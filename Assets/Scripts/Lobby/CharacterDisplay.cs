using UnityEngine;

namespace September.Lobby
{
    public class CharacterDisplay : MonoBehaviour
    {
        [SerializeField] private Transform[] _cameraPoints;
        [SerializeField] private Camera _characterDisplayCamera;

        public void SetCharacter(int index)
        {
            _characterDisplayCamera.transform.position = _cameraPoints[index].position;
            _characterDisplayCamera.transform.rotation = _cameraPoints[index].rotation;
        }
    }
}
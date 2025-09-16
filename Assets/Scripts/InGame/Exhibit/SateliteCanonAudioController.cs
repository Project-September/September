using CRISound;
using UnityEngine;

public class SateliteCanonAudioController : MonoBehaviour
{
    [SerializeField] private Transform _satelitePosition;
    [SerializeField] private string _cueSheet = "ALLCue";
    
    public void PlaySateliteSound(string CueName)
    {
        CRIAudio.PlaySE(_satelitePosition.position, _cueSheet, CueName);
    }
}

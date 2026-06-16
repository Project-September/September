using Fusion;
using InGame.Health;
using InGame.Player;
using TMPro;
using UnityEngine;

public class DamageTextFactory : NetworkBehaviour
{
    [Header("DamageTextUI Settings")]
    [SerializeField] private GameObject _damageText;
    [SerializeField] private GameObject _damageTextAnim;
    [SerializeField] private PlayerHealth _ownPlayer;
    [SerializeField] private float _textOffsetY = 10.0f;
    [SerializeField] private float _randomOffsetRadius = 50.0f;
    [SerializeField] private float _textDestroyTime = 3.0f;

    private GameObject _canvas;
    private Camera _mainCamera;

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ConfigureDamageText(int damage, PlayerRef playerRef)
    {
        if (Runner.LocalPlayer.PlayerId == playerRef.PlayerId)
        {
            CreateDamageText(damage);
        }
    }

    private void OnHit(HitData data)
    {
        RPC_ConfigureDamageText(data.Amount, data.ExecutorRef);
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        _canvas = GameObject.Find("DamageTextCanvas");
    }

    private void OnEnable()
    {
        _ownPlayer.OnHitTaken -= OnHit;
        _ownPlayer.OnHitTaken += OnHit;
    }
    private void OnDisable()
    {
        _ownPlayer.OnHitTaken -= OnHit;
    }
        
    private void CreateDamageText(HitData data) => CreateDamageText(data.Amount);

    private void CreateDamageText(int damage)
    {
        GameObject parentObject = Instantiate(_damageTextAnim);
        GameObject childText = Instantiate(_damageText, parentObject.transform);
        parentObject.transform.SetParent(_canvas.transform);

        parentObject.SetActive(true);
        parentObject.GetComponentInChildren<TextMeshProUGUI>().text = damage.ToString();
        if (damage == 0) parentObject.SetActive(false);

        Vector3 worldPosition = _ownPlayer.transform.position + Vector3.up * _textOffsetY;
        Vector3 randomOffset = new Vector3(
            Random.Range(-_randomOffsetRadius, _randomOffsetRadius),
            Random.Range(-_randomOffsetRadius, _randomOffsetRadius), 0);

        DamageTextBox positionBox = parentObject.GetComponent<DamageTextBox>();
        positionBox.GenerateValueBox(worldPosition, randomOffset, _mainCamera);

        Destroy(parentObject, _textDestroyTime);
    }
}
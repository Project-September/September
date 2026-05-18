using UnityEngine;
using InGame.Player;
using InGame.Health;

public class DamageTextFactory : MonoBehaviour
{
    [Header("DamageTextUI Settings")]
    [SerializeField] private GameObject _damageText;
    [SerializeField] private PlayerHealth _targetPlayer;
    [SerializeField] private GameObject _canvas;

    [Header("RandomOffset Settings")]
    [SerializeField] private float randomOffsetRange = 1.2f;
    [SerializeField] private float destroyTime = 3.0f;

    private void OnEnable()
    {
        _targetPlayer.OnHitTaken += CreateDamageText;
    }

    private void OnDisable()
    {
        _targetPlayer.OnHitTaken -= CreateDamageText;
    }

    private void CreateDamageText(HitData data)
    {
        throw new System.NotImplementedException();
    }

    private void CreateDamageText(int damage)
    {
        var targetObject = new GameObject("TargetPlayer");
        var damageText = Instantiate(_damageText);

        targetObject.transform.SetParent(_targetPlayer.transform);
        damageText.transform.SetParent(_canvas.transform);

        damageText.SetActive(true);

        // UIの表示位置をランダムに更新
        var randomOffset = new Vector3(Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(-randomOffsetRange, randomOffsetRange), 0);
        damageText.transform.position += randomOffset;
        //var randumOffset = Random.insideUnitCircle * randomOffsetRange; // 半径0.5の円内のランダムな位置

        // スポーン位置をプレイヤーの上に設定
        var spawnPosition = _targetPlayer.transform.position + Vector3.up * 2f;
        damageText.transform.position = spawnPosition;

        Destroy(targetObject, destroyTime);
        Destroy(damageText, destroyTime);
    }

    private void TestSpawn()
    {
        CreateDamageText(100);
    }
}
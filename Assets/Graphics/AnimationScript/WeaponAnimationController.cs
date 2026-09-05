using UnityEngine;

public class WeaponUltimateController : MonoBehaviour
{
    [Header("Weapon Animator")]
    [SerializeField] private Animator animator;

    [Header("Weapon Transform")]
    [SerializeField] private Transform weapon;

    [Header("Weapon Sockets")]
    [SerializeField] private Transform socketBody;
    [SerializeField] private Transform socketRoot;
    [SerializeField] private Transform socketLeftHand;


    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (weapon == null)
        {
            weapon = transform;
        }
    }


    // =========================================================
    // Ult1
    // =========================================================

    public void StartUlt1()
    {
        // 武器のUlt1アニメーションを開始
        if (animator != null)
        {
            animator.SetTrigger("WeaponUlt1");
        }

        // Ult1開始時は胴体Socket
        AttachToBody();
    }


    // =========================================================
    // Ult2
    // =========================================================

    public void StartUlt2()
    {
        if (animator != null)
        {
            animator.SetTrigger("WeaponUlt2");
        }
    }


    // =========================================================
    // Ult3
    // =========================================================

    public void StartUlt3()
    {
        if (animator != null)
        {
            animator.SetTrigger("WeaponUlt3");
        }
    }


    // =========================================================
    // Body Socket
    // =========================================================

    public void AttachToBody()
    {
        AttachWeapon(socketBody);
    }


    // =========================================================
    // Root Socket
    // =========================================================

    public void AttachToRoot()
    {
        AttachWeapon(socketRoot);
    }


    // =========================================================
    // Left Hand Socket
    // =========================================================

    public void AttachToLeftHand()
    {
        AttachWeapon(socketLeftHand);
    }


    // =========================================================
    // WeaponをSocketに取り付ける
    // =========================================================

    private void AttachWeapon(Transform socket)
    {
        if (weapon == null)
        {
            Debug.LogError(
                "[WeaponUltimateController] Weaponが設定されていません。",
                this
            );
            return;
        }

        if (socket == null)
        {
            Debug.LogError(
                "[WeaponUltimateController] Socketが設定されていません。",
                this
            );
            return;
        }

        // 一度親を変更
        weapon.SetParent(socket);

        // Socket基準でTransformを完全にリセット
        weapon.localPosition = Vector3.zero;
        weapon.localRotation = Quaternion.identity;
        weapon.localScale = Vector3.one;

        Debug.Log(
            $"Weapon Attach: {socket.name}\n" +
            $"Local Position: {weapon.localPosition}\n" +
            $"Local Rotation: {weapon.localEulerAngles}\n" +
            $"Local Scale: {weapon.localScale}",
            this
        );
    }


    // =========================================================
    // Ult終了
    // =========================================================

    public void EndUltimate()
    {
        // 必要になった場合はここで通常時のSocketへ戻す
    }
}
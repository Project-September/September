using UnityEngine;

public class UltimateAnimationController : MonoBehaviour
{
    [Header("Character Animator")]
    [SerializeField] private Animator animator;

    [Header("Weapon Controller")]
    [SerializeField] private WeaponUltimateController weaponController;

    private bool isUltimate = false;


    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }


    private void Update()
    {
        // Q → Ult1
        if (Input.GetKeyDown(KeyCode.Q))
        {
            StartUltimate();
        }

        // 左クリック → Ult3
        if (Input.GetMouseButtonDown(0))
        {
            TryUlt3();
        }

        // Ult1終了 → Ult2
        CheckUlt1End();
    }


    private void StartUltimate()
    {
        if (isUltimate)
            return;

        isUltimate = true;

        animator.SetTrigger("UltTrigger");

        if (weaponController != null)
        {
            weaponController.StartUlt1();
        }
    }


    private void CheckUlt1End()
    {
        if (!isUltimate)
            return;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Base Layer.Anim_Ult1_Hatano_Body"))
        {
            if (state.normalizedTime >= 1.0f)
            {
                animator.SetTrigger("Ult2Trigger");

                if (weaponController != null)
                {
                    weaponController.StartUlt2();
                }
            }
        }
    }


    private void TryUlt3()
    {
        if (!isUltimate)
            return;

        AnimatorStateInfo state =
            animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Base Layer.Anim_Ult2_Hatano_Body"))
        {
            animator.SetTrigger("FinishUlt");

            if (weaponController != null)
            {
                weaponController.StartUlt3();
            }
        }
    }


    // ========================================
    // Animation Event
    // ========================================

    public void AttachWeaponToBody()
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "WeaponUltimateControllerが設定されていません。",
                this
            );
            return;
        }

        weaponController.AttachToBody();
    }


    public void AttachWeaponToRoot()
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "WeaponUltimateControllerが設定されていません。",
                this
            );
            return;
        }

        weaponController.AttachToRoot();
    }


    public void AttachWeaponToLeftHand()
    {
        if (weaponController == null)
        {
            Debug.LogError(
                "WeaponUltimateControllerが設定されていません。",
                this
            );
            return;
        }

        weaponController.AttachToLeftHand();
    }


    public void EndUltimate()
    {
        isUltimate = false;
    }
}
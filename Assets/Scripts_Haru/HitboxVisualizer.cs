using UnityEngine;

public class HitboxVisualizer : MonoBehaviour
{
    public HitboxData hitboxData;       // ScriptableObjectをアサイン
    public Animator animator;           // Animatorをアサイン
    public string attackTriggerName = "Attack"; // Animatorの攻撃トリガー名

    private bool isAttacking = false;
    public int currentFrame = 0;

    void Update()
    {
        if (animator == null || hitboxData == null) return;

        // 攻撃入力をチェック
        if (Input.GetButtonDown("Fire1")) // 攻撃ボタン
        {
            animator.SetTrigger(attackTriggerName);
            isAttacking = true;
        }

        // 攻撃中はモーションの再生フレームに応じてcurrentFrameを更新
        if (isAttacking)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            float normalizedTime = state.normalizedTime;
            int totalFrames = hitboxData.frames.Length;
            currentFrame = Mathf.Clamp(Mathf.FloorToInt(normalizedTime * totalFrames), 0, totalFrames - 1);

            // 攻撃モーション終了でリセット
            if (normalizedTime >= 1f)
            {
                isAttacking = false;
                currentFrame = 0;
            }

            // Root補正を反映
            transform.position += hitboxData.frames[currentFrame].rootOffset * Time.deltaTime;
        }
    }

    void OnDrawGizmos()
    {
        if (hitboxData == null || hitboxData.frames == null) return;

        var hb = hitboxData.frames[currentFrame];
        if (!hb.active) return; // 攻撃判定フレームのみ表示

        // 赤いHitbox
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + hb.hitboxPos, hb.hitboxSize);

        // 青いRoot補正
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + hb.rootOffset);
    }
}

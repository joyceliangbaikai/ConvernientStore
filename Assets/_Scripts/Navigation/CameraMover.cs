using System.Collections;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public Transform rig;              // CameraRig
    public float moveDuration = 0.6f;  // 移动用时
    public float cooldownAfterMove = 1.2f; // 到达后“加载卡顿”

    public bool IsBusy { get; private set; }  // ✅ 外部可读：移动/冷却中都算忙

    float baseY;
    Coroutine co;

    void Awake()
    {
        if (rig != null)
            baseY = rig.position.y;
    }

    public void MoveTo(Transform target)
    {
        if (target == null || rig == null) return;
        if (IsBusy) return; // ✅ 忙的时候不接受新移动

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(MoveRoutine(target.position));
    }

    IEnumerator MoveRoutine(Vector3 targetPos)
    {
        IsBusy = true;

        Vector3 start = rig.position;

        // 永远锁 Y
        start.y = baseY;
        targetPos.y = baseY;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, moveDuration);
            rig.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        rig.position = targetPos;

        // ✅ 到达后卡一下（加载感）
        if (cooldownAfterMove > 0f)
            yield return new WaitForSeconds(cooldownAfterMove);

        IsBusy = false;
        co = null;
    }
}

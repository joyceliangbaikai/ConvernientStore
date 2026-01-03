using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [Header("Data")]
    public ItemData data;

    [Header("State")]
    public bool collected = false;

    [Header("Optional")]
    public bool onlyInteractIfInShoppingList = false; // 勾上=只允许清单里的物品弹窗

    void OnMouseDown()
    {
        // UI开着 / 输入锁着时，不让再点
        if (InputLock.Locked) return;

        // 没配数据就别玩
        if (data == null)
        {
            Debug.LogWarning($"[ItemPickup] '{name}' has no ItemData assigned.");
            return;
        }

        // 如果你想：已经收集过就不弹（你说想能重复点，那就不拦）
        // if (collected) return;

        // 可选：只允许清单里的物品交互
        if (onlyInteractIfInShoppingList && GameManager.Instance != null)
        {
            if (!GameManager.Instance.currentShoppingList.Contains(data))
            {
                Debug.Log($"[ItemPickup] '{data.displayName}' not in shopping list.");
                return;
            }
        }

        // 打开UI
        if (ItemInspectUI.Instance == null)
        {
            Debug.LogError("[ItemPickup] ItemInspectUI.Instance is null. Put ItemInspectUI in scene.");
            return;
        }

        ItemInspectUI.Instance.Open(this);
    }
}

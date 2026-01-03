using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Item Data")]
    public List<ItemData> allItems = new List<ItemData>();   // 所有可用商品
    public List<ItemData> currentShoppingList = new List<ItemData>(); // 本局购物清单

    [Header("Shopping List Settings")]
    public int shoppingListCount = 3; // 每局要买几个
    private HashSet<string> collectedIds = new HashSet<string>();

    public void OnItemCollected(ItemData item)
    {
        if (item == null) return;
        if (string.IsNullOrEmpty(item.id)) item.id = item.displayName; // 兜底

        if (collectedIds.Add(item.id))
        {
            Debug.Log($"[GameManager] Collected: {item.displayName}");
            // 以后你要“拿到麦片触发熄灯”就写在这里
        }
    }

    void Awake()
    {
        // 单例（现在不需要深究，照抄就行）
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GenerateShoppingList();
    }

    /// <summary>
    /// 随机生成一局购物清单
    /// </summary>
    public void GenerateShoppingList()
    {
        currentShoppingList.Clear();

        if (allItems.Count == 0)
        {
            Debug.LogWarning("[GameManager] No items assigned.");
            return;
        }

        List<ItemData> pool = new List<ItemData>(allItems);

        for (int i = 0; i < shoppingListCount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            currentShoppingList.Add(pool[index]);
            pool.RemoveAt(index);
        }

        Debug.Log("[GameManager] Shopping List Generated:");
        foreach (var item in currentShoppingList)
        {
            Debug.Log($"- {item.displayName}");
        }
    }
}

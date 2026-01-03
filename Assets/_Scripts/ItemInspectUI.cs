using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemInspectUI : MonoBehaviour
{
    public static ItemInspectUI Instance;

    [Header("UI Refs")]
    public GameObject root;          // 整个弹窗Panel（含遮罩）
    public Image packImage;          // 显示包装图
    public TMP_Text titleText;       // 显示名字
    public Button putButton;         // 放入
    public Button closeButton;       // 退出/关闭

    ItemPickup current;

    void Awake()
    {
        // 单例防呆：场景里只能有一个
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CloseImmediate();
    }

    // ✅ 外部调用：ItemPickup 点到后就调用这个
    public void Open(ItemPickup pickup)
    {
        // 防呆：没传进来 / 没data 直接不打开
        if (pickup == null || pickup.data == null)
        {
            Debug.LogWarning("[ItemInspectUI] Open failed: pickup or pickup.data is null.");
            return;
        }

        current = pickup;

        // 打开UI并锁输入
        if (root != null) root.SetActive(true);
        InputLock.Locked = true;

        // 填充内容
        if (packImage != null) packImage.sprite = pickup.data.icon;
        if (titleText != null) titleText.text = pickup.data.displayName;

        // 绑按钮（每次打开都重新绑，避免叠加）
        if (putButton != null)
        {
            putButton.onClick.RemoveAllListeners();
            putButton.onClick.AddListener(OnPut);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }
    }

    void OnPut()
    {
        if (current == null || current.data == null)
        {
            Close();
            return;
        }

        // 已收集（场景商品不消失）
        current.collected = true;

        // 通知 GameManager（如果你有这个函数的话）
        if (GameManager.Instance != null)
            GameManager.Instance.OnItemCollected(current.data);

        Close();
    }

    public void Close()
    {
        current = null;

        if (root != null) root.SetActive(false);
        InputLock.Locked = false;
    }

    void CloseImmediate()
    {
        current = null;

        if (root != null) root.SetActive(false);
        InputLock.Locked = false;
    }
}

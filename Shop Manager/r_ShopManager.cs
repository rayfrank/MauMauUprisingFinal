using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    #region Serializable Enums
    [System.Serializable] public enum r_ShopItemType { All, PrimaryWeapon, SecondaryWeapon, Grenade, Knife }
    [System.Serializable] public enum r_ShopOpentype { Shop, Inventory }
    #endregion

    public class r_ShopManager : MonoBehaviour
    {
        public static r_ShopManager instance;

        #region Public Variables
        [Space(10)] public List<r_ShopItemConfig> m_Items = new List<r_ShopItemConfig>();

        [Header("Content UI")]
        public Button m_ShopButton;
        public Button m_InventoryButton;

        [Header("Content UI")]
        public Transform m_ShopContent;
        public Transform m_InventoryContent;

        [Header("Prefab UI")]
        public r_ShopItemUI m_ShopItem;

        [Header("Balance UI")]
        public Text m_BalanceText;
        public Text m_PurchaseStateText;

        [Header("Shop Settings")]
        public float m_FirstTimeBonusCurrency;
        #endregion

        #region Functions
        private void Awake()
        {
            if (instance)
            {
                Destroy(instance);
                Destroy(instance.gameObject);
            }

            instance = this;

            HandleButtons();
        }

        private void Start() => CheckCurrency();
        #endregion

        #region Actions
        private void HandleButtons()
        {
            this.m_ShopButton.onClick.AddListener(delegate { OnValidateShop(r_ShopOpentype.Shop, r_ShopItemType.All); });
            this.m_InventoryButton.onClick.AddListener(delegate { OnValidateShop(r_ShopOpentype.Inventory, r_ShopItemType.All); });
        }

        private void CheckCurrency()
        {
            if (!PlayerPrefs.HasKey("Currency")) PlayerPrefs.SetFloat("Currency", this.m_FirstTimeBonusCurrency);

            UpdateCurrencyDisplay();
        }

        private void UpdateCurrencyDisplay() => this.m_BalanceText.text = ("Balance: $" + PlayerPrefs.GetFloat("Currency").ToString()).ToUpper();

        private void OnValidateShop(r_ShopOpentype _shopOpenType, r_ShopItemType _itemType)
        {
            switch (_shopOpenType)
            {
                case r_ShopOpentype.Shop: Cleanup(this.m_ShopContent); OnShopOpen(_itemType); break;
                case r_ShopOpentype.Inventory: Cleanup(this.m_InventoryContent); OnInventoryOpen(_itemType); break;
            }
        }

        private void OnShopOpen(r_ShopItemType _itemType)
        {
            r_ShopItemConfig[] _items = GetItems(_itemType).ToArray();

            if (_items.Length > 0)
            {
                foreach (r_ShopItemConfig _item in _items)
                {
                    r_ShopItemUI _itemUI = (r_ShopItemUI)Instantiate(this.m_ShopItem, this.m_ShopContent);

                    _itemUI.OnValidateItem(r_ShopOpentype.Shop, _item);
                }
            }
        }

        private void OnInventoryOpen(r_ShopItemType _itemType)
        {
            r_ShopItemConfig[] _items = GetItems(_itemType).ToArray();

            if (_items.Length > 0)
            {
                foreach (r_ShopItemConfig _item in _items)
                {
                    if (_item.m_IsPurchased)
                    {
                        r_ShopItemUI _itemUI = (r_ShopItemUI)Instantiate(this.m_ShopItem, this.m_InventoryContent);

                        _itemUI.OnValidateItem(r_ShopOpentype.Inventory, _item);
                    }
                }
            }
        }

        private List<r_ShopItemConfig> GetItems(r_ShopItemType _itemType)
        {
            List<r_ShopItemConfig> _items = new List<r_ShopItemConfig>();

            for (int i = 0; i < this.m_Items.Count; i++)
            {
                if (_itemType == r_ShopItemType.All)
                {
                    _items.Add(this.m_Items[i]);
                }
                else
                {
                    if (this.m_Items[i].m_ItemType == _itemType)
                    {
                        _items.Add(this.m_Items[i]);
                    }
                }
            }
            return _items;
        }

        private void Cleanup(Transform _content)
        {
            if (_content.childCount > 0)
            {
                foreach (Transform _transform in _content)
                    Destroy(_transform.gameObject);
            }
        }

        public void PurchaseItem(r_ShopItemConfig _item, r_ShopItemUI _itemUI)
        {
            if (!_item.m_IsPurchased)
            {
                if (GetBalance() >= _item.m_ItemPrice)
                {
                    _itemUI.SuccessfullPurchase();

                    DecreaseBalance(_item.m_ItemPrice);

                    StartCoroutine(UpdatePurchaseStateText(true));

#if UNITY_EDITOR
                    EditorUtility.SetDirty(_item);
#endif
                }
                else
                {
                    Debug.Log("Insufficient balance");

                    StartCoroutine(UpdatePurchaseStateText(false));
                }
            }
        }

        private IEnumerator UpdatePurchaseStateText(bool _purchased)
        {
            this.m_PurchaseStateText.gameObject.SetActive(true);

            if (_purchased)
            {
                this.m_PurchaseStateText.text = ($"<color=green>Successfully Purchased</color>").ToUpper();
            }
            else
            {
                this.m_PurchaseStateText.text = ($"<color=red>Insufficient balance</color>").ToUpper();
            }

            yield return new WaitForSeconds(2f);

            this.m_PurchaseStateText.gameObject.SetActive(false);
        }

        private void DecreaseBalance(float _amount)
        {
            PlayerPrefs.SetFloat("Currency", GetBalance() - _amount);

            UpdateCurrencyDisplay();
        }
        #endregion

        #region Get
        private float GetBalance() => PlayerPrefs.GetFloat("Currency");
        #endregion
    }
}
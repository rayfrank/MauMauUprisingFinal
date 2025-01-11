using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForceCodeFPS
{
    public class r_ShopItemUI : MonoBehaviour
    {
        #region Public variables
        [Header("Item UI")]
        public Text m_ItemNameText;
        public RawImage m_ItemImage;

        [Header("Item Interactable")]
        public Button m_ItemPurchaseButton;

        [Header("Item Data")]
        [HideInInspector] public r_ShopItemConfig m_ShopItemConfig;
        #endregion

        #region Functions
        private void Awake()
        {
            this.m_ItemPurchaseButton.onClick.AddListener(delegate
            {
                if (!this.m_ShopItemConfig.m_IsPurchased)
                {
                //Purchase item if not owned
                r_ShopManager.instance.PurchaseItem(this.m_ShopItemConfig, this);
                }
                else
                {
                //Equip item if owned
                r_LoadoutManagerMenu.instance.SaveLoadoutWeapon(this.m_ShopItemConfig.m_LoadoutWeapon);
                }
            });
        }
        #endregion

        #region Actions
        public void OnValidateItem(r_ShopOpentype _shopOpenType, r_ShopItemConfig _item)
        {
            if (_item != null)
            {
                this.m_ShopItemConfig = _item;

                switch (_shopOpenType)
                {
                    case r_ShopOpentype.Shop: OnShopItem(); break;
                    case r_ShopOpentype.Inventory: OnInventoryItem(); break;
                }
            }
        }

        public void OnShopItem()
        {
            //Set shop item information

            this.m_ItemNameText.text = this.m_ShopItemConfig.m_ItemName + " ($" + this.m_ShopItemConfig.m_ItemPrice.ToString() + ")";

            this.m_ItemImage.texture = this.m_ShopItemConfig.m_ItemTexture;

            this.m_ItemPurchaseButton.interactable = !this.m_ShopItemConfig.m_IsPurchased;

            this.m_ItemPurchaseButton.GetComponentInChildren<Text>().text = this.m_ShopItemConfig.m_IsPurchased ? "Purchased" : "Purchase";
        }

        public void OnInventoryItem()
        {
            //Set inventory item information

            this.m_ItemNameText.text = this.m_ShopItemConfig.m_ItemName;

            this.m_ItemImage.texture = this.m_ShopItemConfig.m_ItemTexture;

            this.m_ItemPurchaseButton.GetComponentInChildren<Text>().text = this.m_ShopItemConfig.m_IsPurchased ? "Equip" : "Purchase";
        }

        public void SuccessfullPurchase()
        {
            //Successfully 
            this.m_ShopItemConfig.m_IsPurchased = true;

            //Setup again to change button state
            OnShopItem();
        }
        #endregion
    }
}
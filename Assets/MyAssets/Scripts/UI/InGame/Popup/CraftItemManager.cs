﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System.Data;

public class CraftItemManager : MonoBehaviour {

    [SerializeField]
    private GameObject itemSlotPrefab;
    [SerializeField]
    private GameObject uiGridObj;
    [SerializeField]
    private GameObject popupObj;
    [SerializeField]
    private UIPopupList selectCraftItemList;
    [SerializeField]
    private UIPopupList selectQuantityList;
    [SerializeField]
    private UISprite spr_afterItemImg;

    private readonly int defaultItemSlot = 9;
    private List<ItemData> itemSlotList = new List<ItemData>();

    private CraftItemListDataFile craftItemDataFile;
    private ItemDataFile itemDataFile;  
    
    void Start ()
    {
        GameObject obj = GameObject.Find("craftItemListDataFile");
        craftItemDataFile = obj.GetComponent<CraftItemListDataFile>();

        obj = GameObject.Find("ItemDataFile");
        itemDataFile = obj.GetComponent<ItemDataFile>();

        spr_afterItemImg.spriteName = string.Empty;

        Ed_OnClickQuantityList = new EventDelegate(this, "OnClickQuantTityList");
        selectQuantityList.onChange.Add(Ed_OnClickQuantityList);

        CreateEmptySlot(defaultItemSlot);
        SetDropDownList();
        ScaleUpEffect();
    }
    
    public void OnClickMakeItem()
    {
        UpdateUserItems();
    }

    private void UpdateUserItems()
    {
        if(ChkPossibleMakeItem() == true)
        {
            // consume user materials, and user item info update
            CraftItem item;
            craftItemDataFile.craftItems.TryGetValue(selectCraftItemList.value, out item);
            foreach (CraftRawMaterial raw in item.rawMaterials)
            {
                int userMatAmount = GetUserMaterialAmount(raw.id);
                int amount = userMatAmount - raw.consumeAmount;

                if (amount == 0) DeleteUserMaterial(raw.id);
                else SetUserMaterialAmount(raw.id, amount);
            }

            // Set CraftItem to user
            ItemInfo itemInfo = itemDataFile.GetItemData(item.craftItemID);
            int craftItemType = int.Parse(itemInfo.type);
            SetCraftItemToUser(item.craftItemID, item.craftItemName,
                int.Parse(selectQuantityList.value),
                craftItemType);

            GameMessage.SetGameMsgType = GameMessage.MESSAGE_TYPE.CRAFT_ITEM_SUCCESS;
            GameMessage.SetMessage("아이템 제작에 성공했습니다.");
            UIPopupManager.OpenGameMessage();
        }
        else
        {
            GameMessage.SetGameMsgType = GameMessage.MESSAGE_TYPE.CRAFT_ITEM_FAIL;
            GameMessage.SetMessage("수량부족으로 아이템 제작이 불가능합니다.");
            UIPopupManager.OpenGameMessage();
        }
    }

    private int GetUserMaterialAmount(string itemID)
    {
        string conn = "URI=file:" + Application.dataPath +
               "/StreamingAssets/GameUserDB/userDB.db";

        int amount;
        using (IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn))
        {
            dbconn.Open(); //Open connection to the database.
            using (IDbCommand dbcmd = dbconn.CreateCommand())
            {
                string sqlQuery = "SELECT amount FROM USER_ITEM WHERE id = "
                                  + "'" + itemID + "'";
                dbcmd.CommandText = sqlQuery;
                IDataReader reader = dbcmd.ExecuteReader();

                amount = 0;
                reader.Read();
                amount = reader.GetInt32(0);

                reader.Close();
                reader = null;
            }
            dbconn.Close();
        }
        return amount;
    }

    private void SetCraftItemToUser(string itemID, string itemName, int itemAmount, int itemType)
    {
        string conn = "URI=file:" + Application.dataPath +
               "/StreamingAssets/GameUserDB/userDB.db";
        string sqlQuery = "INSERT INTO USER_ITEM (name, type, amount, id) VALUES ("
                                 + "'" + itemName + "'" + ", " 
                                 + itemType + ", " + itemAmount + ", " + "'" + itemID + "'" + ")";
        using (IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn))
        {
            dbconn.Open(); //Open connection to the database.
            using (IDbCommand dbcmd = dbconn.CreateCommand())
            {
                try
                {
                    dbcmd.CommandText = sqlQuery;
                    dbcmd.ExecuteNonQuery();
                    dbconn.Close();
                }
                catch // 인벤토리에 중복된 아이템이 있다면, 수량증가를 해야한다.
                {
                    sqlQuery = "SELECT amount FROM USER_ITEM WHERE id = "
                                          + "'" + itemID + "'";
                    dbcmd.CommandText = sqlQuery;
                    IDataReader reader = dbcmd.ExecuteReader();
                    reader.Read();
                    int userInvenAmount = reader.GetInt32(0);
                    userInvenAmount += itemAmount;
                    reader.Close();

                    sqlQuery = "UPDATE USER_ITEM SET amount = " + "'" + userInvenAmount + "'" +
                                " WHERE id = " + "'" + itemID + "'";
                    dbcmd.CommandText = sqlQuery;
                    dbcmd.ExecuteNonQuery();
                    dbconn.Close();
                }
            }
            dbconn.Close();
        }
    }

    private void SetUserMaterialAmount(string itemID, int itemAmount)
    {
        string conn = "URI=file:" + Application.dataPath +
               "/StreamingAssets/GameUserDB/userDB.db";

        using (IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn))
        {
            dbconn.Open(); //Open connection to the database.
            using (IDbCommand dbcmd = dbconn.CreateCommand())
            {
                string sqlQuery = "UPDATE USER_ITEM SET amount = " + "'" + itemAmount + "'" +
                    " WHERE id = " + "'" + itemID + "'";
                dbcmd.CommandText = sqlQuery;
                dbcmd.ExecuteNonQuery();
            }
            dbconn.Close();
        }
    }

    private void DeleteUserMaterial(string itemID)
    {
        string conn = "URI=file:" + Application.dataPath +
               "/StreamingAssets/GameUserDB/userDB.db";

        using (IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn))
        {
            dbconn.Open(); //Open connection to the database.
            using (IDbCommand dbcmd = dbconn.CreateCommand())
            {
                string sqlQuery = "DELETE FROM USER_ITEM WHERE id = " + "'" + itemID + "'";

                dbcmd.CommandText = sqlQuery;
                dbcmd.ExecuteNonQuery();
            }
            dbconn.Close();
        }
    }

    private bool ChkPossibleMakeItem()
    {
        bool isPossibleMakeItem = false;

        CraftItem item;
        craftItemDataFile.craftItems.TryGetValue(selectCraftItemList.value, out item);
        foreach (CraftRawMaterial raw in item.rawMaterials)
        {
            isPossibleMakeItem = ChkMaterialAmount(raw.id,
                raw.consumeAmount * int.Parse(selectQuantityList.value));
            if (isPossibleMakeItem == false) return false;
        }
        return true;
    }

    private bool ChkMaterialAmount(string itemID, int needAmount)
    {
        string conn = "URI=file:" + Application.dataPath +
               "/StreamingAssets/GameUserDB/userDB.db";

        string sqlQuery = "SELECT amount FROM USER_ITEM WHERE id = "
                          + "'" + itemID + "'";
        using (IDbConnection dbconn = (IDbConnection)new SqliteConnection(conn))
        {
            dbconn.Open(); //Open connection to the database.
            using (IDbCommand dbcmd = dbconn.CreateCommand())
            {
                dbcmd.CommandText = sqlQuery;
                using (IDataReader reader = dbcmd.ExecuteReader())
                {
                    try
                    {
                        int amount = 0;
                        reader.Read();
                        amount = reader.GetInt32(0);
                        reader.Close();
                        dbconn.Close();
                        if (amount >= needAmount) return true;
                        else return false;
                    }
                    catch // 사용자에게 해당 재료 아이템이 없다.
                    {
                        reader.Close();
                        dbconn.Close();
                        return false;
                    }
                }
            }
        }
    }

    private void SetDropDownList()
    {
        foreach(var craftItem in craftItemDataFile.craftItems)
        {
            selectCraftItemList.AddItem(craftItem.Value.craftItemName);
        }
        // set event delegate
        Ed_OnClickCraftItemList = new EventDelegate(this, "OnClickCraftItemList");
        selectCraftItemList.onChange.Add(Ed_OnClickCraftItemList);
    }

    private EventDelegate Ed_OnClickQuantityList;
    private void OnClickQuantTityList()
    {
        UpdateConsumeAmount();
    }

    private void UpdateConsumeAmount()
    {
        CraftItem item;
        craftItemDataFile.craftItems.TryGetValue(selectCraftItemList.value, out item);
        int slotIdx = 0;
        foreach (CraftRawMaterial raw in item.rawMaterials)
        {
            string calcedAmount = (raw.consumeAmount * int.Parse(selectQuantityList.value)).ToString();
            itemSlotList[slotIdx].amount = calcedAmount;
            itemSlotList[slotIdx].InitAmountData();
            slotIdx++;
        }
    }

    private EventDelegate Ed_OnClickCraftItemList;
    private void OnClickCraftItemList()
    {
        ShowRawMaterials();
        UpdateConsumeAmount();
    }

    private void ShowRawMaterials()
    {
        ClearItemSlot();

        string selectItemName = selectCraftItemList.value;
        spr_afterItemImg.spriteName = selectItemName;

        CraftItem item;
        craftItemDataFile.craftItems.TryGetValue(selectItemName, out item);
        int slotIdx = 0;
        foreach (CraftRawMaterial raw in item.rawMaterials)
        {
            if (slotIdx > defaultItemSlot) CreateEmptySlot(5);
          
            itemSlotList[slotIdx].itemName = raw.rawMaterialName;
            itemSlotList[slotIdx].type = itemDataFile.GetItemData(raw.id).type;
            itemSlotList[slotIdx].detailInfo = itemDataFile.GetItemData(raw.id).detailInfo;
            itemSlotList[slotIdx].amount = raw.consumeAmount.ToString();
            itemSlotList[slotIdx].InitAllData();
            itemSlotList[slotIdx].OnInfo();

            //set event delegate
            Ed_OnClickItem = new EventDelegate(this, "OnClickItem");
            Ed_OnClickItem.parameters[0].value = itemSlotList[slotIdx];
            itemSlotList[slotIdx].GetComponent<UIButton>().onClick.Add(Ed_OnClickItem);

            slotIdx++;
        }
    }

    private EventDelegate Ed_OnClickItem;
    private void OnClickItem(ItemData itemData)
    {
        SceneToScene_Data.popupItemInfo.Clear();
        SceneToScene_Data.popupItemInfo.id = itemData.id;
        SceneToScene_Data.popupItemInfo.name = itemData.itemName;
        SceneToScene_Data.popupItemInfo.type = itemData.type;
        SceneToScene_Data.popupItemInfo.amount = itemData.amount;
        SceneToScene_Data.popupItemInfo.detailInfo = itemData.detailInfo;

        UIPopupManager.OpenItemData();
    }

    private void ClearItemSlot()
    {
        foreach(ItemData itemData in itemSlotList)
        {
            itemData.itemName = string.Empty;
            itemData.amount = string.Empty;
            itemData.InitAllData();
            itemData.OffInfo();
        }
    }

    private void CreateEmptySlot(int _num)
    {
        for (int idx = 0; idx < _num; ++idx)
        {
            GameObject newItem = Instantiate(itemSlotPrefab,
                new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0)) as GameObject;

            newItem.SetActive(true);
            newItem.GetComponent<ItemData>().OffInfo();
            //item parenting
            newItem.transform.parent = uiGridObj.transform;
            newItem.transform.localScale = new Vector3(1, 1, 1);
            newItem.transform.localPosition = new Vector3(0, 0, 0);

            itemSlotList.Add(newItem.GetComponent<ItemData>());
        }
        uiGridObj.GetComponent<UIGrid>().Reposition();
    }

    private void ScaleUpEffect()
    {
        popupObj.transform.localScale = new Vector3(0, 0, 0);
        Vector3 scaleUp = new Vector3(1, 1, 1);
        iTween.ScaleTo(popupObj, iTween.Hash("scale", scaleUp,
            "name", "scaleUp",
            "time", 1.0f,
            "speed", 10.0f,
            "easetype", iTween.EaseType.linear,
            "looptype", iTween.LoopType.none));
    }
    private void ScaleDownEffect(string _callBack)
    {
        popupObj.transform.localScale = new Vector3(1, 1, 1);
        Vector3 scaleDown = new Vector3(0, 0, 0);
        iTween.ScaleTo(popupObj, iTween.Hash("scale", scaleDown,
            "name", "scaleDown",
            "time", 1.0f,
            "speed", 10.0f,
            "easetype", iTween.EaseType.linear,
            "looptype", iTween.LoopType.none,
            "oncomplete", _callBack,
            "oncompletetarget", gameObject));
    }

    public void OnClose()
    {
        ScaleDownEffect("CallBackPopupClose");
    }

    private void CallBackPopupClose()
    {
        UIPopupManager.CloseCraftItem();
    }
}

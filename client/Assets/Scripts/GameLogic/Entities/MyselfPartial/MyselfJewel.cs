using System;
using System.Collections.Generic;
using Mogo.Util;
using UnityEngine;

namespace Mogo.Game
{
    public partial class EntityMyself
    {
        #region 宝石系统
        void JewelAddResp(LuaTable _table)
        {
            Debug.Log("JewelAddResp");
        }

        void JewelCombineInEquiResp(byte errorId)
        {
            Debug.Log("JewelCombineInEquiResp");
            InsetManager.Instance.JewelCombineInEquiResp(errorId);
        }

        void JewelCombineResp(byte subType, byte level, byte errorId)
        {
            Debug.Log("subType:" + subType + ",level:" + level + ",errorId:" + errorId);
            inventoryManager.JewelCombineResp(subType, level, errorId);
        }

        void JewelCombineInEqiResp(byte errorId)
        {
            inventoryManager.JewelCombineInEqiResp(errorId);
        }

        void JewelCombineAnywayMoneyResp(uint money)
        {
            Debug.Log("JewelCombineAnywayMoneyResp:" + money);
            inventoryManager.JewelCombineAnywayMoneyResp(money);
        }

        void JewelCombineAnywayResp(byte errorId)
        {
            inventoryManager.JewelCombineAnywayResp(errorId);
        }
        void JewelInlayIntoSlotResp(byte errorId)
        {
            inventoryManager.JewelInlayIntoEqiResp(errorId);
        }

        void JewelInlayResp(byte errorId)
        {
            inventoryManager.JewelInlayResp(errorId);
        }

        void JewelOutlayResp(byte errorId)
        {
            inventoryManager.JewelOutlayResp(errorId);
        }

        void JewelCanCombineResp(LuaTable _table)
        {
        }

        void JewelSellResp(int gold)
        {
            inventoryManager.JewelSellResp(gold);
        }
        #endregion
    }
}

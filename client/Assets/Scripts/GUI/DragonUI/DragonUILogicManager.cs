using UnityEngine;
using System.Collections;
using Mogo.Util;

public class DragonUILogicManager
{

    private static DragonUILogicManager m_instance;

    public static DragonUILogicManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new DragonUILogicManager();
            }

            return DragonUILogicManager.m_instance;

        }
    }

    void OnDiamondWishUp()
    {
        Debug.Log("DiamondWishUp");
        EventDispatcher.TriggerEvent(Events.RuneEvent.RMBRefresh);
    }

    void OnGoldWishUp()
    {
        Debug.Log("GoldWishUp");
        EventDispatcher.TriggerEvent(Events.RuneEvent.GameMoneyRefresh);
    }

    void OnGotoRuneUp()
    {
        Debug.Log("GotoRuneUp");
    }

    void OnOneKeyComposeUp()
    {
        Debug.Log("OnekeyComposeUp");
        EventDispatcher.TriggerEvent<bool>(Events.RuneEvent.AutoCombine, true);
    }

    void OnOneKeyPickUpUp()
    {
        Debug.Log("OnekeyPickUpUp");
        EventDispatcher.TriggerEvent(Events.RuneEvent.AutoPickUp);
    }

    void OnDragonUICloseUp()
    {
        Debug.Log("OnDragonUICloseUp");
        EventDispatcher.TriggerEvent(Events.RuneEvent.CloseDragon);
    }

    void OnDragonUIPackageGridUp(int id)
    {
        Debug.Log("DragonUIPackageGridUp " + id);
        EventDispatcher.TriggerEvent<int, bool>(Events.RuneEvent.UseRune, id, true);

    }

    void OnDragonUIPackageGridUpDouble(int id)
    {
        Debug.Log("DragonUIPackageGridUpDouble " + id);
    }

    void OnDragonUIPackageGridDrag(int newGrid, int oldGrid)
    {
        Debug.Log(newGrid + " " + oldGrid);
        EventDispatcher.TriggerEvent<int, int, bool>(Events.RuneEvent.ChangeIndex, oldGrid, newGrid, true);
    }


    public void Initialize()
    {
        DragonUIViewManager.Instance.DIAMONDWISHUP += OnDiamondWishUp;
        DragonUIViewManager.Instance.GOLDWISHUP += OnGoldWishUp;
        DragonUIViewManager.Instance.GOTORUNEUP += OnGotoRuneUp;
        DragonUIViewManager.Instance.ONEKEYCOMPOSEUP += OnOneKeyComposeUp;
        DragonUIViewManager.Instance.ONEKEYPICKUPUP += OnOneKeyPickUpUp;
        DragonUIViewManager.Instance.DRAGONUICLOSEUP += OnDragonUICloseUp;
        DragonUIViewManager.Instance.DRAGONUIPACKAGEGRIDUP += OnDragonUIPackageGridUp;
        DragonUIViewManager.Instance.DRAGONUIPACKAGEGRIDUPDOUBLE += OnDragonUIPackageGridUpDouble;

        EventDispatcher.AddEventListener<int, int>("DragonUIPackageGridDrag", OnDragonUIPackageGridDrag);
    }

    public void Release()
    {
        DragonUIViewManager.Instance.DIAMONDWISHUP -= OnDiamondWishUp;
        DragonUIViewManager.Instance.GOLDWISHUP -= OnGoldWishUp;
        DragonUIViewManager.Instance.GOTORUNEUP -= OnGotoRuneUp;
        DragonUIViewManager.Instance.ONEKEYCOMPOSEUP -= OnOneKeyComposeUp;
        DragonUIViewManager.Instance.ONEKEYPICKUPUP -= OnOneKeyPickUpUp;
        DragonUIViewManager.Instance.DRAGONUICLOSEUP -= OnDragonUICloseUp;
        DragonUIViewManager.Instance.DRAGONUIPACKAGEGRIDUP -= OnDragonUIPackageGridUp;
        DragonUIViewManager.Instance.DRAGONUIPACKAGEGRIDUPDOUBLE -= OnDragonUIPackageGridUpDouble;

        EventDispatcher.RemoveEventListener<int, int>("DragonUIPackageGridDrag", OnDragonUIPackageGridDrag);
    }
}

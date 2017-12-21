using UnityEngine;
using System.Collections;

using Mogo.Util;

public class InsetUILogicManager
{

    private static InsetUILogicManager m_instance;

    public static InsetUILogicManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new InsetUILogicManager();
            }

            return InsetUILogicManager.m_instance;

        }
    }

    void OnInsetEquipmentGridUp(int id)
    {
        Debug.Log(id + " Up");
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_EQUIP_SELECT, id);
    }

    void OnInsetPackageGridUp(int id)
    {
        Debug.Log(id + " Up");
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_JEWEL_SELECT, id);
    }

    void OnInsetDiamondGridUp(int id)
    {
        Debug.Log(id + " Grid UP");
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_JEWEL_SLOT_SELECT,id);

    }

    void OnInsetDiamonUnLoadUp(int id)
    {
        Debug.Log(id + " UnLoad Up");
        EventDispatcher.TriggerEvent<int>(InsetManager.DISASSEMBLE_JEWEL, id);
    }

    void OnInsetDiamondUpdateUp(int id)
    {
        Debug.Log(id + " Update Up");
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_JEWEL_UPGRADE, id);
       
    }

    void OnInsetDiamondGridUpDouble(int id)
    {
        Debug.Log(id + " Grid up Double");
    }

    void OnInsetPacakgeGridDragBegin(int id)
    {

        Debug.Log(id + "Begin");
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_JEWEL_DRAG, id);
    }

    void OnInsetPackageGridDrag(int newId, int oldId)
    {

        Debug.Log(newId + " " + oldId);
        EventDispatcher.TriggerEvent<int>(InsetManager.ON_INSET_JEWEL, newId);
    }

    void OnInsetDialogDiamondTipInsetUp(int i)
    {
        Debug.Log("Inset");
        EventDispatcher.TriggerEvent(InsetManager.ON_INSET_JEWEL,-1);
    }

    public void Initialize()
    {
        InsetUIDict.INSETUIEQUIPMENTGRIDUP += OnInsetEquipmentGridUp;
        InsetUIDict.INSETUIPACKAGEGRIDUP += OnInsetPackageGridUp;

        InsetUIDict.INSETDIAMONDGRIDUP += OnInsetDiamondGridUp;
        InsetUIDict.INSETDIAMONDGRIDUPDOUBLE += OnInsetDiamondGridUpDouble;
        InsetUIDict.INSETDIAMONDUNLOADUP += OnInsetDiamonUnLoadUp;
        InsetUIDict.INSETDIAMONDUPDATEUP += OnInsetDiamondUpdateUp;

        InsetUIDict.INSETPACKAGEGRIDDRAGBEGIN += OnInsetPacakgeGridDragBegin;
        InsetUIDict.INSETPACKAGEGRIDDRAG += OnInsetPackageGridDrag;

        InsetUIDict.INSETDIALOGDIAMONDTIPINSETUP += OnInsetDialogDiamondTipInsetUp;
    }

    public void Release()
    {
        InsetUIDict.INSETUIEQUIPMENTGRIDUP -= OnInsetEquipmentGridUp;
        InsetUIDict.INSETUIPACKAGEGRIDUP -= OnInsetPackageGridUp;

        InsetUIDict.INSETDIAMONDGRIDUP -= OnInsetDiamondGridUp;
        InsetUIDict.INSETDIAMONDGRIDUPDOUBLE -= OnInsetDiamondGridUpDouble;
        InsetUIDict.INSETDIAMONDUNLOADUP -= OnInsetDiamonUnLoadUp;
        InsetUIDict.INSETDIAMONDUPDATEUP -= OnInsetDiamondUpdateUp;

        InsetUIDict.INSETPACKAGEGRIDDRAGBEGIN -= OnInsetPacakgeGridDragBegin;
        InsetUIDict.INSETPACKAGEGRIDDRAG -= OnInsetPackageGridDrag;
        InsetUIDict.INSETDIALOGDIAMONDTIPINSETUP -= OnInsetDialogDiamondTipInsetUp;
    }
}

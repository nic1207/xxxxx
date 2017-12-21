/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：ComposeUIButton
// 创建者：MaiFeo
// 修改者列表：
// 创建日期：
// 模块描述：
//----------------------------------------------------------------*/

using UnityEngine;
using System.Collections;
using Mogo.Util;

public class ComposeUIButton : MonoBehaviour
{
    #region 公共变量

    #endregion

    #region 私有变量

    #endregion


    void Awake()
    {
        gameObject.AddComponent<MogoFakeClick>().ReletedClassType = ReleadClassType.Type_ComposeUI;
    }

    //void OnPress(bool isOver)
    //{
    //    if (isOver)
    //    {
    //    }
    //    else
    //    {
    //        Camera camera = GameObject.Find("MogoMainUI").transform.GetChild(0).GetComponentInChildren<Camera>();
    //        BoxCollider bc = transform.GetComponentInChildren<BoxCollider>();

    //        RaycastHit hit = new RaycastHit();

    //        if (bc.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hit, 10000.0f))
    //        {
    //            if (ComposeUIDict.ButtonTypeToEventUp[transform.name] == null)
    //            {
    //                Debug.LogError("No ButtonTypeToEventUp Info");
    //                return;
    //            }

    //            ComposeUIDict.ButtonTypeToEventUp[transform.name]();
    //        }
    //    }

    //}

    void OnClick()
    {
        if (ComposeUIDict.ButtonTypeToEventUp[transform.name] == null)
        {
            Debug.LogError("No ButtonTypeToEventUp Info");
            return;
        }

        ComposeUIDict.ButtonTypeToEventUp[transform.name]();
    }

    public void FakePress(bool isPressed)
    {
        Debug.Log("FakePressBegin!!!!!!");
        if (ComposeUIDict.ButtonTypeToEventUp[transform.name] == null)
        {
            Debug.LogError("No ButtonTypeToEventUp Info");
            return;
        }

        ComposeUIDict.ButtonTypeToEventUp[transform.name]();
        Debug.Log("FakePress!!!!!!");
    }
}

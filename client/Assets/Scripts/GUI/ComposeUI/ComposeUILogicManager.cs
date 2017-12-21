/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：ComposeUILogicManager
// 创建者：MaiFeo
// 修改者列表：
// 创建日期：
// 模块描述：
//----------------------------------------------------------------*/

using UnityEngine;
using System.Collections;
using Mogo.Util;

public class ComposeUILogicManager
{
    #region 公共变量

    #endregion

    #region 私有变量

    #endregion

    private static ComposeUILogicManager m_instance;

    public static ComposeUILogicManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new ComposeUILogicManager();
            }

            return ComposeUILogicManager.m_instance;

        }
    }

    void OnComposeBuyUp()
    {
        Debug.Log("ComposeBuyUp");
        EventDispatcher.TriggerEvent(ComposeManager.ON_BUY);
    }

    void OnComposeComposeUp()
    {
        Debug.Log("ComposeComposeUp");
        EventDispatcher.TriggerEvent(ComposeManager.ON_COMPOSE);
    }

    void OnComposeComposeNowUp()
    {
        Debug.Log("ComposeComposeNowUp");
        EventDispatcher.TriggerEvent(ComposeManager.ON_COMPOSE_NOW);
    }

    void OnComposeIconGridUp(int id)
    {
        Debug.Log(id);
        EventDispatcher.TriggerEvent<int>(ComposeManager.ON_JEWEL_TYPE_SELECT, id);
    }

    void OnComposeIconChildGridUp(int parentId, int id)
    {
        Debug.Log(parentId + " " + id);
        EventDispatcher.TriggerEvent<int, int>(ComposeManager.ON_JEWEL_SELECT, parentId, id);
    }

    public void Initialize()
    {
        ComposeUIViewManager.Instance.COMPOSEBUYUP += OnComposeBuyUp;
        ComposeUIViewManager.Instance.COMPOSECOMPOSEUP += OnComposeComposeUp;
        ComposeUIViewManager.Instance.COMPOSECOMPOSENOWUP += OnComposeComposeNowUp;

        EventDispatcher.AddEventListener<int>("ComposeIconGridUp", OnComposeIconGridUp);
        EventDispatcher.AddEventListener<int, int>("ComposeIconChildGridUp", OnComposeIconChildGridUp);

    }

    public void Release()
    {

        ComposeUIViewManager.Instance.COMPOSEBUYUP -= OnComposeBuyUp;
        ComposeUIViewManager.Instance.COMPOSECOMPOSEUP -= OnComposeComposeUp;
        ComposeUIViewManager.Instance.COMPOSECOMPOSENOWUP -= OnComposeComposeNowUp;

        EventDispatcher.RemoveEventListener<int>("ComposeIconGridUp", OnComposeIconGridUp);
        EventDispatcher.RemoveEventListener<int, int>("ComposeIconChildGridUp", OnComposeIconChildGridUp);
    }
}

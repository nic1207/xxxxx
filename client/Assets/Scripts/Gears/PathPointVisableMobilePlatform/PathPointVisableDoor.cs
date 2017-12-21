using UnityEngine;
using System.Collections;
using Mogo.Util;

public class PathPointVisableDoor : GearParent
{
    public Transform beginGoTransform;
    public Transform beginBackTransform;

    protected Vector3 beginGo;
    protected Vector3 beginBack;

    void Start()
    {
        gearType = "MobilePlatformDoor";

        beginGo = beginGoTransform.position;
        beginBack = beginBackTransform.position;
    }

    public void ChangeBeginGo()
    {
        Debug.Log("ChangeBeginGo");
        transform.position = beginGo;
    }

    public void ChangeBeginBack()
    {
        Debug.Log("ChangeBeginBack");
        transform.position = beginBack;
    }

    public void ChangeMoving(Transform theTransform)
    {
        Debug.Log("ChangeMoving");
        transform.position = theTransform.position;
    }
}

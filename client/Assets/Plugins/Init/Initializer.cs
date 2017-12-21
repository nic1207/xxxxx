using UnityEngine;

public class Initializer : MonoBehaviour
{
	void Start () 
    {
		#if UNITY_ANDROID
        gameObject.AddComponent<Driver>();
		#elif UNITY_IPHONE
		gameObject.AddComponent<Driver>();
		#else
		gameObject.AddComponent<Driver>();
		#endif
        DestroyImmediate(this);
	}
}

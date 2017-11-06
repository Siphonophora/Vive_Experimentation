using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerManager : MonoBehaviour {

    private SteamVR_TrackedObject trackedObject;
    private SteamVR_Controller.Device device;

    public GameObject ObjectToSpawn;


	// Use this for initialization
	void Start () {
        trackedObject = GetComponent<SteamVR_TrackedObject>();

	}
	
	// Update is called once per frame
	void Update () {
        device = SteamVR_Controller.Input((int)trackedObject.index);

        if (device.GetPressDown(SteamVR_Controller.ButtonMask.Trigger))
        {
            Vector3 loc = trackedObject.transform.position;
            Instantiate(ObjectToSpawn, loc, Quaternion.identity);
        }
	}
}

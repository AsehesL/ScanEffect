using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Example : MonoBehaviour {
    
	
	void Update () {
	    if (Input.GetMouseButtonDown(0))
	    {
	        LedSystem.CallScan(transform.position);
	    }
	}
}

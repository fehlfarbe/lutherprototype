using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnityOSCListenerWorms : MonoBehaviour  {
	public void OSCMessageReceived(OSC.NET.OSCMessage message){	
		string address = message.Address;
		ArrayList args = message.Values;

		//Debug.Log(address);

		switch(address) {
		case "/start":
			Debug.Log("Erkennung läuft");
			break;
		case "/newface":
			Debug.Log("newface id" + args[0]);
			GameObject.Find("init").GetComponent<worms>().triggerPersonIn((int)args[0]);
			break;
		case "/deleteface":
			Debug.Log("deleteface" + args[0]);
			GameObject.Find("init").GetComponent<worms>().triggerPersonOut((int)args[0]);
			break;
		case "/facelist":
			GameObject.Find("init").GetComponent<worms>().handleFacelist((int)args[0], (int)args[3]);
			break;
		case "/end":
			Debug.Log("Erkennung beendet");
			break;
		default: 
			//Debug.Log("no input");
			break;
		}


	}
}

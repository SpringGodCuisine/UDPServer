using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour {
	ClientUdp _udp;
	// Use this for initialization
	void Start () {
		ServerTcp.Instance.StartServer();



		  _udp = new ClientUdp(); 
		_udp.StartClientUdp("192.168.1.18", 1);
		 
	}

	void OnApplicationQuit()
	{
		Debug.Log("quit"); 
		_udp.EndClientUdp();
		ServerTcp.Instance.EndServer();

	}

	 
}

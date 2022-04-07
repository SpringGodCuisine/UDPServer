using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour {
	private ClientUdp udp;
	// Use this for initialization
	private void Start () {
		ServerTcp.Instance.StartServer();
		
		//udp
		udp = new ClientUdp(); 
		udp.StartClientUdp("192.168.3.151", 1);
	}

	private void OnApplicationQuit()
	{
		Debug.Log("quit"); 
		udp.EndClientUdp();
		ServerTcp.Instance.EndServer();
	}
}

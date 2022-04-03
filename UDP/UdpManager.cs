using UnityEngine;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System;
public class UdpManager {
	private static UdpManager singleInstance;
	private static readonly object padlock = new object();

	public UdpClient _udpClient = null;
	public int recvPort;
	public static UdpManager Instance
	{
		get
		{
			lock (padlock)
			{
				if (singleInstance==null)
				{
					singleInstance = new UdpManager();
				}
				return singleInstance;
			}
		}
	}

	private UdpManager()
	{
		CreatUdp ();
	}

	public void Creat(){

	}

	void CreatUdp(){
		_udpClient = new UdpClient (10011);   
		// uint IOC_IN = 0x80000000;
		// uint IOC_VENDOR = 0x18000000;
		// uint SIO_UDP_CONNRESET = IOC_IN | IOC_VENDOR | 12;
		//byte[] optionOutValue = new byte[4];
		//byte[] optionInValue = { Convert.ToByte(false) };
		//_udpClient.Client.IOControl((int)SIO_UDP_CONNRESET, optionInValue, optionOutValue);


		IPEndPoint _localip = (IPEndPoint)_udpClient.Client.LocalEndPoint;
		Debug.Log ("udp端口:" + _localip.Port);
		recvPort = _localip.Port;
	}

	public void Destory(){

		CloseUdpClient ();
		singleInstance = null;
	}

	public void CloseUdpClient(){
		if (_udpClient != null) {
			Debug.Log("CloseUdpClient  **************** ");
			_udpClient.Close ();
			_udpClient = null;
		}
	}

	public UdpClient GetClient(){
		if (_udpClient == null) {
			CreatUdp ();
		}
		return _udpClient;
	}


}

using UnityEngine;
using System;
using System.Text;
using System.Threading;

using System.Net;
using System.Net.Sockets;

public class ClientUdp {
	  

	public int userUid;
	private int sendPortNum;
	private UdpClient sendClient = null;
	private IPEndPoint sendEndPort;
	private bool isRun;
	private string serverIp;

	public void StartClientUdp(string ip,int uid){

		if (sendEndPort != null) {
			Debug.Log ("客户端udp已经启动~");
			return;
		}

		userUid = uid;
		serverIp = ip;
		isRun = true;

		sendClient = UdpManager.Instance.GetClient();
//		sendClient = new UdpClient(NormalData.recvPort);
//		sendEndPort = new IPEndPoint(IPAddress.Parse(_ip), ServerConfig.udpRecvPort);	

		var t = new Thread(new ThreadStart(RecvThread));
		t.Start();


	}

	public void EndClientUdp(){
		try {
			isRun = false;
		 
				UdpManager.Instance.CloseUdpClient();
				sendClient = null;
				sendEndPort = null;
			 
		} catch (Exception ex) {
			Debug.Log ("udp连接关闭异常:" + ex.Message);
		}

	}

	private void CreatSendEndPort(int port){
		sendEndPort = new IPEndPoint(IPAddress.Parse(serverIp), port);
	}

	public void SendMessage(byte[] mes)
	{
		if (!isRun) return;
		try {
			sendClient.Send (mes,mes.Length,sendEndPort);	
            //GameData.Instance().sendNum+=_mes.Length;
			//Debug.Log("发送量:" + _mes.Length.ToString() + "," + GameData.Instance().sendNum.ToString());
		} catch (Exception ex) {
			Debug.Log ("udp发送失败:" + ex.Message);
		}
	}


	public void RecvClientReady(int _userUid){
		if (_userUid == userUid && sendEndPort == null) {
			CreatSendEndPort(sendPortNum);
		}
	}
    // 接收线程不断 监听客户端的消息
	private void RecvThread()
	{

		var endpoint = new IPEndPoint(IPAddress.Parse(serverIp), UdpManager.Instance.recvPort);
		while (isRun)
		{
			try {
				var buf = sendClient.Receive(ref endpoint);

				if (sendEndPort == null) {
					//Debug.Log("接收客户端udp信息:" + endpoint.Port);
					sendPortNum = endpoint.Port;
				}
				var str = System.Text.Encoding.UTF8.GetString(buf);

				Debug.Log("str *** " + str);

				//byte packMessageId = buf[PackageConstant.PackMessageIdOffset];     //消息id (1个字节)
				//Int16 packlength = BitConverter.ToInt16(buf,PackageConstant.PacklengthOffset);  //消息包长度 (2个字节)
				//int bodyDataLenth = packlength - PackageConstant.PacketHeadLength;
				//byte[] bodyData = new byte[bodyDataLenth];
				//Array.Copy(buf, PackageConstant.PacketHeadLength, bodyData, 0, bodyDataLenth);

				//delegate_analyze_message((PBCommon.CSID)packMessageId,bodyData);

				//是客户端,统计接收量
				//GameData.Instance().recvNum+=buf.Length;
				//Debug.Log("发送量:" + buf.Length.ToString() + "," + GameData.Instance().recvNum.ToString());
			} catch (Exception ex) {
				Debug.Log (endpoint.Address+ "udpClient接收数据异常:" + ex.Message);
			}
		}
		Debug.Log ("udp接收线程退出~~~~~");
	}


	
}
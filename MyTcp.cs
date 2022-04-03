using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO; 

public class MyTcp
{
	private static MyTcp singleInstance;
	private static readonly object padlock = new object();

	private byte[] result = new byte[1024];
	private Socket clientSocket;

	public bool isRun = false;

	private Action<bool> ac_connect;
	public static MyTcp Instance
	{
		get
		{
			lock (padlock)  // 加锁保证单例唯一
			{
				if (singleInstance == null)
				{
					singleInstance = new MyTcp();
				}
				return singleInstance;
			}
		}
	}

	public void ConnectServer(string _ip, Action<bool> _result)
	{
		//设定服务器IP地址  
		ac_connect = _result;
		IPAddress ip;
		bool _isRight = IPAddress.TryParse(_ip, out ip);

		if (!_isRight)
		{
			Debug.Log("无效地址......" + _ip);
			_result(false);
			return;
		}
		clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		IPEndPoint _endpoint = new IPEndPoint(ip, 13001);
		Debug.Log("开始连接tcp~");
		clientSocket.BeginConnect(_endpoint, requestConnectCallBack, clientSocket);
	}
	private void requestConnectCallBack(IAsyncResult iar)
	{
		try
		{
			//还原原始的TcpClient对象
			Socket client = (Socket)iar.AsyncState;
			//
			client.EndConnect(iar);

			Debug.Log("连接服务器成功:" + client.RemoteEndPoint.ToString());
			isRun = true;
			ac_connect(true);
			//NetGlobal.Instance().AddAction(() => {
			//	if (ac_connect != null)
			//	{
			//		ac_connect(true);
			//	}
			//});


			Thread myThread = new Thread(ReceiveMessage);
			myThread.Start();
		}
		catch (Exception e)
		{
			ac_connect(false);
			//NetGlobal.Instance().AddAction(() => {
			//	if (ac_connect != null)
			//	{
			//		ac_connect(false);
			//	}
			//});

			Debug.Log("tcp连接异常:" + e.Message);
		}
		finally
		{

		}
	}

	private void ReceiveMessage()
	{

		while (isRun)
		{
			try
			{

				if (!clientSocket.Connected)
				{
					throw new Exception("tcp客户端关闭了~~~");
				}

				//通过clientSocket接收数据  
				int _size = clientSocket.Receive(result);

				if (_size <= 0)
				{
					throw new Exception("客户端关闭了2~");
				}


				byte packMessageId = result[PackageConstant.PackMessageIdOffset];     //消息id (1个字节)
				Int16 packlength = BitConverter.ToInt16(result, PackageConstant.PacklengthOffset);  //消息包长度 (2个字节)
				int bodyDataLenth = packlength - PackageConstant.PacketHeadLength;  // 计算包体长度
				byte[] bodyData = new byte[bodyDataLenth];
				Array.Copy(result, PackageConstant.PacketHeadLength, bodyData, 0, bodyDataLenth);

			//	TcpPB.Instance().AnalyzeMessage((PBCommon.SCID)packMessageId, bodyData);
			}
			catch (Exception ex)
			{
				Debug.Log("接收服务端数据异常:" + ex.Message);
			//	EndClient();
				break;
			}
		}
	}

	public void SendMessage(byte[] _mes)
	{
		if (isRun)
		{
			try
			{
				clientSocket.Send(_mes);
			}
			catch (Exception ex)
			{
			//	EndClient();
				Debug.Log("发送数据异常:" + ex.Message);
			}
		}
	}


	public struct PackageConstant
	{
		public static int PackMessageIdOffset = 0;
		// 消息id (1个字节)
		public static int PacklengthOffset = 1;
		//消息包长度 (2个字节)
		public static int PacketHeadLength = 3;
		//包头长度
	}

	//public class CSData
	//{
	//	public static byte[] GetSendMessage<T>(T pb_Body, PBCommon.CSID messageID)
	//	{
	//		byte[] packageBody = CSData.SerializeData<T>(pb_Body);
	//		byte packMessageId = (byte)messageID; //消息id (1个字节)

	//		int packlength = PackageConstant.PacketHeadLength + packageBody.Length; //消息包长度 (2个字节)
	//		byte[] packlengthByte = BitConverter.GetBytes((short)packlength);

	//		List<byte> packageHeadList = new List<byte>();
	//		//包头信息
	//		packageHeadList.Add(packMessageId);
	//		packageHeadList.AddRange(packlengthByte);
	//		//包体
	//		packageHeadList.AddRange(packageBody);

	//		return packageHeadList.ToArray();
	//	}


	//	public static byte[] SerializeData<T>(T instance)
	//	{
	//		byte[] bytes;
	//		using (var ms = new MemoryStream())
	//		{
	//			ProtoBuf.Serializer.Serialize(ms, instance);
	//			bytes = new byte[ms.Position];
	//			var fullBytes = ms.GetBuffer();
	//			Array.Copy(fullBytes, bytes, bytes.Length);
	//		}
	//		return bytes;
	//	}

	//	public static T DeserializeData<T>(byte[] bytes)
	//	{
	//		using (Stream ms = new MemoryStream(bytes))
	//		{
	//			return ProtoBuf.Serializer.Deserialize<T>(ms);
	//		}
	//	}
	//}

}

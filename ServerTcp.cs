using UnityEngine;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

public class ServerTcp {
 
	static Socket serverSocket;  

	private bool isRun = false;
	private Dictionary<string,Socket> dic_clientSocket = new Dictionary<string, Socket>();


	private static readonly object stLockObj = new object ();
	private static ServerTcp instance;

	int headSize = 2;//包头长度 固定2
	byte[] saveBuffer = null;//不完整的数据包，即用户自定义缓冲区


	public static ServerTcp Instance
	{
		get{ 
			lock (stLockObj) {
				if (instance == null)
				{
					instance = new ServerTcp();
				}	
			}
			return instance;
		}
	}

	private ServerTcp()
	{
		
	}

	public void Destory(){
		instance = null;
	}

	public void StartServer(){

		try {
			IPAddress ip = IPAddress.Parse("192.168.1.5");

			serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);  

			serverSocket.Bind(new IPEndPoint(ip, 13001));  //绑定IP地址：端口  
			serverSocket.Listen(1000);    //设定最多10个排队连接请求  
			Debug.Log("启动监听" + serverSocket.LocalEndPoint.ToString() + "成功");
			isRun = true;

			//通过Clientsoket发送数据  
			Thread myThread = new Thread(ListenClientConnect);  
			myThread.Start();  	

		} catch (Exception ex) {
			Debug.Log ("服务器启动失败:" + ex.Message);
		}    
	}



	private void ListenClientConnect()  
	{  
		while (isRun)  
		{  
			try {
				Socket clientSocket = serverSocket.Accept();   


				Thread receiveThread = new Thread(ReceiveMessage);  
				receiveThread.Start(clientSocket);  	
			} catch (Exception ex) {
				Debug.Log ("监听失败:" + ex.Message);
			}
		}  
	}

    //				clientSocket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

    public void EndServer(){

		if (!isRun) {
			return;
		}

		isRun = false;
		try {
			foreach (var item in dic_clientSocket) {
				item.Value.Close ();
			}

			dic_clientSocket.Clear ();

			if (serverSocket != null) {
				serverSocket.Close ();
				serverSocket = null;	
			}	
		} catch (Exception ex) {
			Debug.Log("tcp服务器关闭失败:" + ex.Message);
		}

	}

	public void CloseClientTcp(string _socketIp){
		try {
			if (dic_clientSocket.ContainsKey(_socketIp)) {
				if (dic_clientSocket [_socketIp] != null) {
					dic_clientSocket [_socketIp].Close();
				}
				dic_clientSocket.Remove (_socketIp);
			}	
		} catch (Exception ex) {
			Debug.Log ("关闭客户端..." + ex.Message);
		}

	}

	public int GetClientCount(){
		return dic_clientSocket.Count;
	}

	public List<string> GetAllClientIp(){
		return new List<string> (dic_clientSocket.Keys);
	}

		 
	private void ReceiveMessage(object clientSocket)  
	{  
		Socket myClientSocket = (Socket)clientSocket;
      //  Debug.Log(myClientSocket.RemoteEndPoint.ToString());
		string _socketIp = myClientSocket.RemoteEndPoint.ToString().Split(':')[0]; 

		Debug.Log ("有客户端连接:" + _socketIp);

		dic_clientSocket[_socketIp] = myClientSocket;	

		bool _flag = true;

		byte[] resultData = new byte[1048];
		while (isRun && _flag)  
		{  
			try  
			{  
 			    Debug.Log("_socketName是否连接:" + myClientSocket.Connected);
			 
				int _size = myClientSocket.Receive(resultData);   
				if (_size <= 0) {
					throw new Exception("客户端关闭了222~");
				}

				// string str = System.Text.Encoding.UTF8.GetString(resultData);
			

				 OnReceive(0, resultData);

				//Int16 packlength = BitConverter.ToInt16(resultData, 0);
				//byte[] bodyData = new byte[packlength];
				//Array.Copy(resultData, 2, bodyData, 0, packlength);
			 //  string str = System.Text.Encoding.UTF8.GetString(bodyData);
				//Debug.Log("str ==" + str);
				//Debug.Log(_size);
				//Int16 packlength  = BitConverter.ToInt16(resultData, 0);
				//byte[] bodyData = new byte[packlength];
				//Array.Copy(resultData, 2, bodyData, 0, packlength); 
				//string str = System.Text.Encoding.UTF8.GetString(bodyData); 
				//Debug.Log(str);


			}
			catch (Exception ex)  
			{  
				Debug.Log(_socketIp + "接收客户端数据异常: " + ex.Message);  

				_flag = false;
				break;  
			}  
		}  
			
		CloseClientTcp (_socketIp);
	}  

 
		
	public void SendMessage(string _socketName,byte[] _mes){
        Debug.Log("SendMessage aaa  ----- _socketName  " + _socketName); 
        if (isRun) {
			try {
				dic_clientSocket [_socketName].Send (_mes);	
			} catch (Exception ex) {
				Debug.Log ("发数据给异常:" + ex.Message);
			}	
		}

	}

	private bool OnReceive(int connId, byte[] bytes)
	{
	 
		// 系统缓冲区长度
		int bytesRead = bytes.Length;
		if (bytesRead > 0)
		{
			if (saveBuffer == null)//第一次接收
				saveBuffer = bytes;//把系统缓冲区数据放在自定义缓冲区里面
			else
				saveBuffer = saveBuffer.Concat(bytes).ToArray();//拼接上次尾包
															
			int haveRead = 0;         //已经完成读取的数据包长度
									 
			int totalLen = saveBuffer.Length; //这里totalLen的长度有可能大于缓冲区大小的(因为 这里的saveBuffer 是系统缓冲区+不完整的数据包)
			while (haveRead <= totalLen)
			{
				//如果在N次拆解后剩余的数据包 小于 包头的长度 
				//则剩下的是非完整的数据包
				if (totalLen - haveRead < headSize)
				{
					byte[] byteSub = new byte[totalLen - haveRead];
					//把剩下不够一个完整的数据包存起来
					Buffer.BlockCopy(saveBuffer, haveRead, byteSub, 0, totalLen - haveRead);
					saveBuffer = byteSub;
					totalLen = 0;
					break;
				}
				//如果够了一个完整包，则读取包头的数据
				byte[] headByte = new byte[headSize];
				Buffer.BlockCopy(saveBuffer, haveRead, headByte, 0, headSize);//从缓冲区里读取包头的字节
				int bodySize = BitConverter.ToInt16(headByte, 0);//从包头里面分析出包体的长度

				//这里的 haveRead=等于N个数据包的长度 从0开始；0,1,2,3....N
				//如果自定义缓冲区拆解N个包后的长度 大于 总长度，说最后一段数据不够一个完整的包了，拆出来保存
				if (haveRead + headSize + bodySize > totalLen)
				{
					byte[] byteSub = new byte[totalLen - haveRead];
					Buffer.BlockCopy(saveBuffer, haveRead, byteSub, 0, totalLen - haveRead);
					saveBuffer = byteSub;
					break;
				}
				else
				{
					if (bodySize == 0)
					{ 
						 saveBuffer = null;
						 break;
					}
					//挨个分解每个包，解析成实际文字 
					String strc = Encoding.UTF8.GetString(saveBuffer, haveRead + headSize, bodySize);
					Debug.Log("得到包"  + strc); 
					//依次累加当前的数据包的长度
					haveRead = haveRead + headSize + bodySize;
					if (headSize + bodySize == bytesRead)//如果当前接收的数据包长度正好等于缓冲区长度，则待拼接的不规则数据长度归0
					{
						saveBuffer = null;//设置空 回到原始状态
						totalLen = 0;//清0
					}
				}
			}
		}
		return true;
	}



	//private static void ReceiveMessage()
	//{
	//	while (true)
	//	{
	//		//接受消息头（消息校验码4字节 + 消息长度4字节 + 身份ID8字节 + 主命令4字节 + 子命令4字节 + 加密方式4字节 = 28字节）
	//		int HeadLength = 28;
	//		//存储消息头的所有字节数
	//		byte[] recvBytesHead = new byte[HeadLength];
	//		//如果当前需要接收的字节数大于0，则循环接收
	//		while (HeadLength > 0)
	//		{
	//			byte[] recvBytes1 = new byte[28];
	//			//将本次传输已经接收到的字节数置0
	//			int iBytesHead = 0;
	//			//如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收
	//			if (HeadLength >= recvBytes1.Length)
	//			{
	//				iBytesHead = socketClient.Receive(recvBytes1, recvBytes1.Length, 0);
	//			}
	//			else
	//			{
	//				iBytesHead = socketClient.Receive(recvBytes1, HeadLength, 0);
	//			}
	//			//将接收到的字节数保存
	//			recvBytes1.CopyTo(recvBytesHead, recvBytesHead.Length - HeadLength);
	//			//减去已经接收到的字节数
	//			HeadLength -= iBytesHead;
	//		}
	//		//接收消息体（消息体的长度存储在消息头的4至8索引位置的字节里）
	//		byte[] bytes = new byte[4];
	//		Array.Copy(recvBytesHead, 4, bytes, 0, 4);
	//		int BodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0));
	//		//存储消息体的所有字节数
	//		byte[] recvBytesBody = new byte[BodyLength];
	//		//如果当前需要接收的字节数大于0，则循环接收
	//		while (BodyLength > 0)
	//		{
	//			byte[] recvBytes2 = new byte[BodyLength < 1024 ? BodyLength : 1024];
	//			//将本次传输已经接收到的字节数置0
	//			int iBytesBody = 0;
	//			//如果当前需要接收的字节数大于缓存区大小，则按缓存区大小进行接收，相反则按剩余需要接收的字节数进行接收
	//			if (BodyLength >= recvBytes2.Length)
	//			{
	//				iBytesBody = socketClient.Receive(recvBytes2, recvBytes2.Length, 0);
	//			}
	//			else
	//			{
	//				iBytesBody = socketClient.Receive(recvBytes2, BodyLength, 0);
	//			}
	//			//将接收到的字节数保存
	//			recvBytes2.CopyTo(recvBytesBody, recvBytesBody.Length - BodyLength);
	//			//减去已经接收到的字节数
	//			BodyLength -= iBytesBody;
	//		}
	//		//一个消息包接收完毕，解析消息包
	//		UnpackData(recvBytesHead, recvBytesBody);
	//	}
	//}
	///// <summary>
	///// 解析消息包
	///// </summary>
	///// <param name="Head">消息头</param>
	///// <param name="Body">消息体</param>
	//public static void UnpackData(byte[] Head, byte[] Body)
	//{
	//	byte[] bytes = new byte[4];
	//	Array.Copy(Head, 0, bytes, 0, 4);
	//	Debug.Log("接收到数据包中的校验码为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

	//	bytes = new byte[8];
	//	Array.Copy(Head, 8, bytes, 0, 8);
	//	Debug.Log("接收到数据包中的身份ID为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt64(bytes, 0)));

	//	bytes = new byte[4];
	//	Array.Copy(Head, 16, bytes, 0, 4);
	//	Debug.Log("接收到数据包中的数据主命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

	//	bytes = new byte[4];
	//	Array.Copy(Head, 20, bytes, 0, 4);
	//	Debug.Log("接收到数据包中的数据子命令为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

	//	bytes = new byte[4];
	//	Array.Copy(Head, 24, bytes, 0, 4);
	//	Debug.Log("接收到数据包中的数据加密方式为：" + IPAddress.NetworkToHostOrder(BitConverter.ToInt32(bytes, 0)));

	//	bytes = new byte[Body.Length];
	//	for (int i = 0; i < Body.Length;)
	//	{
	//		byte[] _byte = new byte[4];
	//		Array.Copy(Body, i, _byte, 0, 4);
	//		i += 4;
	//		int num = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_byte, 0));

	//		_byte = new byte[num];
	//		Array.Copy(Body, i, _byte, 0, num);
	//		i += num;
	//		Debug.Log("接收到数据包中的数据有：" + Encoding.UTF8.GetString(_byte, 0, _byte.Length));
	//	}
	//}
}

using UnityEngine;                        // These are the librarys being used
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.Threading;


public class Socket : MonoBehaviour {
	
	bool socketReady = false;
	Thread receiveThread;
	TcpClient mySocket;
	public NetworkStream theStream;
	StreamWriter theWriter;
	StreamReader theReader;
	public String host = "127.0.0.1";
	public Int32 port = 13000; 
	public String lineRead = "<root><v>5</v><delta>3</delta><brake>1</brake></root>";	


	float parsedSpeed;
	float parsedAngle;
	int parsedBrake;
	TcpListener tcp_listener;
	
	public float getParsedSpeed()
	{
		return parsedSpeed/5;
	}
	
	public float getParsedAngle()
	{
		return parsedAngle;
	}
	
	public float getParsedBrake()
	{
		return parsedBrake;
	}
	
	
	void Start() {
//		receiveThread = new Thread(
//			new ThreadStart(ReceiveData));
//		receiveThread.IsBackground = true;
//		receiveThread.Start();
		
	}

	private  void ReceiveData()
	{
		setupSocket ();
		while (true)
		{
			try
			{
				string text = theReader.ReadLine();
				
				lineRead = text;
				ParseXML();
				Thread.Sleep (1);
			}
			catch (Exception err)
			{
				print(err.ToString());
			}
		}
	}


	
	
	void Update() {

		
	}
	
	public void setupSocket() {                            // Socket setup here
		try {
			IPAddress ip_addy = IPAddress.Parse(host);
			tcp_listener = new TcpListener(ip_addy, port);
			this.tcp_listener.Start();
			mySocket = this.tcp_listener.AcceptTcpClient();
			//mySocket = new TcpClient(Host, Port);
			theStream = mySocket.GetStream();
			theWriter = new StreamWriter(theStream);
			theReader = new StreamReader(theStream);
			socketReady = true;
			Debug.Log("Connection started");
		}
		catch (Exception e) {
			Debug.Log("Socket error:" + e);                // catch any exceptions
		}
	}

	public void ParseXML()
	{
		try{
			XmlDocument xmlDoc = new XmlDocument(); // xmlDoc is the new xml document.
			Debug.Log (lineRead);
			xmlDoc.LoadXml(lineRead); // load the file.
			XmlNodeList levelsList = xmlDoc.GetElementsByTagName("root"); // array of the level nodes.
			
			foreach (XmlNode levelInfo in levelsList)
			{
				XmlNodeList levelcontent = levelInfo.ChildNodes;			
				foreach (XmlNode levelsItens in levelcontent) // levels itens nodes.
				{
					if(levelsItens.Name == "cadence")
					{
						parsedSpeed = float.Parse(levelsItens.InnerText);
					}
					
					if(levelsItens.Name == "delta")
					{
						parsedAngle = float.Parse(levelsItens.InnerText) * 360 / (2*(float)Math.PI);
					}
					
					if(levelsItens.Name == "brake")
					{
						parsedBrake =  int.Parse(levelsItens.InnerText);
					}
				}
			}
		}catch(Exception e){
			Debug.Log ("Write error:" + e);
		}
	}
	void OnApplicationQuit() 
	{
		if (receiveThread != null)
			receiveThread.Abort(); 

		if (mySocket != null)
			mySocket.Close(); 
	}

	
	
}
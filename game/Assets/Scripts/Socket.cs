using UnityEngine;                        // These are the librarys being used
using System.Collections;
using System;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Xml;


public class Socket : MonoBehaviour {
	
	bool socketReady = false;                // global variables are setup here
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
		return parsedSpeed;
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
		
		StartCoroutine(Run());
		
		// setup the server connection when the program starts
	}
	
	IEnumerator Run()
	{
		//setupSocket ();
		while (true) {
			String line = readSocket();
			if (line != null)
				lineRead = line;
			//writeSocket ("<root><torque>0</torque></root>");
			//Debug.Log(lineRead);
			ParseXML();
			//Debug.Log (lineRead);
			yield return new WaitForSeconds(0.01f);
		} 
		
	}
	
	
	// Update is called once per frame
	void Update() {
		// if new data is recieved from Arduino
		//string recievedData = readSocket();            // write it to a string
		
		
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
	
	public void writeSocket(string theLine) {            // function to write data out
		try {
			
			if (!socketReady)
				return;
			String tmpString = theLine;
			theWriter.WriteLine(tmpString);
			theWriter.Flush();
			Debug.Log ("Sent: " + tmpString);
		}catch (Exception e) {
			Debug.Log("Write error:" + e);                // catch any exceptions
		}
		
		
	}
	
	public String readSocket() {// function to read data in
		if (!socketReady) {
			return null;
		}
		if (theStream.DataAvailable) {
			String lineReceived = theReader.ReadLine ();
			//Debug.Log ("Received: " + lineReceived);
			return lineReceived;	
			//return "<root><delta>-0.31</delta><v>5.00</v><cadence>80</cadence><brake>0</brake></root>";
		}
		
		else
			return null;
		
	}
	
	public void closeSocket() {                            // function to close the socket
		if (!socketReady)
			return;
		theWriter.Close();
		theReader.Close();
		mySocket.Close();
		socketReady = false;
	}
	
	public void maintainConnection(){                    // function to maintain the connection (not sure why! but Im sure it will become a solution to a problem at somestage)
		if(!theStream.CanRead) {
			setupSocket();
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
					if(levelsItens.Name == "v")
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
	
	
}
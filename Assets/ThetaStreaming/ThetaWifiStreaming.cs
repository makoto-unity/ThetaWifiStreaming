using UnityEngine;
using System.Collections;
using System.Net;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using LitJson;

public class ThetaWifiStreaming : MonoBehaviour {

	private bool isLooping = true;
	private Renderer myRenderer;
	public string thetaUrl = "http://192.168.1.1:80";
	private string executeCmd = "/osc/commands/execute";
	public JsonData outputJson = new JsonData();

	// Use this for initialization
	IEnumerator Start () {
		string jsonStr;
		myRenderer = GetComponent<Renderer>();

		jsonStr = "{" +
			"\"name\" : \"camera.startSession\", " +
			"\"parameters\": {} " +
			"}";
		yield return StartCoroutine( SendThetaCmd( jsonStr ) );
		print( outputJson["results"]["sessionId"] );
		string sessionId = (string)(outputJson["results"]["sessionId"]);

		string url = thetaUrl + executeCmd;
		var request = HttpWebRequest.Create (url);
		HttpWebResponse response = null;
		request.Method = "POST";
		request.Timeout = (int)(30 * 10000f);
		request.ContentType = "application/json;charset=utf-8";

		byte[] postBytes = Encoding.Default.GetBytes ("{" +
			"\"name\": \"camera._getLivePreview\"," +
			"\"parameters\": { " +
				"\"sessionId\": \"" + sessionId +"\"" +
				"}" +
			"}");
		request.ContentLength = postBytes.Length;

		Stream reqStream = request.GetRequestStream ();
		reqStream.Write (postBytes, 0, postBytes.Length);
		reqStream.Close ();
		Stream stream = request.GetResponse ().GetResponseStream ();

		BinaryReader reader = new BinaryReader (new BufferedStream (stream), new System.Text.ASCIIEncoding ());

		List<byte> imageBytes = new List<byte> ();
		bool isLoadStart = false;
		while( isLooping ) { 
			byte byteData1 = reader.ReadByte ();
			byte byteData2 = reader.ReadByte ();

			if (!isLoadStart) {
				if (byteData1 == 0xFF && byteData2 == 0xD8){
					// mjpeg start! ( [0xFF 0xD8 ... )
					imageBytes.Add(byteData1);
					imageBytes.Add(byteData2);

					isLoadStart = true;
				}
			} else {
				imageBytes.Add(byteData1);
				imageBytes.Add(byteData2);

				if (byteData1 == 0xFF && byteData2 == 0xD9){
					// mjpeg end (... 0xFF 0xD9] )

					Texture2D tex = new Texture2D(2, 2);
					tex.LoadImage ((byte[])imageBytes.ToArray ());
					myRenderer.material.mainTexture = tex;
					imageBytes.Clear();
					yield return null;
					isLoadStart = false;
				}
			}
		}
	}

	public IEnumerator SendThetaCmd ( string inputJsonText ) {

		Dictionary<string, string> header = new Dictionary<string, string> ();
		header.Add ("Content-Type", "application/json; charset=UTF-8");

		byte[] postBytes = Encoding.Default.GetBytes (inputJsonText);

		string url = thetaUrl + executeCmd;
		WWW myWww = new WWW (url, postBytes, header);
		yield return myWww;

		if (myWww.error == null) {
			Debug.Log("Success");
			outputJson = JsonMapper.ToObject( myWww.text );
			print( myWww.text );
		}
		else{
			Debug.Log("Failure");          
		}
	}	
	// Update is called once per frame
	void Update () {
		if ( Input.GetKey(KeyCode.Escape) ) {
			isLooping = false;
		}
	}
}

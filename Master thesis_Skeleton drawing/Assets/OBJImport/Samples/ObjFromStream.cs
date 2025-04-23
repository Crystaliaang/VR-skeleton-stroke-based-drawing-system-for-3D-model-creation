//using Dummiesman;
//using System.IO;
//using System.Text;
//using UnityEngine;

//public class ObjFromStream : MonoBehaviour {
//	void Start () {
//        //make www
//        var www = new WWW("https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj");
//        while (!www.isDone)
//            System.Threading.Thread.Sleep(1);

//        //create stream and load
//        var textStream = new MemoryStream(Encoding.UTF8.GetBytes(www.text));
//        var loadedObj = new OBJLoader().Load(textStream);
//	}

//}


using Dummiesman;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections; // Required for IEnumerator and coroutines


public class ObjFromStream : MonoBehaviour
{
    void Start()
    {
        // Start the coroutine to load the OBJ file
        StartCoroutine(DownloadAndLoadObj("https://people.sc.fsu.edu/~jburkardt/data/obj/lamp.obj"));
    }

    private IEnumerator DownloadAndLoadObj(string url)
    {
        // Use UnityWebRequest to download the OBJ file
        UnityWebRequest www = UnityWebRequest.Get(url);

        // Send the web request and wait for it to complete
        yield return www.SendWebRequest();

        // Check if there's an error
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load OBJ: " + www.error);
        }
        else
        {
            // Get the downloaded OBJ file as text
            string objText = www.downloadHandler.text;

            // Create a memory stream from the downloaded text
            var textStream = new MemoryStream(Encoding.UTF8.GetBytes(objText));

            // Load the OBJ model using Dummiesman OBJLoader
            var loadedObj = new OBJLoader().Load(textStream);

            if (loadedObj != null)
            {
                Debug.Log("OBJ file loaded successfully!");
            }
            else
            {
                Debug.LogError("Failed to load OBJ file into the scene.");
            }
        }
    }
}

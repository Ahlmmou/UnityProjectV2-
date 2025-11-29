using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json.Linq;
using Palmmedia.ReportGenerator.Core.Reporting.Builders;
using UnityEngine;
using UnityEngine.Networking;


namespace Assets.Scripts
{
    public class GeminiChat : MonoBehaviour
    {
        [SerializeField] private string apiKey = "YOUR_KEY_HERE";
        private const string model = "gemini-1.5-flash";
        private string url;

        void Start()
        {
            url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
        }

        public IEnumerator SendMessageToGemini(string message, Action<string> callback)
        {
            JObject json = new JObject
            {
                ["contents"] = new JArray
                {
                    new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["text"] = message }
                        }
                    }
                }
            };

            UnityWebRequest req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json.ToString());
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                callback($"Error: {req.error}");
            }
            else
            {
                try
                {
                    JObject response = JObject.Parse(req.downloadHandler.text);
                    string reply = response["candidates"][0]["content"]["parts"][0]["text"].ToString();
                    callback(reply);
                }
                catch (Exception e)
                {
                    callback($"Error parsing response: {e.Message}");
                }
            }
        }
    }
}

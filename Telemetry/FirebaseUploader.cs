using Firebase.Database;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace TelemetrySystem
{
    public class FirebaseUploader : MonoBehaviour
    {

#if UNITY_ANDROID
        string jsonFileName;
        [SerializeField] string databasePath = "data/events"; // path dentro de la base de datos

        private string jsonString;
        private DatabaseReference dbRef;

        void Start()
        {
            // Obtiene dónde está el root de la base de datos
            dbRef = FirebaseDatabase.DefaultInstance.RootReference;
            jsonFileName = gameObject.GetComponent<Tracker>().finalFileNameDB;
            LoadAndUploadJson();
        }

        private async void LoadAndUploadJson()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, jsonFileName);

            if (File.Exists(filePath))
            {
                jsonString = File.ReadAllText(filePath);
                Debug.Log("JSON data loaded: " + jsonString);

                // Sube el JSON a Firebase
                await UploadJsonToFirebase(jsonString);
            }
            else
            {
                Debug.LogError("Cannot find JSON file!");
            }
        }

        public async Task UploadJsonToFirebase(string jsonString)
        {
            try
            {
                // Usa el metodo de SetRawJsonValueAsync para subir el string del JSON directamente
                await dbRef.Child(databasePath).SetRawJsonValueAsync(jsonString);
                Debug.Log("JSON data uploaded successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error uploading JSON data: " + e.Message);
            }
        }
#endif
    }

}
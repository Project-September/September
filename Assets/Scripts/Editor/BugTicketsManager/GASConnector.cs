#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace September.Editor.BugTicketsManager
{
    [Serializable]
    public class GasBugTicket
    {
        public string id;
        public string title;
        public string description;
        public string priority;
        public string status;
        public string assignedTo;
        public string createdAt;

        public GasBugTicket()
        {
            id = "";
            title = "";
            description = "";
            priority = "Medium";
            status = "Open";
            assignedTo = "";
            createdAt = ""; // GAS側で設定
        }

        public GasBugTicket(string id, string title, string description, string priority, string status,
            string assignedTo, string createdAt)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.priority = priority;
            this.status = status;
            this.assignedTo = assignedTo;
            this.createdAt = createdAt;
        }

        // BugTicketから変換
        public static GasBugTicket FromBugTicket(BugTicket bugTicket)
        {
            return new GasBugTicket(
                bugTicket.id,
                bugTicket.title,
                bugTicket.description,
                bugTicket.priority,
                bugTicket.status,
                bugTicket.assignedTo,
                bugTicket.createdAt
            );
        }

        // BugTicketに変換
        public BugTicket ToBugTicket()
        {
            return new BugTicket(
                id,
                title,
                description,
                priority,
                status,
                assignedTo,
                createdAt
            );
        }
    }

    [Serializable]
    public class GasResponse
    {
        public List<GasBugTicket> records;
    }

    public class GasConnector
    {
        private const string GasURL = "https://script.google.com/macros/s/AKfycbzC-vyipYgz8Qh0ZQJDplgL9pbZFyN34z2GMOdowUuSYgrYwAQHWtWM4aF6lDpMEuEmsQ/exec";

        public GasConnector()
        {
            Debug.Log("GASConnector initialized");
        }

        public async Task<List<BugTicket>> FetchBugTicketsAsync()
        {
            try
            {
                Debug.Log($"Fetching bug tickets from GAS: {GasURL}");

                using (var request = UnityWebRequest.Get(GasURL))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    // Debug.Log($"GAS Request completed with result: {request.result}");
                    // Debug.Log($"Response Code: {request.responseCode}");
                    // Debug.Log($"Response Text: {request.downloadHandler.text}");

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        // Debug.LogError($"GAS API Error: {request.error}");
                        // Debug.LogError($"Response Code: {request.responseCode}");
                        // Debug.LogError($"Response: {request.downloadHandler.text}");
                        throw new Exception($"GAS request failed: {request.error}");
                    }

                    var jsonResponse = request.downloadHandler.text;
                    var response = JsonConvert.DeserializeObject<GasResponse>(jsonResponse);

                    if (response?.records == null)
                    {
                        Debug.LogWarning("No records found in GAS response");
                        return new List<BugTicket>();
                    }

                    // GASBugTicketをBugTicketに変換
                    var bugTickets = new List<BugTicket>();
                    foreach (var gasBug in response.records)
                    {
                        bugTickets.Add(gasBug.ToBugTicket());
                    }

                    Debug.Log($"Successfully fetched {bugTickets.Count} bug tickets from GAS");
                    return bugTickets;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to fetch bug tickets from GAS: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> UpdateBugStatusAsync(string bugId, string newStatus)
        {
            try
            {
                Debug.Log($"Updating bug {bugId} status to {newStatus} via GAS");

                // Send update request with special action parameter
                var updateData = new
                {
                    action = "update",
                    id = bugId,
                    status = newStatus
                };

                var jsonData = JsonConvert.SerializeObject(updateData);
                Debug.Log($"Update request data: {jsonData}");

                using (var request = new UnityWebRequest(GasURL, "POST"))
                {
                    var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    Debug.Log($"Update request completed with result: {request.result}");
                    Debug.Log($"Response Code: {request.responseCode}");
                    Debug.Log($"Response Text: {request.downloadHandler.text}");

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to update bug status: {request.error}");
                        Debug.LogError($"Response: {request.downloadHandler.text}");
                        return false;
                    }

                    var jsonResponse = request.downloadHandler.text;

                    // Check if response contains error
                    if (jsonResponse.Contains("error") || jsonResponse.Contains("Error") ||
                        jsonResponse.Contains("TypeError"))
                    {
                        Debug.LogError($"GAS returned error: {jsonResponse}");
                        return false;
                    }

                    Debug.Log($"Successfully updated bug {bugId} status to {newStatus}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update bug status in GAS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> AddBugTicketAsync(BugTicket bugTicket)
        {
            try
            {
                Debug.Log($"Adding new bug ticket {bugTicket.id} via GAS");

                // GAS script expects an array of row data (matching spreadsheet columns)
                // Spreadsheet columns: id, title, description, priority, status, assignedTo, createdAt
                // createdAt has formula =IF(B2="",,IF(G2>0,G2,NOW())) and should not be overwritten
                var rowData = new object[]
                {
                    bugTicket.id,
                    bugTicket.title,
                    bugTicket.description,
                    bugTicket.priority,
                    bugTicket.status,
                    bugTicket.assignedTo
                    // createdAt列は除外（スプレッドシート側の関数を維持）
                };

                // Wrap in array as GAS script expects an array of rows
                var requestData = new object[] { rowData };
                var jsonData = JsonConvert.SerializeObject(requestData);
                Debug.Log($"Add request data: {jsonData}");

                using (var request = new UnityWebRequest(GasURL, "POST"))
                {
                    var bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");

                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    Debug.Log($"Add request completed with result: {request.result}");
                    Debug.Log($"Response Code: {request.responseCode}");
                    Debug.Log($"Response Text: {request.downloadHandler.text}");

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to add bug ticket: {request.error}");
                        Debug.LogError($"Response: {request.downloadHandler.text}");
                        return false;
                    }

                    var jsonResponse = request.downloadHandler.text;
                    Debug.Log($"Add response: {jsonResponse}");

                    // Check if response contains error
                    if (jsonResponse.Contains("error") || jsonResponse.Contains("Error"))
                    {
                        Debug.LogError($"GAS returned error: {jsonResponse}");
                        return false;
                    }

                    Debug.Log($"Successfully added new bug ticket {bugTicket.id}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to add bug ticket to GAS: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                Debug.Log($"Testing GAS connection: {GasURL}");

                using (var request = UnityWebRequest.Get(GasURL + "?test=true"))
                {
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }

                    Debug.Log($"Connection test result: {request.result}");
                    Debug.Log($"Response Code: {request.responseCode}");
                    Debug.Log($"Response: {request.downloadHandler.text}");

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"GAS connection test failed: {request.error}");
                        return false;
                    }

                    Debug.Log("GAS connection test successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"GAS connection test failed: {ex.Message}");
                return false;
            }
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using UnityEngine.Serialization;

namespace September.Editor.BugTicketsManager
{
    [Serializable]
    public class BugTicket
    {
        public string id;
        public string title;
        public string description;
        public string priority; // "やる", "やらない"
        public string status;   // "Open", "InProgress", "Resolved", "Closed"
        [FormerlySerializedAs("assignee")] public string assignedTo;
        public string createdAt;

        public BugTicket()
        {
            id = "";
            title = "";
            description = "";
            priority = "やる";
            status = "Open";
            assignedTo = "";
            createdAt = ""; // スプレッドシート側で設定される
        }

        public BugTicket(string id, string title, string description, string priority, string status, string assignedTo, string createdAt)
        {
            this.id = id;
            this.title = title;
            this.description = description;
            this.priority = priority;
            this.status = status;
            this.assignedTo = assignedTo;
            this.createdAt = createdAt;
        }
    }
}
#endif
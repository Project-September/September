#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace September.Editor.BugTicketsManager
{
    public class BugTicketsManagerWindow : EditorWindow
    {
        private List<BugTicket> _bugTickets = new();
        private GasConnector _connector;
        private string _searchFilter = "";
        private int _statusFilterIndex;
        private readonly string[] _statusOptions = { "All", "Open", "InProgress", "Resolved", "Closed" };
        private Vector2 _scrollPosition;
        private bool _isLoading;
        private string _statusMessage = "Ready";

        // New Bug Creation
        private bool _showNewBugForm;
        private string _newBugTitle = "";
        private string _newBugDescription = "";
        private int _newBugPriorityIndex;
        private int _newBugAssigneeIndex;
        private readonly string[] _priorityOptions = { "やる", "やらない" };
        private readonly string[] _assigneeOptions = { "ヨシダ", "オカベ", "タケチ", "ウスギ", "コイヌマ", "タカムラ", "シオミ" };

        [MenuItem("September/Bug Tickets Manager")]
        public static void ShowWindow()
        {
            GetWindow<BugTicketsManagerWindow>("Bug Tickets");
        }

        private void OnEnable()
        {
            _connector = new GasConnector();
            // GASは認証不要なので直接データを取得
            RefreshBugList();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawStatusBar();

            if (_showNewBugForm)
            {
                DrawNewBugForm();
            }

            DrawBugList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 検索フィールド
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarTextField, GUILayout.Width(150));

            // 状態フィルタ
            EditorGUILayout.LabelField("Status:", GUILayout.Width(50));
            _statusFilterIndex = EditorGUILayout.Popup(_statusFilterIndex, _statusOptions, EditorStyles.toolbarPopup, GUILayout.Width(100));

            GUILayout.FlexibleSpace();

            // 更新ボタン
            GUI.enabled = !_isLoading;
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                RefreshBugList();
            }

            // 新しいバグ作成ボタン
            if (GUILayout.Button("New Bug", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                _showNewBugForm = !_showNewBugForm;
                if (_showNewBugForm)
                {
                    ClearNewBugForm();
                }
            }

            // 接続テストボタン
            if (GUILayout.Button("Test GAS", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TestGasConnection();
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Status: {_statusMessage}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Total Bugs: {_bugTickets.Count}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBugList()
        {
            var filteredBugs = GetFilteredBugs();

            // ヘッダー
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("ID", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Title", EditorStyles.toolbarButton, GUILayout.Width(200));
            EditorGUILayout.LabelField("Priority", EditorStyles.toolbarButton, GUILayout.Width(80));
            EditorGUILayout.LabelField("Status", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.LabelField("Assignee", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.LabelField("CreatedAt", EditorStyles.toolbarButton, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            // リスト表示
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (filteredBugs.Count == 0)
            {
                EditorGUILayout.HelpBox("No bugs found matching the current filter.", MessageType.Info);
            }
            else
            {
                foreach (var bug in filteredBugs)
                {
                    DrawBugItem(bug);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBugItem(BugTicket bug)
        {
            EditorGUILayout.BeginHorizontal();

            // ID
            EditorGUILayout.LabelField(bug.id, GUILayout.Width(80));

            // Title
            EditorGUILayout.LabelField(bug.title, GUILayout.Width(200));

            // Priority with color
            var oldColor = GUI.color;
            GUI.color = GetPriorityColor(bug.priority);
            EditorGUILayout.LabelField(bug.priority, GUILayout.Width(80));
            GUI.color = oldColor;

            // Status (editable dropdown)
            var statusOptions = new[] { "Open", "InProgress", "Resolved", "Closed" };
            var currentStatusIndex = System.Array.IndexOf(statusOptions, bug.status);
            if (currentStatusIndex == -1) currentStatusIndex = 0;

            var newStatusIndex = EditorGUILayout.Popup(currentStatusIndex, statusOptions, GUILayout.Width(100));
            if (newStatusIndex != currentStatusIndex)
            {
                var newStatus = statusOptions[newStatusIndex];
                UpdateBugStatus(bug, newStatus);
            }

            // Assignee
            EditorGUILayout.LabelField(bug.assignedTo, GUILayout.Width(100));

            // Created date
            EditorGUILayout.LabelField(bug.createdAt, GUILayout.Width(100));

            EditorGUILayout.EndHorizontal();

            // Add separator line
            var rect = GUILayoutUtility.GetLastRect();
            rect.y += rect.height;
            rect.height = 1;
            EditorGUI.DrawRect(rect, Color.gray * 0.3f);
        }

        private Color GetPriorityColor(string priority)
        {
            switch (priority)
            {
                case "やる": return Color.red;
                case "やらない": return Color.white;
                default: return Color.white;
            }
        }

        private List<BugTicket> GetFilteredBugs()
        {
            var filtered = _bugTickets.AsEnumerable();

            // 検索フィルタ
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                filtered = filtered.Where(b =>
                    b.title.ToLower().Contains(_searchFilter.ToLower()) ||
                    b.description.ToLower().Contains(_searchFilter.ToLower()));
            }

            // 状態フィルタ
            if (_statusFilterIndex > 0 && _statusFilterIndex < _statusOptions.Length)
            {
                var selectedStatus = _statusOptions[_statusFilterIndex];
                filtered = filtered.Where(b => b.status == selectedStatus);
            }

            return filtered.ToList();
        }

        private async void RefreshBugList()
        {
            _isLoading = true;
            _statusMessage = "Loading from Google Apps Script...";
            Repaint();

            try
            {
                var fetchedBugs = await _connector.FetchBugTicketsAsync();
                if (fetchedBugs != null)
                {
                    _bugTickets = fetchedBugs;
                    _statusMessage = $"Loaded {_bugTickets.Count} bugs from GAS";
                }
                else
                {
                    _statusMessage = "No bugs found or failed to load from GAS";
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Error: {ex.Message}";
                Debug.LogError($"Failed to refresh bug list: {ex.Message}");

                _statusMessage += " (Using test data as fallback)";
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }

        private async void UpdateBugStatus(BugTicket bug, string newStatus)
        {
            var oldStatus = bug.status;
            bug.status = newStatus;
            // updatedAtは使用しないため削除

            _statusMessage = $"Updating {bug.id} status...";
            Repaint();

            try
            {
                var success = await _connector.UpdateBugStatusAsync(bug.id, newStatus);
                if (!success)
                {
                    bug.status = oldStatus; // 失敗時は元に戻す
                    _statusMessage = $"Failed to update {bug.id}";
                }
                else
                {
                    _statusMessage = $"Updated {bug.id} status to {newStatus}";
                }
            }
            catch (System.Exception ex)
            {
                bug.status = oldStatus; // エラー時は元に戻す
                _statusMessage = $"Error updating {bug.id}: {ex.Message}";
                Debug.LogError($"Failed to update bug status: {ex.Message}");
            }
            finally
            {
                Repaint();
            }
        }


        private void DrawNewBugForm()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create New Bug Ticket", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // タイトル
            EditorGUILayout.LabelField("Title *", EditorStyles.label);
            _newBugTitle = EditorGUILayout.TextField(_newBugTitle);

            // 説明
            EditorGUILayout.LabelField("Description *", EditorStyles.label);
            _newBugDescription = EditorGUILayout.TextArea(_newBugDescription, GUILayout.Height(60));

            // 優先度と担当者を横並びで
            EditorGUILayout.BeginHorizontal();

            // 優先度
            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            EditorGUILayout.LabelField("Priority", EditorStyles.label);
            _newBugPriorityIndex = EditorGUILayout.Popup(_newBugPriorityIndex, _priorityOptions);
            EditorGUILayout.EndVertical();

            // 担当者
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Assignee", EditorStyles.label);
            _newBugAssigneeIndex = EditorGUILayout.Popup(_newBugAssigneeIndex, _assigneeOptions);
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // ボタン
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = !_isLoading && IsNewBugFormValid();
            if (GUILayout.Button("Create Bug", GUILayout.Height(25)))
            {
                CreateNewBug();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Cancel", GUILayout.Height(25)))
            {
                _showNewBugForm = false;
                ClearNewBugForm();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void ClearNewBugForm()
        {
            _newBugTitle = "";
            _newBugDescription = "";
            _newBugPriorityIndex = 1; // Default to Medium
            _newBugAssigneeIndex = 0; // Default to first assignee
        }

        private bool IsNewBugFormValid()
        {
            return !string.IsNullOrWhiteSpace(_newBugTitle) &&
                   !string.IsNullOrWhiteSpace(_newBugDescription);
        }

        private async void CreateNewBug()
        {
            _isLoading = true;
            _statusMessage = "Creating new bug ticket...";
            Repaint();

            try
            {
                // First, refresh data from spreadsheet to get latest IDs
                _statusMessage = "Fetching latest data for ID generation...";
                Repaint();

                try
                {
                    var latestBugs = await _connector.FetchBugTicketsAsync();
                    if (latestBugs != null)
                    {
                        _bugTickets = latestBugs;
                        Debug.Log($"Refreshed bug list with {_bugTickets.Count} items for ID generation");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Could not fetch latest data for ID generation: {ex.Message}");
                    // Continue with existing data
                }

                _statusMessage = "Generating new bug ID...";
                Repaint();

                var newBugId = GenerateNewBugId();
                var newBug = new BugTicket(
                    newBugId,
                    _newBugTitle.Trim(),
                    _newBugDescription.Trim(),
                    _priorityOptions[_newBugPriorityIndex],
                    "Open",
                    _assigneeOptions[_newBugAssigneeIndex],
                    "" // createdAtはスプレッドシート側で設定
                );

                var success = await _connector.AddBugTicketAsync(newBug);
                if (success)
                {
                    _statusMessage = $"Successfully created bug {newBugId}";
                    _showNewBugForm = false;
                    ClearNewBugForm();

                    // Refresh the list to show the new bug
                    RefreshBugList();
                }
                else
                {
                    _statusMessage = "Failed to create bug ticket";
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"Error creating bug: {ex.Message}";
                Debug.LogError($"Failed to create bug ticket: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }

        private string GenerateNewBugId()
        {
            // ID generation based on spreadsheet data (from _bugTickets which contains GAS data)
            var maxId = 0;

            // Check both local test data and fetched spreadsheet data
            foreach (var bug in _bugTickets)
            {
                // Handle both string and numeric IDs from spreadsheet
                string idStr = bug.id ?? "";

                // If it's a numeric ID (from spreadsheet), use it directly
                if (int.TryParse(idStr, out var numericId))
                {
                    maxId = System.Math.Max(maxId, numericId);
                }
                // If it's a BUG-XXX format, extract the number
                else if (idStr.StartsWith("BUG-") && int.TryParse(idStr.Substring(4), out var bugId))
                {
                    maxId = System.Math.Max(maxId, bugId);
                }
            }

            // Generate next ID in numeric format to match spreadsheet
            var nextId = maxId + 1;
            Debug.Log($"Generated new bug ID: {nextId} (based on max existing ID: {maxId})");
            return nextId.ToString();
        }


        private async void TestGasConnection()
        {
            _isLoading = true;
            _statusMessage = "Testing GAS connection...";
            Repaint();

            try
            {
                var success = await _connector.TestConnectionAsync();
                if (success)
                {
                    _statusMessage = "GAS connection test successful!";
                }
                else
                {
                    _statusMessage = "GAS connection test failed";
                }
            }
            catch (System.Exception ex)
            {
                _statusMessage = $"GAS connection test error: {ex.Message}";
                Debug.LogError($"GAS connection test failed: {ex.Message}");
            }
            finally
            {
                _isLoading = false;
                Repaint();
            }
        }
    }
}
#endif

using shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;

public class MainNetworking : MonoBehaviour {
    public static MainNetworking Instance;


    [Header("Connection")]
    [SerializeField]
    private string serverUrl =
        "wss://claimthecity-20-server-production.up.railway.app";

    [SerializeField]
    private float reconnectDelay = 3f;

    [Header("UI")]
    public UIManager _UIManager;

    

    // Messages waiting to be sent.
    private readonly Queue<ISerializable> networkMessageQueue =
        new Queue<ISerializable>();

    private readonly object queueLock = new object();

    private WebSocket websocket;

    private bool connected;
    private bool connecting;
    private bool sending;
    private bool shuttingDown;

    private float nextReconnectTime;


    [Header("Debug")]
    public bool IsAdmin = false;

    // Existing game data.
    [SerializeField] private string cashedTeamPassword;
    private GameTeam cashedTeam;

    [SerializeField] private bool loggedIn = false;
    private GameData cashedGameData;

    public bool IsConnected => connected;
    public bool IsLoggedIn => loggedIn;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("Multiple network instances created;");
        } else {
            Instance = this;
        }
    }
    private void Start() {
        nextReconnectTime = Time.unscaledTime;
        TryConnect();
    }

    private void Update() {
        // NativeWebSocket queues events for the main thread
        // in non-WebGL builds.
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket?.DispatchMessageQueue();
#endif

        // Attempt reconnection at controlled intervals.
        if (!shuttingDown &&
            !connected &&
            !connecting &&
            websocket == null &&
            Time.unscaledTime >= nextReconnectTime) {
            TryConnect();
        }

        // Only send while connected.
        if (connected && websocket != null && !sending) {
            bool hasMessages;

            lock (queueLock) {
                hasMessages = networkMessageQueue.Count > 0;
            }

            if (hasMessages) {
                _ = FlushMessageQueueAsync(websocket);
            }
        }
    }

    private async void TryConnect() {
        if (shuttingDown || connecting || connected)
            return;

        connecting = true;

        WebSocket socket = null;

        try {
            Debug.Log($"Connecting to {serverUrl}");

            socket = new WebSocket(serverUrl);
            websocket = socket;

            socket.OnOpen += () => {
                if (shuttingDown ||
                    !ReferenceEquals(websocket, socket))
                    return;

                connected = true;

                Debug.Log("WebSocket connected.");

                // The server may require a handshake here.
                // Add it once the server protocol is defined.
            };

            socket.OnError += (error) =>
            {
                Debug.LogError(
                    $"WebSocket error at {serverUrl}: {error}"
                );
            };

            socket.OnClose += (code) =>
            {
                Debug.LogWarning(
                    $"WebSocket closed. Code: {code}"
                );

                if (!ReferenceEquals(websocket, socket))
                    return;

                connected = false;
                loggedIn = false;
                websocket = null;

                if (!shuttingDown)
                    nextReconnectTime = Time.unscaledTime + reconnectDelay;
            };

            socket.OnMessage += (bytes) => {
                if (!ReferenceEquals(websocket, socket))
                    return;

                try {
                    Packet packet = new Packet(bytes);
                    ISerializable message = packet.ReadObject();

                    if (message == null) {
                        Debug.LogWarning(
                            "Received an empty or invalid message.");
                        return;
                    }

                    Debug.Log(
                        $"Received message: {message.GetType().Name}");

                    HandleMessage(message);
                } catch (Exception ex) {
                    Debug.LogError(
                        $"Failed to deserialize server message: {ex}");
                }
            };

            await socket.Connect();
        } catch (Exception ex) {
            Debug.LogWarning($"Connection failed: {ex.Message}");

            if (ReferenceEquals(websocket, socket)) {
                websocket = null;
                connected = false;
            }

            if (socket != null)
                await CloseSocketSafely(socket);

            if (!shuttingDown) {
                nextReconnectTime =
                    Time.unscaledTime + reconnectDelay;
            }
        } finally {
            connecting = false;
        }
    }

    // Queue messages without requiring an active connection.
    // Messages remain queued until a send succeeds.
    private void WriteObject<T>(T message)
        where T : ISerializable {
        if (message == null)
            return;

        lock (queueLock) {
            networkMessageQueue.Enqueue(message);
        }
    }

    private async Task FlushMessageQueueAsync(WebSocket socket) {
        if (sending || !connected || shuttingDown)
            return;

        sending = true;

        try {
            while (connected &&
                   !shuttingDown &&
                   ReferenceEquals(websocket, socket)) {
                ISerializable message;

                lock (queueLock) {
                    if (networkMessageQueue.Count == 0)
                        break;

                    message = networkMessageQueue.Peek();
                }

                Packet packet = new Packet();
                packet.Write(message);

                byte[] bytes = packet.GetBytes();

                await socket.Send(bytes);

                // Remove only after Send completes successfully.
                lock (queueLock) {
                    if (networkMessageQueue.Count > 0 &&
                        ReferenceEquals(
                            networkMessageQueue.Peek(), message)) {
                        networkMessageQueue.Dequeue();
                    }
                }

                Debug.Log($"Sent message: {message.GetType().Name}");
            }
        } catch (Exception ex) {
            Debug.LogWarning($"Message send failed: {ex.Message}");

            connected = false;

            if (ReferenceEquals(websocket, socket)) {
                websocket = null;

                if (!shuttingDown) {
                    nextReconnectTime =
                        Time.unscaledTime + reconnectDelay;
                }
            }

            await CloseSocketSafely(socket);
        } finally {
            sending = false;
        }
    }

    private void HandleMessage(ISerializable message) {
        if (message is AcceptClientMessage) {
            Debug.Log("Server accepted client.");
            _UIManager?.ServerFound();
            return;
        } else if (message is LoginResultMessage loginResultMessage) {
            if (loginResultMessage.success) {
                cashedTeam = loginResultMessage.team;
                cashedTeamPassword = loginResultMessage.password;
                loggedIn = true;


                IsAdmin = loginResultMessage.asAdmin;

                _UIManager?.LoggedIn();
                _UIManager?.SetTeamName(cashedTeam.name);

                WriteObject(new RequestDataUpdateMessage());

                Debug.Log($"Logged in as {cashedTeam.name}");
            } else {
                loggedIn = false;
                _UIManager?.LoggedInWrongPass();

                Debug.Log("Login failed: incorrect password.");
            }

            return;
        } else if (message is DataUpdateMessage dataUpdateMessage) {
            cashedGameData = dataUpdateMessage.data;
            _UIManager?.UpdateGameData(cashedGameData);
        } else if (message is CommandLineMessage commandLineMessage) {
            string text = commandLineMessage.output;
            if (text != null && text!="")
                AdminConsole.logConsoleText(text);
        }
    }

    public void AttemptLogin(string pass) {
        if (!connected) {
            Debug.LogWarning("Cannot log in: not connected.");
            return;
        }

        WriteObject(new LoginAttemptMessage(pass));
    }

    public void SendAdminCommand(string command) {
        if (!connected) return;
        WriteObject(new ConsoleCommandMessage(command));
    }

    public void RequestDataUpdate() {
        if (!connected) return;
        WriteObject(new RequestDataUpdateMessage());
    }

    private async Task CloseSocketSafely(WebSocket socket) {
        try {
            await socket.Close();
        } catch (Exception ex) {
            Debug.LogWarning($"Error closing socket: {ex.Message}");
        }
    }

    private void OnApplicationQuit() {
        shuttingDown = true;
        connected = false;

        WebSocket socket = websocket;
        websocket = null;

        if (socket != null) {
            _ = CloseSocketSafely(socket);
        }
    }

    private void OnDestroy() {
        shuttingDown = true;
        connected = false;
        
        WebSocket socket = websocket;
        websocket = null;

        if (socket != null) {
            _ = CloseSocketSafely(socket);
        }

        if (Instance == this) {
            Instance = null;
        }
    }

    internal void SubmitScore(double score, string taskName) {
        WriteObject(new SubmitScoreMessage(cashedTeamPassword, taskName, score));
    }

    internal string GetTeamPass() {
        return cashedTeamPassword;
    }
}
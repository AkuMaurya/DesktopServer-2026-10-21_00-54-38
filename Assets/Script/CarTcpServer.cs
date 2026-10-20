using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TMPro;
using UnityEngine;

public class CarTcpServer : MonoBehaviour
{
    [Header("Server Settings")]
    public int port = 5000;

    // Accepts incoming TCP connections.
    private TcpListener listener;

    // Runs the listening operation separately from Unity's main thread.
    private Thread serverThread;

    // Controls the server's listening loop.
    private volatile bool running;

    // Receives messages from the network thread.
    // Unity's main thread processes these messages in Update().
    private readonly ConcurrentQueue<string> incoming =
        new ConcurrentQueue<string>();

    // Stores the latest validated car-control command.
    private NetworkMessage latestCommand = new NetworkMessage();

    // Used to display incoming data periodically without flooding the Console.
    private float lastDataLogTime;
    private int receivedMessageCount; 
    string ip_address;
    // Lets other scripts read the latest car-control values.
    public NetworkMessage GetCommand()
    {
        return latestCommand;
    }

    void Start()
    {
        // Allow the server to continue running when the application loses focus.
        Application.runInBackground = true;

        running = true;

        Debug.Log("[SERVER] Starting TCP server...");
        Debug.Log($"[SERVER] Configured port: {port}");

        // Start listening in a background thread.
        serverThread = new Thread(Listen);
        serverThread.IsBackground = true;
        serverThread.Start();
    }
    public string GetAddress()
    {
        return ip_address; 
    }
    void Listen()
    {
        try
        {
            // IPAddress.Any allows connections through the computer's
            // available network interfaces, including its LAN IP.
            listener = new TcpListener(IPAddress.Any, port);

            listener.Start();

            Debug.Log($"[SERVER] LISTENING on port {port}");
            // Get all network interfaces on the computer.
            foreach (NetworkInterface networkInterface in
                     NetworkInterface.GetAllNetworkInterfaces())
            {
                // Ignore disconnected network adapters.
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Get the IP addresses assigned to this network adapter.
                IPInterfaceProperties properties =
                    networkInterface.GetIPProperties();

                foreach (UnicastIPAddressInformation address
                         in properties.UnicastAddresses)
                {
                    // Display only IPv4 addresses, excluding localhost.
                    if (address.Address.AddressFamily ==
                            System.Net.Sockets.AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address.Address))
                    {
                        Debug.Log(
                            $"[SERVER] Available IP: {address.Address} | " +
                            $"Port: {port} | Adapter: {networkInterface.Name}"
                        );
                       ip_address = address.Address.ToString();
                    }
                }
            }

            Debug.Log(
                $"[SERVER] Waiting for a client to connect on port {port}..."
            );

            while (running)
            {
                // Wait here until a client connects.
                // Stop() in OnDestroy() can interrupt this operation.
                using (TcpClient client = listener.AcceptTcpClient())
                {
                    client.NoDelay = true;

                    string remoteAddress =
                        client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                    Debug.Log(
                        $"[SERVER] CLIENT CONNECTED: {remoteAddress}"
                    );

                    try
                    {
                        using (NetworkStream stream = client.GetStream())
                        using (StreamReader reader =
                               new StreamReader(stream, Encoding.UTF8))
                        {
                            string line;

                            // Each newline-terminated JSON message is one command.
                            while (running &&
                                   (line = reader.ReadLine()) != null)
                            {
                                // Forward the received message to Unity's main thread.
                                incoming.Enqueue(line);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (running)
                        {
                            Debug.LogWarning(
                                "[SERVER] Error receiving client data: " +
                                e.Message
                            );
                        }
                    }

                    Debug.Log(
                        $"[SERVER] CLIENT DISCONNECTED: {remoteAddress}"
                    );
                }
            }
        }
        catch (SocketException e)
        {
            if (running)
            {
                Debug.LogError(
                    $"[SERVER] SOCKET ERROR: {e.SocketErrorCode} - {e.Message}"
                );
            }
        }
        catch (Exception e)
        {
            if (running)
            {
                Debug.LogError(
                    $"[SERVER] SERVER ERROR: {e.GetType().Name} - {e.Message}"
                );
            }
        }
        finally
        {
            Debug.Log("[SERVER] Listening thread stopped.");
        }
    }

    void Update()
    {
        // This method runs on Unity's main thread.
        // Process every received message and retain the latest command.
        string line;

        while (incoming.TryDequeue(out line))
        {
            try
            {
                Debug.Log("[SERVER] Raw message received: " + line);

                // Convert the received JSON into a NetworkMessage object.
                NetworkMessage msg =
                    JsonUtility.FromJson<NetworkMessage>(line);

                if (msg == null)
                {
                    Debug.LogWarning("[SERVER] Received an empty/invalid message.");
                    continue;
                }

                // Validate the values before other scripts use them.
                msg.steer = Mathf.Clamp(msg.steer, -1f, 1f);
                msg.throttle = Mathf.Clamp01(msg.throttle);
                msg.brake = Mathf.Clamp01(msg.brake);
                msg.gear = Mathf.Clamp(msg.gear, -1, 1);
                msg.focus = Mathf.Clamp(msg.focus, 0, 4);

                // Update the latest command.
                latestCommand = msg;
                receivedMessageCount++;

                // Log the interpreted controls at most once per second.
                // The raw-message log above is useful while debugging;
                // remove it after testing if the Console becomes too busy.
                if (Time.unscaledTime - lastDataLogTime >= 1f)
                {
                    Debug.Log(
                        $"[SERVER] DATA RECEIVED | " +
                        $"Count: {receivedMessageCount} | " +
                        $"Steer: {latestCommand.steer:F2} | " +
                        $"Throttle: {latestCommand.throttle:F2} | " +
                        $"Brake: {latestCommand.brake:F2} | " +
                        $"Gear: {latestCommand.gear} | " +
                        $"Focus: {latestCommand.focus}"
                    );

                    lastDataLogTime = Time.unscaledTime;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "[SERVER] INVALID CLIENT MESSAGE: " + e.Message
                );
            }
        }
    }

    void OnDestroy()
    {
        Debug.Log("[SERVER] Shutting down TCP server...");

        running = false;

        // Stop the listener so that AcceptTcpClient() can exit.
        try
        {
            listener?.Stop();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SERVER] Shutdown warning: " + e.Message);
        }

        // Wait briefly for the server thread to finish.
        if (serverThread != null && serverThread.IsAlive)
        {
            serverThread.Join(500);
        }

        Debug.Log("[SERVER] TCP server shutdown complete.");
    }
}
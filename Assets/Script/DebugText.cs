 
using TMPro; 
using UnityEngine;

public class DebugText : MonoBehaviour
{
    public CarTcpServer carTcpServer;
    public TMP_Text debug_Text; 
    string address;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    { 
        address = "";
    }

    // Update is called once per frame
    void Update()
    {
        if(address != carTcpServer.GetAddress())
        { 
            address = carTcpServer.GetAddress();
            debug_Text.text += $"[SERVER] Available IP: {address} | \n";
        }
        
                           
    }
}

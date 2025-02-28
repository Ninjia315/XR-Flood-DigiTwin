using System.IO;
using UnityEngine;
using QRCoder;
using QRCoder.Unity;
using System.Net;
using System.Net.Sockets;

public class QRCodeTest : MonoBehaviour
{
    public GameObject serverObject;
    public string baseLink;
    public string address;
    public string marker_id;
    public string QRCodeInfo;
    public string filePath;


    // Start is called before the first frame update
    void Start()
    {
        address = GetLocalIPv4();
        QRCodeGenerator qrGenerator = new QRCodeGenerator();

        for (int index = 1; index < 4; index++)
        {
            marker_id = "m" + index;
            QRCodeInfo = baseLink + address + "?" + marker_id;
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(QRCodeInfo, QRCodeGenerator.ECCLevel.Q);
            UnityQRCode qrCode = new UnityQRCode(qrCodeData);
            Texture2D qrCodeAsTexture2D = qrCode.GetGraphic(20);
            File.WriteAllBytes(filePath + marker_id + ".png", qrCodeAsTexture2D.EncodeToPNG());
            GameObject.Find("Cube" + index).GetComponent<Renderer>().material.mainTexture = qrCodeAsTexture2D;
            //Debug.Log(filePath + marker_id + ".png");
    }
        }
        
    string GetLocalIPv4()
    {
        string localIP = "Not Available";
        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error retrieving local IP address: " + e.Message);
        }
        return localIP;
    }

}

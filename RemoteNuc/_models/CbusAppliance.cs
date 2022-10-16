using System.Collections;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RemoteNuc;

public class CbusAppliance : AbstractAppliance
{
    public int automationBase;
    public int automationGroup;
    public int automationId;
    public int automationValue;
    public JArray? stations;
    public JArray? appliances;
    public int? associatedStation;
    public string? ipAddress;

    public CbusAppliance(string type, string automationType, string name, string room, string id, string value, int automationBase,
        int automationGroup, int automationId, int automationValue, JArray? stations, JArray? appliances, int? associatedStation, string? ipAddress)
    {
        this.type = type;
        this.automationType = automationType;
        this.name = name;
        this.room = room;
        this.id = id;
        this.value = value;
        this.automationBase = automationBase;
        this.automationGroup = automationGroup;
        this.automationId = automationId;
        this.automationValue = automationValue;
        this.stations = stations;
        this.appliances = appliances;
        this.associatedStation = associatedStation;
        this.ipAddress = ipAddress;
    }

    /// <summary>
    /// Send a message to the NUC
    /// </summary>
    /// <param name="message"></param>
    /// <returns>A boolean representing if the message was successfully sent.</returns>
    public override bool SetValue(string value)
    {
        if (MainWindow.appKey == null || MainWindow.nucEndPoint == null) return false;

        //Message needs to follow the convention [source]:[destination]:[actionspace]:[additionalinfo]
        string message = $"Android:NUC:Automation:Set:{id}:{value}:null";

        SocketClient client = new SocketClient(EncryptionHelper.Encrypt(message, MainWindow.appKey), MainWindow.nucEndPoint);
        client.send();

        return true;
    }
}
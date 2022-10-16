using System.Net;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// Responsible for organising and storing room and station information.
/// </summary>
public static class ApplianceOrganiser
{
    /// <summary>
    /// A dictionary of the lab rooms, storing an int for the room number and then a 
    /// dictionary of all the lab computers, storing a Unique ID for a machine as well as the
    /// IPEndPoint.
    /// </summary>
    private static Dictionary<string, AbstractAppliance> applianceDictionary = new Dictionary<string, AbstractAppliance>();

    /// <summary>
    /// Hold a dictionary of the basic Id's and their las_config match. I.e key:123123 value:lights-123123
    /// This is currently only used when receiving a message from Cbus and only the number id is present
    /// </summary>
    private static Dictionary<string, string> applianceBasicId = new Dictionary<string, string>();
    
    /// <summary>
    /// Check if a file exists, creates an empty json file if it doesn't.
    /// </summary>
    private static void checkFile()
    {
        if (!File.Exists(@"c:\labs_config\appliance_list.json"))
        {
            // Create a new file     
            using (FileStream fs = File.Create(@"c:\labs_config\appliance_list.json"))
            {
                // Add basic array to file    
                Byte[] title = new UTF8Encoding(true).GetBytes("[]");
                fs.Write(title, 0, title.Length);
            }
        }
    }

    public static AbstractAppliance? findApplianceById(string id)
    {
        if (applianceDictionary.ContainsKey(id))
        {
            return applianceDictionary[id];
        }

        return null;
    }

    public static void clearAppliances()
    {
        applianceDictionary = new Dictionary<string, AbstractAppliance>();
    }

    public static void addAppliance(AbstractAppliance appliance)
    {
        applianceDictionary.Add(appliance.id, appliance);
    }

    /// <summary>
    /// Read the local station_list.json and add all entries to the dictionary.
    /// </summary>
    public static void loadApplianceConfig()
    {
        checkFile();

        JArray applianceGroups = JArray.Parse(File.ReadAllText(@"c:\labs_config\appliance_list.json"));
        foreach (JObject applianceGroup in applianceGroups) // groups
        {
            String group = applianceGroup.GetValue("name").ToString();
            JArray appliances = JArray.Parse(applianceGroup.GetValue("objects").ToString());
            string applianceValue;
            foreach (JObject appliance in appliances)
            {
                String automationType = appliance.GetValue("automationType").ToString();
                if (group.Equals("scenes"))
                {
                    applianceValue = "Off";
                }
                else
                {
                    applianceValue = "";
                }

                AbstractAppliance? newAppliance = null;
                
                if (automationType.Equals("cbus"))
                {
                    newAppliance = new CbusAppliance(group,
                        automationType,
                        appliance.GetValue("name").ToString(),
                        appliance.GetValue("room").ToString(),
                        group + "-" + appliance.GetValue("id"),
                        applianceValue,
                        Int32.Parse(appliance.GetValue("automationBase").ToString()),
                        Int32.Parse(appliance.GetValue("automationGroup").ToString()),
                        Int32.Parse(appliance.GetValue("automationId").ToString()),
                        group.Equals("scenes") ? Int32.Parse(appliance.GetValue("automationValue").ToString()) : 0,
                        appliance.ContainsKey("stations")
                            ? JArray.Parse(appliance.GetValue("stations").ToString())
                            : null,
                        appliance.ContainsKey("appliances")
                            ? JArray.Parse(appliance.GetValue("appliances").ToString())
                            : null,
                        appliance.ContainsKey("associatedStation")
                            ? Int32.Parse(appliance.GetValue("associatedStation").ToString())
                            : null,
                        appliance.ContainsKey("ipAddress") ? appliance.GetValue("ipAddress").ToString() : null);

                    applianceBasicId.Add(appliance.GetValue("id").ToString(), group + "-" + appliance.GetValue("id").ToString());
                }

                if (newAppliance != null)
                {
                    addAppliance(newAppliance);
                }
            }
        }
    }
    
    /// <summary>
    /// Get the details of the current stations the NUC machine knows about.
    /// </summary>
    public static Dictionary<string, AbstractAppliance> getAppliances()
    {
        return applianceDictionary;
    }
}

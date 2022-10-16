using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace RemoteNuc
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int port = 55556;

        public static string? appKey = null;

        /// <summary>
        /// IPEndPoint representing the server that is running on the local machine.
        /// </summary>
        public static IPEndPoint? nucEndPoint = null;

        public MainWindow()
        {
            InitializeComponent();

            bool success = getSetupDetails();

            //Only load the appliance if the necessary details are present
            if (success)
            {
                //Load in the appliances (currently only the scenes)
                ApplianceOrganiser.loadApplianceConfig();
                createButtons();
            }
        }

        /// <summary>
        /// Get the saved NUC address that is created by the NUC program when starting.
        /// </summary>
        /// <returns></returns>
        private bool getSetupDetails()
        {
            string? address = Environment.GetEnvironmentVariable("nucAddress", EnvironmentVariableTarget.User);

            if (address == null)
            {
                nuc_address.Text = "Nuc Address could not be found!";
                return false;
            }

            appKey = Environment.GetEnvironmentVariable("AppKey", EnvironmentVariableTarget.User);

            if (appKey == null)
            {
                warning_message.Text = "Encrpytion Key not set!";
                return false;
            };

            nuc_address.Text = address;
            nucEndPoint = new IPEndPoint((IPAddress)IPAddress.Parse(address), port);

            return true;
        }

        //Create the scene buttons on the window
        public void createButtons()
        {
            Dictionary<string, AbstractAppliance> appliances = ApplianceOrganiser.getAppliances();

            //Track the scene grid
            var sceneRow = 0;
            var sceneCol = 0;

            //Track the appliance grid
            var applianceRow = 0;
            var applianceCol = 0;

            foreach (var item in appliances)
            {
                if (item.Value.automationType.Equals("cbus")) {
                    CbusAppliance appliance = item.Value as CbusAppliance;

                    Button newBtn = new Button();
                    newBtn.Content = appliance.name;
                    newBtn.Height = 23;

                    if(appliance.type == "scenes")
                    {
                        if (sceneCol == 4)
                        {
                            sceneCol = 0;

                            RowDefinition newRow = new RowDefinition();
                            newRow.Height = new GridLength(70);
                            sceneGrid.RowDefinitions.Add(newRow);
                            sceneRow++;
                        }

                        //Create a room label
                        Label label = new Label();
                        label.Content = appliance.room;
                        label.Height = 23;

                        //Create a holder
                        StackPanel stack = new StackPanel();
                        stack.SetValue(Grid.RowProperty, sceneRow);
                        stack.SetValue(Grid.ColumnProperty, sceneCol);
                        stack.Margin = new Thickness(5);

                        stack.Children.Add(label);
                        stack.Children.Add(newBtn);

                        sceneGrid.Children.Add(stack);
                        newBtn.Click += new RoutedEventHandler(scene_onClick_handler);

                        newBtn.Tag = item.Key + ":" + appliance.automationValue;

                        sceneCol++;
                    }
                    else
                    {
                        if (applianceCol == 4)
                        {
                            applianceCol = 0;

                            RowDefinition newRow = new RowDefinition();
                            newRow.Height = new GridLength(70);
                            applianceGrid.RowDefinitions.Add(newRow);
                            applianceRow++;
                        }

                        //Create a room label
                        Label label = new Label();
                        label.Content = appliance.room;
                        label.Height = 23;

                        //Create a holder
                        StackPanel stack = new StackPanel();
                        stack.SetValue(Grid.RowProperty, applianceRow);
                        stack.SetValue(Grid.ColumnProperty, applianceCol);

                        stack.Children.Add(label);
                        stack.Children.Add(newBtn);

                        applianceGrid.Children.Add(stack);
                        newBtn.Click += new RoutedEventHandler(appliance_onClick_handler);

                        newBtn.Tag = item.Key;

                        applianceCol++;
                    }
                }
            }

            //Separate the buttons by the room type
        }

        //Used for auto creation buttons
        public void scene_onClick_handler(object sender, RoutedEventArgs e)
        {
            //tag is combination of id and value [id:value]
            string? s = (sender as Button).Tag.ToString();

            if (s == null) return;

            //string id, string value
            string[]? split = s.Split(":");

            ThreadPool.QueueUserWorkItem(delegate
            {
                //Find appliance
                AbstractAppliance? appliance = ApplianceOrganiser.findApplianceById(split[0]);

                //Call set value on it
                if (appliance != null)
                {
                    appliance.SetValue(split[1]);
                }

                MessageBox.Show("Success, the scene has been set!");
            });
        }

        public void appliance_onClick_handler(object sender, RoutedEventArgs e)
        {
            //tag is the appliances id
            string? id = (sender as Button).Tag.ToString();

            if (id == null) return;

            var dialog = new Dialog();
            if (dialog.ShowDialog() == true)
            {
                string value = dialog.ApplianceTextBox.Text;
 
                ThreadPool.QueueUserWorkItem(delegate
                {
                    //Find appliance
                    AbstractAppliance? appliance = ApplianceOrganiser.findApplianceById(id);

                    //Call set value on it
                    if (appliance != null)
                    {
                        appliance.SetValue(value);
                    }
                });
            }
        }

        //Used for manual entry of Appliance ID and Value
        private void message_Click(object sender, RoutedEventArgs e)
        {
            string applianceId = Appliance_ID.Text;
            string automationValue = Appliance_value.Text;

            //Message needs to follow the convention [source]:[destination]:[actionspace]:[additionalinfo]
            string message = $"Android:NUC:Automation:Set:{applianceId}:{automationValue}:null";

            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketClient client = new SocketClient(EncryptionHelper.Encrypt(message, MainWindow.appKey), MainWindow.nucEndPoint);
                client.send();
            });
        }

        private void launch_Click(object sender, RoutedEventArgs e)
        {
            //Message needs to follow the convention [source]:[destination]:[actionspace]:[additionalinfo]
            string message = $"Android:Station,{stationID.Text}:Steam:Launch:{steamID.Text}";

            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketClient client = new SocketClient(EncryptionHelper.Encrypt(message, MainWindow.appKey), MainWindow.nucEndPoint);
                client.send();
            });
        }

        private void stop_Click(object sender, RoutedEventArgs e)
        {
            //Message needs to follow the convention [source]:[destination]:[actionspace]:[additionalinfo]
            string message = $"Android:Station,{stationID.Text}:CommandLine:StopGame";

            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketClient client = new SocketClient(EncryptionHelper.Encrypt(message, MainWindow.appKey), MainWindow.nucEndPoint);
                client.send();
            });
        }
    }
}

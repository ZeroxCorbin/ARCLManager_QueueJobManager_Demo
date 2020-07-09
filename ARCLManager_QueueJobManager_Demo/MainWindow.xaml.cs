using ARCL;
using ARCLTypes;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace ARCLStream_QueueManager_WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ARCLConnection EM;
        QueueJobManager QM;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            EM = new ARCLConnection(txtConnectionString.Text);
            SychronousCommands sychronousCommands = new SychronousCommands(txtConnectionString.Text);
            List<string> goals;

            if (sychronousCommands.Connect())
                goals = sychronousCommands.GetGoals();
            else
            {
                btnConnect.Background = Brushes.Red;
                return;
            }
            sychronousCommands.Close();

            if (EM.Connect())
            {
                btnConnect.Background = Brushes.Green;

                EM.DataReceived += EM_DataReceived;

                QM = new QueueJobManager(EM);

                QM.Start();

                for (int i = 0; i < 4; i++)
                {
                    ((ComboBox)stkGoalType.Children[i]).Items.Add("pickup");
                    ((ComboBox)stkGoalType.Children[i]).Items.Add("dropoff");
                }
                foreach (ComboBox cmb in stkGoalName.Children)
                {
                    cmb.ItemsSource = goals;
                }

                ThreadPool.QueueUserWorkItem(new WaitCallback(QueueLoop));
            }
            else
            {
                btnConnect.Background = Brushes.Red;
            }


        }

        private void QueueLoop(object sender)
        {
            while (QM != null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(QueueLoopLocal));

                Thread.Sleep(100);
            }
        }
        private void EM_DataReceived(object sender, string data) => 
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action<string>(ARCLDataReceivedViewUpdate), data);

        private void QueueLoopLocal()
        {
            if (QM.IsSynced)
                LblIsSynced.Background = Brushes.LightGreen;
            else
                LblIsSynced.Background = Brushes.LightSalmon;

            LblJobCount.Content = $"Job Count = {QM.Jobs.Count}";
        }

        private void ARCLDataReceivedViewUpdate(string obj) => txtData.Text += obj;

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            EM.Write(txtSendMessage.Text);
        }

        private void btnSendMulti_Click(object sender, RoutedEventArgs e)
        {
            List<QueueJobUpdateEventArgs> goals = new List<QueueJobUpdateEventArgs>();

            int i = 0;
            foreach (ComboBox cmb in stkGoalName.Children)
            {
                if (cmb.SelectedIndex >= 0)
                {
                    QueueJobUpdateEventArgs.GoalTypes type;
                    if (cmb.SelectedIndex == 0)
                        type = QueueJobUpdateEventArgs.GoalTypes.pickup;
                    else
                        type = (QueueJobUpdateEventArgs.GoalTypes)Enum.Parse(typeof(QueueJobUpdateEventArgs.GoalTypes), (string)((ComboBox)stkGoalType.Children[i]).SelectedItem);
                    i++;

                    goals.Add(new QueueJobUpdateEventArgs(null, (string)cmb.SelectedItem, type));
                }
            }

            QM.QueueMulti(goals);
        }
    }
}

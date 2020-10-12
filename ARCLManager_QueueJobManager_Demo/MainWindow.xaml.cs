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
        ARCLConnection Connection { get; set; }
        QueueJobManager QueueJobManager { get; set; }

        public MainWindow() => InitializeComponent();

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connection = new ARCLConnection(txtConnectionString.Text);
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

            if (Connection.Connect())
            {
                btnConnect.Background = Brushes.Green;

                Connection.DataReceived += Connection_DataReceived;

                QueueJobManager = new QueueJobManager(Connection);

                QueueJobManager.Start();

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
            while (QueueJobManager != null)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(QueueLoopLocal));
                Thread.Sleep(100);
            }
        }
        private void Connection_DataReceived(object sender, string data) => 
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action<string>(ARCLDataReceivedViewUpdate), data);

        private void QueueLoopLocal()
        {
            if (QueueJobManager.SyncState.State == SyncStates.TRUE)
                LblIsSynced.Background = Brushes.LightGreen;
            else
                LblIsSynced.Background = Brushes.LightSalmon;

            LblJobCount.Content = $"Job Count = {QueueJobManager.Jobs.Count}";

            StkJobList.Children.Clear();
            foreach (KeyValuePair<string, QueueManagerJob> kv in QueueJobManager.Jobs)
            {
                bool found = false;
                foreach(StackPanel sp in StkJobList.Children)
                {
                    if((string)sp.Tag == kv.Value.ID)
                    {
                        found = true;
                        UpdateJobPanel(kv.Value, sp);
                        break;
                    }
                }
                if(!found)
                    StkJobList.Children.Add(GetJobPanel(kv.Value));
            }
                
        }
        private StackPanel GetJobPanel(QueueManagerJob job)
        {
            StackPanel stk = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                Tag = job.ID
            };

            stk.Children.Add(new Label()
            {
                Content = job.ID,
                Tag = 1
            });

            stk.Children.Add(new Label()
            {
                Content = job.Status.ToString(),
                Tag = 2
            });
            stk.Children.Add(new Label()
            {
                Content = job.SubStatus.ToString(),
                Tag = 3
            });

            Button btn = new Button()
            {
                Content = "Cancel",
                Tag = job.ID,
            };
            stk.Children.Add(btn); 

            btn.Click += Btn_Click;
            return stk;
        }
        private void UpdateJobPanel(QueueManagerJob job, StackPanel stack)
        {
            foreach(object child in stack.Children)
            {
                if (child is Label myObjRef)
                {
                    switch ((string)myObjRef.Tag)
                    {
                        case "1":
                            myObjRef.Content = job.ID;
                            break;
                        case "2":
                            myObjRef.Content = job.Status.ToString();
                            break;
                        case "3":
                            myObjRef.Content = job.SubStatus.ToString();
                            break;
                        default:

                            break;
                    }

                }
            }
        }
        private void Btn_Click(object sender, RoutedEventArgs e)
        {
            QueueJobManager.CancelJob(((Button)sender).Tag.ToString());
        }

        private void ARCLDataReceivedViewUpdate(string obj) => txtData.Text += obj;

        private void BtnSend_Click(object sender, RoutedEventArgs e) => Connection?.Write(txtSendMessage.Text);

        private void BtnSendMulti_Click(object sender, RoutedEventArgs e)
        {
            List<QueueManagerJobSegment> goals = new List<QueueManagerJobSegment>();

            int i = 0;
            foreach (ComboBox cmb in stkGoalName.Children)
            {
                if (cmb.SelectedIndex >= 0)
                {
                    QueueManagerJobSegment.Types type;
                    if (cmb.SelectedIndex == 0)
                        type = QueueManagerJobSegment.Types.pickup;
                    else
                        type = (QueueManagerJobSegment.Types)Enum.Parse(typeof(QueueManagerJobSegment.Types), (string)((ComboBox)stkGoalType.Children[i]).SelectedItem);
                    i++;

                    goals.Add(new QueueManagerJobSegment(null, (string)cmb.SelectedItem, type));
                }
            }

            QueueJobManager.QueueMulti(goals);
        }
    }
}

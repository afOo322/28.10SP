using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DistributedSystemSimulation
{
    public partial class MainWindow : Window
    {
        private ObservableCollection<Node> Nodes { get; set; }
        private bool isRunning;
        private Random random = new Random();
        public MainWindow()
        {
            InitializeComponent();
            Nodes = new ObservableCollection<Node>();
            for (int i = 1; i <= 5; i++)
            {
                Nodes.Add(new Node { Name = $"Node {i}" });
            }
            NodesControl.ItemsSource = Nodes;
        }

        private async void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            isRunning = true;
            await RunSimulation();
        }

        private async Task RunSimulation()
        {
            while (isRunning)
            {
                foreach (var node in Nodes)
                {
                    if (node.IsActive)
                    {
                        await node.ExecuteTask(); LogEvent($"{node.Name} - CPU: {node.CPUUsage}% | Memory: {node.MemoryUsage}%");

                        if (node.CPUUsage > 90)
                        {
                            LoadBalance(node);
                        }
                    }
                }

                SimulateFailures();

                await Task.Delay(1000);
            }
        }

        private void LoadBalance(Node overloadedNode)
        {
            foreach (var node in Nodes.Where(n => n != overloadedNode && n.IsActive))
            {
                int memoryToTransfer = overloadedNode.MemoryUsage / 10; node.MemoryUsage += memoryToTransfer;
                overloadedNode.MemoryUsage -= memoryToTransfer;
                LogEvent($"Load balanced from {overloadedNode.Name} to {node.Name}");
            }
        }

        private void SimulateFailures()
        {
            foreach (var node in Nodes)
            {
                if (random.Next(0, 100) < 5)
                {
                    node.IsActive = false;
                    LogEvent($"{node.Name} has failed.");
                }
            }
        }

        private void StopSimulation_Click(object sender, RoutedEventArgs e)
        {
            isRunning = false;
        }

        private void LogEvent(string message)
        {
            LogTextBox.AppendText($"{DateTime.Now}: {message}\n");
            File.AppendAllText("event_log.txt", $"{DateTime.Now}: {message}\n");
        }
    }

    public class Node : INotifyPropertyChanged
    {
        private static Random random = new Random(); private int cpuUsage;
        private int memoryUsage;
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;

        public int CPUUsage
        {
            get => cpuUsage;
            set
            {
                cpuUsage = value;
                OnPropertyChanged(nameof(CPUUsage));
                OnPropertyChanged(nameof(CpuColor));
            }
        }
        public int MemoryUsage
        {
            get => memoryUsage;
            set
            {
                memoryUsage = Math.Min(100, value); OnPropertyChanged(nameof(MemoryUsage));
            }
        }

        public SolidColorBrush CpuColor => CPUUsage > 80 || MemoryUsage > 90 ? Brushes.Red : Brushes.Green;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public async Task ExecuteTask()
        {
            CPUUsage = random.Next(0, 101); MemoryUsage += random.Next(1, 11);
            await Task.Delay(random.Next(100, 501));
        }
    }
}

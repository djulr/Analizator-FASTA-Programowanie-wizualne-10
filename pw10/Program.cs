// Program.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace FastaAnalyzerWinForms
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private Button loadButton;
        private Button exportButton;
        private ListView sequenceListView;
        private Chart lengthChart;
        private OpenFileDialog openFileDialog;
        private List<SequenceData> sequences = new();

        public MainForm()
        {
            Text = "FASTA Analyzer";
            Width = 1000;
            Height = 700;

            loadButton = new Button { Text = "Load FASTA", Left = 10, Top = 10, Width = 100 };
            loadButton.Click += LoadButton_Click;

            exportButton = new Button { Text = "Export", Left = 120, Top = 10, Width = 100 };
            exportButton.Click += ExportButton_Click;

            sequenceListView = new ListView
            {
                Left = 10,
                Top = 50,
                Width = 460,
                Height = 600,
                View = View.Details,
                FullRowSelect = true
            };
            sequenceListView.Columns.Add("Name", 150);
            sequenceListView.Columns.Add("Length", 100);
            sequenceListView.Columns.Add("CG %", 80);
            sequenceListView.Columns.Add("Codons", 80);

            lengthChart = new Chart { Left = 480, Top = 50, Width = 480, Height = 600 };
            lengthChart.ChartAreas.Add(new ChartArea());
            lengthChart.Series.Add("Lengths");
            lengthChart.Series["Lengths"].ChartType = SeriesChartType.Column;

            openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "FASTA files (*.fasta;*.fa)|*.fasta;*.fa|All files (*.*)|*.*"
            };

            Controls.Add(loadButton);
            Controls.Add(exportButton);
            Controls.Add(sequenceListView);
            Controls.Add(lengthChart);
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                sequences.Clear();
                sequenceListView.Items.Clear();
                lengthChart.Series["Lengths"].Points.Clear();

                foreach (var file in openFileDialog.FileNames)
                {
                    var content = File.ReadAllText(file);
                    var parsed = ParseFasta(content);
                    sequences.AddRange(parsed);
                }

                foreach (var seq in sequences)
                {
                    sequenceListView.Items.Add(new ListViewItem(new[]
                    {
                        seq.Name,
                        seq.Length.ToString(),
                        seq.CGContent.ToString("F2"),
                        seq.Codons.ToString()
                    }));
                    lengthChart.Series["Lengths"].Points.AddXY(seq.Name, seq.Length);
                }
            }
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON file (*.json)|*.json|CSV file (*.csv)|*.csv",
                Title = "Save Statistics"
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (saveFileDialog.FileName.EndsWith(".json"))
                {
                    File.WriteAllText(saveFileDialog.FileName, JsonSerializer.Serialize(sequences, new JsonSerializerOptions { WriteIndented = true }));
                }
                else if (saveFileDialog.FileName.EndsWith(".csv"))
                {
                    string CsvEscape(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

                    var lines = new List<string> { "Name,Length,CG%,Codons,A,C,G,T" };
                    lines.AddRange(sequences.Select(s =>
                        string.Join(",",
                            CsvEscape(s.Name),
                            s.Length.ToString(),
                            s.CGContent.ToString("F2"),
                            s.Codons.ToString(),
                            s.CountA.ToString(),
                            s.CountC.ToString(),
                            s.CountG.ToString(),
                            s.CountT.ToString()
                        )
                    ));
                    File.WriteAllLines(saveFileDialog.FileName, lines);
                }
            }
        }

        private List<SequenceData> ParseFasta(string content)
        {
            var lines = content.Split('\n');
            var sequences = new List<SequenceData>();
            string name = null;
            string seq = "";

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.StartsWith(">"))
                {
                    if (name != null)
                    {
                        sequences.Add(new SequenceData(name, seq));
                    }
                    name = trimmed.Substring(1);
                    seq = "";
                }
                else
                {
                    seq += trimmed.ToUpper();
                }
            }

            if (name != null)
                sequences.Add(new SequenceData(name, seq));

            return sequences;
        }
    }

    public class SequenceData
    {
        public string Name { get; set; }
        public string Sequence { get; set; }
        public int Length => Sequence.Length;
        public double CGContent => Length == 0 ? 0 : ((double)(CountC + CountG) / Length) * 100;
        public int Codons => Math.Max(0, Length - 2);
        public int CountA => Sequence.Count(c => c == 'A');
        public int CountT => Sequence.Count(c => c == 'T');
        public int CountG => Sequence.Count(c => c == 'G');
        public int CountC => Sequence.Count(c => c == 'C');

        public SequenceData(string name, string sequence)
        {
            Name = name;
            Sequence = sequence;
        }
    }
}
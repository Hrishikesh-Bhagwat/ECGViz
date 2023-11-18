using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using brainflow;
using System.Runtime.Remoting.Channels;
using FftSharp;

namespace OpenBCI_GUI
{
    public partial class DataRead : Form
    {
        private List<string> ports;
        SerialPort port;
        Thread fetchThread;
        Thread updateUI;

        bool isConnected = false;
        List<DataPointTimeSeries> channel1 = new List<DataPointTimeSeries>();
        List<DataPointTimeSeries> channel2 = new List<DataPointTimeSeries>();
        List<DataPointTimeSeries> channel3 = new List<DataPointTimeSeries>();

        List<DataPointTimeSeries> channel1F = new List<DataPointTimeSeries>();
        List<DataPointTimeSeries> channel2F = new List<DataPointTimeSeries>();
        List<DataPointTimeSeries> channel3F = new List<DataPointTimeSeries>();

        List<double> channel1FFT = new List<double>();
        List<double> channel2FFT = new List<double>();
        List<double> channel3FFT = new List<double>();



        //const String pythonScript= "C:\\Users\\Hrishikesh\\OneDrive\\Desktop\\scipy_processing\\main.py";
        const String pythonScript = "./scipy_processing/main.py";
        const String pythonPath = "python";

        public DataRead()
        {
            InitializeComponent();
            chart1.Visible = false;
            chart2.Visible = false;
            chart3.Visible = false;

            comboBox2.Visible = false;
            comboBox3.Visible = false;
            label1.Visible  = false;
            label3.Visible = false;

        }

        private void DataRead_Load(object sender, EventArgs e)
        {
            ports = SerialPort.GetPortNames().ToList();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DataSource = ports;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                //disconnnect device;
                if (port != null && port.IsOpen)
                {
                    port.Write("!");
                    port.Close();
                    port.Dispose();
                }
                if (fetchThread != null)
                {
                    fetchThread.Abort();
                }
                changeUIToDisconnected();
                isConnected = false;

                channel1.Clear();
                channel2.Clear();
                channel3.Clear();
                channel1F.Clear();
                channel2F.Clear();
                channel3F.Clear();

                channel1FFT.Clear();
                channel2FFT.Clear();
                channel3FFT.Clear();

                chart1.Visible = false;
                chart2.Visible = false;
                chart3.Visible = false;

                comboBox2.Visible = false;
                comboBox3.Visible = false;

                label1.Visible = false;
                label3.Visible = false;
                label4.Visible  = false;

                timer1.Enabled = false;
                timer2.Enabled = false;
            }
            else
            {
                //connect device
                if (comboBox1.SelectedItem != null)
                {
                    port = new SerialPort(comboBox1.SelectedItem.ToString(), 115200)
                    {
                        DtrEnable = true,
                        RtsEnable = true
                    };
                    port.Open();
                    port.Write("s");
                    chart1.Visible = true;
                    chart2.Visible = true;
                    chart3.Visible = true;

                    comboBox2.Visible = true;
                    comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox2.DataSource = new string[]{"All Channel","Channel 1", "Channel 2", "Channel 3" };
                    comboBox3.Visible = true;
                    comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
                    comboBox3.DataSource = new string[] { "All Channel", "Channel 1", "Channel 2", "Channel 3" };

                    label1.Visible = true;
                    label3.Visible = true;
                    label4.Visible=true;

                    channel3.Clear();
                    channel1.Clear();
                    channel2.Clear();
                    channel1F.Clear();
                    channel2F.Clear();
                    channel3F.Clear();

                    channel1FFT.Clear();
                    channel2FFT.Clear();
                    channel3FFT.Clear();

                    timer1.Interval = 100;
                    timer1.Enabled = true;

                    timer2.Interval = 300;
                    timer2.Enabled = true;

                    fetchThread = new Thread(ReadValuesOnThread);
                    fetchThread.Start();

                    changeUIToConnected();
                    isConnected = true;
                }
            }
        }

        void ReadValuesOnThread()
        {
            bool isFirst = true;
            while (true)
            {
                try
                {
                    if (port != null && port.IsOpen)
                    {
                        if (isFirst)
                        {
                            port.ReadLine();
                            isFirst = false;
                        }
                        else
                        {
                            String dataRaw = port.ReadLine();
                            //Console.WriteLine(dataRaw);

                            List<string> line = dataRaw.Split('z').ToList();
                            double time = 0;
                            //Console.WriteLine(line);

                            for (int i = 0; i < line.Count; i++)
                            {
                                if (line[i].Contains('T'))
                                {
                                    time = double.Parse(line[i].Replace("T", "")) * 0.001;
                                }
                            }

                            for (int i = 0; i < line.Count; i++)
                            {
                                if (line[i].Contains('A'))
                                {
                                    double channel1Double = double.Parse(line[i].Replace("A", ""));
                                    lock (channel1)
                                    {
                                        channel1.Add(new DataPointTimeSeries(time, channel1Double));
                                    }
                                    lock (channel1F)
                                    {
                                        channel1F.Add(new DataPointTimeSeries(time, channel1Double));
                                    }
                                }


                                if (line[i].Contains('B'))
                                {
                                    double channel2Double = double.Parse(line[i].Replace("B", ""));

                                    lock (channel2)
                                    {
                                        channel2.Add(new DataPointTimeSeries(time, channel2Double));
                                    }
                                    lock (channel2F)
                                    {
                                        channel2F.Add(new DataPointTimeSeries(time, channel2Double));
                                    }
                                }
                                if (line[i].Contains('C'))
                                {
                                    double channel3Double = double.Parse(line[i].Replace("C", ""));

                                    lock (channel3)
                                    {
                                        channel3.Add(new DataPointTimeSeries(time, channel3Double));
                                    }
                                    lock (channel3F)
                                    {
                                        channel3F.Add(new DataPointTimeSeries(time, channel3Double));
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private void changeUIToDisconnected()
        {
            comboBox1.Enabled = true;
            ports = SerialPort.GetPortNames().ToList();
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.DataSource = ports;
            button1.FlatStyle = FlatStyle.Standard;
            button1.Text = "Start Streaming";
            button1.ForeColor = Color.Black;
        }

        private void changeUIToConnected()
        {
            comboBox1.Enabled = false;
            button1.Text = "Disconnect";
            button1.ForeColor = Color.Red;
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderColor = Color.Red;
        }

        private int nearestPowerOfTwo(int size)
        {
            return (int)Math.Pow(2, (int)Math.Floor(Math.Log(size, 2)));
        }

        private double[] linspace(double start, double stop, int samples)
        {
            List<double> sampling = new List<double>();
            int count = 1;
            sampling.Add(start);
            while (count < samples)
            {
                sampling.Add(sampling.Last() + ((double)(stop - start)) / ((double)(samples - 1)));
                count++;
            }
            return sampling.ToArray();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            List<double> time1 = new List<double>();
            List<double> channel1Double = new List<double>();
            lock (channel1)
            {
                for (int i = 0; i < channel1.Count; i++)
                {
                    channel1Double.Add(channel1[i].voltage);
                    time1.Add(channel1[i].time);
                }
                channel1.Clear();
            }

            List<double> time2 = new List<double>();
            List<double> channel2Double = new List<double>();
            lock (channel2)
            {
                for (int i = 0; i < channel2.Count; i++)
                {
                    channel2Double.Add(channel2[i].voltage);
                    time2.Add(channel2[i].time);
                }
                channel2.Clear();
            }

            List<double> time3 = new List<double>();
            List<double> channel3Double = new List<double>();
            lock (channel3)
            {
                for (int i = 0; i < channel3.Count; i++)
                {
                    channel3Double.Add(channel3[i].voltage);
                    time3.Add(channel3[i].time);
                }
                channel3.Clear();
            }

            try
            {
                channel1Double = DataFilter.detrend(channel1Double.ToArray(), (int)DetrendOperations.CONSTANT).ToList();
                channel1Double = DataFilter.perform_bandstop(channel1Double.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel1Double = DataFilter.perform_highpass(channel1Double.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel1Double = DataFilter.perform_lowpass(channel1Double.ToArray(), 250, 50, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                for (int i = 0; i < channel1Double.Count; i++)
                {
                    chart1.Series[0].Points.AddXY(time1[i], channel1Double[i]);
                    if (chart1.Series[0].Points.Count > 1024)
                    {
                        chart1.Series[0].Points.RemoveAt(0);
                        chart1.ChartAreas[0].AxisX.Minimum = chart1.Series[0].Points[0].XValue;
                        chart1.ChartAreas[0].AxisX.Maximum = chart1.Series[0].Points[0].XValue + 4;
                        chart1.ChartAreas[0].AxisY.Minimum = -3.5;
                        chart1.ChartAreas[0].AxisY.Maximum = 3.5;
                    }
                }
                channel2Double = DataFilter.detrend(channel2Double.ToArray(), (int)DetrendOperations.CONSTANT).ToList();
                channel2Double = DataFilter.perform_highpass(channel2Double.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel2Double = DataFilter.perform_bandstop(channel2Double.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel2Double = DataFilter.perform_lowpass(channel2Double.ToArray(), 250, 50, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                for (int i = 0; i < channel2Double.Count; i++)
                {
                    chart2.Series[0].Points.AddXY(time2[i], channel2Double[i]);
                    if (chart2.Series[0].Points.Count > 1024)
                    {
                        chart2.Series[0].Points.RemoveAt(0);
                        chart2.ChartAreas[0].AxisX.Minimum = chart2.Series[0].Points[0].XValue;
                        chart2.ChartAreas[0].AxisX.Maximum = chart2.Series[0].Points[0].XValue + 4;
                        chart2.ChartAreas[0].AxisY.Minimum = -3.5;
                        chart2.ChartAreas[0].AxisY.Maximum = 3.5;
                    }
                }
                channel3Double = DataFilter.detrend(channel3Double.ToArray(), (int)DetrendOperations.CONSTANT).ToList();
                channel3Double = DataFilter.perform_highpass(channel3Double.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel3Double = DataFilter.perform_bandstop(channel3Double.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel3Double = DataFilter.perform_lowpass(channel3Double.ToArray(), 250, 50, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();

                for (int i = 0; i < channel3Double.Count; i++)
                {
                    chart3.Series[0].Points.AddXY(time3[i], channel3Double[i]);
                    if (chart3.Series[0].Points.Count > 1024)
                    {
                        chart3.Series[0].Points.RemoveAt(0);
                        chart3.ChartAreas[0].AxisX.Minimum = chart3.Series[0].Points[0].XValue;
                        chart3.ChartAreas[0].AxisX.Maximum = chart3.Series[0].Points[0].XValue + 4;
                        chart3.ChartAreas[0].AxisY.Minimum = -3.5;
                        chart3.ChartAreas[0].AxisY.Maximum = 3.5;
                    }
                }
            }
            catch (Exception m)
            {
                Console.WriteLine("H" + m.Message);
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                //List<double> time1 = new List<double>();
                //List<double> channel1Double = new List<double>();
                lock (channel1F)
                {
                    Console.WriteLine("Channel1F" + channel1F.Count);
                    for (int i = 0; i < channel1F.Count; i++)
                    {
                        channel1FFT.Add(channel1F[i].voltage);
                        //time1.Add(channel1F[i].time);
                    }
                    channel1F.Clear();
                }

                //List<double> time2 = new List<double>();
                //List<double> channel2Double = new List<double>();
                lock (channel2F)
                {
                    Console.WriteLine("Channel2F" + channel2F.Count);
                    for (int i = 0; i < channel2F.Count; i++)
                    {
                        channel2FFT.Add(channel2F[i].voltage);
                        //time2.Add(channel2F[i].time);
                    }
                    channel2F.Clear();
                }

                //List<double> time3 = new List<double>();
                //List<double> channel3Double = new List<double>();
                lock (channel3F)
                {
                    Console.WriteLine("Channel3F" + channel3F.Count);
                    for (int i = 0; i < channel3F.Count; i++)
                    {
                        channel3FFT.Add(channel3F[i].voltage);
                        //time3.Add(channel3F[i].time);
                    }
                    channel3F.Clear();
                }

                Console.WriteLine("Lengths");
                //Console.WriteLine(channel1Double.Count);
                //Console.WriteLine(channel2Double.Count);
                //Console.WriteLine(channel3Double.Count);

                if (channel1FFT.Count > 512)
                {
                    channel1FFT = channel1FFT.Skip(channel1FFT.Count - 512).ToList();
                }


                if (channel2FFT.Count > 512)
                {
                    channel2FFT = channel2FFT.Skip(channel2FFT.Count - 512).ToList();
                }

                if (channel3FFT.Count > 512)
                {
                    channel3FFT = channel3FFT.Skip(channel3FFT.Count - 512).ToList();
                }


                channel1FFT = DataFilter.detrend(channel1FFT.ToArray(), (int)DetrendOperations.LINEAR).ToList();
                channel1FFT = DataFilter.perform_bandstop(channel1FFT.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel1FFT = DataFilter.perform_highpass(channel1FFT.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel1FFT = DataFilter.perform_lowpass(channel1FFT.ToArray(), 250, 45, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();

                //Console.WriteLine("HA");

                channel2FFT = DataFilter.detrend(channel2FFT.ToArray(), (int)DetrendOperations.LINEAR).ToList();
                channel2FFT = DataFilter.perform_highpass(channel2FFT.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel2FFT = DataFilter.perform_bandstop(channel2FFT.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel2FFT = DataFilter.perform_lowpass(channel2FFT.ToArray(), 250, 45, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();

                channel3FFT = DataFilter.detrend(channel3FFT.ToArray(), (int)DetrendOperations.LINEAR).ToList();
                channel3FFT = DataFilter.perform_highpass(channel3FFT.ToArray(), 250, 0.5, 8, (int)FilterTypes.BUTTERWORTH, 0.0).ToList();
                channel3FFT = DataFilter.perform_bandstop(channel3FFT.ToArray(), 250, 48.0, 52.0, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();
                channel3FFT = DataFilter.perform_lowpass(channel3FFT.ToArray(), 250, 45, 8, (int)FilterTypes.BUTTERWORTH_ZERO_PHASE, 0.0).ToList();


                Tuple<double[], double[]> psd1 = DataFilter.get_psd_welch(channel1FFT.ToArray(), channel1FFT.Count, channel1FFT.Count / 2, 250, (int)WindowOperations.HANNING);
                Tuple<double[], double[]> psd2 = DataFilter.get_psd_welch(channel2FFT.ToArray(), channel2FFT.Count, channel2FFT.Count / 2, 250, (int)WindowOperations.HANNING);
                Tuple<double[], double[]> psd3 = DataFilter.get_psd_welch(channel3FFT.ToArray(), channel3FFT.Count, channel3FFT.Count / 2, 250, (int)WindowOperations.HANNING);

                List<Tuple<double[], double[]>> psdList = new List<Tuple<double[], double[]>>
            {
                psd1,
                psd2,
                psd3
            };

                double alpha = 0;
                double beta = 0;
                double gamma = 0;
                double delta = 0;
                double theta = 0;

                int count = 0;
                //new string[]{"All Channel","Channel 1", "Channel 2", "Channel 3" };
                for (int i = 0; i < psdList.Count; i++)
                {
                    if (i == 0 && (comboBox3.SelectedItem.ToString() == "All Channel"|| comboBox3.SelectedItem.ToString() == "Channel 1")) {
                        delta += DataFilter.get_band_power(psdList[i], 2, 4.0);
                        count++;
                        theta += DataFilter.get_band_power(psdList[i], 4.0, 8.0);
                        alpha += DataFilter.get_band_power(psdList[i], 8.0, 13.0);
                        beta += DataFilter.get_band_power(psdList[i], 13.0, 30.0);
                        gamma += DataFilter.get_band_power(psdList[i], 30.0, 45.0);
                    }
                    if (i == 1 && (comboBox3.SelectedItem.ToString() == "All Channel" || comboBox3.SelectedItem.ToString() == "Channel 2"))
                    {
                        count++;
                        delta += DataFilter.get_band_power(psdList[i], 2, 4.0);
                        theta += DataFilter.get_band_power(psdList[i], 4.0, 8.0);
                        alpha += DataFilter.get_band_power(psdList[i], 8.0, 13.0);
                        beta += DataFilter.get_band_power(psdList[i], 13.0, 30.0);
                        gamma += DataFilter.get_band_power(psdList[i], 30.0, 45.0);
                    }
                    if (i == 2 && (comboBox3.SelectedItem.ToString() == "All Channel" || comboBox3.SelectedItem.ToString() == "Channel 3"))
                    {
                        count++;
                        delta += DataFilter.get_band_power(psdList[i], 2, 4.0);
                        theta += DataFilter.get_band_power(psdList[i], 4.0, 8.0);
                        alpha += DataFilter.get_band_power(psdList[i], 8.0, 13.0);
                        beta += DataFilter.get_band_power(psdList[i], 13.0, 30.0);
                        gamma += DataFilter.get_band_power(psdList[i], 30.0, 45.0);
                    }
                }

                double deltaAvg = delta / count;
                double thetaAvg = theta / count;
                double alphaAvg = alpha / count;
                double betaAvg = beta / count;
                double gammaAvg = gamma / count;

                BrainFlowModelParams model_params = new BrainFlowModelParams((int)BrainFlowMetrics.MINDFULNESS, (int)BrainFlowClassifiers.DEFAULT_CLASSIFIER);
                double[] feature_vector = {delta,theta,alpha,beta,gamma};
                MLModel model = new MLModel(model_params);
                model.prepare();
                // Console.WriteLine("Score: " + model.predict(feature_vector)[0]);
                label4.Text = (model.predict(feature_vector)[0]).ToString();
                model.release();

                chart4.Series["power"].Points.Clear();


                chart4.Series["power"].Points.Add(delta);
                chart4.Series["power"].Points[0].Color = Color.Green;
                chart4.Series["power"].Points[0].AxisLabel = "Delta (0.5-4 Hz)";

                chart4.Series["power"].Points.Add(theta);
                chart4.Series["power"].Points[1].Color = Color.Aquamarine;
                chart4.Series["power"].Points[1].AxisLabel = "Theta (4-7 Hz)";

                chart4.Series["power"].Points.Add(alpha);
                chart4.Series["power"].Points[2].Color = Color.Red;
                chart4.Series["power"].Points[2].AxisLabel = "Alpha (7-13 Hz)";

                chart4.Series["power"].Points.Add(beta);
                chart4.Series["power"].Points[3].Color = Color.Blue;
                chart4.Series["power"].Points[3].AxisLabel = "Beta (14-30 Hz)";

                chart4.Series["power"].Points.Add(gamma);
                chart4.Series["power"].Points[4].Color = Color.Orange;
                chart4.Series["power"].Points[4].AxisLabel = "Gamma (>30 Hz)";

                System.Numerics.Complex[] fft_data1 = DataFilter.perform_fft(channel1FFT.Skip(channel1FFT.Count - nearestPowerOfTwo(channel1FFT.Count)).ToArray(), 0, nearestPowerOfTwo(channel1FFT.Count), (int)WindowOperations.NO_WINDOW);
                System.Numerics.Complex[] fft_data2 = DataFilter.perform_fft(channel2FFT.Skip(channel2FFT.Count - nearestPowerOfTwo(channel2FFT.Count)).ToArray(), 0, nearestPowerOfTwo(channel2FFT.Count), (int)WindowOperations.NO_WINDOW);
                System.Numerics.Complex[] fft_data3 = DataFilter.perform_fft(channel3FFT.Skip(channel3FFT.Count - nearestPowerOfTwo(channel3FFT.Count)).ToArray(), 0, nearestPowerOfTwo(channel3FFT.Count), (int)WindowOperations.NO_WINDOW);


                chart5.Series[0].Points.Clear();
                chart5.Series[1].Points.Clear();
                chart5.Series[2].Points.Clear();

                double[] bins = linspace(0, 125, fft_data1.Length);
                for (int i = 0; i < bins.Length; i++)
                {
                    if (comboBox2.SelectedItem.ToString() == "All Channel" || comboBox2.SelectedItem.ToString() == "Channel 1") 
                    {
                        chart5.Series[0].Points.AddXY(bins[i], fft_data1[i].Magnitude);
                    }
                    if (comboBox2.SelectedItem.ToString() == "All Channel" || comboBox2.SelectedItem.ToString() == "Channel 2")
                    {
                        chart5.Series[1].Points.AddXY(bins[i], fft_data2[i].Magnitude);
                    }
                    if (comboBox2.SelectedItem.ToString() == "All Channel" || comboBox2.SelectedItem.ToString() == "Channel 3")
                    {
                        chart5.Series[2].Points.AddXY(bins[i], fft_data3[i].Magnitude);
                    }
                }

            }
            catch
            {
                Console.WriteLine("F");
            }
        }
    }
}

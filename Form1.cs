using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.IO;
using System.Threading;
using System.Collections;
using MathNet.Numerics.LinearAlgebra.Double;

namespace CCS_Reader
{
    public partial class Form1 : Form
    {
        const byte ports_total = 4;

        string[] selected_com = new string[ports_total];
        SerialPort[] active_com = new SerialPort[ports_total];
        Queue[] read_queue = new Queue[ports_total + 1];
        Queue[] byte2read = new Queue[ports_total];
        bool all_connected = false;
        volatile int thread_counter = 0;
        volatile bool[] thread_running = new bool[4];
        volatile bool stop_reading = false;
        volatile System.Diagnostics.Stopwatch control = new System.Diagnostics.Stopwatch();

        public Form1()
        {
            InitializeComponent();
        }

        private void Connect()
        {

            for (int i = 0; i < ports_total; i++)
            {
                active_com[i] = new SerialPort(selected_com[i], 230400, 0, 8, StopBits.One);
                active_com[i].WriteBufferSize = 512;
                active_com[i].ReadBufferSize = 8192;
                active_com[i].Open();
            }
            all_connected = true;
        }

        private void Disconnect()
        {
            for (int i=0; i < ports_total; i++)
                active_com[i].Close();
            all_connected = false;
        }

        private void com_search()
        {
            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports)
            {
                comBox1.Items.Add(port);
                comBox2.Items.Add(port);
                comBox3.Items.Add(port);
                comBox4.Items.Add(port);
            }
            if (ports.Length < 4)
            {
                //MessageBox.Show("Найдено меньше 4 портов.");
                return;
            }
            if (ports.Length != 0)
            {
                comBox1.SelectedIndex = 0;
                selected_com[0] = comBox1.SelectedItem.ToString();
                comBox2.SelectedIndex = 1;
                selected_com[1] = comBox2.SelectedItem.ToString();
                comBox3.SelectedIndex = 2;
                selected_com[2] = comBox3.SelectedItem.ToString();
                comBox4.SelectedIndex = 3;
                selected_com[3] = comBox4.SelectedItem.ToString();
            }  
        }

        private void check_read_available()
        {
            if ((comBox1.Items.Count < 4)||(saveFileDialog.FileName == ""))
                readButton.Enabled = false;
            else
                readButton.Enabled = true;
        }

        private void comBox_Click(object sender, EventArgs e)
        {
            ListBox source = (ListBox)sender;
            int index = Convert.ToInt32((string)source.Tag);
            selected_com[index] = source.SelectedItem.ToString();
            check_read_available();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            com_search();
            saveFileDialog.FileName = "";
            check_read_available();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!all_connected)
            {
                Application.Exit();
                return;
            }
            for (int i = 0; i < ports_total; i++)
            {
                if (active_com[i].IsOpen)
                    active_com[i].Close();
            }
            Application.Exit();
        }

        private void readButton_Click(object sender, EventArgs e)
        {
            Button source = (Button)sender;
            switch (source.Text)
            {
                case "Начать считывание":
                    Connect();
                    source.Text = "Остановить чтение";
                    stop_reading = false;
                    Thread[] reading = new Thread[ports_total];
                    for (int i = 0; i < ports_total; i++)
                    {
                        read_queue[i] = new Queue();
                        //reading[i] = new Thread(thread_read_flexible);
                        reading[i] = new Thread(thread_read_raw);
                        reading[i].Start();
                    }
                    control.Restart();
                    break;
                case "Остановить чтение":
                    stop_reading = true;
                    control.Stop();
                    read_queue[4] = new Queue();
                    //MessageBox.Show("Чтение завершено.");
                    for (int i = 0; i < ports_total; i++)
                        write_data_raw(i);
                    source.Text = "Начать считывание";
                    progressBar.Value = 0;
                    thread_counter = 0;
                    
                    int[] length = {read_queue[0].Count, read_queue[1].Count,
                                   read_queue[2].Count, read_queue[3].Count, read_queue[4].Count};
                    packet[] sensor_1 = new packet[length[0]];
                    packet[] sensor_2 = new packet[length[1]];
                    packet[] sensor_3 = new packet[length[2]];
                    packet[] sensor_4 = new packet[length[3]];
                    packet[] sensor_5 = new packet[length[4]];
                    int counter = 0;
                    bool[] finished = new bool[4 + 1];
                    while (true)
                    {
                        if ((!finished[0])&&(counter < length[0]))
                            sensor_1[counter] = (packet)read_queue[0].Dequeue();
                        else
                            finished[0] = true;
                        if ((!finished[1]) && (counter < length[1]))
                            sensor_2[counter] = (packet)read_queue[1].Dequeue();
                        else
                            finished[1] = true;
                        if ((!finished[2]) && (counter < length[2]))
                            sensor_3[counter] = (packet)read_queue[2].Dequeue();
                        else 
                            finished[2] = true;
                        if ((!finished[3]) && (counter < length[3]))
                            sensor_4[counter] = (packet)read_queue[3].Dequeue();
                        else
                            finished[3] = true;
                        if ((!finished[4]) && (counter < length[4]))
                            sensor_5[counter] = (packet)read_queue[4].Dequeue();
                        else
                            finished[4] = true;
                        counter++;
                        if (finished[0] && finished[1] && finished[2] && finished[3] && finished[4])
                            break;
                    }
                    if (sensor_1.Length != 0)
                        write_data(sensor_1, 1);
                    if (sensor_2.Length != 0)
                        write_data(sensor_2, 2);
                    if (sensor_3.Length != 0)
                        write_data(sensor_3, 3);
                    if (sensor_4.Length != 0)
                        write_data(sensor_4, 4);
                    if (sensor_5.Length != 0)
                        write_data(sensor_5, 5);
                    //MessageBox.Show("Сохранение завершено.");
                    saveBox.Text = "Сохранение завершено";
                    break;
            }
            

            
            
            
            
                    
        }

        //private void thread_read(object source)
        //{

        //    int index = thread_counter++;
        //    thread_running[index] = true;
        //    byte[] buffer = new byte[64];
        //    int counter = 0;
        //    int bytes_read = 0;
        //    packet pack;
        //    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            
        //    timer.Start();
        //    while (true)
        //    {
        //        if (active_com[index].BytesToRead >= 40)
        //        {
        //            bytes_read = active_com[index].Read(buffer, 0, 40);
        //            if (bytes_read == 40)
        //            {
        //                pack = new packet();
        //                pack.frame1 = buffer[0];
        //                pack.type = buffer[1];
        //                pack.ticks = BitConverter.ToUInt32(buffer, 2);
        //                pack.a = new short[3];
        //                pack.a[0] = BitConverter.ToInt16(buffer, 6);
        //                pack.a[1] = BitConverter.ToInt16(buffer, 8);
        //                pack.a[2] = BitConverter.ToInt16(buffer, 10);
        //                pack.w = new short[3];
        //                pack.w[0] = BitConverter.ToInt16(buffer, 12);
        //                pack.w[1] = BitConverter.ToInt16(buffer, 14);
        //                pack.w[2] = BitConverter.ToInt16(buffer, 16);
        //                pack.m = new short[3];
        //                pack.m[0] = BitConverter.ToInt16(buffer, 18);
        //                pack.m[1] = BitConverter.ToInt16(buffer, 20);
        //                pack.m[2] = BitConverter.ToInt16(buffer, 22);
        //                pack.quat = new short[4];
        //                pack.quat[0] = BitConverter.ToInt16(buffer, 24);
        //                pack.quat[1] = BitConverter.ToInt16(buffer, 26);
        //                pack.quat[2] = BitConverter.ToInt16(buffer, 28);
        //                pack.quat[3] = BitConverter.ToInt16(buffer, 30);
        //                pack.bar = BitConverter.ToInt16(buffer, 32);
        //                pack.temper = BitConverter.ToInt16(buffer, 34);
        //                pack.snum = buffer[36];
        //                pack.crc = buffer[37];
        //                pack.frame2 = buffer[38];
        //                pack.frame3 = buffer[39];
        //                read_queue[index].Enqueue(pack);
        //                counter++;
        //                timer.Restart();
        //                if (stop_reading)
        //                    break;
        //            }
        //        }
        //        if (timer.ElapsedMilliseconds >= 1000)
        //            break;
                
        //    }
        //    timer.Stop();
        //    active_com[index].Close();
        //    thread_running[index] = false;
        //}

        //private void thread_read_flexible(object source)
        //{
        //    int index = thread_counter++;
        //    thread_running[index] = true;
        //    byte[] buffer = new byte[64];
        //    byte[] finder = new byte[2];
        //    int counter = 0;
        //    int bytes_read = 0;
        //    packet pack;
        //    System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();

        //    timer.Start();
        //    while (true)
        //    {
        //        if (active_com[index].BytesToRead >= 40)
        //        {
        //            bytes_read = active_com[index].Read(buffer, 0, 1);
        //            finder[1] = buffer[0];
        //            if ((finder[0] == 0x10) && ((finder[1] == 0x41) || (finder[1] == 0x51)))
        //            {
        //                bytes_read = active_com[index].Read(buffer, 2, 38);
        //                pack = new packet();
        //                pack.frame1 = finder[0];
        //                pack.type = finder[1];
        //                pack.ticks = BitConverter.ToUInt32(buffer, 2);
        //                pack.a = new short[3];
        //                pack.a[0] = BitConverter.ToInt16(buffer, 6);
        //                pack.a[1] = BitConverter.ToInt16(buffer, 8);
        //                pack.a[2] = BitConverter.ToInt16(buffer, 10);
        //                pack.w = new short[3];
        //                pack.w[0] = BitConverter.ToInt16(buffer, 12);
        //                pack.w[1] = BitConverter.ToInt16(buffer, 14);
        //                pack.w[2] = BitConverter.ToInt16(buffer, 16);
        //                pack.m = new short[3];
        //                pack.m[0] = BitConverter.ToInt16(buffer, 18);
        //                pack.m[1] = BitConverter.ToInt16(buffer, 20);
        //                pack.m[2] = BitConverter.ToInt16(buffer, 22);
        //                pack.quat = new short[4];
        //                pack.quat[0] = BitConverter.ToInt16(buffer, 24);
        //                pack.quat[1] = BitConverter.ToInt16(buffer, 26);
        //                pack.quat[2] = BitConverter.ToInt16(buffer, 28);
        //                pack.quat[3] = BitConverter.ToInt16(buffer, 30);
        //                pack.bar = BitConverter.ToInt16(buffer, 32);
        //                pack.temper = BitConverter.ToInt16(buffer, 34);
        //                pack.snum = buffer[36];
        //                pack.crc = buffer[37];
        //                pack.frame2 = buffer[38];
        //                pack.frame3 = buffer[39];
        //                if (pack.snum == 0xF1)
        //                    read_queue[index].Enqueue(pack);
        //                counter++;
        //                timer.Restart();
        //                if (stop_reading)
        //                    break;
        //            }
        //            finder[0] = finder[1];
        //        }                    
        //        if (timer.ElapsedMilliseconds >= 1000)
        //            break;
                
        //    }
        //    timer.Stop();
        //    active_com[index].Close();
        //    thread_running[index] = false;
        //}

        private void thread_read_raw(object source)
        {
            int index = thread_counter++;
            thread_running[index] = true;
            
            int read_buffer = 128;
            int len = 0;
            byte[] buffer = new byte[read_buffer];
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            byte2read[index] = new Queue();
            timer.Start();
            while (true)
            {
                if (active_com[index].BytesToRead > read_buffer)
                {
                    len = active_com[index].Read(buffer,0,128);
                    for (int i = 0; i < len; i++)
                    {
                        byte2read[index].Enqueue(buffer[i]);
                    }
                    timer.Restart();
                    if (stop_reading)
                        break;
                }
                if (timer.ElapsedMilliseconds >= 1000)
                    break;
            }
            timer.Stop();
            active_com[index].Close();
            thread_running[index] = false;
        }

        private void write_data(packet[] source, int index)
        {
            int length = source.Length;
            saveBox.Text = "Сохранение файла " + index;
            saveBox.Update();
            progressBar.Value = 0;
            progressBar.Maximum = length;
            FileStream fs_imu = File.Create(saveFileDialog.FileName + "_" + index + ".imu", 2048, FileOptions.None);
            BinaryWriter str_imu = new BinaryWriter(fs_imu);
            Int16 buf16; Byte buf8; Int32 buf32;
            Double bufD; Single bufS; UInt32 bufU32;

            DenseVector Magn_coefs = new DenseVector(12);
            DenseVector Accl_coefs = new DenseVector(12);
            DenseVector Gyro_coefs = new DenseVector(12);
            Kalman_class.Parameters Parameters = new Kalman_class.Parameters(Accl_coefs, Magn_coefs, Gyro_coefs);
            Kalman_class.Sensors Sensors = new Kalman_class.Sensors(new DenseMatrix(1, 3, 0), new DenseMatrix(1, 3, 0), new DenseMatrix(1, 3, 0));
            Matrix Initia_quat = new DenseMatrix(1, 4, 0);
            Initia_quat.At(0, 0, 1);
            Kalman_class.State State = new Kalman_class.State(Math.Pow(10, 2), Math.Pow(10, 2), Math.Pow(10, -3),
                Math.Pow(10, -6), Math.Pow(10, -15), Math.Pow(10, -15), Initia_quat);
            double[] angles = new double[3];
            double[] mw, ma, mm;
            ma = new double[3];
            mw = new double[3];
            mm = new double[3];
            Tuple<Vector, Kalman_class.Sensors, Kalman_class.State> AHRS_result;

            double[] magn_c = new double[12];
            double[] accl_c = new double[12];
            double[] gyro_c = new double[12];

            double[] read_coefs = { 0.833F/1000, 0.04, 142.9F };
            //read_coefs[1] *= (Math.PI / 180);
            //double[] read_coefs = { 1,1,1 };
            if (index == 3)
            {
                read_coefs[0] = 3.9F/1000;
                read_coefs[1] = 1;
                read_coefs[2] = 1;
                //length *= 2;
            }
            for (int i = 0; i < length; i++)
            {
                progressBar.Value++;

                Sensors.a.At(0, 0, source[i].a[0] * read_coefs[0]);
                Sensors.a.At(0, 1, source[i].a[1] * read_coefs[0]);
                Sensors.a.At(0, 2, source[i].a[2] * read_coefs[0]);

                Sensors.w.At(0, 0, source[i].w[0] * read_coefs[1]);
                Sensors.w.At(0, 1, source[i].w[1] * read_coefs[1]);
                Sensors.w.At(0, 2, source[i].w[2] * read_coefs[1]);

                Sensors.m.At(0, 0, source[i].m[0] * read_coefs[2]);
                Sensors.m.At(0, 1, source[i].m[1] * read_coefs[2]);
                Sensors.m.At(0, 2, source[i].m[2] * read_coefs[2]);

                AHRS_result = Kalman_class.AHRS_LKF_EULER(Sensors, State, Parameters);

                State = AHRS_result.Item3;
                //------------------------------------------------------------------------
                //mm = single_correction(magn_c, m[i, 0], m[i, 1], m[i, 2]);
                //ma = single_correction(accl_c, a[i, 0], a[i, 1], a[i, 2]);
                //mw = single_correction(gyro_c, w[i, 0], w[i, 1], w[i, 2]);
                //----------------------------------------------------------------------
                mw[0] = source[i].w[0] * read_coefs[1];
                mw[1] = source[i].w[1] * read_coefs[1];
                mw[2] = source[i].w[2] * read_coefs[1];
                ma[0] = source[i].a[0] * read_coefs[0];
                ma[1] = source[i].a[1] * read_coefs[0];
                ma[2] = source[i].a[2] * read_coefs[0];
                mm[0] = source[i].m[0] * read_coefs[2];
                mm[1] = source[i].m[1] * read_coefs[2];
                mm[2] = source[i].m[2] * read_coefs[2];

                //if ((i >= 100) && (index == 1))
                //{
                //    double qwa = Math.Sqrt(Math.Pow(ma[0], 2) + Math.Pow(ma[1], 2) + Math.Pow(ma[2], 2));
                //    double qww = Math.Sqrt(Math.Pow(mw[0], 2) + Math.Pow(mw[1], 2) + Math.Pow(mw[2], 2));
                //    double qwm = Math.Sqrt(Math.Pow(mm[0], 2) + Math.Pow(mm[1], 2) + Math.Pow(mm[2], 2));
                //    double co = 1 / qwm;
                //}
                //----------------------------------------------------------------------
                angles[0] = (AHRS_result.Item1.At(0));
                angles[1] = (AHRS_result.Item1.At(1));
                angles[2] = (AHRS_result.Item1.At(2));

                // IMU
                buf16 = (Int16)(angles[0] * 10000);
                str_imu.Write(buf16);
                buf16 = (Int16)(angles[1] * 10000);
                str_imu.Write(buf16);
                buf16 = (Int16)(angles[2] * 10000);
                str_imu.Write(buf16);

                buf16 = (Int16)(mw[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mw[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mw[2] * 3000);
                str_imu.Write(buf16);

                buf16 = (Int16)(ma[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(ma[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(ma[2] * 3000);
                str_imu.Write(buf16);

                buf16 = (Int16)(mm[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mm[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mm[2] * 3000);
                str_imu.Write(buf16);

                buf16 = (Int16)(source[i].quat[0]);
                str_imu.Write(buf16);

                //buf32 = (Int32)(counter[i]);
                buf32 = (Int32)(source[i].ticks);
                str_imu.Write(buf32);

                buf8 = (Byte)(0);
                str_imu.Write(buf8);
            }
            
            str_imu.Flush();
            str_imu.Close();
            
        }

        private void write_data_raw(int index)
        {
            byte[] full_file = new byte[byte2read[index].Count];
            packet pack = new packet();
            byte[] temp = new byte[2];
            int crc;
            progressBar.Value = 0;
            byte type_flag = 0;
            switch (index)
            {
                case 0:
                    type_flag = 65;
                    break;
                case 1:
                    type_flag = 81;
                    break;
            }
            if (full_file.Length != 0)
            {
                for (int i = 0; i < full_file.Length; i++)
                {
                    full_file[i] = (byte)byte2read[index].Dequeue();
                    
                }
                progressBar.Maximum = full_file.Length - 39;
                for (int i = 0; i < full_file.Length - 39; i++)
                {
                    if (i % 50 == 0)
                        progressBar.Value += 49;
                    if ((full_file[i] == 0x10) && (full_file[i + 38] == 0x10) && (full_file[i + 39] == 0x03) &&
                        (full_file[i + 1] == type_flag))
                    {
                        crc = 0;
                        for (int j = i + 1; j < i + 37; j++)
                        {
                            crc = crc ^ full_file[j];
                        }
                        //crc = full_file[i + 37];
                        if (crc == full_file[i + 37])
                        {
                            pack = new packet();
                            pack.frame1 = full_file[i];
                            pack.type = full_file[i + 1];
                            pack.ticks = BitConverter.ToUInt32(full_file, i + 2);
                            //0.833F/1000, 0.04, 142.9F
                            pack.a = new short[3];
                            pack.a[0] = BitConverter.ToInt16(full_file, i + 6);
                            pack.a[1] = BitConverter.ToInt16(full_file, i + 8);
                            pack.a[2] = BitConverter.ToInt16(full_file, i + 10);
                            pack.w = new short[3];
                            //pack.w[0] = BitConverter.ToInt16(full_file, i + 12);
                            //pack.w[1] = BitConverter.ToInt16(full_file, i + 14);
                            //pack.w[2] = BitConverter.ToInt16(full_file, i + 16);
                            temp[0] = full_file[i + 13];
                            temp[1] = full_file[i + 12];
                            pack.w[0] = BitConverter.ToInt16(temp, 0);
                            temp[0] = full_file[i + 15];
                            temp[1] = full_file[i + 14];
                            pack.w[1] = BitConverter.ToInt16(temp,0);
                            temp[0] = full_file[i + 17];
                            temp[1] = full_file[i + 16];
                            pack.w[2] = BitConverter.ToInt16(temp, 0);
                            pack.m = new short[3];
                            pack.m[0] = BitConverter.ToInt16(full_file, i + 18);
                            pack.m[1] = BitConverter.ToInt16(full_file, i + 20);
                            pack.m[2] = BitConverter.ToInt16(full_file, i + 22);
                            pack.quat = new short[4];
                            pack.quat[0] = BitConverter.ToInt16(full_file, i + 24);
                            pack.quat[1] = BitConverter.ToInt16(full_file, i + 26);
                            pack.quat[2] = BitConverter.ToInt16(full_file, i + 28);
                            pack.quat[3] = BitConverter.ToInt16(full_file, i + 30);
                            pack.bar = BitConverter.ToInt16(full_file, i + 32);
                            pack.temper = BitConverter.ToInt16(full_file, i + 34);
                            pack.snum = full_file[i + 36];
                            pack.crc = full_file[i + 37];
                            pack.frame2 = full_file[i + 38];
                            pack.frame3 = full_file[i + 39];
                            

                            if (pack.snum == 241) // right
                                read_queue[0].Enqueue(pack);
                            else if (pack.snum == 225) // left
                                read_queue[1].Enqueue(pack);
                            else if (pack.snum == 1) // can
                                read_queue[2].Enqueue(pack);
                            else
                                read_queue[3].Enqueue(pack);
                        }
                    }

                }
            }
        }

        private void fileButton_Click(object sender, EventArgs e)
        {
            saveFileDialog.InitialDirectory = "";
            saveFileDialog.Filter = "Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Выберите папку и имя файлов для сохранения данных";
            saveFileDialog.AddExtension = false;
            DialogResult result = saveFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                
            }
            else
            {
                saveFileDialog.FileName = "";
                MessageBox.Show("Неудалось выбрать файл");
            }
            check_read_available();
            
        }

        private double[] get_accl_coefs(int index)
        {
            double[] result = new double[0];
            switch (index)
            {
                case 1:
                    double[] temp1 = { -0.0081, 0.0401, -0.0089, 0.0301,
                                        -0.0111, 0.0323, 0.0093, 0.0104, 0.0085, -0.0921, -0.1201, -0.1454 };
                    result = temp1;
                    break;
                case 2:
                    double[] temp2 = {0.043713810744819,   0.032204099593533,   0.014469608704782,   0.067770825077312,
                       0.124497968267554,  -0.069373064241876,  -0.020960499722065,  -0.119156909250651,
                       0.044421201741530,  -0.179463499557114,  -0.234503197266493,  -0.036294503190156 };
                    result = temp2;
                    break;
                case 3:
                    double[] temp3 = {0.024167075678158,   0.017921002182728,   0.027208980951327,   0.047646758555060,
                      -0.052181643992751,  -0.056215499304436,  -0.011304687635132,   0.087617101877347,
                       0.051573793476529,  -0.045096522590058,  -0.034826544109968,  -0.215775421842763 };
                    result = temp3;
                    break;
                case 4:
                    double[] temp4 = {0.114432559973540,   0.106838802436507,   0.051899182252855,   0.042431602755402,
                      -0.208355094982050,  -0.058741377932612,  -0.017284017902782,   0.109414756776846,
                       0.041872917797213,  -0.207903904900104,   0.069870156128220,  -0.420879615848955 };
                    result = temp4;
                    break;
                case 5:
                    double[] temp5 = {-0.089083516880583,  -0.212101554759966,  -0.074106774400082,   0.140475177135465,
                       0.050748404792032,   0.035466506409495,   0.135105447115893,   0.005040120014690,
                       0.034685336520958,  -0.105165286994008,  -0.049062087663207,   0.090272091944523 };
                    result = temp5;
                    break;
                case 6:
                    double[] temp6 = {-0.067103542084971,   0.079590227298170,   0.070742925683350,  -0.041636392993342,
                       0.220644176913664,  -0.170330066249381,   0.073407186641070,  -0.024528044153757,
                      -0.047531378473926,   0.078357526154640,  -0.155837767265987,  -0.238145173236917 };
                    result = temp6;
                    break;
                case 7:
                    double[] temp7 = {0.045134621164588,   0.068462421832649,   0.017700087773030,  -0.225994306022918,
                       0.088191771774388,  -0.179589945451456,   0.025986312625294,  -0.040027912330171,
                      -0.046415300400838,  -0.158947267757270,  -0.067242068899347,  -0.124325353336784 };
                    result = temp7;
                    break;
                case 8:
                    double[] temp8 = {0.104118389703866,   0.084958239636849,   0.032826888550085,  -0.140404308281075,
                      -0.017921707825125,   0.061748807252006,  -0.006715952930192,   0.012639691500341,
                       0.046309410792156,  -0.218542898428085,  -0.003731519086623,  -0.319272823668150 };
                    result = temp8;
                    break;
                case 9:
                    double[] temp9 = {0.083867364580635,   0.080541880516270,   0.112368594320036,   0.122244712046047,
                      -0.047981420650493,  -0.184024814423127,  -0.097675623527326,  -0.054026830167556,
                       0.174687241854919,  -0.252086667315104,   0.043187133234385,  -0.178080825685559 };
                    result = temp9;
                    break;
                case 10:
                    double[] temp10 = {-0.076212684578814,   0.036874608262696,  -0.006395329952186,  -0.048533020632689,
                       0.031596659443431,   0.080190089955160,   0.065893547405957,  -0.066878035481739,
                      -0.029911464522049,  -0.031068180680396,  -0.065093789262652,  -0.172359413648919 };
                    result = temp10;
                    break;
                case 11:
                    double[] temp11 = {0.019329839277928,   0.049200059518738,   0.044353143292047,   0.058542188048022,
                        0.062731113213260,  -0.028231153395039,  -0.121639863668443,  -0.066633118358829,
                        0.133914377849888,  -0.142745425008951,  -0.028743819809420,  -0.321475954225317 };
                    result = temp11;
                    break;
                case 12:
                    double[] temp12 = {-0.036279717606262,   0.053297786065231,   0.004975622946153,   0.006037301787334,
                       0.076916729330439,  -0.039059863553166,   0.022282449734369,  -0.072682985769156,
                       0.021034870244154,  -0.031357531783622,   0.048738371353602,  -0.305991758949551 };
                    result = temp12;
                    break;
                case 13:
                    double[] temp13 = {0.022459687724866,  -0.062077105256798,   0.046795345083684,  -0.281967510046409,
                      -0.111189762502934,   0.153912645890540,   0.113497567735599,  -0.236258801745200,
                       0.107476182651660,  -0.460519774903838,   0.115898799892106,  -0.013940296751586 };
                    result = temp13;
                    break;
                case 14:
                    double[] temp14 = {0.030554866588497,   0.041807742536512,   0.014637539550826,   0.070444712524782,
                      -0.045520819149987,  -0.053792609796159,   0.067583771817011,   0.044201999575329,
                      -0.068058903538097,  -0.133268083186438,  -0.157601990534356,  -0.125072062660633 };
                    result = temp14;
                    break;
                case 15:
                    double[] temp15 = {-0.091419543228102,   0.013318237063326,   0.015108665155053,  -0.045927518891181,
                       0.138517055483565,   0.115478666476207,   0.028512863952903,   0.154290791103919,
                      -0.019524419888084,  -0.171930546561895,  -0.301610286853745,   0.050182040765164 };
                    result = temp15;
                    break;
                default:
                    result = new double[12];
                    break;
            }

            return result;
        }

        private double[] get_magn_coefs(int index)
        {
            double[] result = new double[0];
            switch (index)
            {
                case 1:
                    double[] temp1 = {-1.3000, -1.3499, -1.2180, -0.5191, 1.2598, 0.8968,
                                         1.1259, -0.8541, -1.4552, -0.1676, 0.0201, 0.1032 };
                    result = temp1;
                    break;
                case 2:
                    double[] temp2 = {-1.021584893111037,  -1.156822840186938,  -1.059527431706735,   0.405595881908856,
                       0.954589891425432,  -0.069541017965736,   0.991639731762399,  -1.012866646546024,
                      -0.886632983175849,  -0.065839849453693,   0.059178141113860,   0.229560149816208 };
                    result = temp2;
                    break;
                case 3:
                    double[] temp3 = {0.238481805936342,  -0.611518102386768,  -0.521946604623734,  -0.152499267950600,
                       0.380041463141051,  -0.089921812966795,  -0.029707070918285,   0.777735008725552,
                      -0.263038218323014,   1.102625052444242,  -0.174805428028501,   0.764500679478734 };
                    result = temp3;
                    break;
                case 4:
                    double[] temp4 = {-0.912310601920176,  -0.035122859804258,  -0.980824053980284,  -0.900924288276854,
                      -0.392141063030012,  -0.212147078216013,   0.370184540925128,  -0.138611784797257,
                       0.592181216595855,  -0.716249303213732,   0.743440070172089,   0.417833919696198 };
                    result = temp4;
                    break;
                case 5:
                    double[] temp5 = {-0.810295610308645,  -0.669847770840476,   0.029900885597606,  -0.314087876668212,
                        -0.489991754352551,  -0.196143690771996,   0.785352181879690,  -0.148859193029027,
                        0.475841206204676,  -0.674462559992835,   0.797605908162643,   1.039652155282594 };
                    result = temp5;
                    break;
                case 6:
                    double[] temp6 = {-0.170985488990754,  -0.564445824510332,  -0.156834833150726,  -0.308524525175852,
                       0.442976518336103,  -0.358382840398978,  -0.080578225278669,   0.617372103949350,
                      -0.541560455196209,   0.718141250811105,  -0.546677511918727,   1.013917065538093 };
                    result = temp6;
                    break;
                case 7:
                    double[] temp7 = {0.240929547813333,  -0.055331307635026,   0.080962491207260,   0.111673173134500,
                      -0.437371531722260,   0.086278316536676,  -0.156009284276460,  -0.450383773465459,
                      -0.232586543173110,   1.800622790457513,   0.479391322929313,  -1.645807763696289 };
                    result = temp7;
                    break;
                case 8:
                    double[] temp8 = {-0.657211729785750,  -0.751518105983042,  -0.502998987070861,   0.346979570434948,
                      -0.626698848495405,   0.854645387115907,  -0.722084569035857,  -0.328055065021069,
                      -0.229782299399703,  -0.357543764086528,  -0.658127285343566,   0.346576778962207 };
                    result = temp8;
                    break;
                case 9:
                    double[] temp9 = {-0.626836215836308,  -0.013759850146307,  -0.583068885503070,   0.859006753692338,
                      -0.291104097898408,   0.359496708414707,  -0.377515731153946,  -0.003959749268316,
                      -0.269023628503547,  -0.839470996073222,  -0.734894289704346,   0.173586855759935 };
                    result = temp9;
                    break;
                case 10:
                    double[] temp10 = {-0.369311287771329,  -0.619181805694075,   0.215860213252399,  -0.202174116935395,
                      -0.469592802232751,  -0.047691482924571,   0.275440905803689,  -0.360029056603724,
                       0.184994296407090,  -0.770865355323793,   0.374651486398312,   1.142034879854652 };
                    result = temp10;
                    break;
                case 11:
                    double[] temp11 = {0.608714881216204,   0.726988256983956,   0.548780040935389,   0.016242466383838,
                       0.008309177330682,  -0.358605622379460,  -0.406898271841329,  -0.176070107760827,
                       0.023260260319988,  -0.615605787372553,                   0,  -0.736861602878822 };
                    result = temp11;
                    break;
                case 12:
                    double[] temp12 = {0.174793461009216,  -0.040792970147182,   0.416590023305863,   0.023815283418015,
                      -0.400898824923381,  -0.056977093394991,  -0.137939234650323,  -0.275073547098045,
                      -0.226813306028076,   1.386816689877866,   0.503695987968843,  -1.311523063280292 };
                    result = temp12;
                    break;
                case 13:
                    double[] temp13 = {-0.363402668053064,  -1.094360523138415,  -0.337177750046436,  -0.150554901049520,
                       0.760814281494252,  -0.527695809751047,  -0.366683816407952,   0.694017973652759,
                      -0.257690691379724,   0.940590498718544,  -0.603082996186667,   0.915339225862064 };
                    result = temp13;
                    break;
                case 14:
                    double[] temp14 = {0.239432068058017,  -0.725844731558349,  -1.151204001242439,   0.549384999741005,
                       0.017479681993509,   0.446775656927678,  -0.105826078256205,  -0.233477063739925,
                      -0.014288927249738,   1.072218089476175,   0.355024708548398,  -0.194902780695747 };
                    result = temp14;
                    break;
                case 15:
                    double[] temp15 = {0.106974544824006,  -0.921067620149456,  -0.544783980541099,  -0.113463751891383,
                       0.575096563967160,  -0.215872226257520,  -0.106232440232434,   0.665018879611869,
                      -0.120415653314238,   0.975928019145445,  -0.232693130860269,   0.505738827768481 };
                    result = temp15;
                    break;
                default:
                    result = new double[12];
                    break;
            }
            return result;
        }

        private double[] single_correction(double[] coefs, double xdata, double ydata, double zdata)
        {
            double[] result = new double[3];
            Matrix B = new DiagonalMatrix(3, 3, 1);
            Matrix A = new DenseMatrix(3, 3);
            A.At(0, 0, coefs[0]);
            A.At(0, 1, coefs[3]);
            A.At(0, 2, coefs[4]);
            A.At(1, 0, coefs[5]);
            A.At(1, 1, coefs[1]);
            A.At(1, 2, coefs[6]);
            A.At(2, 0, coefs[7]);
            A.At(2, 1, coefs[8]);
            A.At(2, 2, coefs[2]);
            Matrix B1 = Kalman_class.Matrix_Minus(B, A);
            Matrix C = new DenseMatrix(3, 1);
            C.At(0, 0, xdata);
            C.At(1, 0, ydata);
            C.At(2, 0, zdata);
            Matrix D = new DenseMatrix(3, 1);
            D.At(0, 0, coefs[9]);
            D.At(1, 0, coefs[10]);
            D.At(2, 0, coefs[11]);
            Matrix res = new DenseMatrix(3, 1);
            res = Kalman_class.Matrix_Mult(B1, Kalman_class.Matrix_Minus(C, D));
            result[0] = res.At(0, 0);
            result[1] = res.At(1, 0);
            result[2] = res.At(2, 0);
            return result;
        }


    }

    public struct packet
    {
        public byte frame1;
        public byte type;
        public uint ticks;
        public short[] a;
        public short[] w;
        public short[] m;
        public short[] quat;
        public short bar;
        public short temper;
        public byte snum;
        public byte crc;
        public byte frame2;
        public byte frame3;
    }
}

//typedef __packed struct
//{
//uint8_t frame1; // 0
//uint8_t type; // 1
//uint32_t ticks; // 2, 3, 4, 5
//int16_t a[3]; // 6,7 8,9 10,11
//int16_t w[3]; // 12,13 14,15 16,17
//int16_t m[3]; // 18,19 20,21, 22,23
//int16_t quat[4]; // 24,25 26,27, 28,29, 30,31
//int16_t bar; //32 33
//int16_t temper; //34 35
//uint8_t snum; //36
//uint8_t crc; // 37
//uint8_t frame2; //38
//uint8_t frame3; //39
//} data_packet_struct_t; /* 40 bytes */

//frame1 = 0x10
//frame2 = 0x10
//frame3 = 0x03
//baudrate = 230400
//type = 0x41
//snum = 0xF1 или 0xE1 (правое левое весло)

//ADXL345
//пакет такой же, данные есть только в ticks, a, type, crc и фрэймы, остальное нули
//type = 0x51
//snum = 0x01
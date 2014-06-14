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
        Queue[] read_queue = new Queue[5]; // five sensors (so far)
        Queue GPS_queue = new Queue(); // five sensors (so far)
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
                active_com[i] = new SerialPort(selected_com[i], 400000, 0, 8, StopBits.One);
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
            if (ports.Length <4)
            {
                MessageBox.Show("Найдено меньше 4 портов.\nПроверьте подключение или переустановите драйвера.");
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
            if ((comBox1.Items.Count < 2)||(saveFileDialog.FileName == ""))
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
                    for (int i = 0; i < 5; i++)
                    {
                        read_queue[i] = new Queue();
                    }
                       
                    for (int i = 0; i < ports_total; i++)
                    {
                        //read_queue[i] = new Queue();
                        
                        //reading[i] = new Thread(thread_read_flexible);
                        reading[i] = new Thread(thread_read_raw);
                        reading[i].Start();
                    }
                    control.Restart();
                    break;
                case "Остановить чтение":
                    stop_reading = true;
                    control.Stop();
                    //read_queue[4] = new Queue();
                    //MessageBox.Show("Чтение завершено.");
                    MessageBox.Show("Количество байт принятых с СОМ портов:\n" + byte2read[0].Count + " " + byte2read[1].Count + " " + byte2read[2].Count +
                        " " + byte2read[3].Count);
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
                    MessageBox.Show("Количество пакетов принятых с датчиков:\n" + sensor_1.Length + " " + sensor_2.Length + " " + sensor_3.Length
                         + " " + sensor_4.Length + " " + sensor_5.Length);
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

                    int glen = GPS_queue.Count;
                    GPS_packet[] pack1 = new GPS_packet[glen];
                    for (int i = 0; i < glen; i++)
                        pack1[i] = (GPS_packet)GPS_queue.Dequeue();
                    if (pack1.Length != 0)
                        write_gps(pack1, 1);
                    
                        //MessageBox.Show("Сохранение завершено.");
                        saveBox.Text = "Сохранение завершено";
                    break;
            }
            

            
                    
        }

        private void write_gps(GPS_packet[] source, int index)
        {
            int length = source.Length;
            saveBox.Text = "Сохранение файла " + index;
            saveBox.Update();
            progressBar.Value = 0;
            progressBar.Maximum = length;
            string additional = "";
            switch (index)
            {
                case 1:
                    additional = "left_oar";
                    break;
                case 2:
                    additional = "right_oar";
                    break;
                case 3:
                    additional = "first_hand";
                    break;
                case 4:
                    additional = "second_hand";
                    break;
                case 5:
                    additional = "seat";
                    break;
            }
            FileStream fs_gps = File.Create(saveFileDialog.FileName + "_" + additional + ".gps", 2048, FileOptions.None);
            BinaryWriter str_gps = new BinaryWriter(fs_gps);
            Int16 buf16; Byte buf8; Int32 buf32;
            Double bufD; Single bufS; UInt32 bufU32;

            
            double[] read_coefs = new double[0];
            double additional_mult = 1;
            for (int i = 0; i < length; i++)
            {
                progressBar.Invoke(new Action(() => progressBar.Value++));
                // GPS
                bufD = (Double)(source[i].lat) / ((180 / Math.PI) * 16.66);
                str_gps.Write(bufD);
                bufD = (Double)(source[i].lon) / ((180 / Math.PI) * 16.66);
                str_gps.Write(bufD);
                bufD = (Double)(0);
                str_gps.Write(bufD);

                bufS = (Single)(source[i].time);
                str_gps.Write(bufS);
                bufS = (Single)(source[i].speed);
                str_gps.Write(bufS);
                bufS = (Single)(0);
                str_gps.Write(bufS);
                str_gps.Write(bufS);

                //bufU32 = (UInt32)(i);
                bufU32 = (UInt32)(source[i].ticks_gps);
                str_gps.Write(bufU32);
                buf8 = (Byte)(0);
                str_gps.Write(buf8);
                str_gps.Write(buf8);
                str_gps.Write(buf8);
            }
            // Запись даты в конец gps файла
            int day = (int)source[length - 1].date / 10000;
            int month = (int)(source[length - 1].date - day * 10000) / 100;
            int year = (int)(2000 + source[length - 1].date - day * 10000 - month * 100);
            string datarec = String.Format("{0:d2}.{1:d2}.{2:d4}", day, month, year);
            str_gps.Write(datarec);
      


            str_gps.Flush();
            str_gps.Close();

        }

 
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
                //if (timer.ElapsedMilliseconds >= 1000)
                //    break;
                if (stop_reading)
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
            string additional = "";
            double[] magn_c = new double[12];
            double[] accl_c = new double[12];
            double[] gyro_c = new double[12];
            double Mk = 1;
            double Wk = 1;
            double Ak = 1;
            switch (index)
            {
                case 1:
                    additional = "left_oar";
                    double[] temp1 = { 0.021012275928952,   0.067860169975568,   0.000127689343889,   0.045296151352541,
                                      -0.009324017077391,   0.026722923577867,   0.008307857594568,   0.005284287915262,
                                      -0.035947192687517,  -0.012542925115207,   0.061512131892943,   0.004409782721854 };

                    accl_c = temp1;
                    Ak = 0.996259867299029;
                    double[] temp11 = { 0.002873025761360,  -0.000277783847693,  -0.008949237789463,   0.024839518300883,
                                        0.018134114269133,   0.004957678597933,   0.059903966032509,  -0.020001702990621,
                                       -0.048455202844697,   0.040007724599001,  -0.002723436449578,   0.028657182612509 };
                    magn_c = temp11;
                    Mk = 1.989928230657113;
                    Wk = 1.024951581925949;
                    break;
                case 2:
                    additional = "right_oar";
                    double[] temp2 = { 0.006489103530672,   0.007711972047671,   0.008910927008256,   0.004526628966860,
                                      -0.030613701020120,   0.009733926042102,   0.000520434141698,  -0.020438431372878,
                                      -0.000690207812167,   0.003527827615877,   0.003268096379523,  -0.009084146831565 };

                    accl_c = temp2;
                    Ak = 0.993917643789002;
                    double[] temp22 = {-0.004338472524034,  -0.007279143920897,  -0.016596561254121,  -0.040100105586272,
                                        0.026447190185438,   0.062723828297715,   0.014235202706375,  -0.032778454891706,
                                       -0.016686216543873,   0.049183804843751,  -0.170661765769371,   0.174784618340082 };
                    magn_c = temp22;
                    Mk = 1.981050375613622;
                    Wk = 0.999473669119779;
                    break;
                case 3:
                    additional = "first_hand";
                    double[] temp3 = { 0.012248789806271,   0.010963952889031,   0.010082940049261,   0.017732310046796,
                                      -0.010807018986018,  -0.017866987099261,  -0.038976713533177,   0.011937965171044,
                                       0.017418474194167,   0.010157002539499,   0.006639710879831,  -0.008997250633174 };

                    accl_c = temp3;
                    Ak = 0.995116429313122;
                    double[] temp33 = {-0.024902579646778,  -0.020478721707093,  -0.026353375666180,  -0.001744353714878,
                                        0.001038721251414,  -0.008552827405651,   0.000013148017514,   0.005334631470956,
                                       -0.004628038571913,  -0.041660570389140,   0.203013174006507,   0.110944181992475 };
                    magn_c = temp33;
                    Mk = 2.000804739331122;
                    Wk = 1.005957834380545;
                    break;
                case 4:
                    additional = "second_hand";
                    double[] temp4 = { 0.009773445955991,   0.010515969102165,   0.046332523300049,  -0.009734857220358,
                                       0.088901575635824,   0.011068909952491,  -0.012572850762404,   0.170713670954205,
                                      -0.008974449587517,   0.001063872406408,  -0.005104328004269,  -0.003338723771119 };
                    accl_c = temp4;
                    Ak = 0.995096553223738;
                    double[] temp44 = { 0.014780841871083,   0.018746310904878,   0.009620181136691,  -0.006116082059700,
                                       -0.000324924134046,  -0.004326570176434,  -0.007611544691470,   0.009652319917883,
                                        0.002813293660957,   0.093081170636918,   0.035125384985654,  -0.014316950547061 };
                    magn_c = temp44;
                    Mk = 2.063752665877927;
                    Wk = 1.013532577606638;
                    break;
                case 5:
                    additional = "seat";
                    double[] temp5 = { 0.018860976044349,   0.012994456079329,  -0.017726498806178,   0.014155888603851,
                                       0.135660705289298,  -0.005517590513633,  -0.006515740524433,   0.089222505526992,
                                       0.007556195473340,  -0.034224056011697,  -0.032625814493799,  -0.091725250270515 };
                    accl_c = temp5;
                    Ak = 0.990392687940513;
                    break;     
            }
            FileStream fs_imu = File.Create(saveFileDialog.FileName + "_" + additional + ".imu", 2048, FileOptions.None);
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
            Kalman_class.State State = new Kalman_class.State(Kalman_class.ACCLERATION_NOISE, Kalman_class.MAGNETIC_FIELD_NOISE, Kalman_class.ANGULAR_VELOCITY_NOISE,
                Math.Pow(10, -6), Math.Pow(10, -15), Math.Pow(10, -15), Initia_quat);

            double[] angles = new double[3];
            double[] mw, ma, mm;
            ma = new double[3];
            mw = new double[3];
            mm = new double[3];
            Tuple<Vector, Kalman_class.Sensors, Kalman_class.State> AHRS_result;



            double[] w_helper = new double[source.Length];
            double[] anglex = new double[source.Length];
            double[] angley = new double[source.Length];
            double[] anglez = new double[source.Length];

            //for (int i = 0; i < w_helper.Length; i++) w_helper[i] = source[i].w[0];
            //anglex = Signal_processing.Zero_average_corr(w_helper, w_helper.Length);
            //for (int i = 0; i < w_helper.Length; i++) w_helper[i] = source[i].w[1];
            //angley = Signal_processing.Zero_average_corr(w_helper, w_helper.Length);
            //for (int i = 0; i < w_helper.Length; i++) w_helper[i] = source[i].w[2];
            //anglez = Signal_processing.Zero_average_corr(w_helper, w_helper.Length);

            double[] read_coefs = new double[0];
            double additional_mult = 1;
            switch (source[0].type)
                {
                    case 0x41:
                        double[] tempc1 = { 0.000833, 0.04 * Math.PI / 180F, 0.00014286, 0.00002, 31, 0.07386, 0, 0 };
                        read_coefs = tempc1;
                        break;
                    case 0x51:
                        double[] tempc2 = { 0.0039, 0, 0, 0, 0, 0, 0, 0 };
                        read_coefs = tempc2;
                        break;
                    case 0x61:
                        double[] tempc3 = { 0.0008, 0.02 * Math.PI / 180F, 0.0001, 0.00004, 25, 0.00565, 0.0055, 0.000030518 };
                        read_coefs = tempc3;
                        additional_mult = -1;
                        break;
                }

            double[] quats = new double[4];
            DenseMatrix quat_m = new DenseMatrix(1, 4);
            Matrix DCM = new DenseMatrix(3, 3);
            Matrix sens = new DenseMatrix(1, 3);
            Matrix res = new DenseMatrix(1, 3);
            double tempr = new double();
            double sqr = new double();
            
            for (int i = 0; i < length; i++)
            {
                progressBar.Value++;
                // умножены на -1 для того чтобы оси соответствовали правой тройке
                // и осям на датчиках
                mw[0] = source[i].w[0] * read_coefs[1]*Wk;// *-1;
                mw[1] = source[i].w[1] * read_coefs[1]*Wk;// * -1;
                mw[2] = source[i].w[2] * read_coefs[1]*Wk;// * -1;
                ma[0] = source[i].a[0] * read_coefs[0]*Ak;// * -1;
                ma[1] = source[i].a[1] * read_coefs[0]*Ak;// * -1;
                ma[2] = source[i].a[2] * read_coefs[0]*Ak;// * -1;
                mm[0] = source[i].m[0] * read_coefs[2]*Mk;// * -1;
                mm[1] = source[i].m[1] * read_coefs[2]*Mk;// * -1;
                mm[2] = source[i].m[2] * read_coefs[2]*Mk;// * -1;

                
                //mw = single_correction(gyro_c, w[i, 0], w[i, 1], w[i, 2]);

                Sensors.a.At(0, 0, ma[0]);
                Sensors.a.At(0, 1, ma[1]);
                Sensors.a.At(0, 2, ma[2]);

                Sensors.w.At(0, 0, mw[0]);
                Sensors.w.At(0, 1, mw[1]);
                Sensors.w.At(0, 2, mw[2]);

                Sensors.m.At(0, 0, mm[0]);
                Sensors.m.At(0, 1, mm[1]);
                Sensors.m.At(0, 2, mm[2]);

                AHRS_result = Kalman_class.AHRS_LKF_EULER(Sensors, State, Parameters);

                State = AHRS_result.Item3;
                mm = single_correction(magn_c, mm[0], mm[1], mm[2]);
                ma = single_correction(accl_c, ma[0], ma[1], ma[2]);
                //------------------------------------------------------------------------
                //mm = single_correction(magn_c, m[i, 0], m[i, 1], m[i, 2]);
                //ma = single_correction(accl_c, a[i, 0], a[i, 1], a[i, 2]);
                //mw = single_correction(gyro_c, w[i, 0], w[i, 1], w[i, 2]);
                //----------------------------------------------------------------------
                
                //----------------------------------------------------------------------
                angles[0] = (AHRS_result.Item1.At(0));
                angles[1] = (AHRS_result.Item1.At(1));
                angles[2] = (AHRS_result.Item1.At(2));
                //angles[0] = (anglez[i]);
                //angles[1] = (angley[i]);
                //angles[2] = (anglex[i]);

                //sqr = Math.Sqrt(mm[0]*mm[0] + mm[1]*mm[1] + mm[2]*mm[2]);
                //mm[0] = mm[0] / sqr;
                //mm[1] = mm[1] / sqr;
                //mm[2] = mm[2] / sqr;
                //sqr = sqr;

                //if (source[i].quat[1] != 0)
                //{
                //    quats[0] = source[i].quat[0] * read_coefs[6];
                //    quats[1] = source[i].quat[1] * read_coefs[7];
                //    quats[2] = source[i].quat[2] * read_coefs[7];
                //    quats[3] = source[i].quat[3] * read_coefs[7];
                //    quat_m.At(0, 0, quats[0]);
                //    quat_m.At(0, 1, quats[1]);
                //    quat_m.At(0, 2, quats[2]);
                //    quat_m.At(0, 3, quats[3]);
                //    DCM = Kalman_class.quat_to_DCM(quat_m);
                //    res = Kalman_class.dcm2angle(DCM);
                //    //angles = quat2angle(quats);
                //    //DCM = Kalman_class.Matrix_Transpose(quat2dcm(quats));
                //    angles[0] = res.At(0, 0);
                //    angles[1] = res.At(0, 1);
                //    angles[2] = res.At(0, 2);
                //    //DMC = Kalman_class.Matrix_Transpose(DMC);
                //    /*sens.At(0, 0, ma[0]);
                //    sens.At(0, 1, ma[1]);
                //    sens.At(0, 2, ma[2]);
                //    res = Kalman_class.Matrix_Mult(sens, DCM);
                //    ma[0] = res.At(0, 0);
                //    ma[1] = res.At(0, 1);
                //    ma[2] = res.At(0, 2);

                //    sens.At(0, 0, mw[0]);
                //    sens.At(0, 1, mw[1]);
                //    sens.At(0, 2, mw[2]);
                //    res = Kalman_class.Matrix_Mult(sens, DCM);
                //    mw[0] = res.At(0, 0);
                //    mw[1] = res.At(0, 1);
                //    mw[2] = res.At(0, 2);

                //    sens.At(0, 0, mm[0]);
                //    sens.At(0, 1, mm[1]);
                //    sens.At(0, 2, mm[2]);
                //    res = Kalman_class.Matrix_Mult(sens, DCM);
                //    mm[0] = res.At(0, 0);
                //    mm[1] = res.At(0, 1);
                //    mm[2] = res.At(0, 2);*/
                //}

                tempr = read_coefs[4] + source[i].temper * read_coefs[5];

                // IMU
                // QQ
                buf16 = (Int16)(angles[0] * 10000);
                str_imu.Write(buf16);
                buf16 = (Int16)(angles[1] * 10000);
                str_imu.Write(buf16);
                buf16 = (Int16)(angles[2] * 10000);
                str_imu.Write(buf16);

                // w
                buf16 = (Int16)(mw[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mw[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mw[2] * 3000);
                str_imu.Write(buf16);

                // a
                buf16 = (Int16)(ma[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(ma[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(ma[2] * 3000);
                str_imu.Write(buf16);
                // m
                buf16 = (Int16)(mm[0] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mm[1] * 3000);
                str_imu.Write(buf16);
                buf16 = (Int16)(mm[2] * 3000);
                str_imu.Write(buf16);

                buf16 = (Int16)(tempr); // t
                str_imu.Write(buf16);

                //buf32 = (Int32)(counter[i]);
                buf32 = (Int32)(source[i].ticks); // pps_imu
                str_imu.Write(buf32);

                buf8 = (Byte)(0);   // check_sum
                str_imu.Write(buf8);
            }
            
            str_imu.Flush();
            str_imu.Close();
            
        }

        private void write_data_raw(int index)
        {
            byte[] full_file = new byte[byte2read[index].Count];
            //byte[] full_file = File.ReadAllBytes("D:\\Projects\\КСК синхронизация\\3 этап\\rec2_3.log");
            string[] name = saveFileDialog.FileName.Split('\\');
            string path = "";
            for (int i = 0; i < name.Length - 1; i++)
                path += name[i] + "\\";
            FileStream fs_log = File.Create(path + "raw_data\\" + name[name.Length - 1] + "_" + index + ".log", 2048, FileOptions.None);
            BinaryWriter str_log = new BinaryWriter(fs_log);
            packet pack = new packet();
            GPS_packet gps_pack = new GPS_packet();
            byte[] pack2 = new byte[26];
            byte[] buffer = new byte[2];
            byte[] buffer2 = new byte[4];
            byte[] temp = new byte[2];
            int crc;
            progressBar.Value = 0;
            byte type_flag = 0;
            bool first_init = true;
            double[] read_coefs = new double[10];
            if (full_file.Length != 0)
            {
                for (int i = 0; i < full_file.Length; i++)
                {
                    full_file[i] = (byte)byte2read[index].Dequeue();

                }
                str_log.Write(full_file);
                str_log.Flush();
                str_log.Close();
                progressBar.Maximum = full_file.Length - 39;
                for (int i = 0; i < full_file.Length - 39; i++)
                {
                    if (i % 50 == 0)
                        progressBar.Value += 49;
                    if ((full_file[i] == 0x10) && (full_file[i + 38] == 0x10) && (full_file[i + 39] == 0x03) &&
                        ((full_file[i + 1] == 0x41) || (full_file[i + 1] == 0x51) || (full_file[i + 1] == 0x61)))
                    {

                        type_flag = full_file[i + 1];
                        
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
                            pack.w[0] = BitConverter.ToInt16(full_file, i + 12);
                            pack.w[1] = BitConverter.ToInt16(full_file, i + 14);
                            pack.w[2] = BitConverter.ToInt16(full_file, i + 16);
                            //temp[0] = full_file[i + 13];
                            //temp[1] = full_file[i + 12];
                            //pack.w[0] = BitConverter.ToInt16(temp, 0);
                            //temp[0] = full_file[i + 15];
                            //temp[1] = full_file[i + 14];
                            //pack.w[1] = BitConverter.ToInt16(temp,0);
                            //temp[0] = full_file[i + 17];
                            //temp[1] = full_file[i + 16];
                            //pack.w[2] = BitConverter.ToInt16(temp, 0);
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
                            

                            switch (type_flag)
                            {
                                case 0x41:
                                    if (pack.snum == (0xF0 ^ 1))
                                        read_queue[0].Enqueue(pack); // left oar
                                    else if (pack.snum == (0xE0 ^ 1))
                                        read_queue[1].Enqueue(pack); // right oar
                                    break;
                                case 0x51:
                                    if (pack.snum == 1)
                                        read_queue[4].Enqueue(pack); // seat
                                    break;
                                case 0x61:
                                    if (pack.snum == (0xF0 ^ 1))
                                        read_queue[2].Enqueue(pack); // first hand
                                    else if (pack.snum == (0xE0 ^ 1))
                                        read_queue[3].Enqueue(pack); // second hand
                                    break;
                                
                            }
                        }
                    }
                    if ((full_file[i + 29] == 3) && (full_file[i + 28] == 16) && (full_file[i] == 16) &&
                    (full_file[i + 1] == 50))   // условие начала GPS пакета
                    {
                        crc = 50;
                        for (int j = 0; j < 26; j++)
                        {
                            pack2[j] = full_file[i + j + 2];
                            if (j < 25)
                                crc = crc ^ pack2[j];
                        }
                        if (crc == pack2[pack2.Length - 1])
                        {
                            gps_pack = new GPS_packet();
                            //ticks2[k2] = pack2[3] + pack2[2] * (int)Math.Pow(2, 8) +
                            //    pack2[1] * (int)Math.Pow(2, 16) + pack2[0] * (int)Math.Pow(2, 24);
                            gps_pack.ticks_gps = BitConverter.ToUInt32(pack2, 0);
                            buffer2[0] = pack2[4]; buffer2[1] = pack2[5]; buffer2[2] = pack2[6]; buffer2[3] = pack2[7];
                            gps_pack.lat = ((double)BitConverter.ToInt32(buffer2, 0)) / 600000;
                            buffer2[0] = pack2[8]; buffer2[1] = pack2[9]; buffer2[2] = pack2[10]; buffer2[3] = pack2[11];
                            gps_pack.lon = ((double)BitConverter.ToInt32(buffer2, 0)) / 600000;
                            buffer[0] = pack2[12]; buffer[1] = pack2[13];
                            gps_pack.speed = (double)BitConverter.ToInt16(buffer, 0) / 100;
                            buffer[0] = pack2[14]; buffer[1] = pack2[15];
                            gps_pack.course = (double)BitConverter.ToInt16(buffer, 0) / 160;
                            buffer2[0] = pack2[16]; buffer2[1] = pack2[17]; buffer2[2] = pack2[18]; buffer2[3] = pack2[19];
                            gps_pack.time = ((double)BitConverter.ToInt32(buffer2, 0)) / 10;
                            gps_pack.stat = pack2[20];
                            buffer2[0] = pack2[21]; buffer2[1] = pack2[22]; buffer2[2] = pack2[23]; buffer2[3] = pack2[24];
                            gps_pack.date = ((double)BitConverter.ToInt32(buffer2, 0));
                            GPS_queue.Enqueue(gps_pack);

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

        private double[] quat2angle(double[] quat)
        {
            double[] result = new double[3];
            //result[0] = Math.Atan2(2 * quat[2] * quat[0] - 2 * quat[1] * quat[3], 1 - 2 * Math.Pow(quat[2], 2) - 2 * Math.Pow(quat[3], 2));
            //result[1] = Math.Atan2(2 * quat[1] * quat[0] - 2 * quat[2] * quat[3], 1 - 2 * Math.Pow(quat[1], 2) - 2 * Math.Pow(quat[3], 2));
            //result[2] = Math.Asin(2 * quat[1] * quat[2] + 2 * quat[0] * quat[3]);
            // w x y z
            result[0] = Math.Atan2(2 * quat[0] * quat[1] + 2 * quat[2] * quat[3], 1 - 2 * Math.Pow(quat[1], 2) - 2 * Math.Pow(quat[2], 2));
            result[2] = Math.Atan2(2 * quat[0] * quat[3] + 2 * quat[1] * quat[2], 1 - 2 * Math.Pow(quat[2], 2) - 2 * Math.Pow(quat[3], 2));
            result[1] = Math.Asin(2 * quat[0] * quat[2] - 2 * quat[1] * quat[3]);
            return result;
        }

        private Matrix quat2dcm(double[] quat)
        {
            Matrix result = new DenseMatrix(3, 3, 0);
            result.At(0, 0, (1 - 2 * (quat[2] * quat[2] + quat[3] * quat[3])));
            result.At(0, 1, 2 * (quat[1] * quat[2] - quat[3] * quat[0]));
            result.At(0, 2, 2 * (quat[1] * quat[3] + quat[2] * quat[0]));
            result.At(1, 0, 2 * (quat[1] * quat[2] + quat[3] * quat[0]));
            result.At(1, 1, 1 - 2 * (quat[1] * quat[1] + quat[3] * quat[3]));
            result.At(1, 2, 2 * (quat[2] * quat[3] - quat[1] * quat[0]));
            result.At(2, 0, 2 * (quat[1] * quat[3] - quat[2] * quat[0]));
            result.At(2, 1, 2 * (quat[2] * quat[3] + quat[1] * quat[0]));
            result.At(2, 2, 1 - 2 * (quat[1] * quat[1] + quat[2] * quat[2]));
            return result;
        }

        private Matrix angle2dcm(double[] angles)
        {
            // z y x
            Matrix result = new DenseMatrix(3, 3, 0);
            double Cos1 = Math.Cos(angles[0]);
            double Cos2 = Math.Cos(angles[1]);
            double Cos3 = Math.Cos(angles[2]);
            double Sin1 = Math.Sin(angles[0]);
            double Sin2 = Math.Sin(angles[1]);
            double Sin3 = Math.Sin(angles[2]);
            result.At(0, 0, Cos2 * Cos1);
            result.At(0, 1, Cos2 * Sin1);
            result.At(0, 2, -Sin2);
            result.At(1, 0, Sin3 * Sin2 * Cos1 - Cos3 * Sin1);
            result.At(1, 1, Sin3 * Sin2 * Sin1 + Cos3 * Cos1);
            result.At(1, 2, Sin3 * Cos2);
            result.At(2, 0, Cos3 * Sin2 * Cos1 + Sin3 * Sin1);
            result.At(2, 1, Cos3 * Sin2 * Sin1 - Sin3 * Cos1);
            result.At(2, 2, Cos3 * Cos2);
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
        // IMU part
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

    public struct GPS_packet
    {
        // GPS part
        public uint ticks_gps;
        public double lat;
        public double lon;
        public double speed;
        public double course;
        public double time;
        public double date;
        public byte stat;
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
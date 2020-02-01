using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using System.Threading;
using System.Net;

namespace CallsUdp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            //создаем поток для записи нашей речи
            input = new WaveIn();
            //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input.WaveFormat = new WaveFormat(8000, 16, 1);
            //добавляем код обработки нашего голоса, поступающего на микрофон
            input.DataAvailable += VoiceInput;
            //создаем поток для прослушивания входящего звука
            output = new WaveOut();
            //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
            bufferStream = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            //привязываем поток входящего звука к буферному потоку
            output.Init(bufferStream);
            //сокет для отправки звука
            client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            connected = true;
            listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //создаем поток для прослушивания
            in_thread = new Thread(new ThreadStart(Listening));
            //запускаем его
            in_thread.Start();
            
            EnableStartCalling();

        }

        //Подключены ли мы
        private bool connected;
        //сокет отправитель
        Socket client;
        //поток для нашей речи
        WaveIn input;
        //поток для речи собеседника
        WaveOut output;
        //буфферный поток для передачи через сеть
        BufferedWaveProvider bufferStream;
        //поток для прослушивания входящих сообщений
        Thread in_thread;
        //сокет для приема (протокол UDP)
        Socket listeningSocket;


        //Обработка нашего голоса
        private void VoiceInput(object sender, WaveInEventArgs e)
        {
            try
            {
                //Подключаемся к удаленному адресу
                IPEndPoint remote_point = new IPEndPoint(IPAddress.Parse(destinationIp.Text), 5555);
                //посылаем байты, полученные с микрофона на удаленный адрес
                client.SendTo(e.Buffer, remote_point);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        //Прослушивание входящих подключений
        private void Listening()
        {
            //Прослушиваем по адресу
            IPEndPoint localIP = new IPEndPoint(GetLocalIpAddress(), 5555);
            listeningSocket.Bind(localIP);
            //начинаем воспроизводить входящий звук
            output.Play();
            //адрес, с которого пришли данные
            EndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
            //бесконечный цикл
            while (connected == true)
            {
                try
                {
                    //промежуточный буфер
                    byte[] data = new byte[65535];
                    //получено данных
                    int received = listeningSocket.ReceiveFrom(data, ref remoteIp);
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    bufferStream.AddSamples(data, 0, received);
                }
                catch (SocketException ex)
                { return; }
            }
        }

        private void AppClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            input.StopRecording();
            connected = false;
            listeningSocket.Close();
            listeningSocket.Dispose();

            client.Close();
            client.Dispose();
            if (output != null)
            {

                output.Stop();
                output.Dispose();
                output = null;
            }
            if (input != null)
            {
                input.Dispose();
                input = null;
            }
            bufferStream = null;
        }

        private void StopCalling()
        {
            input.StopRecording();
            output.Stop();
        }

        private void Start(object sender, RoutedEventArgs e)
        {
                        IPAddress temp;
            if (!IPAddress.TryParse(destinationIp.Text, out temp))
            {
                MessageBox.Show("Введены неверные данные!");
                return;
            }
            DisableStartCalling();
            input.StartRecording();
        }

        private IPAddress GetLocalIpAddress()
        {
            System.Net.IPAddress ip = System.Net.Dns.GetHostByName(System.Net.Dns.GetHostName()).AddressList[1];
            return ip;
        }

        private void Stop(object sender, RoutedEventArgs e)
        {
            StopCalling();
            EnableStartCalling();
        }

        private void EnableStartCalling()
        {
            destinationIp.IsEnabled = true;
            startButton.IsEnabled = true;
            stopButton.IsEnabled = false;
        }

        private void DisableStartCalling()
        {
            destinationIp.IsEnabled = false;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;
        }
    }
}


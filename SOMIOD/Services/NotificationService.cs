using SOMIOD.App.Helpers;
using SOMIOD.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using uPLibrary.Networking.M2Mqtt; // Certifica-te que instalaste esta biblioteca
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SOMIOD.Services
{
    public static class NotificationService
    {
        // -----------------------------------------------------------------
        // MÉTODO PRINCIPAL: Chamado após a criação de um ContentInstance
        // -----------------------------------------------------------------
        public static void FireEvent(string appName, string containerName, ContentInstance data, int currentEvent)
        {
            // O Canal MQTT é a rota para o Container (como diz o enunciado)
            // Exemplo: "api/somiod/lighting/light-bulb"
            string channel = $"api/somiod/{appName.Trim()}/{containerName.Trim()}";
            System.Diagnostics.Debug.WriteLine($">>> TOPICO FINAL: '{channel}'");

            string eventLabel = (currentEvent == 1) ? "creation" : "deletion"; 

            // 1. Procurar Subscrições para este Container
            string query = @"
                 SELECT S.Endpoint, S.Event 
                 FROM Subscription S
                 JOIN Container C ON S.ParentContainerId = C.Id
                 JOIN Application A ON C.ParentAppId = A.Id
                 WHERE A.Name = @AppName AND C.Name = @ContainerName
                 AND S.Event IN (@CurrentEvent, '3')";

            List<SqlParameter> paramsList = new List<SqlParameter>
            {
                new SqlParameter("@AppName", appName),
                new SqlParameter("@ContainerName", containerName),
                new SqlParameter("@CurrentEvent",currentEvent.ToString()) // Filtra por 1 ou 2
            }; 

            var dt = SqlDataHelper.ExecuteQuery(query, paramsList);

            // ADICIONA ISTO:
            System.Diagnostics.Debug.WriteLine($">>> Subscritores encontrados: {dt.Rows.Count}");


            if (dt.Rows.Count == 0) {
                System.Diagnostics.Debug.WriteLine(">>> ERRO: Nenhuma subscrição encontrada na BD para este contentor!");
                return;
            }
              // Não há subscritores. Fim.

            // 2. Se houver subscritores, enviar a notificação
            foreach (System.Data.DataRow row in dt.Rows)
            {
                string endpoint = (string)row["Endpoint"];
                // Deves analisar o endpoint (http:// ou mqtt://) para escolher o método de envio

                if (endpoint.StartsWith("mqtt://"))
                {
                    SendMqttNotification(endpoint, channel, data,eventLabel);
                }
                // else if (endpoint.StartsWith("http://"))
                // {
                //    SendHttpNotification(endpoint, data);
                // }
            }
        }

        // -----------------------------------------------------------------
        // ENVIO MQTT
        // -----------------------------------------------------------------
        private static void SendMqttNotification(string fullEndpoint, string channel, ContentInstance data,String EventLabel)
        {
            try
            {
                // Endpoint: "mqtt://127.0.0.1:1883" -> precisamos de "127.0.0.1" e 1883
                string[] parts = fullEndpoint.Replace("mqtt://", "").Split(':');
                string brokerIp = parts[0];
                int brokerPort = int.Parse(parts[1]);

                // 1. Conectar ao Broker (Mosquitto)
                var client = new MqttClient(brokerIp, brokerPort, false, null, null, MqttSslProtocols.None); 
                client.Connect(Guid.NewGuid().ToString());

                if (client.IsConnected)
                {
                    System.Diagnostics.Debug.WriteLine($">>> LIGADO ao Broker! A publicar no tópico: {channel}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(">>> ERRO: Falha ao ligar ao Broker Mosquitto.");
                }


                if (client.IsConnected)
                {
                    string message = $"{{ \"event\": \"{EventLabel}\", \"resource\": {Newtonsoft.Json.JsonConvert.SerializeObject(data)}, \"content\": \"{data.Content}\" }}";

                    // Publica e guarda o ID da mensagem
                    ushort msgId = client.Publish(
                        channel.Trim(), // Garante que não há espaços no tópico
                        Encoding.UTF8.GetBytes(message),
                        MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE,
                        false);

                    // DÁ UM PEQUENO TEMPO PARA O BROKER RESPONDER (IMPORTANTE)
                    System.Threading.Thread.Sleep(200);

                    client.Disconnect();
                    System.Diagnostics.Debug.WriteLine($">>> Mensagem enviada com ID {msgId} para o tópico {channel}");
                }
            }
            catch (Exception ex)
            {
                // Aqui deverias fazer logging do erro
                System.Diagnostics.Debug.WriteLine($"Erro ao enviar MQTT: {ex.Message}");
            }
        }
    }
}
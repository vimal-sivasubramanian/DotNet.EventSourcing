﻿using DotNet.CleanArchitecture.Core.Interfaces.MessageBrokers;
using DotNet.CleanArchitecture.Core.Models;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNet.CleanArchitecture.MessageBrokers.AzureServiceBus
{
    public class AzureServiceBusReceiver<TKey, TValue> : IMessageReceiver<TKey, TValue>
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly QueueClient _queueClient;

        public AzureServiceBusReceiver(string connectionString, string queueName)
        {
            _connectionString = connectionString;
            _queueName = queueName;
            _queueClient = new QueueClient(_connectionString, _queueName);
        }

        public void Receive(Action<Message<TKey, TValue>> action)
        {
            var messageHandlerOptions = new MessageHandlerOptions(ExceptionReceivedHandler)
            {
                MaxConcurrentCalls = 1,
                AutoComplete = false,
            };

            _queueClient.RegisterMessageHandler((Message message, CancellationToken token) =>
            {
                action(new Message<TKey, TValue>
                {
                    Key = JsonConvert.DeserializeObject<TKey>(message.MessageId),
                    Value = JsonConvert.DeserializeObject<TValue>(Encoding.UTF8.GetString(message.Body))
                });
                return _queueClient.CompleteAsync(message.SystemProperties.LockToken);
            }, messageHandlerOptions);
        }

        private Task ExceptionReceivedHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            Console.WriteLine($"Message handler encountered an exception {exceptionReceivedEventArgs.Exception}.");
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;
            Console.WriteLine("Exception context for troubleshooting:");
            Console.WriteLine($"- Endpoint: {context.Endpoint}");
            Console.WriteLine($"- Entity Path: {context.EntityPath}");
            Console.WriteLine($"- Executing Action: {context.Action}");
            return Task.CompletedTask;
        }
    }
}

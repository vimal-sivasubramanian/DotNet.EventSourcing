﻿using Confluent.Kafka;
using DotNet.EventSourcing.Core.Interfaces.MessageBrokers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNet.EventSourcing.MessageBrokers.Kafka
{
    public class KafkaReceiver<TKey, TValue> : IMessageReceiver<TKey, TValue>, IDisposable
    {
        private readonly IConsumer<TKey, TValue> _consumer;

        public KafkaReceiver(string bootstrapServers, string topic, string groupId)
        {
            var config = new ConsumerConfig
            {
                GroupId = groupId,
                BootstrapServers = bootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            _consumer = new ConsumerBuilder<TKey, TValue>(config)
                .Build();
            _consumer.Subscribe(topic);
        }

        public void Dispose()
        {
            _consumer.Dispose();
        }

        public void Receive(Action<Core.Models.Message<TKey, TValue>> action)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    StartReceiving(action, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Closing consumer.");
                    _consumer.Close();
                }
            });
        }

        private void StartReceiving(Action<Core.Models.Message<TKey, TValue>> action, CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);

                    if (consumeResult.IsPartitionEOF)
                    {
                        continue;
                    }

                    action(new Core.Models.Message<TKey, TValue> { Key = consumeResult.Message.Key, Value = consumeResult.Message.Value });
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Consume error: {e.Error.Reason}");
                }
            }
        }
    }
}

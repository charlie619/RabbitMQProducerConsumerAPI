using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var app = builder.Build();

            app.MapGet("/receive", async () =>
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };

                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new AsyncEventingBasicConsumer(channel);
                var messages = new List<string>();

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    messages.Add(message);
                    Console.WriteLine($"Received: {message}");
                };

                await channel.BasicConsumeAsync(queue: "hello", autoAck: true, consumer: consumer);

                //Allow some time to process messages
                await Task.Delay(1000);

                return Results.Ok(messages);
            });

            app.MapPost("/send", async (string message) =>
            {
                var factory = new ConnectionFactory() { HostName = "localhost" };

                using var connection = await factory.CreateConnectionAsync();
                using var channel = await connection.CreateChannelAsync();

                await channel.QueueDeclareAsync(queue: "hello", durable: false, exclusive: false, autoDelete: false, arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                var props = new BasicProperties();
                props.ContentType = "text/plain";
                props.DeliveryMode = DeliveryModes.Transient;

                await channel.BasicPublishAsync(exchange: "", routingKey: "hello", mandatory: false, basicProperties: props, body: body);

                return Results.Ok($"Sent: {message}");
            });


            app.Run();
        }
    }
}

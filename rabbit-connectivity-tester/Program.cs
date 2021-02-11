using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;

namespace rabbit_connectivity_tester
{
    class Program
    {
        static Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
                new Option<string>(new[] {"--host-name", "-h"}, description: "Rabbit hostname") { IsRequired = true },
                new Option<string>(new[] {"--virtual-host", "-vh"}, description: "Rabbit virtualhost name") { IsRequired = true },
                new Option<string>(new[] {"--user-name", "-u"}, description: "Username to connect to virtual host") { IsRequired = true },
                new Option<string>(new[] {"--password", "-p"}, description: "Password to connect to virtual host") { IsRequired = true }
            };

            rootCommand.Description = "Rabbit Connectivity Checker";

            rootCommand.Handler = CommandHandler.Create<string, string, string, string>((hostName, virtualHost, username, password) => Run(args, hostName, virtualHost, username, password));
            
            return rootCommand.InvokeAsync(args);
        }

        private static int Run(string[] args, string hostName, string virtualHost, string username, string password)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Logging is initialised");
            
            bool TestConnection(bool useTls)
            {
                try
                {
                    if(useTls)
                        logger.Info("Testing secure connection (port 5671)");
                    else
                        logger.Info("Testing insecure connection (port 5672)");
                    
                    logger.Info($"Attempting to connect to amqp{(useTls ? "s" :"")}://{username}:****@{hostName}/{virtualHost}");

                    var connectionFactory = new ConnectionFactory
                    {
                        Uri = new Uri($"amqp{(useTls ? "s" :"")}://{username}:{password}@{hostName}/{virtualHost}")
                    };
                    
                    var connection = connectionFactory.CreateConnection();
                    connection.ConnectionBlocked += (sender, eventArgs) => logger.Warn($"Connection is blocked:{eventArgs.Reason}");
                    connection.CallbackException += (sender, eventArgs) => logger.Warn(eventArgs.Exception, $"Connection Exception:{eventArgs.Exception.Message}");
                    connection.ConnectionUnblocked += (sender, eventArgs) => logger.Info($"Connection is unblocked");
                    connection.ConnectionShutdown += (sender, eventArgs) => logger.Info($"Connection is shutting down:{eventArgs.ReplyText}");
                    
                    logger.Info($"Is connection open:{connection.IsOpen}");

                    logger.Info("Opening channel");
                    var channel = connection.CreateModel();
                    
                    channel.CallbackException += (sender, eventArgs) => logger.Warn(eventArgs.Exception, $"Channel Exception:{eventArgs.Exception.Message}");
                    channel.ModelShutdown += (sender, eventArgs) => logger.Info($"Channel is shutting down:{eventArgs.ReplyText}");
                    
                    logger.Info($"Is Channel Open:{channel.IsOpen}");
                    
                    logger.Info("Closing channel");
                    channel.Close();
                    
                    logger.Info("Closing connection");
                    connection.Close();
                }
                catch (Exception ex)
                {
                    // NLog: catch any exception and log it.
                    logger.Error(ex, "Test failed because of exception", ex);
                    return false;
                }

                return true;
            }
            

            var result = TestConnection(true);
            if (!result)
            {
                logger.Info("Secure connection failed .. testing insecure connection (port 15671)");
                result = TestConnection(false);
            }
            
            LogManager.Shutdown();

            return result ? 0 : 1;
        }
    }
}
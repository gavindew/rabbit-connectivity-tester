using System;
using System.CommandLine;
using System.Threading.Tasks;
using NLog;
using RabbitMQ.Client;

namespace rabbit_connectivity_tester;

public static class Program
{
    private static Task<int> Main(string[] args)
    {
        var hostOption = new Option<string>(new[] { "--hostname", "-hn" }, description: "Rabbit hostname") { IsRequired = true };
        var virtualHostOption = new Option<string>(new[] { "--virtualhost", "-vh" }, description: "Rabbit virtualhost name") { IsRequired = true };
        var usernameOption = new Option<string>(new[] { "--username", "-u" }, description: "Username to connect to virtual host") { IsRequired = true };
        var passwordOption = new Option<string>(new[] { "--password", "-p" }, description: "Password to connect to virtual host") { IsRequired = true }; 
        
        var rootCommand = new RootCommand("Rabbit Connectivity Checker");
        rootCommand.AddOption(hostOption);
        rootCommand.AddOption(virtualHostOption);
        rootCommand.AddOption(usernameOption);
        rootCommand.AddOption(passwordOption);

        rootCommand.SetHandler(Run, hostOption, virtualHostOption, usernameOption, passwordOption);
            
        return rootCommand.InvokeAsync(args);
    }

    private static Task<int> Run(string hostName, string virtualHost, string username, string password)
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

        return Task.FromResult(result ? 0 : 1);
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TennisBookings.ScoreProcessor.Logging;
using TennisBookings.ScoreProcessor.Processing;
using TennisBookings.ScoreProcessor.Sqs;

namespace TennisBookings.ScoreProcessor.BackgroundServices
{
    public class ScoreProcessingService : BackgroundService
    {
        private readonly ILogger<ScoreProcessingService> _logger;
        private readonly ISqsMessageChannel _sqsMessageChannel;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;

        public ScoreProcessingService(
            ILogger<ScoreProcessingService> logger,
            ISqsMessageChannel sqsMessageChannel,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime hostApplicationLifetime
           )
        {
            _logger = logger;
            _sqsMessageChannel = sqsMessageChannel;
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Started score processing service.");

            // Delegado que se llamará cuando el token de cancelación sea cancelado
            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Ending score processing service.");
            });

            try
            {
                await foreach (var message in _sqsMessageChannel.Reader.ReadAllAsync(stoppingToken))
                {        
                    _logger.LogInformation("Read message to process from channel.");

                    using var scope = _serviceProvider.CreateScope();

                    var scoreProcessor = scope.ServiceProvider.GetRequiredService<IScoreProcessor>();

                    await scoreProcessor.ProcessScoresFromMessageAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Swallow this since we expect this to occur when shutdown has been singaled
                _logger.OperationCancelledExceptionOccurred();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception was thrown. Triggering application shut down");
            }
            finally
            {
                // Al llegar aquí, puede que la aplicación ya esté parando por ejemplo en caso de OperationCanceledException.
                // No pasa nada por llamar a StopApplication varias veces, sólo la primera tiene efecto
                // Cuando la aplicación se cierra de forma correcta, el token de cancelación pasado a cada método ExecuteAsync de los BackgroundService se marcará como cancelado
                _hostApplicationLifetime.StopApplication();
            }
        }
    }
}

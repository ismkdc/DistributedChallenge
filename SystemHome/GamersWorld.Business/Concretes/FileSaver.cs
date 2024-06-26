using GamersWorld.Business.Contracts;
using GamersWorld.Common.Enums;
using GamersWorld.Common.Requests;
using GamersWorld.Common.Responses;
using GamersWorld.Events;
using GamersWorld.MQ;
using Microsoft.Extensions.Logging;

namespace GamersWorld.Business.Concretes;

public class FileSaver(ILogger<FileSaver> logger, IEventQueueService eventQueueService)
    : IDocumentWriter
{
    private readonly ILogger<FileSaver> _logger = logger;
    private readonly IEventQueueService _eventQueueService = eventQueueService;

    public async Task<BusinessResponse> SaveTo(DocumentSaveRequest payload)
    {
        if (payload == null || payload.Content == null)
        {
            _logger.LogError("Paylod or content is null");
            return new BusinessResponse
            {
                StatusCode = StatusCode.Fail,
                Message = "Payload or content is null"
            };
        }

        //QUESTION : Diyelimki dosyanın yazıldığı disk dolu veya arızalandı. Sistem nasıl tepki vermeli?
        try
        {
            await File.WriteAllBytesAsync(
                        Path.Combine(Environment.CurrentDirectory, $"{payload.DocumentId}.csv")
                        , payload.Content);


            var reportIsHereEvent = new ReportIsHereEvent
            {
                Time = DateTime.Now,
                TraceId = payload.TraceId,
                CreatedReportId = payload.DocumentId,
            };
            _eventQueueService.PublishEvent(reportIsHereEvent);

            return new BusinessResponse
            {
                StatusCode = StatusCode.DocumentSaved,
                Message = $"{payload.Content.Length} bytes saved."
            };
        }
        catch (Exception excp)
        {
            //QUESTION: Exception söz konusu ise, TraceId'ye sahip olaylar silsilesinin akibeti ne olacak?
            _logger.LogError(excp, "Error on document saving!");
            return new BusinessResponse
            {
                StatusCode = StatusCode.Fail,
                Message = $"Exception. {excp.Message}"
            };
        }
    }
}
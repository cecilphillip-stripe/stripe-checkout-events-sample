using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using CloudNative.CloudEvents.Core;
using CloudNative.CloudEvents.SystemTextJson;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace StripeCheckoutEvents.Serverless;

public class Function
{
    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
    }

    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            context.Logger.LogInformation($"Processing message => {message.MessageId}");
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"SQS Message body => {message.Body}");
       
        if (message.MessageAttributes.ContainsKey("contentType"))
        {
             var contentType = message.MessageAttributes["contentType"].StringValue;
             context.Logger.LogInformation($"SQS Message Data contentType => {contentType}");
             if (MimeUtilities.IsCloudEventsContentType(contentType))
             {
                 using var jDoc = JsonDocument.Parse(message.Body);
                 var cloudEventFormatter = new JsonEventFormatter<QueueMessagePayload>();
                 var cloudEvent = cloudEventFormatter.ConvertFromJsonElement(jDoc.RootElement, null);
                 var cloudEventData = cloudEvent.Data as QueueMessagePayload;
                 context.Logger.LogInformation($"Parsed Cloud event => {cloudEvent.Id}");
                 context.Logger.LogInformation(
                     $"Initiating fulfillment workflow for checkout session  => {cloudEventData.CheckoutSessionID}");
             }
        }
        
        await Task.CompletedTask;
        context.Logger.LogInformation($"Processed message => {message.MessageId}");
    }
}

public record QueueMessagePayload(string CheckoutSessionID, string Status);
using System;
using System.Activities;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Web;
using AdamSaplingWorkflows.Models;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Workflow;

// ReSharper disable UnusedMember.Global

namespace AdamSaplingWorkflows.Workflows
{
    public class TimeZone : CodeActivity
    {
        // sets up input arguments
        #region input arguments

        [RequiredArgument]
        [Input(nameof(InputSchoolAddressStreet))]
        public InArgument<string> InputSchoolAddressStreet { get; set; }

        [RequiredArgument]
        [Input(nameof(InputSchoolAddressCity))]
        public InArgument<string> InputSchoolAddressCity { get; set; }

        [RequiredArgument]
        [Input(nameof(InputSchoolAddressState))]
        public InArgument<string> InputSchoolAddressState { get; set; }

        [RequiredArgument]
        [Input(nameof(InputSchoolAddressZip))]
        public InArgument<string> InputSchoolAddressZip { get; set; }

        #endregion
        // sets up output arguments
        #region output arguments

        [Output(nameof(TimeZoneName))]
        public OutArgument<string> TimeZoneName { get; set; }

        #endregion

        // internal variables
        private ITracingService _tracingService;
        private const string MissingTimeZoneValue = "Time zone not found based on provided address";

        protected override void Execute(CodeActivityContext context)
        {
            // initiates tracing service
            _tracingService = context.GetExtension<ITracingService>();

            TimeZoneName.Set(context, MissingTimeZoneValue);

            // sets time zone

            TimeZoneName.Set(context,
                GetTimeZone(InputSchoolAddressStreet.Get<string>(context), InputSchoolAddressCity.Get<string>(context),
                    InputSchoolAddressState.Get<string>(context), InputSchoolAddressZip.Get<string>(context)));
        }

        public string GetTimeZone(string inputSchoolAddressStreet = null,
            string inputSchoolAddressCity = null, string inputSchoolAddressState = null,
            string inputSchoolAddressZip = null)
        {
            // builds URI to be passed to the HTTP client
            var uriBuilder = new UriBuilder(@"https://dev.virtualearth.net/REST/v1/TimeZone/");
            var parsedQuery = HttpUtility.ParseQueryString(uriBuilder.Query);
            parsedQuery["key"] = "[your bing API key]";
            parsedQuery["query"] =
                $@"{inputSchoolAddressStreet} {inputSchoolAddressCity} {inputSchoolAddressState} {inputSchoolAddressZip}";
            uriBuilder.Query = parsedQuery.ToString();

            try
            {
                using (var deSerializeMemoryStream = new MemoryStream())
                {
                    //JSON string that we get from web api 
                    var responseString = new HttpClient().GetStringAsync(uriBuilder.Uri).Result;

                    //initialize DataContractJsonSerializer object and pass TimeZoneModel class type to it
                    var serializer = new DataContractJsonSerializer(typeof(TimeZoneModel));
                    
                    //user stream writer to write JSON string data to memory stream
                    var writer = new StreamWriter(deSerializeMemoryStream);
                    writer.Write(responseString);
                    writer.Flush();
                    deSerializeMemoryStream.Position = 0;

                    //get the Deserialized data in object of type TimeZoneModel
                    var serializedObject = serializer.ReadObject(deSerializeMemoryStream) as TimeZoneModel;

                    return serializedObject?.ResourceSets.FirstOrDefault() is ResourceSet resourceSet &&
                           resourceSet.Resources.FirstOrDefault() is Resource resource &&
                           resource.TimeZoneAtLocation.FirstOrDefault() is TimeZoneAtLocation timeZoneAtLocation &&
                           timeZoneAtLocation.TimeZone.FirstOrDefault() is Timezone timeZoneElement &&
                           timeZoneElement.GenericName is string genericName
                        ? genericName
                        : MissingTimeZoneValue;
                }
            }
            catch (Exception exception)
            {
                // if an exception is thrown, log the exception and then return the default value for missing
                var exceptionText = $"An exception occurred: {exception.Message} ({exception.GetType()}) (uri = {uriBuilder.Uri})";
                Console.WriteLine(exceptionText);
                _tracingService?.Trace(exceptionText);
                return MissingTimeZoneValue;
            }
        }
    }
}

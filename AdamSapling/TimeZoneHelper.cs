using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace AdamSapling
{
    public class TimeZoneHelper : IPlugin
    {
        private readonly Guid _adminUserId;

        public TimeZoneHelper()
        {
            //if (Guid.TryParse(adminUserId, out var result))
            //{
            //    _adminUserId = result;
            //}
            //else
            //{
            //    throw new InvalidPluginExecutionException("The admin user is missing");
            //}
        }

        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(typeof(IPluginExecutionContext)) is IPluginExecutionContext context &&
                serviceProvider.GetService(typeof(IOrganizationServiceFactory)) is IOrganizationServiceFactory serviceFactory &&
                serviceProvider.GetService(typeof(ITracingService)) is ITracingService tracing &&
                serviceFactory.CreateOrganizationService(context.UserId) is IOrganizationService service &&
                serviceFactory?.CreateOrganizationService(_adminUserId) is IOrganizationService adminService &&
                context.PrimaryEntityName == "adamsap_school")
            {
                //var schoolAddressEntity = GetAddressInfo(adminService, context.PrimaryEntityId.ToString());
                //var restSetting = GetRestInfo(adminService, "BingGetPoint");

                //if (restSetting == null)
                //{
                //    throw new InvalidPluginExecutionException("REST API Data not present");
                //}

                //var apiData = RestApiCall(restSetting, tracing);
                var apiData = RestApiCall(null, tracing);

                //EnterNoteRecord(service, "User service", apiData, regarding, tracing);
                //EnterNoteRecord(adminService, "Admin Service", apiData, regarding, tracing);
            }
        }

        private static Entity GetAddressInfo(IOrganizationService adminService, string id)
        {
            var restInfo = new Entity();

            var query = new QueryExpression()
            {
                Distinct = true,
                EntityName = "adamsap_school",
                ColumnSet = new ColumnSet("adamsap_schooladdressstreet", "adamsap_schooladdresscity",
                    "adamsap_schoolstate", "adamsap_schooladdresszip"),
                Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                            FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression("adamsap_schoolid", ConditionOperator.Equal, id )
                            }
                        }
                    }
                }
            };

            var results = adminService.RetrieveMultiple(query);

            if (results.Entities.Count > 0)
            {
                restInfo = results.Entities[0];
            }

            return restInfo;
        }

        private static Entity GetRestInfo(IOrganizationService adminService, string serviceName)
        {
            var restInfo = new Entity();

            var query = new QueryExpression
            {
                Distinct = true,
                EntityName = "adamsap_envvarconfiguration",
                ColumnSet = new ColumnSet("adamsap_host", "adamsap_url", "adamsap_key"),
                Criteria =
                {
                    Filters =
                    {
                        new FilterExpression
                        {
                            FilterOperator = LogicalOperator.And,
                            Conditions =
                            {
                                new ConditionExpression("adamsap_host", ConditionOperator.Equal, serviceName )
                            }
                        }
                    }
                }
            };

            var results = adminService.RetrieveMultiple(query);

            if (results.Entities.Count > 0)
            {
                restInfo = results.Entities[0];
            }

            return restInfo;
        }

        private static string RestApiCall(Entity restApiInfo, ITracingService tracing)
        {
            string result;

            const string key = "AnKkRcs9vKmajbA4kfwIXwWi0BuhmUfye-zTdxmX8IPVy1ZaNplbaXrRzjL1YNgc";
            const string query = "1 Microsoft Way, Redmond, WA";

            var url = $"http://dev.virtualearth.net/REST/v1/Locations?q={query}";

            var request = (HttpWebRequest)WebRequest.Create(url);

            request.ContentType = "application/json";
            request.Method = "POST";
            request.Accept = "application/json";
            request.Headers.Add("key", key);
            request.ContentType = "text/plain";
            request.PreAuthenticate = true;

            //await Task.Delay(0);

            try
            {
                using (var responseReader = new StreamReader(request.GetResponse().GetResponseStream() ??
                                                             throw new InvalidOperationException(
                                                                 "Unable to get REST response stream")))
                {
                    result = responseReader.ReadToEnd();
                    tracing.Trace($"REST API results: {result}");
                }
            }
            catch (WebException we)
            {
                var allomentNumber = string.Empty;
                var streamData = new StringBuilder();

                streamData.AppendLine("---HTTP Error---");

                WebResponse webResponse = (HttpWebResponse)we.Response;

                var stream = webResponse.GetResponseStream();
                var encode = Encoding.GetEncoding("utf-8");
                var readStream = new StreamReader(stream ?? throw new InvalidOperationException(), encode);
                var read = new char[256];

                var count = readStream.Read(read, 0, 256);

                while (count > 0)
                {
                    // Dump the 256 characters on a string and display the string onto the console.
                    var str = new string(read, 0, count);
                    streamData.Append(str);
                    count = readStream.Read(read, 0, 256);
                }

                stream.Close();
                webResponse.Close();

                result = streamData.ToString();

            }
            return result;
        }

        private void EnterNoteRecord(IOrganizationService service, string serviceType, string noteText,
            EntityReference objectId, ITracingService tracing)
        {
            var note = new Entity("annotation");
            note.Attributes.Add("subject", "The service " + serviceType);
            note.Attributes.Add("notetext", noteText);
            note.Attributes.Add("objectid", objectId);

            var recordId = service.Create(note);
            tracing.Trace($"New note ID is {recordId.ToString()}");
        }
    }
}

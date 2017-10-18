﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Rabbit.Cloud.Client;
using Rabbit.Cloud.Client.Abstractions;
using Rabbit.Cloud.Client.Abstractions.Extensions;
using Rabbit.Cloud.Client.Http;
using Rabbit.Cloud.Client.Middlewares;
using Rabbit.Cloud.Cluster;
using Rabbit.Cloud.Extensions.Consul;
using Rabbit.Extensions.Configuration;
using Rabbit.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp
{
    /*    public class UserMode
        {
            public string Name { get; set; }
            public ushort Age { get; set; }
        }

        public class CustomFilterAttribute : Attribute, IRequestFilter, IResultFilter, IExceptionFilter
        {
            #region Implementation of IRequestFilter

            public void OnRequestExecuting(RequestExecutingContext context)
            {
                Console.WriteLine("OnRequestExecuting");
            }

            public void OnRequestExecuted(RequestExecutedContext context)
            {
                Console.WriteLine("OnRequestExecuted");
            }

            #endregion Implementation of IRequestFilter

            #region Implementation of IResultFilter

            public void OnResultExecuting(ResultExecutingContext context)
            {
                Console.WriteLine("OnResultExecuting");
            }

            public void OnResultExecuted(ResultExecutedContext context)
            {
                Console.WriteLine("OnResultExecuted");
            }

            #endregion Implementation of IResultFilter

            #region Implementation of IExceptionFilter

            public void OnException(ExceptionContext context)
            {
                Console.WriteLine("OnException");
            }

            #endregion Implementation of IExceptionFilter
        }

        [FacadeClient("userService")]
        [ToHeader("interface", "IUserService"), ToHeader("service", "userService"), ToHeader("rabbit.chooser", "RoundRobin")]
        public interface IUserService
        {
            [RequestMapping("api/User/{id}")]
            [CustomFilter]
            [ToHeader("method", "GetUserAsync"), ToHeader("returnType", "UserMode")]
            Task<UserMode> GetUserAsync(long id, [ToHeader]string version = "1.0.0");

            [RequestMapping("api/User/{id}", "PUT")]
            Task<object> PutUserAsync(long id, UserMode model);
        }*/

    public class Program
    {
        private static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .AddCommandLine(args)
                .Build()
                .EnableTemplateSupport();

            var services = new ServiceCollection()
                .AddLogging()
                .AddRabbitCloudCore()
                .AddConsulDiscovery(configuration)
                .AddServiceInstanceChoose()
                .Services
                .AddServiceExtensions()
                .BuildRabbitServiceProvider();

            IRabbitApplicationBuilder app = new RabbitApplicationBuilder(services);

            app
                .Use(async (c, next) =>
                {
                    await next();
                })
                .UseMiddleware<RequestServicesContainerMiddleware>()
                .UseHighAvailability()
                .UseLoadBalance()
                .UseMiddleware<HttpServiceMiddleware>();

            var invoker = app.Build();

            var client = new HttpClient(new RabbitHttpClientHandler(invoker));

            //            Console.WriteLine(await client.GetStringAsync("http://userService/api/User/1"));

            var content=await client.PutAsync("http://userService/api/User/1", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", "mj"),
            }));
            Console.WriteLine(await content.Content.ReadAsStringAsync());

            return;

            var rabbitContext = new HttpRabbitContext();

            var request = rabbitContext.Request;
            request.RequestUri = new Uri("http://userService/api/User/1");
            /*            request.Method = "PUT";
                        var content = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("name", "test"),
                        });
                        request.Body = await content.ReadAsStreamAsync();
                        foreach (var httpContentHeader in content.Headers)
                        {
                            request.Headers[httpContentHeader.Key] = new StringValues(httpContentHeader.Value.ToArray());
                        }*/

            await invoker(rabbitContext);

            var response = rabbitContext.Response;

            Console.WriteLine(new StreamReader(response.Body).ReadToEnd());
            /*var proxyFactory = new ProxyFactory(services);

            var userService = proxyFactory.GetProxy<IUserService>(invoker);

            while (true)
            {
                try
                {
                    var user = await userService.GetUserAsync(1);
                    Console.WriteLine(JsonConvert.SerializeObject(user));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                Console.ReadLine();
            }

            var result = await userService.PutUserAsync(1, new UserMode
            {
                Name = "test",
                Age = 123
            });

            Console.WriteLine(JsonConvert.SerializeObject(result));
            return;*/
        }
    }
}
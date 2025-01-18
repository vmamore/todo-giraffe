module Todo.Tests

open App
open System
open Xunit
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Hosting
open System.Net.Http
open System.Net
open System.Text.Json

let createHost() =
    WebHostBuilder()
        .UseTestServer()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .UseUrls("localhost:5005")

let testRequest (request : HttpRequestMessage) =
    let resp = task {
        use server = new TestServer(createHost())
        use client = server.CreateClient()
        let! response = request |> client.SendAsync
        return response
    }
    resp.Result

[<Fact>]
let ``Hello world endpoint says hello`` () =
    let response = testRequest (new HttpRequestMessage(HttpMethod.Get, "/hello"))
    let content = response.Content.ReadAsStringAsync().Result
    Assert.Equal(response.StatusCode, HttpStatusCode.OK)
    Assert.Equal(content, "hello world!")


[<Fact>]
let ``Create todo`` () =
    let todoRequest =  { Description = "Wash the dishes!" }
    let requestMessage = new HttpRequestMessage(HttpMethod.Post, "/todo")
    requestMessage.Content <- new StringContent(JsonSerializer.Serialize todoRequest, System.Text.Encoding.UTF8 , "application/json")
    let response = testRequest (requestMessage)
    let content = response.Content.ReadAsStringAsync().Result
    Assert.Equal(response.StatusCode, HttpStatusCode.OK)
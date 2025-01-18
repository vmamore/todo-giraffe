module Todo.Tests

open System
open Xunit
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.TestHost
open Microsoft.AspNetCore.Hosting
open System.Net.Http
open System.Net

let createHost() =
    WebHostBuilder()
        .UseTestServer()
        .Configure(Action<IApplicationBuilder> App.configureApp)
        .ConfigureServices(App.configureServices)
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

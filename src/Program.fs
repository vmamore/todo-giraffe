module Todo.App

open System
open Giraffe
open Giraffe.EndpointRouting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type Todo = { Id: Guid; Description: string; Done: bool; CreatedAt: DateTime; DoneAt: DateTime option }

type TodoRequest = { Description: string }

let strToGuidOption (str: string) : Guid option =
    match Guid.TryParse str with
    | true, g -> Some g
    | false, _ -> None

type TodoRepo() =
    
    let mutable todoList = []
    
    member this.GetAll() = todoList
    
    member this.GetOne(id: string) : Todo option =
        strToGuidOption id
        |> Option.bind (fun id ->
            todoList |> List.tryFind (fun i -> i.Id = id)
        )
    
    member this.Create(description: string) : Todo =
        let newTodo = { Id = Guid.NewGuid(); Description = description; Done = false; CreatedAt = DateTime.UtcNow; DoneAt = None }
        todoList <- newTodo :: todoList
        newTodo
        
    // member this.MarkAsDone(id: string) : Todo option =
    //     let todo = this.GetOne(id)
    //     let newTodo = match todo with
    //     | Some todo ->  { todo with Done = true; DoneAt = Some DateTime.UtcNow }
    //     | None _ -> None
       
    
// ---------------------------------
// Web app
// ---------------------------------

let todoRepo = TodoRepo()

module TodoHandlers =

    let helloWorld =
        handleContext( fun ctx -> "hello world!" |> ctx.WriteTextAsync)
    
    let getAll =
        handleContext( fun ctx -> todoRepo.GetAll() |> ctx.WriteJsonAsync)

    let getOne (id: string) =
        handleContext( fun ctx ->
            task {
                Console.WriteLine $"Id = {id}"
                let todo = todoRepo.GetOne id
                return! ctx.WriteJsonAsync todo
            }
        )

    let create=
        handleContext( fun (ctx: HttpContext) -> 
            task {
                let! todo = ctx.BindJsonAsync<TodoRequest>()
                todoRepo.Create todo.Description |> ignore
                return! todoRepo.GetAll() |> ctx.WriteJsonAsync
            }
        )
    
let endpoints =
    [ 
        GET [ 
            route "/hello" TodoHandlers.helloWorld
            route "/todo" TodoHandlers.getAll
            routef "/todo/%s" TodoHandlers.getOne   
        ]
        POST [
            route "/todo" TodoHandlers.create
        ]
    ]

let notFoundHandler = "Not Found" |> text |> RequestErrors.notFound
    
let configureApp (app: IApplicationBuilder) =
    app.UseRouting()
       .UseGiraffe(endpoints)
       .UseGiraffe(notFoundHandler)
    
let configureServices (services : IServiceCollection) =
    services.AddRouting().AddGiraffe() |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services
    
    let app = builder.Build()
    
    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    configureApp app

    app.Run()

    0 // Exit code
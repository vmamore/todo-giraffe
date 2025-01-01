open System
open System.Threading.Tasks
open Giraffe
open Giraffe.EndpointRouting
open Giraffe.ViewEngine
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type Todo = { Id: Guid; Description: string; Done: bool; CreatedAt: DateTime; DoneAt: DateTime option }

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
    
let renderWithBaseLayout 
    (headSlot: XmlNode list)
    (bodySlot: XmlNode list)
    (footerSlot: XmlNode list)
    : XmlNode
    = 
    html [] [
        head [] [
            title [] [str "MyTodoApp"]
            meta [
                _charset "UTF-8";
                _name "viewport";
                _content "width=device-width, initial-scale=1"
            ];
            yield! headSlot;
        ]
        body [] [
            yield! bodySlot;
        ]
        footer [] [
            yield! footerSlot;
        ]
    ]
    
let renderIndexPage
    (todoList: Todo list)
    : Task<XmlNode>
    =
    task {
        let count = todoList.Length

        let itemListMarkup = 
            div [] [
                h3 [] [ str $"Total Count: {count}"]
                ul [] [
                    yield! 
                        todoList
                        |> List.map (fun i -> 
                            li [] [ str (i.Description.ToString()) ]
                        )
                ]
                hr []
                a [ _href "/todo"] [ str "Create New Todo" ]
            ]
            
        return 
            renderWithBaseLayout
                [] 
                [ itemListMarkup ]
                []
    }
    
let renderDetailPageMarkup 
    (item: Todo option)
    : XmlNode 
    =
    let itemMarkup = 
        div [] [
            match item with 
            | None -> h3 [] [ str "Not Found!" ]
            | Some i ->
                h3 [] [ str $"Description: {i.Description.ToString()}" ]
                h2 [] [ str $"Created at: {i.CreatedAt}" ]
                h2 [] [ str $"Done at: {i.DoneAt}" ]
        ]
     
    renderWithBaseLayout
        [] 
        [ itemMarkup ]
        []
        
let renderCreateTodoPageMarkup : XmlNode 
    =
    let createTodoMarkup = 
        div [] [
            form [_action "/todo"; _method "post"] [
                input [_placeholder "Todo Description"; _type "text"; _name "description"; _id "description"]
                button [ _type "Submit" ] [ str "Enviar" ]
            ]
        ]
     
    renderWithBaseLayout
        [] 
        [ createTodoMarkup ]
        []
    
    
// ---------------------------------
// Web app
// ---------------------------------

let todoRepo = TodoRepo()

let getAllHttpHandler =
    handleContext( fun (ctx: HttpContext) -> 
        task {
            let! indexPageMarkup = renderIndexPage(todoRepo.GetAll())

            return! 
                indexPageMarkup
                |> RenderView.AsString.htmlNode
                |> ctx.WriteHtmlStringAsync
        }
    )
    
let detailHttpHandler
    (id: string)
    =
    handleContext( fun (ctx: HttpContext) -> 
        task {

            let item = todoRepo.GetOne(id)
            let detailPageMarkup = renderDetailPageMarkup item

            return! 
                detailPageMarkup
                |> RenderView.AsString.htmlNode
                |> ctx.WriteHtmlStringAsync
        }
    )
    
let showCreateHttpHandler=
    handleContext( fun (ctx: HttpContext) -> 
        task {
            return! 
                renderCreateTodoPageMarkup
                |> RenderView.AsString.htmlNode
                |> ctx.WriteHtmlStringAsync
        }
    )
    
let createTodoHttpHandler=
    handleContext( fun (ctx: HttpContext) -> 
        task {
            let description = ctx.Request.Form.Item "description" 
            
            todoRepo.Create description |> ignore
                
            let! indexPageMarkup = renderIndexPage(todoRepo.GetAll())
            
            return! 
                indexPageMarkup
                |> RenderView.AsString.htmlNode
                |> ctx.WriteHtmlStringAsync
        }
    )
    
let endpoints =
    [ 
        GET [ 
            route "/" (getAllHttpHandler)
            route "/todo" (showCreateHttpHandler)
            routef "/detail/%s" detailHttpHandler   
        ]
        POST [
            route "/todo" (createTodoHttpHandler)
        ]
    ]

let notFoundHandler = "Not Found" |> text |> RequestErrors.notFound
    
let configureApp (app: IApplicationBuilder) =
    app.UseRouting().UseGiraffe(endpoints).UseGiraffe(notFoundHandler)
    
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
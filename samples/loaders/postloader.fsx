#r "../../src/Fornax.Core/bin/Release/netstandard2.0/Fornax.Core.dll"
#r "Markdig.dll"

open Markdig

type Post = {
    link : string
    title: string
    author: string option
    published: System.DateTime option
    tags: string list
    content: string
}
with
    static member Construct (lst : (string * string * string option * System.DateTime option * string list * string) []) =
        lst
        |> Array.map (fun (link, title, author, published, tags, content) ->
            {
                link = link
                title = title
                author = author
                published = published
                tags = tags
                content = content
            })
        |> Array.toList

let markdownPipeline =
    MarkdownPipelineBuilder()
        .UsePipeTables()
        .UseGridTables()
        .Build()

let isSeparator (input : string) =
    input.StartsWith "---"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns content of config that should be used for the page
let getConfig (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    fileContent
    |> Array.splitAt indexOfSeperator
    |> fst
    |> String.concat "\n"

///`fileContent` - content of page to parse. Usually whole content of `.md` file
///returns HTML version of content of the page
let getContent (fileContent : string) =
    let fileContent = fileContent.Split '\n'
    let fileContent = fileContent |> Array.skip 1 //First line must be ---
    let indexOfSeperator = fileContent |> Array.findIndex isSeparator
    let _, content = fileContent |> Array.splitAt indexOfSeperator

    let content = content |> Array.skip 1 |> String.concat "\n"
    Markdown.ToHtml(content, markdownPipeline)

let trimString (str : string) =
    str.Trim().TrimEnd('"').TrimStart('"')

let loadFile n =
    let text = System.IO.File.ReadAllText n

    let config = (getConfig text).Split( '\n') |> List.ofArray

    let content = getContent text

    let link = "/" + System.IO.Path.Combine("posts", (n |> System.IO.Path.GetFileNameWithoutExtension) + ".html").Replace("\\", "/")

    let title = config |> List.find (fun n -> n.ToLower().StartsWith "title" ) |> fun n -> n.Split(':').[1] |> trimString

    let author =
        try
            config |> List.tryFind (fun n -> n.ToLower().StartsWith "author" ) |> Option.map (fun n -> n.Split(':').[1] |> trimString)
        with
        | _ -> None

    let published =
        try
            config |> List.tryFind (fun n -> n.ToLower().StartsWith "published" ) |> Option.map (fun n -> n.Split(':').[1] |> trimString |> System.DateTime.Parse)
        with
        | _ -> None

    let tags =
        try
            let x =
                config
                |> List.tryFind (fun n -> n.ToLower().StartsWith "tags" )
                |> Option.map (fun n -> n.Split(':').[1] |> trimString |> fun n -> n.Split ',' |> Array.toList )
            defaultArg x []
        with
        | _ -> []

    (link, title, author, published, tags, content)

let loader (projectRoot: string) (siteContet: SiteContents) =
    let postsPath = System.IO.Path.Combine(projectRoot, "posts")
    System.IO.Directory.GetFiles postsPath
    |> Array.filter (fun n -> n.EndsWith ".md")
    |> Array.map loadFile
    |> Post.Construct
    |> List.iter (fun p -> siteContet.Add p)

    siteContet
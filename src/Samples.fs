﻿namespace WebSharper.UI.Next

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Client
open WebSharper.UI.Next.Html
open WebSharper.UI.Next.Notation
open WebSharper.UI.Next.SiteCommon

/// A little framework for displaying samples on the site.
[<JavaScript>]
module Samples =

    // First, define the samples type, which specifies metadata and a rendering
    // function for each of the samples.
    // A Sample consists of a file name, identifier, list of keywords,
    // rendering function, and title.

    type Visuals<'T> =
        {
            Desc : 'T -> Doc
            Main : 'T -> Doc
        }

    let Sidebar vPage samples =
        let renderItem sample =
            let attrView =
                View.FromVar vPage
                |> View.Map (fun pg -> pg.PageSample)
            let pred s = Option.exists (fun smp -> sample.Meta.FileName = smp.Meta.FileName) s
            let activeAttr = Attr.DynamicClass "active" attrView pred
            Doc.Link sample.Meta.Title
                [cls "list-group-item"; activeAttr]
                (fun () -> Var.Set vPage sample.SamplePage)
                :> Doc

        divc "col-md-3" [
            h4 [text "Samples"]
            List.map renderItem samples |> Doc.Concat
        ]

    let RenderContent sample =
        divc "samples col-md-9" [
            div [
                divc "row" [
                    h1 [text sample.Meta.Title]
                    div [
                        p [ sample.Description ]
                        p [
                            aAttr
                                [ attr.href ("https://github.com/intellifactory/websharper.ui.next.samples/blob/master/src/" + sample.Meta.Uri + ".fs") ]
                                [text "View Source"]
                        ]
                    ]
                ]

                divc "row" [
                    p [ sample.Body ]
                ]
            ]
        ]

    let Render vPage pg samples =
        let sample =
            match pg.PageSample with
            | Some s -> s
            | None -> failwith "Attempted to render non-sample on samples page"

        sectionAttr [cls "block-small"] [
            divc "container" [
                divc "row" [
                    Sidebar vPage samples
                    RenderContent sample
                ]
            ]
        ]

    let CreateRouted router init vis meta =
        let sample =
            {
                Body = Doc.Empty
                Description = Doc.Empty
                Meta = meta
                Router = Unchecked.defaultof<_>
                RouteId = Unchecked.defaultof<_>
                SamplePage = Unchecked.defaultof<_>
            }
        let r =
             Router.Route router init (fun id cur ->
                sample.RouteId <- id
                sample.Body <- vis.Main cur
                sample.Description <- vis.Desc cur
                let page = mkPage sample.Meta.Title id Samples
                page.PageSample <- Some sample
                page.PageRouteId <- id
                sample.SamplePage <- page
                page
             )
             |> Router.Prefix meta.Uri
        sample.Router <- r
        sample

    let CreateSimple vis meta =
        let unitRouter = RouteMap.Create (fun () -> []) (fun _ -> ())
        let sample =
            {
                Body = vis.Main ()
                Description = vis.Desc ()
                Meta = meta
                Router = Unchecked.defaultof<_>
                RouteId = Unchecked.defaultof<_>
                SamplePage = Unchecked.defaultof<_>
            }

        sample.Router <-
            // mkPage name routeId ty
            Router.Route unitRouter () (fun id cur ->
                let page = mkPage sample.Meta.Title id Samples
                sample.RouteId <- id
                page.PageSample <- Some sample
                page.PageRouteId <- id
                sample.SamplePage <- page
                page)
            |> Router.Prefix meta.Uri
        sample

    [<Sealed>]
    type Builder<'T>(create: Visuals<'T> -> Meta -> Sample) =

        let mutable meta =
            {
                FileName = "Unknown.fs"
                Keywords = []
                Title = "Unknown"
                Uri = "unknown"
            }

        let mutable vis =
            {
                Desc = fun _ -> Doc.Empty
                Main = fun _ -> Doc.Empty
            }

        member b.Create () =
            create vis meta

        member b.FileName x =
            meta <- { meta with FileName = x }; b

        member b.Id x =
            meta <- { meta with Title = x; Uri = x }; b

        member b.Keywords x =
            meta <- { meta with Keywords = x }; b

        member b.Render f =
            vis <- { vis with Main = (fun x -> f x :> Doc) }; b

        member b.RenderDescription f =
            vis <- { vis with Desc = (fun x -> f x :> Doc) }; b

        member b.Title x =
            meta <- { meta with Title = x }; b

        member b.Uri x =
            meta <- { meta with Uri = x }; b

    let Build () =
        Builder CreateSimple

    let Routed (router, init) =
        Builder (CreateRouted router init)

    let InitialSamplePage samples =
        (List.head samples).SamplePage

    let SamplesRouter samples =
        Router.Merge [ for s in samples -> s.Router ]
        |> Router.Prefix "samples"
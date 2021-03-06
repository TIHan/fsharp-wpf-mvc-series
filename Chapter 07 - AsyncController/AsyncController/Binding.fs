﻿[<AutoOpen>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FSharp.Windows.Binding

open System.Reflection
open System.Windows
open System.Windows.Data 
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns

type PropertyInfo with
    member this.DependencyProperty = 
        let dpInfo = 
            this.DeclaringType.GetField(this.Name + "Property", BindingFlags.Static ||| BindingFlags.Public ||| BindingFlags.FlattenHierarchy)
        assert (dpInfo <> null)
        dpInfo.GetValue(null, [||]) |> unbox<DependencyProperty> 

let rec (|PropertyPath|_|) = function 
    | PropertyGet( Some( Value _), sourceProperty, []) -> Some sourceProperty.Name
    | Coerce( PropertyPath path, _) 
    | SpecificCall <@ string @> (None, _, [ PropertyPath path ]) -> Some path
    | _ -> None

type Expr with
    member this.ToBindingExpr() = 
        match this with
        | PropertySet
            (
                Some( FieldGet( Some( PropertyGet( Some (Value( view, _)), window, [])), control)),
                targetProperty, 
                [], 
                PropertyPath path
            ) ->
                let target : FrameworkElement = (view, [||]) |> window.GetValue |> control.GetValue |> unbox
                let binding = Binding(path, ValidatesOnDataErrors = true)
                target.SetBinding(targetProperty.DependencyProperty, binding)
        | _ -> invalidArg "expr" (string this) 

type Binding with
    static member FromExpression expr = 
        let rec split = function 
            | Sequential(head, tail) -> head :: split tail
            | tail -> [ tail ]

        for e in split expr do
            let be = e.ToBindingExpr()
            assert not be.HasError
    


module NullKill

open System.Reflection
open System.Collections.Generic
open Microsoft.FSharp.Reflection

let rec CheckEnumerable thing =
    let e = box thing :?> System.Collections.IEnumerable
    let typed =
        e
        |> Seq.cast
    if Seq.length typed > 0 then
        typed
        |> Seq.map (fun i -> HasNoNulls' i <| i.GetType())
        |> Seq.reduce (&&)
    else
        true

and CheckField thing (field : FieldInfo) =
    if field.FieldType.IsValueType then
        true
    else
        let o = field.GetValue(thing)
        let t = field.FieldType
        HasNoNulls' o t
        
and CheckFields thing (t : System.Type) =
    if t.IsValueType then
        true
    else
        match t.GetFields () with
        | f when Array.empty = f ->
            true
        | fields ->
            fields
            |> Array.map (CheckField thing)
            |> Array.reduce (&&)

and CheckProperty thing (prop : PropertyInfo) =
    if prop.PropertyType.IsValueType || prop.GetIndexParameters().Length > 0 then
        true
    else
        let filter = TypeFilter(fun t _ -> t = typeof<System.Collections.IEnumerable>)
        let enumerables = prop.PropertyType.FindInterfaces(filter, null)
        let o = prop.GetValue(thing, null)
        let t = prop.PropertyType
        if enumerables.Length > 0 then
            HasNoNulls' o t && (CheckEnumerable o)
        else
            HasNoNulls' o t

and CheckProperties thing (t : System.Type) =
    if t.IsValueType then
        true
    else
        match t.GetProperties () with
        | f when Array.empty = f ->
            true
        | fields ->
            fields
            |> Array.map (CheckProperty thing)
            |> Array.reduce (&&)

and private HasNoNulls' (thing : obj) thingType =
    if thing = null then
        if FSharpType.IsUnion thingType then
            true
        else
            false
    else
        CheckProperties thing thingType && CheckFields thing thingType

let HasNoNulls<'a> (thing : 'a) =
    HasNoNulls' thing (typedefof<'a>)

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
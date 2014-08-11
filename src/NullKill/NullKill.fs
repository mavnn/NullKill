module NullKill

open System.Reflection
open Microsoft.FSharp.Reflection

let rec private CheckEnumerable depth thing =
    let e = box thing :?> System.Collections.IEnumerable
    let typed =
        e
        |> Seq.cast
    if Seq.length typed > 0 then
        typed
        |> Seq.map (fun i -> HasNoNulls' (depth + 1) i <| i.GetType())
        |> Seq.reduce (&&)
    else
        true

and private CheckField depth thing (field : FieldInfo) =
    if field.FieldType.IsValueType then
        true
    else
        let o = field.GetValue(thing)
        let t = field.FieldType
        HasNoNulls' (depth + 1) o t
        
and private CheckFields depth thing (t : System.Type) =
    if t.IsValueType then
        true
    else
        match t.GetFields () with
        | f when Array.empty = f ->
            true
        | fields ->
            fields
            |> Array.map (CheckField depth thing)
            |> Array.reduce (&&)

and private CheckProperty depth thing (prop : PropertyInfo) =
    if prop.PropertyType.IsValueType || prop.GetIndexParameters().Length > 0 then
        true
    else
        let filter = TypeFilter(fun t _ -> t = typeof<System.Collections.IEnumerable>)
        let enumerables = prop.PropertyType.FindInterfaces(filter, null)
        let o = prop.GetValue(thing, null)
        let t = prop.PropertyType
        if enumerables.Length > 0 then
            HasNoNulls' (depth + 1) o t && (CheckEnumerable depth o)
        else
            HasNoNulls' (depth + 1) o t

and private CheckProperties depth thing (t : System.Type) =
    if t.IsValueType then
        true
    else
        match t.GetProperties () with
        | f when Array.empty = f ->
            true
        | fields ->
            fields
            |> Array.map (CheckProperty depth thing)
            |> Array.reduce (&&)

and private HasNoNulls' depth (thing : obj) thingType =
    match thing with
    | _ when depth > 50 ->
        true
    | t when (box t) = null ->
        if FSharpType.IsUnion thingType then
            true
        else
            false
    | _ ->
        match thingType with
        | t when t = typeof<string> ->
            true
        | _ ->
            CheckProperties depth thing thingType && CheckFields depth thing thingType

let HasNoNulls<'a> (thing : 'a) =
    HasNoNulls' 0 thing (typedefof<'a>)

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
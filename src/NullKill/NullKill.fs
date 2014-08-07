module NullKill

open System.Reflection
open Microsoft.FSharp.Reflection

let rec CheckField thing (field : FieldInfo) =
    if field.FieldType.IsValueType then
        true
    else
        let o = field.GetValue()
        let t = field.FieldType
        HasNoNulls' o t
        
and CheckFields thing =
    let t = thing.GetType()
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
    if prop.PropertyType.IsValueType then
        true
    else
        let o = prop.GetValue(thing, null)
        let t = prop.GetType()
        HasNoNulls' o t

and CheckProperties thing =
    let t = thing.GetType()
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
        CheckProperties thing && CheckFields thing

let HasNoNulls<'a> (thing : 'a) =
    HasNoNulls' thing (typedefof<'a>)

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
module NullKill

open System.Reflection

let rec CheckField thing (field : FieldInfo) =
    if field.FieldType.IsValueType then
        true
    else
        let o = field.GetValue()
        HasNoNulls o
        
and CheckFields thing =
    let t = thing.GetType()
    if t.IsValueType then
        true
    else
        t.GetFields ()
        |> Array.map (CheckField thing)
        |> Array.reduce (&&)

and CheckProperty thing (prop : PropertyInfo) =
    if prop.PropertyType.IsValueType then
        true
    else
        let o = prop.GetValue(thing, null)
        HasNoNulls o

and CheckProperties thing =
    let t = thing.GetType()
    if t.IsValueType then
        true
    else
        t.GetProperties ()
        |> Array.map (CheckProperty thing)
        |> Array.reduce (&&)

and HasNoNulls thing =
    if thing = null then
        false
    else
        CheckProperties thing && CheckFields thing

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
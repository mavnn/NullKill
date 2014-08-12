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
        |> Seq.map (fun i -> i <> null && HasNoNulls' (depth + 1) i <| i.GetType())
        |> Seq.reduce (&&)
    else
        true

and private CheckGEnumerable (eType : System.Type) depth thing =
    let genType = eType.GetGenericArguments().[0]
    if genType.FullName = null && genType.Name = "T" then
        CheckEnumerable depth thing
    else
        let getEnumerator =
            eType
                .GetMethod("GetEnumerator")
        let enumerator =
            match getEnumerator.IsGenericMethodDefinition with
            | true ->
                getEnumerator.MakeGenericMethod(genType).Invoke(thing, null)
            | false ->
                getEnumerator.Invoke(thing, null)
        let moveNext =
            enumerator
                .GetType()
                .GetMethod("MoveNext")
        let current =
            enumerator.GetType().GetMethod("get_Current")
        let rec iter list =
            if moveNext.Invoke(enumerator, null) :?> bool then
                iter <| current.Invoke(enumerator, null)::list
            else
                list        
        let objs = iter []
        objs
        |> List.map (fun o -> HasNoNulls' (depth + 1) o genType)
        |> List.fold (&&) true

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
        let get = prop.GetGetMethod()
        if get.ContainsGenericParameters && (not get.IsGenericMethod) then
            true
        else
            let o =
                if get.IsGenericMethod then
                    get
                        .MakeGenericMethod(prop.PropertyType)
                        .Invoke(thing, null)
                else
                    get.Invoke(thing, null)
            let t = prop.PropertyType
            HasNoNulls' (depth + 1) o t

and private CheckProperties depth thing (t : System.Type) =
    match t with
    | _ when t.IsValueType ->
        true
    | _ ->
        let checkableProps =
            t.GetProperties (BindingFlags.Instance ||| BindingFlags.Public)
        match checkableProps with
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
    | _ when thing = null ->
        if FSharpType.IsUnion thingType then
            true
        else
            false
    | _ ->
        let genumFilter = 
            TypeFilter(
                fun t _ -> 
                    t.IsGenericType
                    && t.GetGenericTypeDefinition () = typeof<System.Collections.Generic.IEnumerable<_>>.GetGenericTypeDefinition())
        let genumerables = thingType.FindInterfaces(genumFilter, null)
        let enumFilter = TypeFilter(fun t _ -> t = typeof<System.Collections.IEnumerable>)
        let enumerables = thingType.FindInterfaces(enumFilter, null)
        match thingType with
        | t when t = typeof<string> ->
            true
        | _ ->
            if genumerables.Length > 0 then
                CheckGEnumerable (Seq.head genumerables) depth thing && CheckProperties depth thing thingType && CheckFields depth thing thingType
            else if enumerables.Length > 0 then
                CheckEnumerable depth thing && CheckProperties depth thing thingType && CheckFields depth thing thingType
            else
                CheckProperties depth thing thingType && CheckFields depth thing thingType

let HasNoNulls<'a> (thing : 'a) =
    HasNoNulls' 0 thing (typedefof<'a>)

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
module NullKill

open System
open System.Reflection
open Microsoft.FSharp.Reflection
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Linq.RuntimeHelpers.LeafExpressionConverter

let private core =
    AppDomain.CurrentDomain.GetAssemblies()
    |> Seq.find (fun a -> a.GetName().Name = "FSharp.Core")

let private seqMod =
    core.GetTypes()
    |> Seq.filter FSharpType.IsModule
    |> Seq.find (fun t -> t.FullName = "Microsoft.FSharp.Collections.SeqModule")

let private map eType =
    let openMap =
        seqMod.GetMethod("Map")
    openMap.MakeGenericMethod [|eType;typeof<bool>|]

let private cast eType =
    let openCast =
        seqMod.GetMethod("Cast")
    openCast.MakeGenericMethod [|eType|]

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
    let genMap = map genType
    let genCast = cast genType
    let check = 
        let o = Var("o", genType)
        let var = Expr.Var(o)
        let objVar = Expr.Coerce(var, typeof<obj>)
        Expr.Lambda(o, <@@ HasNoNulls' (depth + 1) (%%objVar) genType @@>)
        |> EvaluateQuotation
    let castSeq =
        genCast.Invoke(null, [|thing|])
    let mappedSeq = genMap.Invoke(null, [|check;castSeq|]) :?> seq<bool>
    mappedSeq
    |> Seq.fold (&&) true

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

and private CheckProperty depth thing thingType (prop : PropertyInfo) =
    if prop.PropertyType.IsValueType
       || prop.GetIndexParameters().Length > 0 
       || ((prop.Name = "Head" || prop.Name = "Tail" || prop.Name = "TailOrNull") && FSharpType.IsUnion thingType) then
        true
    else
        let get = prop.GetGetMethod()
        let o =
            if get.IsGenericMethod then
                get
                    .MakeGenericMethod(prop.PropertyType)
                    .Invoke(thing, null)
            else
                if get.ReturnType.IsGenericParameter then
                    get.Invoke(thing, null)
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
            if FSharpType.IsRecord t then
                FSharpType.GetRecordFields t
            elif t.IsGenericType && t.GetGenericTypeDefinition() = [].GetType().GetGenericTypeDefinition() then
                [||]
            else
                t.GetProperties (BindingFlags.Instance ||| BindingFlags.Public)
        match checkableProps with
        | p when Array.empty = p ->
            true
        | properties ->
            properties
            |> Array.map (CheckProperty depth thing t)
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
                let enumerableChildrenNoNulls = CheckGEnumerable (Seq.head genumerables) depth thing
                let propertiesNoNulls = CheckProperties depth thing thingType
                let fieldsNoNulls = CheckFields depth thing thingType
                enumerableChildrenNoNulls && propertiesNoNulls && fieldsNoNulls
            else if enumerables.Length > 0 then
                CheckEnumerable depth thing && CheckProperties depth thing thingType && CheckFields depth thing thingType
            else
                CheckProperties depth thing thingType && CheckFields depth thing thingType

let HasNoNulls<'a> (thing : 'a) =
    HasNoNulls' 0 thing (typeof<'a>)

let Check thing =
    if HasNoNulls thing then
        Some thing
    else
        None
        
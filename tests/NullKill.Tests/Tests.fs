module NullKill.Tests

open System
open NullKill
open CSharp.TestTypes
open NUnit.Framework
open FsCheck

type Testable() =
    [<DefaultValue>] val mutable Field1 : System.DateTime
    member val Prop1 = null with get, set
    member val Prop2 = null with get, set

[<Test>]
let ``null objects should fail`` () =
    HasNoNulls null
    |> Assert.IsFalse

[<Test>]
let ``NullKill should detect null properties`` () =
    let t = Testable()
    t.Field1 <- System.DateTime.Now
    let r = HasNoNulls (t)
    Assert.IsFalse(r, "Null property values not detected")

[<Test>]
let ``NullKill should detect null fields`` () =
    let t = Testable()
    t.Prop1 <- System.DateTime.Now :> obj
    t.Prop2 <- System.DateTime.Now :> obj
    let r = HasNoNulls (Testable())
    Assert.IsFalse(r, "Null field values not detected")

[<Test>]
let ``NullKill should pass objects with no nulls`` () =
    let t = Testable()
    t.Prop1 <- System.DateTime.Now :> obj
    t.Prop2 <- System.DateTime.Now :> obj
    t.Field1 <- System.DateTime.Now
    let r = HasNoNulls t
    Assert.IsTrue(r, "No null fields or properties - should pass")

[<Test>]
let ``Test unnested C# type`` () =
    let t = SingleLayer()
    Assert.IsFalse(HasNoNulls t, "t is not initialized, should be false")

[<Test>]
let ``Test unnested C# populated`` () =
    let t = SingleLayer ()
    t.Dates <- System.Collections.Generic.List<DateTime>()
    t.Id <- Guid.NewGuid()
    t.Date <- DateTime()
    Assert.IsTrue(HasNoNulls t, "All values have been initialized, should be true")

[<Test>]
let ``Test Recursing down works for finding nulls`` () =
    let t = NestedClass()
    t.Id <- Guid.NewGuid()
    t.Single <- SingleLayer()
    t.Date <- DateTime()
    Assert.IsFalse(HasNoNulls t, "Property Single has a type with null properties")

[<Test>]
let ``Test Recursing down works if all set`` () =
    let t = NestedClass()
    t.Id <- Guid.NewGuid()
    t.Single <- SingleLayer()
    t.Single.Dates <- System.Collections.Generic.List<DateTime>()
    t.Single.Date <- DateTime()
    Assert.IsTrue(HasNoNulls t, "All properties set up")
    
[<Test>]
let ``None should not count as a null`` () =
    Assert.IsTrue(HasNoNulls None, "None is not null, whatever the compiler thinks")

[<Test>]
let ``Null nullables is okay`` () =
    let t = WithNullable()
    t.NullableInt <- System.Nullable()
    Assert.IsTrue(HasNoNulls t)

[<Test>]
let ``Indexed properties should have all values checked`` () =
    let t = WithIndexedProperty(false)
    Assert.IsFalse(HasNoNulls t)

[<Test>]
let ``Indexed properties should pass if all items complete`` () =
    let t = WithIndexedProperty(true)
    Assert.IsTrue(HasNoNulls t)

[<Test>]
let ``IEnumerables with nulls in should be detected`` () =
    let t = System.Collections.Generic.List<obj>()
    t.Add(box null)
    Assert.IsFalse(HasNoNulls t)

[<Test>]
let ``Recursive data types don't overflow`` () =
    let f = IO.FileInfo("NotOnThisDisk")
    Assert.IsFalse(HasNoNulls f)

[<Test>]
let ``Common data types: String`` () =
    let test =
        fun (str : string) ->
            if str = null then
                not <| HasNoNulls str
            else
                HasNoNulls str
    Check.QuickThrowOnFailure test

[<Test>]
let ``Common data types: DateTime`` () =
    let test =
        fun (dt : DateTime) ->
            HasNoNulls dt
    Check.QuickThrowOnFailure test

[<Test>]
let ``Common data types: Guid`` () =
    let test =
        fun (g : Guid) ->
            HasNoNulls g
    Check.QuickThrowOnFailure test

[<Test>]
let ``Common data types: Boolean`` () =
    Assert.IsTrue(HasNoNulls <| true)
    Assert.IsTrue(HasNoNulls <| false)

[<Test>]
let ``Common data types: C# List`` () =
    let test =
        fun (g : System.Collections.Generic.List<_>) ->
            HasNoNulls g = (g |> Seq.map HasNoNulls |> Seq.fold (&&) true)
    Check.QuickThrowOnFailure test

[<Test>]
let ``Lists of lists are checked for nulls`` () =
    let l = System.Collections.Generic.List<string>()
    l.Add (null)
    let p = System.Collections.Generic.List<_>()
    p.Add (l)
    Assert.IsFalse <| HasNoNulls p

[<Test>]
let ``Lists of lists are passed without nulls`` () =
    let l = System.Collections.Generic.List<string>()
    l.Add "Bob"
    let p = System.Collections.Generic.List<_>()
    p.Add l
    Assert.IsTrue <| HasNoNulls p

[<Test>]
let ``Common data types: Arrays`` () =
    let test =
        fun (a : _ []) ->
            HasNoNulls a = (a |> Array.map HasNoNulls |> Array.fold (&&) true)
    Check.QuickThrowOnFailure test

[<Test>]
let ``Common data types: List`` () =
    let test =
        fun (a : _ list) ->
            HasNoNulls a = (a |> List.map HasNoNulls |> List.fold (&&) true)
    Check.QuickThrowOnFailure test

[<Test>]
let ``Empty List is not null`` () =
    Assert.IsTrue(HasNoNulls [])

type RecordWithGeneric<'a> = { GenericThing : 'a }
        
[<Test>]
let ``Record with generic properties`` () =
    Assert.IsTrue (HasNoNulls { GenericThing = "Bob" })    
        
[<Test>]
let ``Record with generic properties should detect nulls`` () =
    Assert.IsFalse (HasNoNulls { GenericThing = null })    

type TestEnum =
    | CaseOne = 1
    | CaseTwo = 2

[<Test>]
let ``Enum is not null`` () =
    Assert.IsTrue (HasNoNulls TestEnum.CaseOne)
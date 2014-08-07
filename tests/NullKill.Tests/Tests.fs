module NullKill.Tests

open System
open NullKill
open CSharp.TestTypes
open NUnit.Framework

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
